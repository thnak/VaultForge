using System.Text;
using BrainNet.Utils;
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
        var result = await wikipediaBusinessLayer.CreateAsync(new WikipediaDatasetModel()
        {
            Title = title,
            Text = content,
            Url = url,
            Language = lang ?? "en-US"
        });
        if(result.IsSuccess)
            return Ok();
        return BadRequest(result.Message);
    }

    [HttpPost("search")]
    public async Task<IActionResult> GetWikipedias([FromForm] string query)
    {
        var token = HttpContext.RequestAborted;
        var result = await wikipediaBusinessLayer.SearchRag(query, 10, token);
        StringBuilder sb = new();
        foreach (var searchScore in result.GroupBySearchScore())
        {
            var wiki = wikipediaBusinessLayer.Get(searchScore.Value.Key);
            if (wiki != null)
            {
                sb.AppendLine($"{searchScore.Score:P1}: {searchScore.Value.Title}\n{wiki.Text}\nUrl: {wiki.Url}");
            }
        }

        var planText = sb.ToString();
        return Content(planText, "text/plain", Encoding.UTF8);
    }
}