using BusinessModels.Resources;

namespace BusinessModels.General.Results;

public class Result<T>
{
    public T? Value { get; }
    public bool IsSuccess { get; }
    public string Message { get; }
    public ErrorType ErrorType { get; }

    protected Result(T value, bool isSuccess, string message, ErrorType errorType)
    {
        Value = value;
        IsSuccess = isSuccess;
        Message = message;
        ErrorType = errorType;
    }

    public static Result<T> Success(T value) => new(value, true, AppLang.Success, ErrorType.None);
    public static Result<bool> Success(string message) => new(true, true, message, ErrorType.None);
    public static Result<T?> Failure(string message, ErrorType errorType) => new(default, false, message, errorType);
}