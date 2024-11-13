namespace WebApp.Client.Models;

/// <summary>
///     Contains the result of a protected browser storage operation.
/// </summary>
public class ProtectedBrowserStorageResult<TValue>(bool success, TValue? value)
{
    /// <summary>
    ///     Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; } = success;

    /// <summary>
    ///     Gets the result value of the operation.
    /// </summary>
    public TValue? Value { get; } = value;
}