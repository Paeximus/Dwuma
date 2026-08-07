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

        public async Task<ProfileResponse> SaveProfileAsync(
    int userId,
    ProfileRequest request,
    CancellationToken cancellationToken = default)
        {
            User user =
                await _db.Users
                    .FirstOrDefaultAsync(
                        existingUser =>
                            existingUser.Id == userId,
                        cancellationToken)
                ?? throw new InvalidOperationException(
                    "Authenticated user was not found.");

            user.FullName =
                $"{request.FirstName} {request.LastName}".Trim();

            user.UpdatedAt = DateTime.UtcNow;

            Profile? profile =
                await _db.Profiles
                    .FirstOrDefaultAsync(
                        existingProfile =>
                            existingProfile.UserId == userId,
                        cancellationToken);

            if (profile is null)
            {
                profile = new Profile
                {
                    UserId = userId
                };

                _db.Profiles.Add(profile);
            }

            user.FullName =
                $"{request.FirstName} {request.LastName}"
           .Trim();

            user.UpdatedAt =
                DateTime.UtcNow;

            MapRequestToProfile(
                request,
                profile);

            profile.UpdatedAt =
                DateTime.UtcNow;

            await SyncSkillsAsync(
                userId,
                request,
                cancellationToken);

            await _db.SaveChangesAsync(
                cancellationToken);

            return new ProfileResponse
            {
                UserId = user.Id,
                Message = "Profile saved.",
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = user.Email
            };
        }

        // ─── GET PROFILE ──────────────────────────────────────────────────────

        public async Task<ProfileRequest?> GetProfileAsync(
    int userId,
    CancellationToken cancellationToken = default)
        {
            User? user =
                await _db.Users
                    .AsNoTracking()
                    .Include(existingUser =>
                        existingUser.Profile)
                    .Include(existingUser =>
                        existingUser.Skills)
                    .FirstOrDefaultAsync(
                        existingUser =>
                            existingUser.Id == userId,
                        cancellationToken);

            if (user is null)
            {
                return null;
            }

            string[] nameParts =
                (user.FullName ?? string.Empty)
                    .Split(
                        ' ',
                        2,
                        StringSplitOptions.RemoveEmptyEntries);

            return new ProfileRequest
            {
                FirstName =
                    nameParts.ElementAtOrDefault(0)
                    ?? string.Empty,

                LastName =
                    nameParts.ElementAtOrDefault(1)
                    ?? string.Empty,

                Email = user.Email,

                Institution =
                    user.Profile?.Institution,

                DegreeLevel =
                    user.Profile?.Degree,

                FieldOfStudy =
                    user.Profile?.FieldOfStudy,

                GraduationYear =
                    user.Profile?.GraduationYear
                        ?.ToString(),

                Classification =
                    user.Profile?.GpaClassification,

                Industries =
                    SplitCsv(
                        user.Profile
                            ?.PreferredIndustries),

                JobTypes =
                    SplitCsv(
                        user.Profile
                            ?.JobTypePreference),

                WorkLocation =
                    user.Profile?.LocationPreference,

                SalaryRange =
                    user.Profile?.SalaryExpectation,

                CareerGoal =
                    user.Profile?.CareerGoals,

                TechSkills =
                    user.Skills
                        .Where(skill =>
                            skill.SkillType ==
                            "technical")
                        .Select(skill =>
                            skill.SkillName)
                        .ToList(),

                SoftSkills =
                    user.Skills
                        .Where(skill =>
                            skill.SkillType ==
                            "soft")
                        .Select(skill =>
                            skill.SkillName)
                        .ToList(),

                Languages =
                    user.Skills
                        .Where(skill =>
                            skill.SkillType ==
                            "language")
                        .Select(skill =>
                            skill.SkillName)
                        .ToList(),

                Certifications =
                    user.Skills
                        .Where(skill =>
                            skill.SkillType ==
                            "certification")
                        .Select(skill =>
                            skill.SkillName)
                        .ToList()
            };
        }

        // ─── SAVE CV ──────────────────────────────────────────────────────────

        public async Task SaveCvAsync(
    int userId,
    IFormFile file,
    CancellationToken cancellationToken = default)
        {
            User user =
                await _db.Users.FindAsync(
                    [userId],
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "Authenticated user was not found.");

            string extension =
                Path.GetExtension(file.FileName)
                    .ToLowerInvariant();

            string safeFileName =
                $"{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension}";

            string filePath =
                Path.Combine(
                    _cvStoragePath,
                    safeFileName);

            await using (
                var stream =
                    new FileStream(
                        filePath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None))
            {
                await file.CopyToAsync(
                    stream,
                    cancellationToken);
            }

            var cvDocument =
                new CvDocument
                {
                    UserId = userId,
                    FileName = file.FileName,
                    FilePath = filePath,
                    FileType =
                        extension.TrimStart('.'),
                    IsBaseCv = true,
                    CreatedAt = DateTime.UtcNow
                };

            _db.CvDocuments.Add(
                cvDocument);

            try
            {
                string parsedText =
                    await ExtractCvTextAsync(
                        filePath,
                        file.ContentType,
                        cancellationToken);

                cvDocument.Changelog =
                    parsedText;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "CV text extraction failed for user {UserId}. The file will still be stored.",
                    userId);
            }

            await _db.SaveChangesAsync(
                cancellationToken);
        }

        // ─── SKILLS SYNC ──────────────────────────────────────────────────────

        private async Task SyncSkillsAsync(
     int userId,
     ProfileRequest request,
     CancellationToken cancellationToken = default)
        {
            List<Skill> existingSkills =
                await _db.Skills
                    .Where(skill =>
                        skill.UserId == userId)
                    .ToListAsync(
                        cancellationToken);

            _db.Skills.RemoveRange(
                existingSkills);

            var skills =
                new List<Skill>();

            skills.AddRange(
                request.TechSkills.Select(
                    skillName =>
                        new Skill
                        {
                            UserId = userId,
                            SkillName = skillName.Trim(),
                            SkillType = "technical",
                            ProficiencyLevel =
                                "intermediate"
                        }));

            skills.AddRange(
                request.SoftSkills.Select(
                    skillName =>
                        new Skill
                        {
                            UserId = userId,
                            SkillName = skillName.Trim(),
                            SkillType = "soft",
                            ProficiencyLevel =
                                "intermediate"
                        }));

            skills.AddRange(
                request.Languages.Select(
                    skillName =>
                        new Skill
                        {
                            UserId = userId,
                            SkillName = skillName.Trim(),
                            SkillType = "language",
                            ProficiencyLevel =
                                "fluent"
                        }));

            skills.AddRange(
                request.Certifications.Select(
                    skillName =>
                        new Skill
                        {
                            UserId = userId,
                            SkillName = skillName.Trim(),
                            SkillType =
                                "certification",
                            ProficiencyLevel =
                                "certified"
                        }));

            skills =
                skills
                    .Where(skill =>
                        !string.IsNullOrWhiteSpace(
                            skill.SkillName))
                    .ToList();

            if (skills.Count > 0)
            {
                await _db.Skills.AddRangeAsync(
                    skills,
                    cancellationToken);
            }
        }

        // ─── CV TEXT EXTRACTION ───────────────────────────────────────────────

        private async Task<string> ExtractCvTextAsync(
            string filePath,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_geminiKey)) return string.Empty;
            if (!contentType.Contains("pdf")) return string.Empty;

            byte[] fileBytes =
                await File.ReadAllBytesAsync(
                    filePath,
                    cancellationToken); var base64    = Convert.ToBase64String(fileBytes);

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

            HttpResponseMessage response = await _httpClient.PostAsync("https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key=" + _geminiKey, content, cancellationToken);
            if (!response.IsSuccessStatusCode) return string.Empty;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
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
