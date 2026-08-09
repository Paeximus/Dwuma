using Dwuma.Scraping.Models;
using Dwuma.Scraping.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dwuma.Controllers;

[ApiController]
[Route("api/scraper")]
public sealed class ScraperController : ControllerBase
{
    private readonly InterviewQuestionScraper
        _scraper;

    public ScraperController(
        InterviewQuestionScraper scraper)
    {
        _scraper = scraper;
    }

    [HttpGet("interview-questions")]
    public async Task<ActionResult<
        List<ScrapedInterviewQuestion>>>
        ScrapeInterviewQuestions(
            [FromQuery] string url,
            [FromQuery] string source = "Web",
            [FromQuery] string? role = null,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return BadRequest(
                new
                {
                    message =
                        "Please provide a URL."
                });
        }

        try
        {
            var questions =
                await _scraper.ScrapeAsync(
                    url,
                    source,
                    role,
                    cancellationToken);

            return Ok(questions);
        }
        catch (HttpRequestException exception)
        {
            return BadRequest(
                new
                {
                    message =
                        "The website could not be retrieved.",

                    error =
                        exception.Message
                });
        }
    }
}