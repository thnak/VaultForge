﻿using System.Diagnostics;
using System.Globalization;
using System.Net.Mime;
using System.Web;
using Business.Utils.Excel;
using BusinessModels.Resources;
using BusinessModels.System;
using BusinessModels.System.InternetOfThings;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Mvc;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

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

    [HttpGet("get-excel-record")]
    public async Task<IActionResult> SummaryExcelRecord(string sensorId, int page, int pageSize, DateTime startTime, DateTime endTime)
    {
        endTime = endTime.AddDays(1);
        HSSFWorkbook workbook = new HSSFWorkbook();
        HSSFFont myFont = (HSSFFont)workbook.CreateFont();
        myFont.FontHeightInPoints = 11;
        myFont.FontName = "Tahoma";
        // Defining a border
        HSSFCellStyle borderedCellStyle = (HSSFCellStyle)workbook.CreateCellStyle();
        borderedCellStyle.SetFont(myFont);
        borderedCellStyle.BorderLeft = BorderStyle.Medium;
        borderedCellStyle.BorderTop = BorderStyle.Medium;
        borderedCellStyle.BorderRight = BorderStyle.Medium;
        borderedCellStyle.BorderBottom = BorderStyle.Medium;
        borderedCellStyle.VerticalAlignment = VerticalAlignment.Center;
        
        HSSFCellStyle dateStyle = (HSSFCellStyle)workbook.CreateCellStyle();
        IDataFormat dataFormat = workbook.CreateDataFormat();
        dateStyle.SetFont(myFont);
        dateStyle.BorderLeft = BorderStyle.Medium;
        dateStyle.BorderTop = BorderStyle.Medium;
        dateStyle.BorderRight = BorderStyle.Medium;
        dateStyle.BorderBottom = BorderStyle.Medium;
        dateStyle.VerticalAlignment = VerticalAlignment.Center;
        dateStyle.DataFormat = dataFormat.GetFormat("yyyy-MM-dd HH:mm:ss"); 

        ISheet sheet = workbook.CreateSheet("Report");
        //Creat The Headers of the excel
        int rowIndex = 0;
        IRow headerRow = sheet.CreateRow(rowIndex++);

        //Create The Actual Cells

        headerRow.CreateCellWithValue(0, AppLang.Timestamp, borderedCellStyle);
        headerRow.CreateCellWithValue(1, AppLang.Value, borderedCellStyle);
        headerRow.CreateCellWithValue(2, AppLang.Signal_strength, borderedCellStyle);
        headerRow.CreateCellWithValue(3, AppLang.Image, borderedCellStyle);

        // This Where the Data row starts from
        var data = businessLayer.Where(x => x.Metadata.RecordedAt >= startTime && x.Metadata.RecordedAt < endTime && x.Metadata.SensorId == sensorId);
        List<IoTRecord> records = new List<IoTRecord>();
        await foreach (var record in data)
        {
            records.Add(record);
        }

        foreach (var batchErrorReport in records)
        {
            //Creating the CurrentDataRow
            IRow currentRow = sheet.CreateRow(rowIndex++);
            currentRow.CreateCellWithValue(0, batchErrorReport.Metadata.RecordedAt, dateStyle);
            currentRow.CreateCellWithValue(1, batchErrorReport.Metadata.SensorData, borderedCellStyle);
            currentRow.CreateCellWithValue(2, batchErrorReport.Metadata.SignalStrength, borderedCellStyle);
            currentRow.CreateCellWithValue(3, batchErrorReport.Metadata.ImagePath, borderedCellStyle);
        }

        // Auto sized all the affected columns
        int lastColumNum = sheet.GetRow(0).LastCellNum;
        for (int i = 0; i <= lastColumNum; i++)
        {
            sheet.AutoSizeColumn(i);
        }

        // Write Excel to disk 
        MemoryStream ms = new MemoryStream();
        workbook.Write(ms);
        ms.Seek(0, SeekOrigin.Begin);
        Response.RegisterForDisposeAsync(ms);
        Response.RegisterForDispose(workbook);
        var now = DateTime.UtcNow;
        var cd = new ContentDisposition
        {
            FileName = HttpUtility.HtmlEncode($"Report {startTime.ToLocalTime().ToString(CultureInfo.CurrentCulture)}-{endTime.ToLocalTime().ToString(CultureInfo.CurrentCulture)}.xls"),
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