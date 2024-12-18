using BusinessModels.System.InternetOfThings;

namespace Business.Data.Interfaces.InternetOfThings;

public interface IDeviceDataLayer : IMongoDataInitializer, IDataLayerRepository<IoTDevice>
{
}