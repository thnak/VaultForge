using BusinessModels.System.InternetOfThings;

namespace Business.Data.Interfaces.InternetOfThings;

public interface IIotRecordDataLayer : IMongoDataInitializer, IDataLayerRepository<IoTRecord>
{
    
}