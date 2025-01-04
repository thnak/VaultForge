using BusinessModels.General.Results;
using BusinessModels.System.InternetOfThings;
using BusinessModels.System.InternetOfThings.type;

namespace Business.Business.Interfaces.InternetOfThings;

public interface IIotRecordBusinessLayer : IBusinessLayerRepository<IoTRecord>, IExtendService
{
    public Task<Result<bool>> UpdateIotValue(string key, float value, ProcessStatus processStatus, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateIoTValuesBatch(IEnumerable<IoTRecordUpdateModel> updates, CancellationToken cancellationToken = default);
}