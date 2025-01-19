using System.Diagnostics;
using System.Net.Mime;
using Business.Utils.Excel;
using BusinessModels.Resources;
using BusinessModels.System;
using BusinessModels.System.InternetOfThings;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Mvc;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

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
        endTime = endTime.AddDays(1);

        var data = businessLayer.Where(x => x.Metadata.RecordedAt >= startTime && x.Metadata.RecordedAt < endTime && x.Metadata.SensorId == sensorId);
        List<IoTRecord> records = new();
        await foreach (var record in data) records.Add(record);

        SignalrResultValue<IoTRecord> result = new()
        {
            Data = records.OrderByDescending(x => x.Metadata.RecordedAt).Skip(page * pageSize).Take(pageSize).ToArray(),
            Total = records.Count()
        };
        var json = result.ToJson();
        return Content(json, MediaTypeNames.Application.Json);
    }

    [HttpGet("get-excel-record")]
    public async Task<IActionResult> SummaryExcelRecord(string sensorId, int page, int pageSize, DateTime startTime, DateTime endTime)
    {
        var cancelToken = HttpContext.RequestAborted;

        endTime = endTime.AddDays(1);
        var workbook = new XSSFWorkbook();
        Response.RegisterForDispose(workbook);

        var myFont = (XSSFFont)workbook.CreateFont();
        myFont.FontHeightInPoints = 11;
        myFont.FontName = "Tahoma";
        // Defining a border

        var borderedCellStyle = (XSSFCellStyle)workbook.CreateCellStyle();
        borderedCellStyle.SetFont(myFont);
        borderedCellStyle.BorderLeft = BorderStyle.Medium;
        borderedCellStyle.BorderTop = BorderStyle.Medium;
        borderedCellStyle.BorderRight = BorderStyle.Medium;
        borderedCellStyle.BorderBottom = BorderStyle.Medium;
        borderedCellStyle.VerticalAlignment = VerticalAlignment.Center;

        var dateStyle = (XSSFCellStyle)workbook.CreateCellStyle();
        var dataFormat = workbook.CreateDataFormat();
        dateStyle.SetFont(myFont);
        dateStyle.BorderLeft = BorderStyle.Medium;
        dateStyle.BorderTop = BorderStyle.Medium;
        dateStyle.BorderRight = BorderStyle.Medium;
        dateStyle.BorderBottom = BorderStyle.Medium;
        dateStyle.VerticalAlignment = VerticalAlignment.Center;
        dateStyle.DataFormat = dataFormat.GetFormat("yyyy-MM-dd HH:mm:ss");

        var sheet = workbook.CreateSheet("Report");
        //Creat The Headers of the excel
        var rowIndex = 0;
        var headerRow = sheet.CreateRow(rowIndex++);

        //Create The Actual Cells

        headerRow.CreateCellWithValue(0, AppLang.Timestamp, borderedCellStyle);
        headerRow.CreateCellWithValue(1, AppLang.Value, borderedCellStyle);
        headerRow.CreateCellWithValue(2, AppLang.Signal_strength, borderedCellStyle);
        headerRow.CreateCellWithValue(3, AppLang.Image, borderedCellStyle);

        // This Where the Data row starts from
        var data = businessLayer.Where(x => x.Metadata.RecordedAt >= startTime && x.Metadata.RecordedAt < endTime && x.Metadata.SensorId == sensorId, cancelToken);
        List<IoTRecord> records = new();
        await foreach (var record in data) records.Add(record);

        var memoryStream = new MemoryStream();
        Response.RegisterForDisposeAsync(memoryStream);

        foreach (var batchErrorReport in records)
        {
            //Creating the CurrentDataRow
            var currentRow = sheet.CreateRow(rowIndex);
            currentRow.CreateCellWithValue(0, batchErrorReport.Metadata.RecordedAt, dateStyle);
            currentRow.CreateCellWithValue(1, batchErrorReport.Metadata.SensorData, borderedCellStyle);
            currentRow.CreateCellWithValue(2, batchErrorReport.Metadata.SignalStrength, borderedCellStyle);
            currentRow.CreateCellWithValue(3, batchErrorReport.Metadata.ImagePath, borderedCellStyle);
            var file = fileSystemServe.Get(batchErrorReport.Metadata.ImagePath);
            if (file == null)
                continue;
            try
            {
                memoryStream.SetLength(0);
                await raidService.ReadGetDataAsync(memoryStream, file.AbsolutePath, cancelToken);
                var imageIndex = workbook.AddPicture(memoryStream.ToArray(), PictureType.JPEG);
                var helper = workbook.GetCreationHelper();
                var drawing = sheet.CreateDrawingPatriarch();
                var anchor = helper.CreateClientAnchor();
                anchor.Col1 = 4; //0 index based column
                anchor.Col2 = 4;
                anchor.Row1 = rowIndex; //0 index based row
                anchor.Row2 = rowIndex;
                var picture = drawing.CreatePicture(anchor, imageIndex);
                picture.Resize();
                picture.Resize(0.1);
            }
            catch (Exception)
            {
                //
            }

            rowIndex++;
        }

        // Auto sized all the affected columns
        var lastColumNum = sheet.GetRow(0).LastCellNum + 1;
        for (var i = 0; i <= lastColumNum; i++) sheet.AutoSizeColumn(i);

        var ms = new MemoryStream();
        Response.RegisterForDisposeAsync(ms);

        workbook.Write(ms, true);
        ms.Seek(0, SeekOrigin.Begin);
        var now = DateTime.UtcNow;
        var cd = new ContentDisposition
        {
            FileName = $"Report {startTime.ToLocalTime():dd/MM/yy} {endTime.ToLocalTime():dd/MM/yy}.xlsx",
            Inline = false, // false = prompt the user for downloading;  true = browser to try to show the file inline,
            CreationDate = now,
            ModificationDate = now,
            ReadDate = now
        };

        Response.Headers.Append("Content-Disposition", cd.ToString());
        Response.StatusCode = 200;
        Response.ContentLength = ms.Length;

        return new FileStreamResult(ms, "application/vnd.ms-excel")
        {
            FileDownloadName = cd.FileName,
            LastModified = DateTimeOffset.Now,
            EnableRangeProcessing = true
        };
    }

    [HttpPost("compute-record")]
    public async Task<IActionResult> SummaryRecord([FromForm] DateTime startDate, [FromForm] DateTime endDate)
    {
        var stopwatch = Stopwatch.StartNew();
        List<IoTRecord> reorderRecords = new();

        var cancelToken = HttpContext.RequestAborted;

        try
        {
            var cursors = businessLayer.Where(x => x.Metadata.RecordedAt >= startDate && x.Metadata.RecordedAt <= endDate, cancelToken, model => model.Metadata.SensorData);
            await foreach (var record in cursors) reorderRecords.Add(record);

            var totalValue = reorderRecords.Sum(x => x.Metadata.SensorData);
            var totalRecords = reorderRecords.Count;
            stopwatch.Stop();
            var result = $"Total records: {totalRecords:N0} with value {totalValue:N0} in {stopwatch.ElapsedMilliseconds:N0} ms.";
            return Content(result, MediaTypeNames.Text.Plain);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(429, string.Empty);
        }
    }
}