using BusinessModels.System.InternetOfThings;

namespace Business.Business.Interfaces.InternetOfThings;

public interface IIoTBusinessLayer : IBusinessLayerRepository<IoTRecord>, IExtendService;