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

    public async Task<Result<bool>> TryProcessRequest(Func<CancellationToken, Task> process, CancellationToken cancellationToken = default)
    {
        if (_circuitBreaker.CircuitState == CircuitState.Open)
        {
            logger.LogWarning("Circuit is open, rejecting request.");
            return Result<bool>.Failure("Circuit is open, rejecting request.", ErrorType.PermissionDenied);
        }

        try
        {
            await _circuitBreaker.ExecuteAsync(process, cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (BrokenCircuitException)
        {
            logger.LogWarning("Circuit is open, rejecting request.");
            return Result<bool>.Failure("Circuit is open, rejecting request.", ErrorType.Unknown);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Request failed: {ex.Message}");
            return Result<bool>.Failure($"Request failed: {ex.Message}", ErrorType.Unknown);
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