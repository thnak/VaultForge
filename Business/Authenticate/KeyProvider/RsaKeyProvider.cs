using System.Security.Cryptography;

namespace Business.Authenticate.KeyProvider;

/// <summary>
///     Cung cấp khóa công khai
/// </summary>
public class RsaKeyProvider
{
    public RsaKeyProvider()
    {
        using var rsa = RSA.Create();
        PrivateKey = rsa.ExportParameters(true);
        PublicKey = rsa.ExportParameters(false);
    }

    /// <summary>
    ///     Public key theo singleton
    /// </summary>
    public RSAParameters PublicKey { get; }

    /// <summary>
    ///     Private key theo singleton
    /// </summary>
    public RSAParameters PrivateKey { get; }

    /// <summary>
    ///     Tạo mới RSA non-singleton
    /// </summary>
    /// <returns></returns>
    public (RSAParameters, RSAParameters) GetRsa()
    {
        using var rsa = RSA.Create();
        var privateKey = rsa.ExportParameters(true);
        var publicKey = rsa.ExportParameters(false);
        return (privateKey, publicKey);
    }
}