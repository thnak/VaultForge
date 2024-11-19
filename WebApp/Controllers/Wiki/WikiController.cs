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
}