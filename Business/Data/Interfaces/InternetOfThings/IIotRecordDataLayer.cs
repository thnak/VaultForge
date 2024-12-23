using BusinessModels.General.Results;
using BusinessModels.System.InternetOfThings;
using BusinessModels.System.InternetOfThings.type;

namespace Business.Data.Interfaces.InternetOfThings;

public interface IIotRecordDataLayer : IMongoDataInitializer, IDataLayerRepository<IoTRecord>
{
    Task<Result<bool>> UpdateIotValue(string key, float value, ProcessStatus processStatus, CancellationToken cancellationToken = default);
}