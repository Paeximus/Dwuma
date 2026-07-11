using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Dwuma.Models;

namespace Dwuma.Services
{
    public class SkillsGapService
    {
        private readonly HttpClient _httpClient;
        private readonly string _geminiKey;
        private readonly ILogger<SkillsGapService> _logger;

        public SkillsGapService(HttpClient httpClient, IConfiguration config, ILogger<SkillsGapService> logger)
        {
            _httpClient = httpClient;
            _geminiKey = config["Gemini:ApiKey"] ?? throw new Exception("Gemini API key not configured.");
            _logger = logger;
        }

        public async Task<SkillsGapResponse> AnalyseAsync(SkillsGapRequest request)
        {
            var prompt = BuildPrompt(request);
            var rawJson = await CallGeminiAsync(prompt);
            return ParseResponse(rawJson);
        }

        // ─── PROMPT CONSTRUCTION ─────────────────────────────────────────────

        private static string BuildPrompt(SkillsGapRequest r)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are an expert career advisor specialising in the Ghanaian job market.");
            sb.AppendLine("Perform a structured skills gap analysis and return ONLY valid JSON — no markdown, no preamble.");
            sb.AppendLine();
            sb.AppendLine("USER PROFILE:");
            sb.AppendLine($"- Current skills: {string.Join(", ", r.Skills)}");
            if (!string.IsNullOrWhiteSpace(r.Education))   sb.AppendLine($"- Education: {r.Education} in {r.FieldOfStudy}");
            if (!string.IsNullOrWhiteSpace(r.Experience))  sb.AppendLine($"- Years of experience: {r.Experience}");
            sb.AppendLine();
            sb.AppendLine("TARGET ROLE:");
            sb.AppendLine($"- Job Title: {r.JobTitle}");
            if (!string.IsNullOrWhiteSpace(r.Industry))    sb.AppendLine($"- Industry: {r.Industry}");
            if (!string.IsNullOrWhiteSpace(r.JobDescription))
            {
                sb.AppendLine("- Job Description:");
                sb.AppendLine(r.JobDescription);
            }
            sb.AppendLine();
            sb.AppendLine("INSTRUCTIONS:");
            sb.AppendLine("1. Identify the key skills required for this role.");
            sb.AppendLine("2. For each skill, assess whether the user has it (present), partially has it (partial), or lacks it (absent).");
            sb.AppendLine("3. For absent/partial skills, recommend specific learning resources available to Ghanaian graduates.");
            sb.AppendLine("4. Calculate an overall match percentage (0–100).");
            sb.AppendLine("5. Write a 2–3 sentence honest but encouraging summary.");
            sb.AppendLine();
            sb.AppendLine("Return ONLY this JSON structure:");
            sb.AppendLine(@"{
  ""matchPercentage"": 72,
  ""summary"": ""..."",
  ""skills"": [
    {
      ""name"": ""Python"",
      ""status"": ""present"",
      ""level"": ""intermediate"",
      ""description"": ""You listed Python and it is core to this role.""
    },
    {
      ""name"": ""Machine Learning"",
      ""status"": ""absent"",
      ""level"": ""intermediate"",
      ""description"": ""Most data analyst roles in Ghana now expect basic ML knowledge.""
    }
  ],
  ""resources"": [
    {
      ""name"": ""Machine Learning Specialisation"",
      ""platform"": ""Coursera"",
      ""description"": ""Andrew Ng's beginner-friendly ML course. Audit for free."",
      ""url"": ""https://www.coursera.org/specializations/machine-learning-introduction"",
      ""isFree"": true
    }
  ]
}");

            return sb.ToString();
        }


        private async Task<string> CallGeminiAsync(string prompt)
        {
            var requestBody = new
            {
                model = "gpt-4o",
                max_tokens = 2000,
                temperature = 0.4,
                messages = new[]
                {
                    new { role = "system", content = "You are a career advisor AI. Always respond with valid JSON only." },
                    new { role = "user",   content = prompt }
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _geminiKey);

            var response = await _httpClient.PostAsync("https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key=" + _geminiKey, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("GeminiAI error: {Status} — {Body}", response.StatusCode, errorBody);
                throw new Exception($"GeminiAI API error: {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            // Extract the assistant message content
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? throw new Exception("Empty response from GeminiAI.");
        }


        private SkillsGapResponse ParseResponse(string rawJson)
        {
            // Strip any accidental markdown fences
            var cleaned = rawJson.Trim();
            if (cleaned.StartsWith("```")) cleaned = cleaned.Split('\n', 2)[1];
            if (cleaned.EndsWith("```"))   cleaned = cleaned[..^3];
            cleaned = cleaned.Trim();

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<SkillsGapResponse>(cleaned, options)
                    ?? throw new Exception("Null deserialization result.");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse GPT-4o response: {Raw}", rawJson);
                throw new Exception("AI returned an unexpected format. Please try again.");
            }
        }
    }
}
