using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace BusinessModels.General.Results;

public class ResponseDataResult<T>
{
    public string Message { get; set; } = string.Empty;
    public HttpStatusCode StatusCode { get; set; }

    [MemberNotNullWhen(true, nameof(Data))]
    public bool IsSuccessStatusCode { get; set; }

    public T? Data { get; set; }
}