namespace Business.SignalRHub.System.Interfaces;

public interface IIoTSensorSignal
{
    Task ReceiveMessage(string message);
    Task ReceiveCount(ulong count);
    Task ReceiveValue(double value);
}