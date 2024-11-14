using Business.Services.Configure;
using BusinessModels.General.Results;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace Business.Services.Http.CircuitBreakers;

public class IoTCircuitBreakerService(ApplicationConfiguration options, ILogger<IoTCircuitBreakerService> logger)
{
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker = Policy
        .Handle<Exception>()
        .CircuitBreakerAsync(exceptionsAllowedBeforeBreaking: options.GetIoTCircuitBreaker.ExceptionsAllowedBeforeBreaking, durationOfBreak: TimeSpan.FromSeconds(options.GetIoTCircuitBreaker.DurationOfBreakInSecond));

    // Break after 5 failures
    // Stop for 30 seconds

    public async Task<bool> TryProcessRequest(Func<Task> process)
    {
        if (_circuitBreaker.CircuitState == CircuitState.Open)
        {
            logger.LogWarning("Circuit is open, rejecting request.");
            return false;
        }

        try
        {
            await _circuitBreaker.ExecuteAsync(process);
            return true;
        }
        catch (BrokenCircuitException)
        {
            logger.LogWarning("Circuit is open, rejecting request.");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Request failed: {ex.Message}");
            return false;
        }
    }

    public async Task<Result<T?>> TryProcessRequest<T>(Func<Task<Result<T?>>> process)
    {
        if (_circuitBreaker.CircuitState == CircuitState.Open)
        {
            logger.LogWarning("Circuit is open, rejecting request.");
            return Result<T>.Failure("Circuit is open, rejecting request.", ErrorType.Validation);
        }

        try
        {
            return await _circuitBreaker.ExecuteAsync(process);
        }
        catch (BrokenCircuitException)
        {
            logger.LogWarning("Circuit is open, rejecting request.");
            return Result<T?>.Failure("Circuit is open, rejecting request.", ErrorType.Validation);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Request failed: {ex.Message}");
            return Result<T?>.Failure("Circuit is open, rejecting request.", ErrorType.Unknown);
        }
    }
}