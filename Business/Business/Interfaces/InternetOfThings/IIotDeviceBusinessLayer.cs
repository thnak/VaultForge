using BusinessModels.General.Results;
using BusinessModels.System.InternetOfThings;

namespace Business.Business.Interfaces.InternetOfThings;

public interface IIotDeviceBusinessLayer : IBusinessLayerRepository<IoTDevice>
{
    public Result<bool> ValidateUser(string deviceId, string password);
    public Task<Result<bool>> UpdateLastServiceTime(string deviceId);
}