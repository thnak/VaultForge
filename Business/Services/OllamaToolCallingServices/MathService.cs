using System.Globalization;
using System.Text.Json;
using Business.Services.OllamaToolCallingServices.Interfaces;

namespace Business.Services.OllamaToolCallingServices;

public class MathService : IMathService
{
    public Task<string> GetCurrentTime(bool useUtc, CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(useUtc ? DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss") : DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
        }
        catch (Exception e)
        {
            return Task.FromResult(e.Message);
        }
    }

    public Task<string> CompareTime(string firstTime, string secondTime, string timeFormat = "HH:MM:SS", CancellationToken cancellationToken = default)
    {
        try
        {
            if (!DateTime.TryParseExact(firstTime, timeFormat, null, DateTimeStyles.None, out DateTime time1))
            {
                return Task.FromResult($"the {nameof(firstTime)} was wrong format. please try again");
            }

            if (!DateTime.TryParseExact(secondTime, timeFormat, null, DateTimeStyles.None, out DateTime time2))
            {
                return Task.FromResult($"the {nameof(secondTime)} was wrong format. please try again");
            }

            if (time1 == time2)
                return Task.FromResult("0");
            if (time1 > time2)
                return Task.FromResult("1");
            return Task.FromResult("-1");
        }
        catch (Exception e)
        {
            return Task.FromResult(e.Message);
        }
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

    public Task<string> TimeDifference(string timeString1, string timeString2, string timeFormat = "HH:MM:SS", CancellationToken cancellationToken = default)
    {
        try
        {
            if (!DateTime.TryParseExact(timeString1, timeFormat, null, DateTimeStyles.None, out DateTime time1))
            {
                return Task.FromResult($"the {nameof(timeString1)} was wrong format. please try again with {timeFormat}");
            }

            if (!DateTime.TryParseExact(timeString2, timeFormat, null, DateTimeStyles.None, out DateTime time2))
            {
                return Task.FromResult($"the {nameof(timeString2)} was wrong format. please try again with {timeFormat}");
            }

            if (time1 <= time2)
            {
                (time1, time2) = (time2, time1);
            }

            var timeSpan = time1 - time2;
            return Task.FromResult(timeSpan.ToString(@"hh\:mm\:ss"));
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