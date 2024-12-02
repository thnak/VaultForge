using System.Diagnostics.CodeAnalysis;
using BusinessModels.System.InternetOfThings;

namespace Business.Business.Interfaces.InternetOfThings;

public interface IIotRequestQueue
{
    Task<bool> QueueRequest(IoTRecord data, CancellationToken cancellationToken = default);
    ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default);
    void Reset();
    bool TryRead([MaybeNullWhen(false)] out IoTRecord item);
    long GetTotalRequests();
    ulong GetTotalRequests(string deviceId);
    float GetLastRecord(string deviceId);
    void SetTotalRequests(string deviceId, ulong totalRequests);
    void IncrementTotalRequests(string deviceId);
}