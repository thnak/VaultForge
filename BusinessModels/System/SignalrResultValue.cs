using MessagePack;

namespace BusinessModels.System;

[MessagePackObject]
public class SignalrResultValue<T>
{
    [Key(0)] public bool Success { get; set; }
    [Key(1)] public string Message { get; set; } = string.Empty;
    [Key(2)] public T[] Data { get; set; } = [];
    [Key(3)] public long Total { get; set; }

    public override string ToString()
    {
        return Message;
    }
}

[MessagePackObject]
public class SignalrResult
{
    [Key(0)] public bool Success { get; set; }
    [Key(1)] public string Message { get; set; } = string.Empty;

    public override string ToString()
    {
        return Message;
    }
}