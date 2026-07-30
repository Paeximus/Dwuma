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
          maxOutputTokens: 3500,
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

        private static string LimitText(
            string text,
            int maximumCharacters)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            text = text.Trim();

            return text.Length <= maximumCharacters
                ? text
                : text[..maximumCharacters];
        }

        private static string BuildPrompt(
        TailorRequest request)
        {
            string tailoringFocus =
                string.IsNullOrWhiteSpace(request.TailoringFocus)
                    ? "Improve relevance to the target role."
                    : request.TailoringFocus.Trim();

            string companyName =
                string.IsNullOrWhiteSpace(request.CompanyName)
                    ? "Not provided"
                    : request.CompanyName.Trim();

            string cvText =
                LimitText(
                    request.CvText,
                    18_000);

            string jobDescription =
                LimitText(
                    request.JobDescription,
                    10_000);

            return $$"""
    You are an ATS optimisation expert, professional CV writer, and recruitment specialist.

    Tailor the candidate's CV for the target job while preserving complete factual accuracy.

    CRITICAL TRUTHFULNESS RULES:

    1. Use only facts explicitly stated in the original CV.
    2. Never invent or assume skills, qualifications, certifications, projects, responsibilities, achievements, employment, or experience.
    3. Do not add a technology to the tailored CV merely because it appears in the job description.
    4. Do not convert an interest, learning goal, course, certification, exposure, or general knowledge into practical experience.
    5. Do not describe the candidate as experienced, familiar, proficient, skilled, or knowledgeable in a technology unless the original CV explicitly supports that statement.
    6. If a required job skill is absent from the original CV, place it only in "missingKeywords".
    7. Never insert missing job requirements into the CV's skills section.
    8. Do not add numerical achievements, percentages, quantities, business results, or performance improvements unless they are present in the original CV.
    9. Do not add deployment, cloud, Docker, Azure, testing, leadership, teamwork, or production experience unless explicitly stated in the original CV.
    10. Improve wording, organisation, grammar, clarity, and relevance while preserving the original factual meaning.
    11. A professional summary may be created, but every claim in it must be directly supported by the original CV.
    12. Use graduate-level wording when the candidate has limited professional experience.
    13. Do not exaggerate academic or personal projects as commercial, production, or professional experience.
    14. Describe academic and personal project experience as project-based experience where appropriate.
    15. Do not add inferred outcomes such as improved quality, increased usability, ensured integrity, enhanced performance, strengthened collaboration, or increased reliability unless the original CV explicitly states those outcomes.
    16. Do not change a factual verb into a stronger verb unless the original CV supports it.
    17. Do not change "developed" into "designed and developed" unless both activities are explicitly stated.
    18. Do not claim the candidate led, managed, owned, architected, delivered, or drove work unless the original CV explicitly says so.
    19. Do not describe a system as robust, scalable, production-ready, secure, high-performance, or enterprise-grade unless the original CV explicitly supports that description.
    20. Do not transform participation into ownership or assistance into leadership.

    ATS RULES:

    21. Improve ATS keyword alignment using only keywords supported by the original CV.
    22. Reorder existing skills and experience to prioritise information relevant to the job.
    23. Use clear section headings and concise bullet points.
    24. Use achievement-oriented wording only where the original CV contains a genuine result or contribution.
    25. Missing job keywords must be reported in "missingKeywords", not inserted into "tailoredCv".
    26. The ATS score must reflect the candidate's actual match before adding any missing skills.
    27. Keep the tailored CV professional, concise, and suitable for the stated role.
    28. Matched keywords must appear directly in or be clearly supported by the original CV.
    29. Do not count unsupported synonyms as matched keywords.
    30. Keep the ATS score realistic and do not inflate it because the CV has been rewritten.

    TOKEN-SAVING RULES:

    31. Keep the tailored CV close to the original CV's length.
    32. Do not repeat the same information in multiple sections.
    33. Keep the ATS summary under 80 words.
    34. Return no more than 12 matched keywords.
    35. Return no more than 10 missing keywords.
    36. Return no more than 8 changelog items.
    37. Keep each changelog reason under 25 words.
    38. Avoid unnecessary explanations and long introductory text.

    JSON OUTPUT RULES:

    39. Return only one valid JSON object.
    40. Do not return Markdown.
    41. Do not return code fences.
    42. Do not include explanations before or after the JSON.
    43. Escape all line breaks inside "tailoredCv" using \n.
    44. Escape quotation marks inside JSON strings.
    45. Ensure the response can be parsed by System.Text.Json.
    46. "matchedKeywords" must contain only keywords supported by the original CV.
    47. "missingKeywords" must contain important job requirements not supported by the original CV.
    48. Each changelog item must describe a real change made to the CV.
    49. Do not place unsupported skills inside "tailoredCv" and also list them as missing.
    50. Return an ATS score between 0 and 100.
    51. Return arrays even when they are empty.
    52. Do not return null values.

    TARGET JOB

    Job title:
    {{request.JobTitle}}

    Company:
    {{companyName}}

    Tailoring focus:
    {{tailoringFocus}}

    Job description:
    {{jobDescription}}

    ORIGINAL CANDIDATE CV

    {{cvText}}

    RETURN THIS EXACT JSON STRUCTURE:

    {
      "tailoredCv": "Candidate Name\nPROFESSIONAL SUMMARY\nAccurate summary based only on the original CV.\n\nSKILLS\nOnly skills explicitly supported by the original CV.",
      "atsScore": 0,
      "atsSummary": "A truthful summary explaining the level of alignment between the original CV and the job.",
      "matchedKeywords": [
        "Only keywords explicitly supported by the original CV"
      ],
      "missingKeywords": [
        "Important job requirements not stated in the original CV"
      ],
      "changelog": [
        {
          "type": "rewrite",
          "section": "Professional Summary",
          "reason": "Improved clarity without adding unsupported claims."
        }
      ]
    }

    Before returning the JSON, perform a final factual verification:

    - Compare every skill in tailoredCv against the original CV.
    - Compare every claim in the professional summary against the original CV.
    - Compare every project bullet against the original CV.
    - Compare every experience bullet against the original CV.
    - Remove unsupported adjectives and outcomes.
    - Move unsupported job requirements to missingKeywords.
    - Ensure every matched keyword is supported by the original CV.
    - Ensure no facts were invented.

    OUTPUT JSON ONLY.
    """;
        }

    }

}