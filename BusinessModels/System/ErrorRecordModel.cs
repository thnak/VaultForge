namespace BusinessModels.System;

public class ErrorRecordModel
{
    public ErrorRecordModel()
    {
    }

    public ErrorRecordModel(Exception exception)
    {
        Message = exception.Message;
        RequestId = exception.TargetSite?.Name ?? string.Empty;
        Src = exception.Source ?? string.Empty;
        Href = exception.HelpLink ?? string.Empty;
    }

    public string RequestId { get; set; } = string.Empty;
    public string Src { get; set; } = string.Empty;
    public string Href { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}