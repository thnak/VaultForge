using System.Text;
using Business.Business.Interfaces.Wiki;
using BusinessModels.Wiki;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.Wiki;

[Route("api/[controller]")]
[ApiController]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
public class WikiController(IWikipediaBusinessLayer wikipediaBusinessLayer) : ControllerBase
{
    [HttpPost("insert")]
    public async Task<IActionResult> InsertNew([FromForm] string title, [FromForm] string content, [FromForm] string url, [FromForm] string? lang)
    {
        await wikipediaBusinessLayer.CreateAsync(new WikipediaDatasetModel()
        {
            Title = title,
            Text = content,
            Url = url,
            Language = lang ?? "en-US"
        });
        return Ok();
    }

    [HttpGet("search")]
    public async Task<IActionResult> GetWikipedias([FromQuery] string query)
    {
        var token = HttpContext.RequestAborted;
        var result = await wikipediaBusinessLayer.SearchRag(query, 10, token);
        StringBuilder sb = new();
        foreach (var searchScore in result)
        {
            sb.AppendLine($"{searchScore.Score:P1} {searchScore.Value.Key}: {searchScore.Value.Title}");
        }

        var planText = sb.ToString();
        return Content(planText, "text/plain", Encoding.UTF8);
    }
}