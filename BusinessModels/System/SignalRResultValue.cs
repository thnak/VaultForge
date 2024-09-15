using MessagePack;

namespace BusinessModels.System;

[MessagePackObject]
public class SignalRResultValue<T>
{
    [Key(0)] public bool Success { get; set; }
    [Key(1)] public string Message { get; set; } = string.Empty;
    [Key(2)] public T[] Data { get; set; } = [];
    [Key(3)] public long Total { get; set; }
}

[MessagePackObject]
public class SignalRResult
{
    [Key(0)] public bool Success { get; set; }
    [Key(1)] public string Message { get; set; } = string.Empty;
}