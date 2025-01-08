using BusinessModels.System.InternetOfThings;

namespace WebApp.Client.Pages;

public partial class Weather(ILogger<Weather> logger)
{
    private const string DbName = "MyDatabase";
    private const string StoreName = "MyStore";
    private const int Version = 2;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await InitializeDb();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task InitializeDb()
    {
        var result = await IotDeviceIndexedDbService.CreateStoreAsync(DbName, StoreName, Version, UpgradeDatabaseAsync);
        logger.LogInformation(result.Message);
    }

    private async Task UpgradeDatabaseAsync(int oldVersion, int newVersion)
    {
        logger.LogInformation($"Upgrading database from version {oldVersion} to {newVersion}.");

        if (oldVersion < 1)
        {
            logger.LogInformation("Creating initial object store...");
            // No further action needed; handled by openDb JavaScript code
        }

        if (oldVersion < 2)
        {
            logger.LogInformation("Adding indexes...");
            // Custom logic for upgrading schema
        }
    }

    private async Task AddNewItem()
    {
        var m = new IoTDevice() { DeviceId = Guid.NewGuid().ToString() };
        var result = await IotDeviceIndexedDbService.AddItemAsync(DbName, StoreName, m);
        ToastService.ShowInfo(result.Message);
    }
}