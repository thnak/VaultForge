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

    public static Result<T> Success(T value) => new Result<T>(value, true, AppLang.Success, ErrorType.None);
    public static Result<bool> Success(string message) => new Result<bool>(true, true, message, ErrorType.None);
    public static Result<T?> Failure(string message, ErrorType errorType) => new Result<T?>(default(T), false, message, errorType);
}

public enum ErrorType
{
    /// <summary>
    /// No error
    /// </summary>
    None,

    /// <summary>
    /// Record not found
    /// </summary>
    NotFound,

    /// <summary>
    /// Duplicate record
    /// </summary>
    Duplicate,

    /// <summary>
    /// Operation cancelled
    /// </summary>
    Cancelled,

    /// <summary>
    /// Validation error
    /// </summary>
    Validation,

    /// <summary>
    /// General database error
    /// </summary>
    Database,

    /// <summary>
    /// Unhandled error
    /// </summary>
    Unknown
}