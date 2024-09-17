using System.ComponentModel;
using Ollama;

namespace Business.Services.OllamaToolCallingServices.Interfaces;

[OllamaTools]
public interface IMathService
{
    [Description("respone to user")]
    public Task<string> Response2User([Description("origin prompt")] string message, CancellationToken cancellationToken = default);

    [Description("call to compares two numbers. It returns 1 if the first number is greater, -1 if the second number is greater, and 0 if they are equal.")]
    public Task<string> CompareNumbers([Description("the first number")] string numberA, [Description("the second number")] string numberB, CancellationToken cancellationToken = default);


    [Description("call to adds two numbers. for example 1 + 1 = 2")]
    public Task<string> AddNumber([Description("the first number")] string numberA, [Description("the second number")] string numberB, CancellationToken cancellationToken = default);

    [Description("call o subtracts the second number from the first. for example 1 - 1 = 0")]
    public Task<string> Subtract([Description("the first number")] string numberA, [Description("the second number")] string numberB, CancellationToken cancellationToken = default);


    [Description("call to multiplies two numbers. for example 1 * 5 = 5")]
    public Task<string> Multiply([Description("the first number")] string numberA, [Description("the second number")] string numberB, CancellationToken cancellationToken = default);

    [Description("call to divides the first number by the second. It raises an error if the denominator is zero or inputs are invalid. . for example 10 / 1 = 1")]
    public Task<string> Divide([Description("The numerator number")] string numberA, [Description("The denominator number")] string numberB, CancellationToken cancellationToken = default);

    [Description("call to pow two number. for example 5 ^ 5 = 25")]
    public Task<string> Pow([Description("the first number")] string numberA, [Description("the second number")] string numberB, CancellationToken cancellationToken = default);

    [Description("call to sorts a list of numbers and returns the sorted list.")]
    public Task<List<double>> SortListOfNumber([Description("The list of numbers to be sorted, for example [0, 3, 5, 9]")] string listNumbers, CancellationToken cancellationToken = default);
}