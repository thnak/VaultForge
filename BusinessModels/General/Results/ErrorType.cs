namespace BusinessModels.General.Results;

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
    Unknown,
    /// <summary>
    /// Permission denid
    /// </summary>
    PermissionDenied,
}