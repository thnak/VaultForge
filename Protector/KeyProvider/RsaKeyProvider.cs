using System.Security.Cryptography;

namespace Protector.KeyProvider;

public class RsaKeyProvider
{
    /// <summary>
    /// Public key theo singleton
    /// </summary>
    public RSAParameters PublicKey { get; }
    
    /// <summary>
    /// Private key theo singleton
    /// </summary>
    public RSAParameters PrivateKey { get; }

    public RsaKeyProvider()
    {
        using var rsa = RSA.Create();
        PrivateKey = rsa.ExportParameters(includePrivateParameters: true);
        PublicKey = rsa.ExportParameters(includePrivateParameters: false);
    }

    /// <summary>
    /// Tạo mới RSA non-singleton
    /// </summary>
    /// <returns></returns>
    public (RSAParameters, RSAParameters) GetRsa()
    {
        using var rsa = RSA.Create();
        var privateKey = rsa.ExportParameters(includePrivateParameters: true);
        var publicKey = rsa.ExportParameters(includePrivateParameters: false);
        return (privateKey, publicKey);
    }
}