using System.Diagnostics.CodeAnalysis;

namespace BrainNet.Models.Result;

public class InferenceResult<T>
{
    public T? Value { get; set; }

    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsSuccess { get; set; }

    public string Message { get; } = string.Empty;

    public InferenceErrorType InferenceErrorType { get; set; }

    public InferenceResult()
    {
    }

    private InferenceResult(T? value, bool isSuccess, string message, InferenceErrorType inferenceErrorType)
    {
        Value = value;
        IsSuccess = isSuccess;
        Message = message;
        InferenceErrorType = inferenceErrorType;
    }

    // Success method with a generic value
    public static InferenceResult<T> Success(T value) => new(value, true, "Success", InferenceErrorType.None);

    // Success method with a message (optional use case)
    public static InferenceResult<T> SuccessWithMessage(T value, string message) => new(value, true, message, InferenceErrorType.None);
    public static InferenceResult<T> SuccessWithMessage(T value, string message, InferenceErrorType status) => new(value, true, message, status);

    // Failure method with a message and error type
    public static InferenceResult<T> Failure(string message, InferenceErrorType inferenceErrorType) => new(default, false, message, inferenceErrorType);
    public static InferenceResult<T> Canceled(string message) => new(default, false, message, InferenceErrorType.Cancelled);


    public override string ToString()
    {
        return Message;
    }
}

public enum InferenceErrorType
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
    Unknown,

    /// <summary>
    /// Permission denid
    /// </summary>
    PermissionDenied,
}