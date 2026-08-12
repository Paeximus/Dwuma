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

            TailorResponse result = ParseResponse(rawResponse);

            result.TailoredCv =
                NormalizeTailoredCvText(
                    result.TailoredCv);

            return result;
        }

        public async Task<string> ParseCvFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new Exception("CV file is empty.");
            }

            string extension =
                Path.GetExtension(file.FileName)
                    .ToLowerInvariant();

            // TXT
            if (file.ContentType == "text/plain" ||
                extension == ".txt")
            {
                using var reader =
                    new StreamReader(file.OpenReadStream());

                string text =
                    await reader.ReadToEndAsync();

                return CleanParsedCvText(text);
            }

            // PDF
            if (file.ContentType == "application/pdf" ||
                extension == ".pdf")
            {
                using var stream =
                    file.OpenReadStream();

                using var pdf =
                    PdfDocument.Open(stream);

                var builder =
                    new StringBuilder();

                foreach (var page in pdf.GetPages())
                {
                    string pageText =
                        page.Text;

                    if (string.IsNullOrWhiteSpace(pageText))
                    {
                        continue;
                    }

                    builder.AppendLine(
                        pageText.Trim());

                    builder.AppendLine();
                }

                string text =
                    builder.ToString();

                if (string.IsNullOrWhiteSpace(text))
                {
                    throw new Exception(
                        "No readable text found in the PDF.");
                }

                return CleanParsedCvText(text);
            }

            // DOCX
            if (file.ContentType ==
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document" ||
                extension == ".docx")
            {
                using var memoryStream =
                    new MemoryStream();

                await file.CopyToAsync(
                    memoryStream);

                memoryStream.Position = 0;

                using var document =
                    WordprocessingDocument.Open(
                        memoryStream,
                        false);

                Body? body =
                    document.MainDocumentPart?
                        .Document?
                        .Body;

                if (body == null)
                {
                    throw new Exception(
                        "The Word document does not contain readable content.");
                }

                var builder =
                    new StringBuilder();

                foreach (Paragraph paragraph
                         in body.Descendants<Paragraph>())
                {
                    string text =
                        paragraph.InnerText?.Trim()
                        ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        builder.AppendLine();
                        continue;
                    }

                    builder.AppendLine(text);
                }

                string extractedText =
                    builder.ToString();

                if (string.IsNullOrWhiteSpace(
                        extractedText))
                {
                    throw new Exception(
                        "No readable text found in the Word document.");
                }

                return CleanParsedCvText(
                    extractedText);
            }

            throw new Exception(
                "Only PDF, DOCX and TXT files are supported.");
        }

        public async Task<byte[]> GenerateDocxAsync(
    DownloadRequest request)
        {
            if (string.IsNullOrWhiteSpace(
                    request.TailoredCv))
            {
                throw new Exception(
                    "No CV content provided.");
            }

            string cvText =
                NormalizeTailoredCvText(
                    request.TailoredCv);

            using var memoryStream =
                new MemoryStream();

            using (var document =
                WordprocessingDocument.Create(
                    memoryStream,
                    DocumentFormat.OpenXml
                        .WordprocessingDocumentType.Document,
                    true))
            {
                MainDocumentPart mainPart =
                    document.AddMainDocumentPart();

                mainPart.Document =
                    new Document();

                Body body =
                    new Body();

                AddPageSettings(body);

                string[] lines =
                    cvText.Split(
                        '\n',
                        StringSplitOptions.None);

                bool firstContentLine = true;

                foreach (string rawLine in lines)
                {
                    string line =
                        rawLine.Trim();

                    if (string.IsNullOrWhiteSpace(
                            line))
                    {
                        continue;
                    }

                    if (firstContentLine)
                    {
                        AddNameParagraph(
                            body,
                            line);

                        firstContentLine = false;
                        continue;
                    }

                    if (IsCvHeading(line))
                    {
                        AddHeadingParagraph(
                            body,
                            line);

                        continue;
                    }

                    if (IsBullet(line))
                    {
                        string bulletText =
                            RemoveBulletPrefix(line);

                        AddBulletParagraph(
                            body,
                            bulletText);

                        continue;
                    }

                    AddNormalParagraph(
                        body,
                        line);
                }

                mainPart.Document.Append(
                    body);

                mainPart.Document.Save();
            }

            return await Task.FromResult(
                memoryStream.ToArray());
        }

        private static void AddNameParagraph(
    Body body,
    string text)
        {
            var paragraph =
                new Paragraph();

            var properties =
                new ParagraphProperties(
                    new Justification
                    {
                        Val = JustificationValues.Center
                    },
                    new SpacingBetweenLines
                    {
                        After = "120"
                    });

            paragraph.Append(
                properties);

            var run =
                new Run();

            run.Append(
                new RunProperties(
                    new Bold(),
                    new FontSize
                    {
                        Val = "32"
                    },
                    new RunFonts
                    {
                        Ascii = "Arial",
                        HighAnsi = "Arial"
                    }));

            run.Append(
                new Text(text)
                {
                    Space =
                        DocumentFormat.OpenXml
                            .SpaceProcessingModeValues
                            .Preserve
                });

            paragraph.Append(run);

            body.Append(paragraph);
        }

        private static string RemoveBulletPrefix( string line)
        {
            string value =
                line.Trim();

            if (value.StartsWith("• ") ||
                value.StartsWith("- ") ||
                value.StartsWith("* "))
            {
                return value[2..].Trim();
            }

            return value;
        }

        private static bool IsBullet(string line)
        {
            return
                line.StartsWith("• ") ||
                line.StartsWith("- ") ||
                line.StartsWith("* ");
        }

        private static bool IsCvHeading( string line)
        {
            string normalized =
                line.Trim()
                    .TrimEnd(':')
                    .ToUpperInvariant();

            string[] headings =
            [
                "PROFESSIONAL SUMMARY",
        "CAREER SUMMARY",
        "PROFILE",
        "SUMMARY",

        "SKILLS",
        "CORE SKILLS",
        "TECHNICAL SKILLS",
        "KEY SKILLS",

        "WORK EXPERIENCE",
        "PROFESSIONAL EXPERIENCE",
        "EMPLOYMENT HISTORY",
        "EXPERIENCE",

        "PROJECTS",
        "PROJECT EXPERIENCE",

        "EDUCATION",
        "ACADEMIC BACKGROUND",

        "CERTIFICATIONS",
        "CERTIFICATES",

        "ACHIEVEMENTS",
        "AWARDS",

        "VOLUNTEER EXPERIENCE",

        "LEADERSHIP EXPERIENCE",

        "REFERENCES"
            ];

            return headings.Contains(
                normalized);
        }

        private static void AddBulletParagraph( Body body, string text)
        {
            var paragraph =
                new Paragraph();

            paragraph.Append(
                new ParagraphProperties(
                    new Indentation
                    {
                        Left = "360",
                        Hanging = "180"
                    },
                    new SpacingBetweenLines
                    {
                        After = "60",
                        Line = "276",
                        LineRule =
                            LineSpacingRuleValues.Auto
                    }));

            var run =
                new Run();

            run.Append(
                new RunProperties(
                    new FontSize
                    {
                        Val = "21"
                    },
                    new RunFonts
                    {
                        Ascii = "Arial",
                        HighAnsi = "Arial"
                    }));

            run.Append(
                new Text(
                    $"• {text}")
                {
                    Space =
                        DocumentFormat.OpenXml
                            .SpaceProcessingModeValues
                            .Preserve
                });

            paragraph.Append(run);

            body.Append(paragraph);
        }

        private static void AddNormalParagraph( Body body, string text)
        {
            var paragraph =
                new Paragraph();

            paragraph.Append(
                new ParagraphProperties(
                    new SpacingBetweenLines
                    {
                        After = "80",
                        Line = "276",
                        LineRule =
                            LineSpacingRuleValues.Auto
                    }));

            var run =
                new Run();

            run.Append(
                new RunProperties(
                    new FontSize
                    {
                        Val = "21"
                    },
                    new RunFonts
                    {
                        Ascii = "Arial",
                        HighAnsi = "Arial"
                    }));

            run.Append(
                new Text(text)
                {
                    Space =
                        DocumentFormat.OpenXml
                            .SpaceProcessingModeValues
                            .Preserve
                });

            paragraph.Append(run);

            body.Append(paragraph);
        }

        private static void AddHeadingParagraph( Body body, string text)
        {
            var paragraph =
                new Paragraph();

            paragraph.Append(
                new ParagraphProperties(
                    new SpacingBetweenLines
                    {
                        Before = "220",
                        After = "80"
                    }));

            var run =
                new Run();

            run.Append(
                new RunProperties(
                    new Bold(),
                    new FontSize
                    {
                        Val = "23"
                    },
                    new RunFonts
                    {
                        Ascii = "Arial",
                        HighAnsi = "Arial"
                    }));

            run.Append(
                new Text(
                    text.ToUpperInvariant()));

            paragraph.Append(run);

            body.Append(paragraph);
        }

        private static void AddPageSettings(Body body)
        {
            var sectionProperties =
                new SectionProperties();

            var pageMargin =
                new PageMargin
                {
                    Top = 720,
                    Bottom = 720,
                    Left = 900,
                    Right = 900
                };

            sectionProperties.Append(
                pageMargin);

            body.Append(
                sectionProperties);
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

        public string NormalizeTailoredCvText(
    string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            string cleaned =
                text
                    .Replace("\r\n", "\n")
                    .Replace("\r", "\n")
                    .Replace('\u00A0', ' ')
                    .Trim();

            // Remove Markdown fences if Gemini adds them
            cleaned =
                cleaned.Replace(
                    "```text",
                    "",
                    StringComparison.OrdinalIgnoreCase);

            cleaned =
                cleaned.Replace(
                    "```markdown",
                    "",
                    StringComparison.OrdinalIgnoreCase);

            cleaned =
                cleaned.Replace(
                    "```",
                    "");

            // Reduce excessive spaces
            cleaned =
                System.Text.RegularExpressions.Regex
                    .Replace(
                        cleaned,
                        @"[ \t]+",
                        " ");

            // Maximum of one empty line
            cleaned =
                System.Text.RegularExpressions.Regex
                    .Replace(
                        cleaned,
                        @"\n[ \t]*\n[ \t]*\n+",
                        "\n\n");

            string[] lines =
                cleaned.Split('\n');

            var result =
                new List<string>();

            foreach (string rawLine in lines)
            {
                string line =
                    rawLine.Trim();

                // Fix bullets
                if (line.StartsWith("- "))
                {
                    line =
                        "• " +
                        line[2..].Trim();
                }
                else if (line.StartsWith("* "))
                {
                    line =
                        "• " +
                        line[2..].Trim();
                }

                result.Add(line);
            }

            return string.Join(
                Environment.NewLine,
                result)
                .Trim();
        }


        public string CleanParsedCvText(
           string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            text = text
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace('\u00A0', ' ');

            string[] rawLines =
                text.Split('\n');

            var cleanedLines =
                new List<string>();

            bool previousWasBlank = false;

            foreach (string rawLine in rawLines)
            {
                string line =
                    System.Text.RegularExpressions.Regex
                        .Replace(
                            rawLine.Trim(),
                            @"[ \t]+",
                            " ");

                if (string.IsNullOrWhiteSpace(line))
                {
                    if (!previousWasBlank &&
                        cleanedLines.Count > 0)
                    {
                        cleanedLines.Add(
                            string.Empty);
                    }

                    previousWasBlank = true;
                    continue;
                }

                cleanedLines.Add(line);

                previousWasBlank = false;
            }

            return string.Join(
                Environment.NewLine,
                cleanedLines)
                .Trim();
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
    16. You may replace weak or informal verbs with professional action verbs when the new verb accurately describes the same activity stated in the original CV. Do not increase the candidate's level of responsibility, ownership, or achievement.
    17. Do not change "developed" into "designed and developed" unless both activities are explicitly stated.
    18. Do not claim the candidate led, managed, owned, architected, delivered, or drove work unless the original CV explicitly says so.
    19. Do not describe a system as robust, scalable, production-ready, secure, high-performance, or enterprise-grade unless the original CV explicitly supports that description.
    20. Do not transform participation into ownership or assistance into leadership.

        TAILORING TRANSFORMATION RULES:

    21. The tailored CV must be meaningfully rewritten and reorganised for the target role while preserving factual accuracy.
    22. Do not simply reproduce the original CV with minor grammatical corrections.
    23. Rewrite existing experience and project bullets to emphasise aspects most relevant to the target job.
    24. Reorder skills so the most relevant supported skills appear first.
    25. Reorder projects and experience entries when doing so improves relevance to the target role.
    26. Rewrite the professional summary specifically for the target job using only evidence from the original CV.
    27. Remove or shorten low-relevance wording when necessary to make job-relevant information more prominent.
    28. Convert vague or poorly structured sentences into concise professional CV bullet points without changing their factual meaning.
    29. You may combine closely related facts from the same role or project into a stronger, clearer bullet, provided no new information is introduced.
    30. You may use terminology from the job description when it accurately describes something already demonstrated in the original CV.
    31. Prefer job-relevant terminology over generic wording where both expressions have the same factual meaning.
    32. The final CV should visibly differ from the original in organisation, emphasis, wording, and prioritisation whenever the original information allows it.
    33. Do not make changes merely for visual difference. Every change must improve relevance, clarity, ATS compatibility, or professional presentation.
    34. When sufficient relevant information exists, rewrite at least the professional summary and the most relevant experience/project bullets rather than returning them unchanged.

    ATS RULES:

    35. Improve ATS keyword alignment by identifying terminology in the job description that is equivalent to facts already demonstrated in the original CV. Use that terminology naturally where factually justified.
    36. Reorder existing skills and experience to prioritise information relevant to the job.
    37. Use clear section headings and concise bullet points.
    38. Use achievement-oriented wording only where the original CV contains a genuine result or contribution.
    39. Missing job keywords must be reported in "missingKeywords", not inserted into "tailoredCv".
    40. The ATS score must reflect the candidate's actual match before adding any missing skills.
    41. Keep the tailored CV professional, concise, and suitable for the stated role.
    42. Matched keywords must appear directly in or be clearly supported by the original CV.
    43. Do not count unsupported synonyms as matched keywords.
    44. Keep the ATS score realistic and do not inflate it because the CV has been rewritten.
    TOKEN-SAVING RULES:

    45. Keep the tailored CV concise, but allow reasonable restructuring, rewriting, and expansion where needed to clearly present existing experience relevant to the target role.
    46. Do not repeat the same information in multiple sections.
    47. Keep the ATS summary under 80 words.
    48. Return no more than 12 matched keywords.
    49. Return no more than 10 missing keywords.
    50. Return no more than 8 changelog items.
    51. Keep each changelog reason under 25 words.
    52. Avoid unnecessary explanations and long introductory text.

    CV FORMATTING RULES:

    53. Return the complete CV content in "tailoredCv".
    54. Preserve the candidate's name and contact information.
    55. Put each major CV section heading on its own line.
    56. Separate major sections using exactly one blank line.
    57. Use concise bullet points for experience, projects, achievements, and responsibilities.
    58. Each bullet point must appear on its own line.
    59. Do not combine multiple experience bullets into a paragraph.
    60. Keep employer, role, institution, qualification, and date information clearly separated.
    61. Do not use tables.
    62. Do not use Markdown headings such as #, ##, or ###.
    63. Do not use Markdown bold markers such as **.
    64. Do not use code fences.
    65. Use these section names when the corresponding information exists:
        PROFESSIONAL SUMMARY
        SKILLS
        WORK EXPERIENCE
        PROJECTS
        EDUCATION
        CERTIFICATIONS
    66. Do not create a section when the original CV contains no information for that section.
    67. Preserve readable whitespace and logical section ordering.

    JSON OUTPUT RULES:

    68. Return only one valid JSON object.
    69. Do not return Markdown.
    70. Do not return code fences.
    71. Do not include explanations before or after the JSON.
    72. Escape all line breaks inside "tailoredCv" using \n.
    73. Escape quotation marks inside JSON strings.
    74. Ensure the response can be parsed by System.Text.Json.
    75. "matchedKeywords" must contain only keywords supported by the original CV.
    76. "missingKeywords" must contain important job requirements not supported by the original CV.
    77. Each changelog item must describe a real change made to the CV.
    78. Do not place unsupported skills inside "tailoredCv" and also list them as missing.
    79. Return an ATS score between 0 and 100.
    80. Return arrays even when they are empty.
    81. Do not return null values.

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
    "tailoredCv": "Candidate Name\nEmail | Phone | Location\n\nPROFESSIONAL SUMMARY\nAccurate professional summary based only on the original CV.\n\nSKILLS\n• Supported skill one\n• Supported skill two\n\nWORK EXPERIENCE\nJob Title | Company | Dates\n• Truthful responsibility from the original CV.\n• Truthful achievement from the original CV.\n\nEDUCATION\nQualification | Institution | Dates"      "atsScore": 0,
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