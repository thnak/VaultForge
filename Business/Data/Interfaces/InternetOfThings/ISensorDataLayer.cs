using BusinessModels.System.InternetOfThings;

namespace Business.Data.Interfaces.InternetOfThings;

public interface ISensorDataLayer: IMongoDataInitializer, IDataLayerRepository<IoTSensor>
{
    
}