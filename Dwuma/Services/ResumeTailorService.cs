using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Dwuma.Models;
using UglyToad.PdfPig;

namespace Dwuma.Services
{
    public class ResumeTailorService
    {
        private readonly GeminiService _gemini;
        private readonly ILogger<ResumeTailorService> _logger;

        public ResumeTailorService(
            GeminiService gemini,
            ILogger<ResumeTailorService> logger)
        {
            _gemini = gemini;
            _logger = logger;
        }

        public async Task<TailorResponse> TailorAsync(
            TailorRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateRequest(request);

            var prompt = BuildPrompt(request);

            _logger.LogInformation(
                "Tailoring CV for role {Role}",
                request.JobTitle);

            var rawResponse =
      await _gemini.GenerateJsonAsync(
          prompt,
          responseSchema: null,
          maxOutputTokens: 5000,
          cancellationToken: cancellationToken);

            _logger.LogInformation("Gemini Response:\n{Response}", rawResponse);

            return ParseResponse(rawResponse);
        }

        public async Task<string> ParseCvFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new Exception("CV file is empty.");

            if (file.ContentType == "text/plain")
            {
                using var reader =
                    new StreamReader(file.OpenReadStream());

                return await reader.ReadToEndAsync();
            }

            if (file.ContentType == "application/pdf")
            {
                using var stream = file.OpenReadStream();
                using var pdf = PdfDocument.Open(stream);

                var text = string.Join(
                    Environment.NewLine,
                    pdf.GetPages().Select(p => p.Text));

                if (string.IsNullOrWhiteSpace(text))
                    throw new Exception("No readable text found.");

                return text;
            }

            throw new Exception(
                "Only PDF and TXT files are supported.");
        }

        public async Task<byte[]> GenerateDocxAsync(
            DownloadRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TailoredCv))
                throw new Exception("No CV content provided.");

            using var memoryStream = new MemoryStream();

            using (var document =
                   WordprocessingDocument.Create(
                       memoryStream,
                       DocumentFormat.OpenXml.WordprocessingDocumentType.Document,
                       true))
            {
                var mainPart =
                    document.AddMainDocumentPart();

                mainPart.Document = new Document();

                var body = new Body();

                var lines = request.TailoredCv.Split(
                    Environment.NewLine,
                    StringSplitOptions.None);

                foreach (var line in lines)
                {
                    body.Append(
                        new Paragraph(
                            new Run(
                                new Text(line))));
                }

                mainPart.Document.Append(body);
                mainPart.Document.Save();
            }

            return await Task.FromResult(
                memoryStream.ToArray());
        }

        private static void ValidateRequest(
            TailorRequest request)
        {
            if (request == null)
                throw new Exception("Request is required.");

            if (string.IsNullOrWhiteSpace(request.CvText))
                throw new Exception("CV text is required.");

            if (string.IsNullOrWhiteSpace(request.JobDescription))
                throw new Exception("Job description is required.");

            if (string.IsNullOrWhiteSpace(request.JobTitle))
                throw new Exception("Job title is required.");
        }

        private TailorResponse ParseResponse(string raw)
        {
            try
            {
                _logger.LogInformation(
                    "Raw JSON received:\n{Json}",
                    raw);

                var start = raw.IndexOf('{');
                var end = raw.LastIndexOf('}');

                if (start >= 0 && end > start)
                {
                    raw = raw.Substring(start, end - start + 1);
                }

                return JsonSerializer.Deserialize<TailorResponse>(
                    raw,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? throw new Exception("Failed to deserialize response.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed JSON:\n{Json}",
                    raw);

                throw;
            }
        }

        private static string BuildPrompt(
    TailorRequest request)
        {
            return $$"""
            You are an ATS optimization expert, professional CV writer, and recruitment specialist.

            Your task is to tailor the candidate's CV to match the target role while remaining completely truthful.

            IMPORTANT RULES:

            1. Return ONLY valid JSON.
            2. Do NOT return markdown.
            3. Do NOT return code fences.
            4. Do NOT return explanations before or after the JSON.
            5. Never invent experience, qualifications, certifications, projects, achievements, or skills.
            6. Rephrase existing content to better align with the job description.
            7. Improve ATS keyword matching.
            8. Use achievement-oriented language where appropriate.
            9. Add a professional summary if one does not already exist.
            10. Escape ALL line breaks inside "tailoredCv" using \\n.
            11. Escape any quotation marks inside text using \\".
            12. The response must be parseable by System.Text.Json.

            JOB TITLE:
            {{request.JobTitle}}

            COMPANY:
            {{request.CompanyName}}

            JOB DESCRIPTION:
            {{request.JobDescription}}

            CANDIDATE CV:
            {{request.CvText}}

            RETURN THIS EXACT JSON SHAPE:

            {
              "tailoredCv": "John Doe\\nProfessional Summary\\nExperienced Data Analyst...",
              "atsScore": 85,
              "atsSummary": "Strong alignment with the job description.",
              "matchedKeywords": [
                "SQL",
                "Python",
                "Data Analysis"
              ],
              "missingKeywords": [
                "Power BI"
              ],
              "changelog": [
                {
                  "type": "rewrite",
                  "section": "Professional Summary",
                  "reason": "Improved alignment with target role."
                }
              ]
            }

            OUTPUT JSON ONLY.
            NO MARKDOWN.
            NO EXPLANATIONS.
            NO EXTRA TEXT.
            """;
        }
    }
    }