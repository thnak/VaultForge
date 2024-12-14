using BusinessModels.System.ComputeVision;

namespace Business.Data.Interfaces.ComputeVision;

public interface IYoloLabelDataLayer : IMongoDataInitializer, IDataLayerRepository<YoloLabel>
{
}