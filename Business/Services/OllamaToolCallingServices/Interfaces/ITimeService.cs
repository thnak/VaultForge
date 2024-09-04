using System.ComponentModel;
using Ollama;

namespace Business.Services.OllamaToolCallingServices.Interfaces;

[OllamaTools]
public interface ITimeService
{
    [Description("get the current timestamp in dd-MM-yyyy HH:mm:ss format")]
    public Task<string> GetCurrentTimeStamp([Description("set true to get current timestamp in UTC when user requested. otherwise set false to get GMT+7")] bool useUtc, CancellationToken cancellationToken = default);

    [Description("get the current hour in HH:mm format")]
    public Task<string> GetCurrentHour([Description("set true to get current hours in UTC when user requested. otherwise set false to get GMT+7")] bool useUtc, CancellationToken cancellationToken = default);
    
    [Description("compares two timestamp in the same format as provided. It returns 1 if the first time is later, -1 if the second time is later, and 0 if they are equal.")]
    public Task<string> CompareTime([Description("The first timestamp")] string firstTime, [Description("The second timestamp")] string secondTime, [Description("time format in c#. For example hh:MM:ss")] string timeFormat = "hh:MM:ss", CancellationToken cancellationToken = default);
    
    [Description("calculates the difference between two timestamp. It returns the difference as a dictionary with keys 'hours', 'minutes', and 'seconds'.")]
    public Task<string> TimeDifference([Description("The first time")] string timeString1, [Description("The second time")] string timeString2, [Description("time format. For example hh:MM:ss")] string timeFormat = "hh:MM:ss", CancellationToken cancellationToken = default);

}