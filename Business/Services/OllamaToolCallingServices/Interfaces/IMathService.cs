using System.ComponentModel;
using Ollama;

namespace Business.Services.OllamaToolCallingServices.Interfaces;

[OllamaTools]
public interface IMathService
{
    [Description("get the current time in dd-MM-yyyy HH:mm:ss format")]
    public Task<string> CurrentHour([Description("set true to get current time in UTC time. otherwise get GMT+7")] bool useUtc, CancellationToken cancellationToken = default);

    [Description("This function compares two times represented as strings after converting them to datetime objects. It returns 1 if the first time is later, -1 if the second time is later, and 0 if they are equal.")]
    public Task<string> CompareTime([Description("The first time as a string.")] string timeString1, [Description("The second time as a string.")] string timeString2, string timeFormat = "HH:MM:SS", CancellationToken cancellationToken = default);

    [Description("This function compares two numbers represented as strings after converting them to integers or floats. It returns 1 if the first number is greater, -1 if the second number is greater, and 0 if they are equal.")]
    public Task<int> CompareNumbers([Description("the first number as a string")] string numberA, [Description("the second number as a string")] string numberB, CancellationToken cancellationToken = default);

    [Description("This function calculates the difference between two times represented as strings. It returns the difference as a dictionary with keys 'hours', 'minutes', and 'seconds'.")]
    public Task<string> TimeDifference([Description("The first time as a string.")] string timeString1, [Description("The second time as a string.")] string timeString2, string timeFormat = "HH:MM:SS", CancellationToken cancellationToken = default);

    [Description("This function adds two numbers represented as strings.")]
    public Task<double> AddNumber([Description("the first number as a string")] string numberA, [Description("the second number as a string")] string numberB, CancellationToken cancellationToken = default);

    [Description("This function subtracts the second number from the first, both represented as strings.")]
    public Task<double> Subtract([Description("the first number as a string")] string numberA, [Description("the second number as a string")] string numberB, CancellationToken cancellationToken = default);


    [Description("This function multiplies two numbers represented as strings.")]
    public Task<double> Multiply([Description("the first number as a string")] string numberA, [Description("the second number as a string")] string numberB, CancellationToken cancellationToken = default);

    [Description("This function divides the first number by the second, both represented as strings. It raises an error if the denominator is zero or inputs are invalid.")]
    public Task<double> Divide([Description("The numerator as a string.")] string numberA, [Description("The denominator as a string.")] string numberB, CancellationToken cancellationToken = default);

    [Description("This function sorts a list of numbers represented as strings and returns the sorted list as strings. All items in the list must be valid numeric strings.")]
    public Task<List<double>> SortListOfNumber([Description("The list of numbers to be sorted, for example [0, 3, 5, 9]")] string listNumbers, CancellationToken cancellationToken = default);
}