using BusinessModels.System.ComputeVision;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.ComputeVision;

public partial class YoloLabelController
{
    [HttpGet("get-by-file")]
    public async Task<IActionResult> GetLabelByFileId(string id)
    {
        List<YoloLabel> labels = [];
        await foreach (var label in yoloLabelDataLayer.FindAsync(id)) labels.Add(label);

        labels = [..labels.DistinctBy(x => x.Id)];

        return Ok(labels);
    }
}