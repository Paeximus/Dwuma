using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Dwuma.Models;
using Dwuma.Models.Data.DwumaContext;
using Microsoft.EntityFrameworkCore;

namespace Dwuma.Services
{
    public class ProfileService
    {
        private readonly DwumaContext _db;
        private readonly HttpClient _httpClient;
        private readonly string _geminiKey;
        private readonly string _cvStoragePath;
        private readonly ILogger<ProfileService> _logger;

        public ProfileService(
            DwumaContext db,
            HttpClient httpClient,
            IConfiguration config,
            ILogger<ProfileService> logger)
        {
            _db           = db;
            _httpClient   = httpClient;
            _geminiKey = config["Gemini:ApiKey"] ?? string.Empty;
            _cvStoragePath = config["CvStorage:Path"]
                ?? Path.Combine(Directory.GetCurrentDirectory(), "cv-uploads");
            _logger = logger;

            Directory.CreateDirectory(_cvStoragePath);
        }

        // ─── SAVE PROFILE ─────────────────────────────────────────────────────

        public async Task<ProfileResponse> SaveProfileAsync(ProfileRequest req)
        {
            // 1. Find or create the User row
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);

            if (user == null)
            {
                user = new User
                {
                    FullName     = $"{req.FirstName} {req.LastName}",
                    Email        = req.Email,
                    PasswordHash = string.Empty, // set properly when auth is added
                    CreatedAt    = DateTime.UtcNow,
                    UpdatedAt    = DateTime.UtcNow,
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync(); // generates user.Id
            }
            else
            {
                user.FullName  = $"{req.FirstName} {req.LastName}";
                user.UpdatedAt = DateTime.UtcNow;
            }

            // 2. Find or create the Profile row (1-to-1 with User)
            var profile = await _db.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile == null)
            {
                profile = new Profile { UserId = user.Id };
                _db.Profiles.Add(profile);
            }

            MapRequestToProfile(req, profile);
            profile.UpdatedAt = DateTime.UtcNow;

            // 3. Sync skills into SKILLS table
            await SyncSkillsAsync(user.Id, req);

            await _db.SaveChangesAsync();

            return new ProfileResponse
            {
                UserId    = user.Id,
                Message   = "Profile saved.",
                FirstName = req.FirstName,
                LastName  = req.LastName,
                Email     = req.Email,
            };
        }

        // ─── GET PROFILE ──────────────────────────────────────────────────────

        public async Task<ProfileRequest?> GetProfileAsync(int userId)
        {
            var user = await _db.Users
                .Include(u => u.Profile)
                .Include(u => u.Skills)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return null;

            var nameParts = (user.FullName ?? "").Split(' ', 2);

            return new ProfileRequest
            {
                FirstName      = nameParts.ElementAtOrDefault(0) ?? "",
                LastName       = nameParts.ElementAtOrDefault(1) ?? "",
                Email          = user.Email,
                Institution    = user.Profile?.Institution,
                DegreeLevel    = user.Profile?.Degree,
                FieldOfStudy   = user.Profile?.FieldOfStudy,
                GraduationYear = user.Profile?.GraduationYear?.ToString(),
                Classification = user.Profile?.GpaClassification,
                Industries     = SplitCsv(user.Profile?.PreferredIndustries),
                JobTypes       = SplitCsv(user.Profile?.JobTypePreference),
                WorkLocation   = user.Profile?.LocationPreference,
                SalaryRange    = user.Profile?.SalaryExpectation,
                CareerGoal     = user.Profile?.CareerGoals,
                TechSkills     = user.Skills
                                    .Where(s => s.SkillType == "technical")
                                    .Select(s => s.SkillName)
                                    .ToList(),
                SoftSkills     = user.Skills
                                    .Where(s => s.SkillType == "soft")
                                    .Select(s => s.SkillName)
                                    .ToList(),
                Languages      = user.Skills
                                    .Where(s => s.SkillType == "language")
                                    .Select(s => s.SkillName)
                                    .ToList(),
                Certifications = user.Skills
                                    .Where(s => s.SkillType == "certification")
                                    .Select(s => s.SkillName)
                                    .ToList(),
            };
        }

        // ─── SAVE CV ──────────────────────────────────────────────────────────

        public async Task SaveCvAsync(int userId, IFormFile file)
        {
            var user = await _db.Users.FindAsync(userId)
                ?? throw new Exception("User not found.");

            var safeFileName = $"{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";
            var filePath     = Path.Combine(_cvStoragePath, safeFileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            // Store CV record in CV_DOCUMENTS table
            var cvDoc = new CvDocument
            {
                UserId     = userId,
                FileName   = file.FileName,
                FilePath   = filePath,
                FileType   = Path.GetExtension(file.FileName).TrimStart('.'),
                IsBaseCv   = true,
                CreatedAt  = DateTime.UtcNow,
            };

            _db.CvDocuments.Add(cvDoc);

            try
            {
                var parsedText = await ExtractCvTextAsync(filePath, file.ContentType);
                // Store parsed text in changelog field as a temporary measure
                // (add a parsed_text column to CV_DOCUMENTS if you want proper storage)
                cvDoc.Changelog = parsedText;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CV text extraction failed — storing file only.");
            }

            await _db.SaveChangesAsync();
        }

        // ─── SKILLS SYNC ──────────────────────────────────────────────────────

        private async Task SyncSkillsAsync(int userId, ProfileRequest req)
        {
            // Remove old skills for this user and re-insert
            var existing = _db.Skills.Where(s => s.UserId == userId);
            _db.Skills.RemoveRange(existing);

            var allSkills = new List<Skill>();

            allSkills.AddRange(req.TechSkills.Select(s => new Skill
                { UserId = userId, SkillName = s, SkillType = "technical", ProficiencyLevel = "intermediate" }));

            allSkills.AddRange(req.SoftSkills.Select(s => new Skill
                { UserId = userId, SkillName = s, SkillType = "soft", ProficiencyLevel = "intermediate" }));

            allSkills.AddRange(req.Languages.Select(s => new Skill
                { UserId = userId, SkillName = s, SkillType = "language", ProficiencyLevel = "fluent" }));

            allSkills.AddRange(req.Certifications.Select(s => new Skill
                { UserId = userId, SkillName = s, SkillType = "certification", ProficiencyLevel = "certified" }));

            if (allSkills.Any())
                await _db.Skills.AddRangeAsync(allSkills);
        }

        // ─── CV TEXT EXTRACTION ───────────────────────────────────────────────

        private async Task<string> ExtractCvTextAsync(string filePath, string contentType)
        {
            if (string.IsNullOrEmpty(_geminiKey)) return string.Empty;
            if (!contentType.Contains("pdf")) return string.Empty;

            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var base64    = Convert.ToBase64String(fileBytes);

            var requestBody = new
            {
                model      = "gpt-4o",
                max_tokens = 1500,
                messages   = new[]
                {
                    new {
                        role    = "user",
                        content = new object[]
                        {
                            new { type = "text", text = "Extract key information from this CV as plain text: name, education, work experience, skills, certifications. Be concise." },
                            new { type = "document", source = new { type = "base64", media_type = "application/pdf", data = base64 } }
                        }
                    }
                }
            };

            var content  = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _geminiKey);

            var response = await _httpClient.PostAsync("https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key=" + _geminiKey, content);
            if (!response.IsSuccessStatusCode) return string.Empty;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;
        }

        // ─── HELPERS ──────────────────────────────────────────────────────────

        private static void MapRequestToProfile(ProfileRequest req, Profile profile)
        {
            profile.Institution        = req.Institution;
            profile.Degree             = req.DegreeLevel;
            profile.FieldOfStudy       = req.FieldOfStudy;
            profile.GraduationYear     = int.TryParse(req.GraduationYear, out var yr) ? yr : null;
            profile.GpaClassification  = req.Classification;
            profile.PreferredIndustries = string.Join(",", req.Industries);
            profile.JobTypePreference  = string.Join(",", req.JobTypes);
            profile.LocationPreference = req.WorkLocation;
            profile.SalaryExpectation  = req.SalaryRange;
            profile.CareerGoals        = req.CareerGoal;
        }

        private static List<string> SplitCsv(string? csv) =>
            string.IsNullOrWhiteSpace(csv)
                ? new()
                : csv.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
    }
}
