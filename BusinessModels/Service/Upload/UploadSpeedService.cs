﻿using System.Timers;

namespace BusinessModels.Service.Upload;

public class UploadSpeedService : IDisposable
{
    private readonly Dictionary<string, UploadTracker> _uploadTrackers = new();
    private readonly global::System.Timers.Timer _updateTimer;
    private bool _started;

    public UploadSpeedService()
    {
        // Timer to periodically calculate speeds (e.g., every second)
        _updateTimer = new global::System.Timers.Timer(TimeSpan.FromSeconds(1)); // 1-second interval
        _updateTimer.Elapsed += TimerElapsed;
        _updateTimer.AutoReset = true;
        _updateTimer.Start();
    }

    public void Stop()
    {
        _updateTimer.Stop();
        _started = false;
    }

    public void Start()
    {
        if (_started)
            return;
        _updateTimer.Start();
        _started = true;
    }

    private void TimerElapsed(object? sender, ElapsedEventArgs e)
    {
        UpdateAllTrackers();
        Trackers?.Invoke();
    }

    public Func<Task>? Trackers { get; set; }

    /// <summary>
    /// Adds or updates the uploaded bytes for a file.
    /// </summary>
    /// <param name="fileId">The unique file identifier.</param>
    /// <param name="uploadedBytes">The total bytes uploaded so far.</param>
    public void AddOrUpdate(string fileId, long uploadedBytes)
    {
        if (!_uploadTrackers.TryGetValue(fileId, out var tracker))
        {
            tracker = new UploadTracker();
            _uploadTrackers[fileId] = tracker;
        }

        tracker.UpdateUploadedBytes(uploadedBytes);
    }

    public void CompleteItem(string fileId)
    {
        if (!_uploadTrackers.TryGetValue(fileId, out var tracker))
        {
            tracker = new UploadTracker();
            _uploadTrackers[fileId] = tracker;
        }

        tracker.MarkAsComplete();
    }

    /// <summary>
    /// Gets the current speed string for a file.
    /// </summary>
    /// <param name="fileId">The unique file identifier.</param>
    /// <returns>The upload speed as a formatted string.</returns>
    public string GetSpeedString(string fileId)
    {
        return _uploadTrackers.TryGetValue(fileId, out var tracker) ? tracker.GetSpeedString() : "0 Bytes/s";
    }

    /// <summary>
    /// Completes the upload for a file and removes its tracker.
    /// </summary>
    /// <param name="fileId">The unique file identifier.</param>
    public void CompleteUpload(string fileId)
    {
        _uploadTrackers.Remove(fileId);
    }

    public void UpdateAllTrackers()
    {
        foreach (var tracker in _uploadTrackers.Values)
        {
            tracker.CalculateSpeed();
        }
    }

    public void Dispose()
    {
        _updateTimer.Elapsed -= TimerElapsed;
        _updateTimer.Dispose();
    }

    private class UploadTracker
    {
        private long _lastUploadedBytes = 0;
        private long _currentUploadedBytes = 0;
        private double _currentSpeed = 0; // Speed in bytes per second
        private double _maxSpeed = 0;
        private bool _completed = false;

        public void UpdateUploadedBytes(long uploadedBytes)
        {
            _currentUploadedBytes = uploadedBytes;
        }

        public void MarkAsComplete()
        {
            _completed = true;
            CalculateSpeed();
        }

        public void CalculateSpeed()
        {
            var uploadedSinceLastCheck = _currentUploadedBytes - _lastUploadedBytes;
            _currentSpeed = uploadedSinceLastCheck; // Since timer interval is 1 second
            _lastUploadedBytes = _currentUploadedBytes;
            _maxSpeed = Math.Max(_currentSpeed, _maxSpeed);
        }

        public string GetSpeedString()
        {
            const long KB = 1024;
            const long MB = KB * 1024;

            var speed = _completed ? _maxSpeed : _currentSpeed;

            return speed switch
            {
                >= MB => $"{(speed / MB):F2} MB/s",
                >= KB => $"{(speed / KB):F2} KB/s",
                _ => $"{speed:F2} Bytes/s"
            };
        }
    }
}