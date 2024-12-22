﻿using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.InternetOfThings.Record;

public partial class IoTController
{
    [HttpPost("update-record-value")]
    public async Task<IActionResult> UpdateRecord([FromForm] string sensorId, [FromForm] float value)
    {
        var result = await iIotRecordBusinessService.UpdateIotValue(sensorId, value);
        return result.IsSuccess ? Ok(result.Message) : BadRequest(result.Message);
    }
}