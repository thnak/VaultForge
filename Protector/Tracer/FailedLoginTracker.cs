namespace Protector.Tracer;

public class FailedLoginTracker
{
    private readonly TimeSpan _blockDuration = TimeSpan.FromMinutes(15);
    private readonly Dictionary<string, (int AttemptCount, DateTime? BlockUntil)> _failedAttempts = new();
    private readonly int _maxAttempts = 5;

    public bool IsBlocked(string ipAddress)
    {
        if (_failedAttempts.TryGetValue(ipAddress, out var entry))
        {
            if (entry.BlockUntil.HasValue && DateTime.UtcNow < entry.BlockUntil.Value)
            {
                return true;
            }
            if (entry.BlockUntil.HasValue && DateTime.UtcNow >= entry.BlockUntil.Value)
            {
                _failedAttempts.Remove(ipAddress);
            }
        }
        return false;
    }

    public void RegisterFailedAttempt(string ipAddress)
    {
        if (_failedAttempts.TryGetValue(ipAddress, out var entry))
        {
            entry.AttemptCount++;
            if (entry.AttemptCount >= _maxAttempts)
            {
                entry.BlockUntil = DateTime.UtcNow.Add(_blockDuration);
            }
            _failedAttempts[ipAddress] = entry;
        }
        else
        {
            _failedAttempts[ipAddress] = (1, null);
        }
    }

    public void ResetFailedAttempts(string ipAddress)
    {
        if (_failedAttempts.ContainsKey(ipAddress))
        {
            _failedAttempts.Remove(ipAddress);
        }
    }
}