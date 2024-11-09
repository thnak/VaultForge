namespace Business.SignalRHub.System.Interfaces;

public interface IClock
{
    Task ShowTime(DateTime currentTime);
}