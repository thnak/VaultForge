namespace BusinessModels.System;

public class ErrorRecordModel
{
    public string RequestId { get; set; } = string.Empty;
    public string Src { get; set; } = string.Empty;
    public string Href { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}