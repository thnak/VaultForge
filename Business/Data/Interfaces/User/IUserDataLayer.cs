using BusinessModels.People;

namespace Business.Data.Interfaces.User;

public interface IUserDataLayer : IMongoDataInitializer, IDataLayerRepository<UserModel>
{
    List<string> GetAllRoles(string userName);
}