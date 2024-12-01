using System.Collections.Concurrent;

namespace Business.SignalRHub.ClientManager.Implements;

public class SignalrClientManager
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> _concurrentDictionary = new();

    private void AddNewConnection(string group, string connectionId, bool state = true)
    {
        _concurrentDictionary.AddOrUpdate(group, new ConcurrentDictionary<string, bool>(), (_, value) =>
        {
            value.AddOrUpdate(connectionId, true, (_, _) => state);
            return value;
        });
    }

    public void RemoveConnection(string sensorId, string connectionId)
    {
        if (_concurrentDictionary.TryGetValue(sensorId, out var connections))
        {
            connections.TryRemove(connectionId, out _);

            // Remove the sensorId entry if no connections are left
            if (connections.IsEmpty)
            {
                _concurrentDictionary.TryRemove(sensorId, out _);
            }
        }
    }

    public List<string> GetConnections(string sensorId)
    {
        _concurrentDictionary.TryGetValue(sensorId, out var connections);
        return connections?.Keys.ToList() ?? [];
    }
}