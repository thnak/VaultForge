using System.Globalization;
using System.Text.Json;
using Business.Services.OllamaToolCallingServices.Interfaces;

namespace Business.Services.OllamaToolCallingServices;

public class MathService : IMathService
{
    public Task<string> Response2User(string message, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("");
    }

    public Task<string> CompareNumbers(string numberA, string numberB, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!double.TryParse(numberA, NumberFormatInfo.InvariantInfo, out double value1))
                return Task.FromResult($"{nameof(numberA)} is not a number. please try again");
            if (!double.TryParse(numberB, NumberFormatInfo.InvariantInfo, out double value2))
                return Task.FromResult<string>($"{nameof(numberB)} is not a number. please try again");
            if (value1 > value2)
                return Task.FromResult("1");
            if (value1 < value2)
                return Task.FromResult("-1");
            return Task.FromResult("0");
        }
        catch (Exception e)
        {
            return Task.FromResult(e.Message);
        }
    }


    public Task<string> AddNumber(string numberA, string numberB, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!double.TryParse(numberA, NumberFormatInfo.InvariantInfo, out double value1))
                return Task.FromResult($"{nameof(numberA)} is not a number. please try again");
            if (!double.TryParse(numberB, NumberFormatInfo.InvariantInfo, out double value2))
                return Task.FromResult<string>($"{nameof(numberB)} is not a number. please try again");

            return Task.FromResult($"{value1 + value2}");
        }
        catch (Exception e)
        {
            return Task.FromResult(e.Message);
        }
    }

    public Task<string> Subtract(string numberA, string numberB, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!double.TryParse(numberA, NumberFormatInfo.InvariantInfo, out double value1))
                return Task.FromResult($"{nameof(numberA)} is not a number. please try again");
            if (!double.TryParse(numberB, NumberFormatInfo.InvariantInfo, out double value2))
                return Task.FromResult<string>($"{nameof(numberB)} is not a number. please try again");

            return Task.FromResult($"{value1 - value2}");
        }
        catch (Exception e)
        {
            return Task.FromResult(e.Message);
        }
    }

    public Task<string> Multiply(string numberA, string numberB, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!double.TryParse(numberA, NumberFormatInfo.InvariantInfo, out double value1))
                return Task.FromResult($"{nameof(numberA)} is not a number. please try again");
            if (!double.TryParse(numberB, NumberFormatInfo.InvariantInfo, out double value2))
                return Task.FromResult<string>($"{nameof(numberB)} is not a number. please try again");

            return Task.FromResult($"{value1 * value2}");
        }
        catch (Exception e)
        {
            return Task.FromResult(e.Message);
        }
    }

    public Task<string> Divide(string numberA, string numberB, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!double.TryParse(numberA, NumberFormatInfo.InvariantInfo, out double value1))
                return Task.FromResult($"{nameof(numberA)} is not a number. please try again");
            if (!double.TryParse(numberB, NumberFormatInfo.InvariantInfo, out double value2))
                return Task.FromResult<string>($"{nameof(numberB)} is not a number. please try again");

            return Task.FromResult($"{value1 / value2}");
        }
        catch (Exception e)
        {
            return Task.FromResult(e.Message);
        }
    }

    public Task<string> Pow(string numberA, string numberB, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!double.TryParse(numberA, NumberFormatInfo.InvariantInfo, out double value1))
                return Task.FromResult($"{nameof(numberA)} is not a number. please try again");
            if (!double.TryParse(numberB, NumberFormatInfo.InvariantInfo, out double value2))
                return Task.FromResult<string>($"{nameof(numberB)} is not a number. please try again");

            return Task.FromResult($"{Math.Pow(value1, value2)}");
        }
        catch (Exception e)
        {
            return Task.FromResult(e.Message);
        }
    }

    public Task<List<double>> SortListOfNumber(string listNumbers, CancellationToken cancellationToken = default)
    {
        try
        {
            var textPlan = JsonSerializer.Deserialize<List<double>>(listNumbers) ?? [];
            textPlan.Sort();
            return Task.FromResult(textPlan);
        }
        catch (Exception)
        {
            return Task.FromResult<List<double>>([]);
        }
    }
}