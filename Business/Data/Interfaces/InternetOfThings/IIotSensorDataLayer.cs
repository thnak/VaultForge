using BusinessModels.System.InternetOfThings;

namespace Business.Data.Interfaces.InternetOfThings;

public interface IIotSensorDataLayer: IMongoDataInitializer, IDataLayerRepository<IoTSensor>
{
    
}