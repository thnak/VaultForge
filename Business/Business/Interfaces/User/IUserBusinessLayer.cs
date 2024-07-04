using System.Security.Claims;
using BusinessModels.People;
using BusinessModels.Secure;

namespace Business.Business.Interfaces.User;

public interface IUserBusinessLayer : IBusinessLayerRepository<UserModel>
{
    (bool, string) Authenticate(RequestLoginModel model);
    (bool, string) ValidateUsername(string username);
    (bool, string) ValidatePassword(string username, string password);

    /// <summary>
    ///     Tạo đối tượng xác thực cho người dùng
    /// </summary>
    /// <param name="userName">Chuỗi string chưa trải qua hàm băm</param>
    /// <returns></returns>
    ClaimsIdentity CreateIdentity(string userName);

    /// <summary>
    ///     Lấy tất cả quyền thuộc về User
    /// </summary>
    /// <param name="userName">Chuỗi string chưa trải qua hàm băm</param>
    /// <returns></returns>
    List<Claim> GetAllClaim(string userName);
    /// <summary>
    ///     Lấy tất cả quyền thuộc về User
    /// </summary>
    /// <param name="user"></param>
    /// <param name="userName">Chuỗi string chưa trải qua hàm băm</param>
    /// <returns></returns>
    List<Claim> GetAllClaim(UserModel user, string userName);
}