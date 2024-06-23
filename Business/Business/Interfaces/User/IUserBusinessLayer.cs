using System.Security.Claims;
using BusinessModels.People;
using BusinessModels.Secure;

namespace Business.Business.Interfaces.User;

public interface IUserBusinessLayer : IBusinessLayerRepository<UserModel>
{
    (bool, string) Authenticate(RequestLoginModel model);
    ClaimsIdentity CreateIdentity(string userName);
    
    List<Claim> GetAllClaim(string userName);
    List<Claim> GetAllClaim(UserModel userModel);
}