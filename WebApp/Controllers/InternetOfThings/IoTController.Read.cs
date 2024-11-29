﻿using System.Diagnostics;
using System.Net.Mime;
using BusinessModels.System.InternetOfThings;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.InternetOfThings;

public partial class IoTController
{
    [HttpPost("compute-record")]
    public async Task<IActionResult> SummaryRecord([FromForm] DateTime startDate, [FromForm] DateTime endDate)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        List<IoTRecord> reorderRecords = new List<IoTRecord>();

        var cancelToken = HttpContext.RequestAborted;

        try
        {
            var cursors = businessLayer.Where(x => x.Timestamp >= startDate && x.Timestamp <= endDate, cancelToken, model => model.SensorData);
            await foreach (var record in cursors)
            {
                reorderRecords.Add(record);
            }

            var totalValue = reorderRecords.Sum(x => x.SensorData);
            var totalRecords = reorderRecords.Count;
            stopwatch.Stop();
            string result = $"Total Records: {totalRecords:N0} with value {totalValue:N0} in {stopwatch.ElapsedMilliseconds:N0} ms.";
            return Content(result, MediaTypeNames.Text.Plain);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(429, string.Empty);
        }
    }
}