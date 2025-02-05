﻿using System.Collections.Concurrent;
using BrainNet.Service.ObjectDetection.Implements;
using BrainNet.Service.ObjectDetection.Interfaces;

namespace Business.Services.TaskQueueServices;

public interface IYoloSessionManager
{
    void CleanupExpiredSessions();
    
    bool TryGetService(Guid userId, out IYoloInferenceSessionService? sessionService);
    Guid RegisterService(Stream modelStream);
    Task RunOneAsync(CancellationToken cancellationToken = default);
}

public class YoloSessionManager(TimeProvider timeProvider) : IYoloSessionManager
{
    private readonly ConcurrentDictionary<Guid, IYoloInferenceSessionService?> _sessions = new();


    public bool TryGetService(Guid userId, out IYoloInferenceSessionService? sessionService)
    {
        return _sessions.TryGetValue(userId, out sessionService);
    }

    public Guid RegisterService(Stream modelStream)
    {
        Guid userId = Guid.NewGuid();
        var session = _sessions.GetOrAdd(userId, _ => new YoloInferenceSessionService(timeProvider));
        session!.Initialize(modelStream);
        return userId;
    }

    public async Task RunOneAsync(CancellationToken cancellationToken = default)
    {
        List<Task> tasks = new();
        foreach (var (_, session) in _sessions)
        {
            if (session != null && !session.IsExpired())
            {
                tasks.Add(session.RunOneAsync(cancellationToken));
            }
        }
        await Task.WhenAll(tasks);
    }

    public void CleanupExpiredSessions()
    {
        foreach (var (userId, session) in _sessions)
        {
            if (session != null && session.IsExpired())
            {
                if (_sessions.TryRemove(userId, out var removedSession))
                {
                    removedSession!.Dispose();
                }
            }
        }
    }
}