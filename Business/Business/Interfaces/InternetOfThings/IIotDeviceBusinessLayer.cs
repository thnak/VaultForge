using BusinessModels.General.Results;
using BusinessModels.System.InternetOfThings;
using BusinessModels.System.InternetOfThings.status;

namespace Business.Business.Interfaces.InternetOfThings;

public interface IIotDeviceBusinessLayer : IBusinessLayerRepository<IoTDevice>
{
    public Result<bool> ValidateUser(string deviceId, string password);
    public Task<Result<bool>> UpdateLastServiceTime(string deviceId, IoTDeviceStatus status);
}