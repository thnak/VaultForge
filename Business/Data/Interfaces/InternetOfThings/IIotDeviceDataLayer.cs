using BusinessModels.System.InternetOfThings;

namespace Business.Data.Interfaces.InternetOfThings;

public interface IIotDeviceDataLayer : IMongoDataInitializer, IDataLayerRepository<IoTDevice>
{
}