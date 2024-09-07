using MessagePack;

namespace BusinessModels.System;

[MessagePackObject]
public class SignalRResult<T>
{
    [Key(1)] public bool Success { get; set; }
    [Key(2)] public string Message { get; set; } = string.Empty;
    [Key(3)] public T[] Data { get; set; } = [];
    [Key(4)] public long Total { get; set; }
}

[MessagePackObject]
public class SignalRResult
{
    [Key(1)] public bool Success { get; set; }
    [Key(2)] public string Message { get; set; } = string.Empty;
}