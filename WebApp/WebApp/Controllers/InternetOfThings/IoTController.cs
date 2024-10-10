using Business.Business.Interfaces.InternetOfThings;
using BusinessModels.System.InternetOfThings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.InternetOfThings;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Route("api/[controller]")]
[ApiController]
public class IoTController(IIoTBusinessLayer ioTBusinessLayer, Logger<IoTController> logger) : ControllerBase
{
    [HttpPost("add-record")]
    public async Task<IActionResult> AddRecord([FromForm] string deviceId, [FromForm] double value)
    {
        var cancelToken = HttpContext.RequestAborted;
        try
        {
            IoTRecord record = new IoTRecord(deviceId, value);
            await ioTBusinessLayer.CreateAsync(record, cancelToken);
            return Ok();
        }
        catch (OperationCanceledException e)
        {
            logger.LogInformation(e.Message);
            return BadRequest();
        }
    }
}