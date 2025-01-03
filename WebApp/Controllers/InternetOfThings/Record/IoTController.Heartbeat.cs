﻿using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.InternetOfThings.Record;

public partial class IoTController
{
    [HttpGet("api/{device}/heartbeat")]
    public IActionResult Heartbeat([FromQuery] string device)
    {
        return Ok();
    }
}