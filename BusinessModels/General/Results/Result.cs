using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using BusinessModels.Resources;

namespace BusinessModels.General.Results;

public class Result<T>
{
    public T? Value { get; set; }

    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsSuccess { get; set; }

    public string Message { get; } = string.Empty;

    public ErrorType ErrorType { get; set; }

    public Result()
    {
    }

    private Result(T? value, bool isSuccess, string message, ErrorType errorType)
    {
        Value = value;
        IsSuccess = isSuccess;
        Message = message;
        ErrorType = errorType;
    }

    // Success method with a generic value
    public static Result<T> Success(T value) => new(value, true, AppLang.Success, ErrorType.None);

    // Success method with a message (optional use case)
    public static Result<T> SuccessWithMessage(T value, string message) => new(value, true, message, ErrorType.None);
    public static Result<T> SuccessWithMessage(T value, string message, ErrorType status) => new(value, true, message, status);

    // Failure method with a message and error type
    public static Result<T> Failure(string message, ErrorType errorType) => new(default, false, message, errorType);
    public static Result<T> Canceled(string message) => new(default, false, message, ErrorType.Cancelled);


    public override string ToString()
    {
        return Message;
    }
}