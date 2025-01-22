namespace WorkerService1;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddHostedService<PinInterupWorker>();
        builder.Services.AddHostedService<IotDeviceWorker>();

        var host = builder.Build();
        host.Run();
    }
}