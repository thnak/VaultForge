using Business.Data.Interfaces.ComputeVision;
using BusinessModels.System.ComputeVision;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.ComputeVision;

public partial class YoloLabelController
{
    [HttpPost("add")]
    public async Task<IActionResult> AddLabel([FromForm] string fileId, [FromForm] int label, [FromForm] float x, [FromForm] float y, [FromForm] float width, [FromForm] float height)
    {
        YoloLabel newLabel = new YoloLabel()
        {
            FileId = fileId,
            Label = label,
            X = x,
            Y = y,
            Width = width,
            Height = height
        };
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        await yoloLabelDataLayer.CreateAsync(newLabel);
        return CreatedAtAction(nameof(IYoloLabelDataLayer), new { id = newLabel.Id.ToString() }, newLabel);
    }
}