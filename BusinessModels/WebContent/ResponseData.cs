using System.Net;

namespace BusinessModels.WebContent;

public class ResponseData<T>
{
    public string Message { get; set; } = string.Empty;
    public HttpStatusCode StatusCode { get; set; } 
    public bool IsSuccessStatusCode { get; set; }
    public T? Data { get; set; }
}