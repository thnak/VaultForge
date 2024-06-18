using Business.Utils;
using Microsoft.AspNetCore.Mvc;
using Protector.KeyProvider;

namespace CodeWithMe.Controllers.Security;

[ApiController]
[Route("api/[controller]/[action]")]
public class SecureDataController(RsaKeyProvider rsaKeyProvider) : ControllerBase
{
    [HttpGet]
    public IActionResult GetPublicKey()
    {
        var publicKey = rsaKeyProvider.PublicKey;
        if (publicKey is { Modulus: not null, Exponent: not null })
        {
            return Ok(new
            {
                Modulus = publicKey.Modulus.ToBase64String(),
                Exponent = publicKey.Exponent.ToBase64String()
            });
        }
        return BadRequest();
    }
}