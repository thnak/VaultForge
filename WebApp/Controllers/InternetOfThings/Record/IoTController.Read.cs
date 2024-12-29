using System.Diagnostics;
using System.Net.Mime;
using BusinessModels.System;
using BusinessModels.System.InternetOfThings;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.InternetOfThings.Record;

public partial class IoTController
{
    [HttpGet("v1/get-count/{device}")]
    public IActionResult GetCount(string device)
    {
        var total = requestQueueHostedService.GetTotalRequests(device);
        return Ok(total);
    }

    [HttpGet("v1/get-last-record/{device}")]
    public IActionResult GetLastRecord(string device)
    {
        var total = requestQueueHostedService.GetLastRecord(device);
        return Ok(total);
    }

    [HttpPost("get-record")]
    public async Task<IActionResult> SummaryRecord([FromForm] string sensorId, [FromForm] int page, [FromForm] int pageSize, [FromForm] DateTime startTime, [FromForm] DateTime endTime)
    {
        // var startDateTime = startUnixSecond.UnixSecondToDateTime();
        endTime = endTime.AddDays(1);

        var data = businessLayer.Where(x => x.Metadata.RecordedAt >= startTime && x.Metadata.RecordedAt < endTime && x.Metadata.SensorId == sensorId);
        List<IoTRecord> records = new List<IoTRecord>();
        await foreach (var record in data)
        {
            records.Add(record);
        }

        SignalrResultValue<IoTRecord> result = new()
        {
            Data = records.OrderByDescending(x => x.Metadata.RecordedAt).Skip(page * pageSize).Take(pageSize).ToArray(),
            Total = records.Count(),
        };
        var json = result.ToJson();
        return Content(json, MediaTypeNames.Application.Json);
    }

    [HttpPost("compute-record")]
    public async Task<IActionResult> SummaryRecord([FromForm] DateTime startDate, [FromForm] DateTime endDate)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        List<IoTRecord> reorderRecords = new List<IoTRecord>();

        var cancelToken = HttpContext.RequestAborted;

        try
        {
            var cursors = businessLayer.Where(x => x.Metadata.RecordedAt >= startDate && x.Metadata.RecordedAt <= endDate, cancelToken, model => model.Metadata.SensorData);
            await foreach (var record in cursors)
            {
                reorderRecords.Add(record);
            }

            var totalValue = reorderRecords.Sum(x => x.Metadata.SensorData);
            var totalRecords = reorderRecords.Count;
            stopwatch.Stop();
            string result = $"Total records: {totalRecords:N0} with value {totalValue:N0} in {stopwatch.ElapsedMilliseconds:N0} ms.";
            return Content(result, MediaTypeNames.Text.Plain);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(429, string.Empty);
        }
    }
}