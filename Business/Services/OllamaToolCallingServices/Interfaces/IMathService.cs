using System.ComponentModel;
using Ollama;

namespace Business.Services.OllamaToolCallingServices.Interfaces;

[OllamaTools]
public interface IMathService
{
    [Description("get the current timestamp in dd-MM-yyyy HH:mm:ss format")]
    public Task<string> GetCurrentTimeStamp([Description("set true to get current timestamp in UTC. otherwise get GMT+7")] bool useUtc, CancellationToken cancellationToken = default);
    
    [Description("get the current hour in HH:mm format")]
    public Task<string> GetCurrentHour([Description("set true to get current hour in UTC. otherwise get GMT+7")] bool useUtc, CancellationToken cancellationToken = default);

    [Description("compares two timestamp in the same format as provided. It returns 1 if the first time is later, -1 if the second time is later, and 0 if they are equal.")]
    public Task<string> CompareTime([Description("The first timestamp")] string firstTime, [Description("The second timestamp")] string secondTime, [Description("time format in c#. For example hh:MM:ss")] string timeFormat = "hh:MM:ss", CancellationToken cancellationToken = default);

    [Description("compares two numbers. It returns 1 if the first number is greater, -1 if the second number is greater, and 0 if they are equal.")]
    public Task<string> CompareNumbers([Description("the first number")] string numberA, [Description("the second number")] string numberB, CancellationToken cancellationToken = default);

    [Description("calculates the difference between two timestamp. It returns the difference as a dictionary with keys 'hours', 'minutes', and 'seconds'.")]
    public Task<string> TimeDifference([Description("The first time")] string timeString1, [Description("The second time")] string timeString2, [Description("time format. For example hh:MM:ss")] string timeFormat = "hh:MM:ss", CancellationToken cancellationToken = default);

    [Description("adds two numbers. for example 1 + 1 = 2")]
    public Task<string> AddNumber([Description("the first number")] string numberA, [Description("the second number")] string numberB, CancellationToken cancellationToken = default);

    [Description("subtracts the second number from the first.")]
    public Task<string> Subtract([Description("the first number")] string numberA, [Description("the second number")] string numberB, CancellationToken cancellationToken = default);


    [Description("multiplies two numbers")]
    public Task<string> Multiply([Description("the first number")] string numberA, [Description("the second number")] string numberB, CancellationToken cancellationToken = default);

    [Description("divides the first number by the second. It raises an error if the denominator is zero or inputs are invalid.")]
    public Task<string> Divide([Description("The numerator number")] string numberA, [Description("The denominator number")] string numberB, CancellationToken cancellationToken = default);

    [Description("pow two number")]
    public Task<string> Pow([Description("the first number")] string numberA, [Description("the second number")] string numberB, CancellationToken cancellationToken = default);
    
    [Description("sorts a list of numbers and returns the sorted.")]
    public Task<List<double>> SortListOfNumber([Description("The list of numbers to be sorted, for example [0, 3, 5, 9]")] string listNumbers, CancellationToken cancellationToken = default);
}