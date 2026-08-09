using AngleSharp.Html.Parser;
using Dwuma.Scraping.Models;

namespace Dwuma.Scraping.Services;

public sealed class InterviewQuestionScraper
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InterviewQuestionScraper> _logger;

    public InterviewQuestionScraper(
        HttpClient httpClient,
        ILogger<InterviewQuestionScraper> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<ScrapedInterviewQuestion>> ScrapeAsync(
        string url,
        string sourceName,
        string? role = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException(
                "A URL is required.",
                nameof(url));
        }

        _logger.LogInformation(
            "Scraping interview questions from {Url}",
            url);

        string html =
            await _httpClient.GetStringAsync(
                url,
                cancellationToken);

        var parser = new HtmlParser();

        var document =
            await parser.ParseDocumentAsync(
                html,
                cancellationToken);

        var questions =
            new List<ScrapedInterviewQuestion>();

        var elements =
            document.QuerySelectorAll(
                "p, li, h2, h3, h4");

        foreach (var element in elements)
        {
            string text =
                CleanText(
                    element.TextContent);

            if (!LooksLikeQuestion(text))
            {
                continue;
            }

            questions.Add(
                new ScrapedInterviewQuestion
                {
                    Question = text,
                    Role = role,
                    Source = sourceName,
                    SourceUrl = url,
                    ScrapedAt = DateTime.UtcNow
                });
        }

        questions =
            questions
                .GroupBy(
                    question =>
                        question.Question.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                    group.First())
                .ToList();

        _logger.LogInformation(
            "Found {Count} interview questions from {Url}",
            questions.Count,
            url);

        return questions;
    }

    private static bool LooksLikeQuestion(
    string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        text = text.Trim();

        if (text.Length < 15)
        {
            return false;
        }

        if (text.Length > 500)
        {
            return false;
        }

        // Normal question
        if (text.EndsWith("?"))
        {
            return true;
        }

        string lower =
            text.ToLowerInvariant();

        string[] interviewPromptStarts =
        [
            "tell me about",
        "describe ",
        "explain ",
        "walk me through",
        "give me an example",
        "share an example",
        "discuss ",
        "how would you",
        "how do you",
        "what would you",
        "what do you",
        "why do you",
        "when have you"
        ];

        return interviewPromptStarts.Any(
            prefix =>
                lower.StartsWith(prefix));
    }

    private static string CleanText(
    string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string cleaned =
            string.Join(
                " ",
                text.Split(
                    new[]
                    {
                    ' ',
                    '\n',
                    '\r',
                    '\t'
                    },
                    StringSplitOptions
                        .RemoveEmptyEntries));

        // Remove numbering such as:
        // 1. Question
        // 2) Question
        // 3 - Question
        // 4: Question
        cleaned =
            System.Text.RegularExpressions.Regex.Replace(
                cleaned,
                @"^\s*\d+\s*[\.\)\-:]\s*",
                "");

        return cleaned.Trim();
    }
}