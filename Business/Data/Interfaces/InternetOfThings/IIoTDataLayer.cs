using BusinessModels.System.InternetOfThings;

namespace Business.Data.Interfaces.InternetOfThings;

public interface IIoTDataLayer: IMongoDataInitializer, IDataLayerRepository<IoTRecord>
{
    
}