using System.Globalization;
using System.Text.Json;
using Business.Services.OllamaToolCallingServices.Interfaces;

namespace Business.Services.OllamaToolCallingServices;

public class MathService : IMathService
{
    public Task<string> CurrentHour(bool useUtc, CancellationToken cancellationToken = default)
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

    public Task<string> CompareTime(string timeString1, string timeString2, string timeFormat = "HH:MM:SS", CancellationToken cancellationToken = default)
    {
        try
        {
            if (!DateTime.TryParseExact(timeString1, timeFormat, null, DateTimeStyles.None, out DateTime time1))
            {
                return Task.FromResult($"something was wrong with {nameof(timeString1)}");
            }

            if (!DateTime.TryParseExact(timeString2, timeFormat, null, DateTimeStyles.None, out DateTime time2))
            {
                return Task.FromResult($"something was wrong with {nameof(timeString2)}");
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

    public Task<int> CompareNumbers(string numberA, string numberB, CancellationToken cancellationToken = default)
    {
        try
        {
            double.TryParse(numberA, NumberFormatInfo.InvariantInfo, out double value1);
            double.TryParse(numberB, NumberFormatInfo.InvariantInfo, out double value2);
            if (value1 > value2)
                return Task.FromResult(1);
            if (value1 < value2)
                return Task.FromResult(-1);
            return Task.FromResult(0);
        }
        catch (Exception)
        {
            return Task.FromResult(0);
        }
    }

    public Task<string> TimeDifference(string timeString1, string timeString2, string timeFormat = "HH:MM:SS", CancellationToken cancellationToken = default)
    {
        try
        {
            if (!DateTime.TryParseExact(timeString1, timeFormat, null, DateTimeStyles.None, out DateTime time1))
            {
                return Task.FromResult($"something was wrong with {nameof(timeString1)}");
            }

            if (!DateTime.TryParseExact(timeString2, timeFormat, null, DateTimeStyles.None, out DateTime time2))
            {
                return Task.FromResult($"something was wrong with {nameof(timeString2)}");
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

    public Task<double> AddNumber(string numberA, string numberB, CancellationToken cancellationToken = default)
    {
        try
        {
            double.TryParse(numberA, NumberFormatInfo.InvariantInfo, out double value1);
            double.TryParse(numberB, NumberFormatInfo.InvariantInfo, out double value2);
            return Task.FromResult(value1 + value2);
        }
        catch (Exception)
        {
            return Task.FromResult(0d);
        }
    }

    public Task<double> Subtract(string numberA, string numberB, CancellationToken cancellationToken = default)
    {
        try
        {
            double.TryParse(numberA, NumberFormatInfo.InvariantInfo, out double value1);
            double.TryParse(numberB, NumberFormatInfo.InvariantInfo, out double value2);
            return Task.FromResult(value1 - value2);
        }
        catch (Exception)
        {
            return Task.FromResult(0d);
        }
    }

    public Task<double> Multiply(string numberA, string numberB, CancellationToken cancellationToken = default)
    {
        try
        {
            double.TryParse(numberA, NumberFormatInfo.InvariantInfo, out double value1);
            double.TryParse(numberB, NumberFormatInfo.InvariantInfo, out double value2);
            return Task.FromResult(value1 * value2);
        }
        catch (Exception)
        {
            return Task.FromResult(0d);
        }
    }

    public Task<double> Divide(string numberA, string numberB, CancellationToken cancellationToken = default)
    {
        try
        {
            double.TryParse(numberA, NumberFormatInfo.InvariantInfo, out double value1);
            double.TryParse(numberB, NumberFormatInfo.InvariantInfo, out double value2);
            return Task.FromResult(value1 / value2);
        }
        catch (Exception)
        {
            return Task.FromResult(0d);
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