using System.Device.Gpio;

namespace WorkerService1;

public class PinInterupWorker(ILogger<PinInterupWorker> logger, TimeProvider timeProvider) : BackgroundService
{
    const int Pin = 21;
    const string Alert = "ALERT 🚨";
    const string Ready = "READY ✅";
    private GpioController? _controller;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _controller = new GpioController();
            _controller.OpenPin(Pin, PinMode.InputPullUp);
            _controller.RegisterCallbackForPinValueChangedEvent(Pin, PinEventTypes.Falling | PinEventTypes.Rising, OnPinEvent);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return;
        }

        logger.LogInformation($"Initial status ({timeProvider.GetLocalNow()}): {(_controller.Read(Pin) == PinValue.High ? Alert : Ready)}");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    void OnPinEvent(object sender, PinValueChangedEventArgs args)
    {
        logger.LogInformation($"({timeProvider.GetLocalNow()}) {(args.ChangeType is PinEventTypes.Rising ? Alert : Ready)}");
    }
}