using BusinessModels.System.InternetOfThings.type;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.InternetOfThings.Record;

public partial class IoTController
{
    [HttpPost("update-record-value")]
    public async Task<IActionResult> UpdateRecord([FromForm] string sensorId, [FromForm] float value)
    {
        var result = await iIotRecordBusinessService.UpdateIotValue(sensorId, value, ProcessStatus.Completed);
        return result.IsSuccess ? Ok(result.Message) : BadRequest(result.Message);
    }
}