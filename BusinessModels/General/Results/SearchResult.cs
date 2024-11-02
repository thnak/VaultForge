using BusinessModels.Resources;

namespace BusinessModels.General.Results;

public class SearchResult<T>
{
    public IReadOnlyCollection<SearchScore<T>> Value { get; }
    public bool IsSuccess { get; }
    public string Message { get; }
    public ErrorType ErrorType { get; }
    public int TotalCount { get; }

    protected SearchResult(IReadOnlyCollection<SearchScore<T>> value, bool isSuccess, string message, ErrorType errorType)
    {
        Value = value;
        TotalCount = value.Count;
        IsSuccess = isSuccess;
        Message = message;
        ErrorType = errorType;
    }

    public static SearchResult<T> Success(IReadOnlyCollection<SearchScore<T>> value) => new(value, true, AppLang.Success, ErrorType.None);
    public static SearchResult<T> Failure(string message, ErrorType errorType) => new([], false, message, errorType);
}

public class SearchScore<T>(T value, double score)
{
    public T Value { get; set; } = value;
    public double Score { get; set; } = score;
}