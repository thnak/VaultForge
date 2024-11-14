using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Business.Data.Interfaces;
using Business.Services.Configure;
using BusinessModels.Secure;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using Protector;

namespace Business.Authenticate.TokenProvider;

public interface IJsonWebTokenCertificateProvider
{
    string GenerateJwtToken(string username, int expiresHours = ProtectorTime.JsonWebTokenMaxAge);
    string GenerateJwtToken(List<Claim> claims, int expiresHours = ProtectorTime.JsonWebTokenMaxAge);
    ClaimsPrincipal? GetClaimsFromToken(string token);
    TokenModel GenNeverExpireToken(string username, List<Claim> claims);
    TokenModel? GetNeverExpireToken(string id);
    ClaimsPrincipal? GetClaimsFromToken(TokenModel model);
}

public class JsonWebTokenCertificateProvider : IJsonWebTokenCertificateProvider
{
    private const string SearchIndexString = "TotkenSearchIndex";
    private const string TableName = "Totken";

    public JsonWebTokenCertificateProvider(ApplicationConfiguration settings, IMongoDataLayerContext context)
    {
        X509Certificate2 certificate = X509CertificateLoader.LoadPkcs12FromFile(settings.GetAppCertificate.FilePath, settings.GetAppCertificate.Password);
        Key = new X509SecurityKey(certificate);

        DataDb = context.MongoDatabase.GetCollection<TokenModel>(TableName);
        var keys = Builders<TokenModel>.IndexKeys.Ascending(x => x.ExpireTime);
        var indexModel = new CreateIndexModel<TokenModel>(keys);

        var searchIndexKeys = Builders<TokenModel>.IndexKeys.Text(x => x.CreateByUser);
        var searchIndexOptions = new CreateIndexOptions
        {
            Name = SearchIndexString
        };

        var searchIndexModel = new CreateIndexModel<TokenModel>(searchIndexKeys, searchIndexOptions);
        DataDb.Indexes.CreateOneAsync(searchIndexModel);
        DataDb.Indexes.CreateOne(indexModel);
    }

    private X509SecurityKey Key { get; }
    private IMongoCollection<TokenModel> DataDb { get; }

    public string GenerateJwtToken(string username, int expiresHours = ProtectorTime.JsonWebTokenMaxAge)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username)
            }),
            Expires = DateTime.UtcNow.AddHours(expiresHours),
            SigningCredentials = new SigningCredentials(Key, SecurityAlgorithms.RsaSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateJwtToken(List<Claim> claims, int expiresHours = ProtectorTime.JsonWebTokenMaxAge)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(expiresHours),
            SigningCredentials = new SigningCredentials(Key, SecurityAlgorithms.RsaSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// </summary>
    /// <param name="token"></param>
    /// <returns>null if ClaimsPrincipal is null or the jwt is outdated</returns>
    public ClaimsPrincipal? GetClaimsFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = Key,
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true
        };
        try
        {
            var jwtSecurityToken = new JwtSecurityToken(token);

            var claimPri = tokenHandler.ValidateToken(token, validationParameters, out _);

            if (claimPri == null) return default;

            if (jwtSecurityToken.ValidTo == DateTime.MinValue)
            {
                var claimTokenId = claimPri.Claims.FirstOrDefault(x => x.Type == TableName);
                if (claimTokenId == null) return default;

                var tokenModel = GetNeverExpireToken(claimTokenId.Value);
                if (tokenModel == null) return default;
                return tokenModel.ExpireTime > DateTime.UtcNow ? default : claimPri;
            }

            return claimPri;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public TokenModel GenNeverExpireToken(string username, List<Claim> claims)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var model = new TokenModel
        {
            CreateByUser = username
        };

        claims.Add(new Claim(TableName, model.Id.ToString()));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = new SigningCredentials(Key, SecurityAlgorithms.RsaSha256),
            Expires = DateTime.MinValue
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        model.Token = tokenString;


        DataDb.InsertOne(model);
        return model;
    }

    public TokenModel? GetNeverExpireToken(string id)
    {
        if (ObjectId.TryParse(id, out var objectId))
        {
            var filter = Builders<TokenModel>.Filter.Eq(x => x.Id, objectId);
            var token = DataDb.Find(filter).FirstOrDefault();
            if (token == null) return default;
            return token.ExpireTime > DateTime.UtcNow ? default : token;
        }

        return default;
    }


    public ClaimsPrincipal? GetClaimsFromToken(TokenModel model)
    {
        if (model.ExpireTime < DateTime.UtcNow)
            return GetClaimsFromToken(model.Token);
        return default;
    }
}