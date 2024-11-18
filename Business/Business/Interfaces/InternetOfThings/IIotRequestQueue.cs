using System.Diagnostics.CodeAnalysis;
using BusinessModels.System.InternetOfThings;

namespace Business.Business.Interfaces.InternetOfThings;

public interface IIotRequestQueue
{
    Task<bool> QueueRequest(IoTRecord data, CancellationToken cancellationToken = default);
    ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default);
    bool TryRead([MaybeNullWhen(false)] out IoTRecord item);
    long GetTotalRequests();
    long GetTotalRequests(string deviceId);
}