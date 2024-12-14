using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.ComputeVision;

public partial class YoloLabelController
{
    [HttpGet("delete-by-id")]
    public async Task<IActionResult> DeleteLabelById(string id)
    {
        await yoloLabelDataLayer.DeleteAsync(id);
        return Ok();
    }
}