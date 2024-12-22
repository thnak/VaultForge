using BusinessModels.General.Results;
using BusinessModels.System.InternetOfThings;

namespace Business.Business.Interfaces.InternetOfThings;

public interface IIotRecordBusinessLayer : IBusinessLayerRepository<IoTRecord>, IExtendService
{
    public Task<Result<bool>> UpdateIotValue(string key, float value, CancellationToken cancellationToken = default);
}