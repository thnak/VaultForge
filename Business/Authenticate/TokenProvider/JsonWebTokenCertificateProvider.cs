using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Business.Constants.Protector;
using Business.Data.Interfaces;
using Business.Services.Configure;
using BusinessModels.Secure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Business.Authenticate.TokenProvider;

public interface IJsonWebTokenCertificateProvider
{
    string GenerateJwtToken(string username, int expiresHours = ProtectorTime.JsonWebTokenMaxAge);
    string GenerateJwtToken(List<Claim> claims, int expiresHours = ProtectorTime.JsonWebTokenMaxAge);
    ClaimsPrincipal? GetClaimsFromToken(string token);
    TokenModel GenNeverExpireToken(string username, List<Claim> claims);
    TokenModel? GetNeverExpireToken(string id);
    ClaimsPrincipal? GetClaimsFromToken(TokenModel model);
    SecurityKey GetSigningKey();
    TokenValidationParameters GetTokenValidationParameters();
    ClaimsPrincipal? ValidateToken(string token, out SecurityToken? validatedToken);
    string GenerateToken(IEnumerable<Claim> claims, TimeSpan expiration);
    bool IsTokenValid(string token);
}

public class JsonWebTokenCertificateProvider : IJsonWebTokenCertificateProvider
{
    private const string DataProtectorPurpose = "JWTDataProtector";
    private const string SearchIndexString = "TotkenSearchIndex";
    private const string TableName = "Totken";
    private IMongoCollection<TokenModel> DataDb { get; }
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly SecurityKey _signingKey;

    public JsonWebTokenCertificateProvider(IMongoDataLayerContext context, IDataProtectionProvider dataProtectionProvider, ApplicationConfiguration applicationConfiguration)
    {
        _dataProtectionProvider = dataProtectionProvider;
        _applicationConfiguration = applicationConfiguration;
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
        var protector = _dataProtectionProvider.CreateProtector(DataProtectorPurpose);
        var secret = protector.Protect(Encoding.UTF8.GetBytes(applicationConfiguration.GetAuthenticate.Pepper));
        _signingKey = new SymmetricSecurityKey(secret);
    }


    public string GenerateJwtToken(string username, int expiresHours = ProtectorTime.JsonWebTokenMaxAge)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(ClaimTypes.Name, username)
            ]),
            Expires = DateTime.UtcNow.AddHours(expiresHours),
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256)
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
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256)
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
            IssuerSigningKey = _signingKey,
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
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256),
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
            if (token == null) return null;
            return token.ExpireTime > DateTime.UtcNow ? null : token;
        }

        return null;
    }


    public ClaimsPrincipal? GetClaimsFromToken(TokenModel model)
    {
        if (model.ExpireTime < DateTime.UtcNow)
            return GetClaimsFromToken(model.Token);
        return null;
    }


    public SecurityKey GetSigningKey() => _signingKey;

    public TokenValidationParameters GetTokenValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _applicationConfiguration.GetAuthenticate.Issuer,
            ValidAudience = _applicationConfiguration.GetAuthenticate.Audience,
            IssuerSigningKey = _signingKey,
            ClockSkew = TimeSpan.Zero // Adjust for clock drift
        };
    }

    public string GenerateToken(IEnumerable<Claim> claims, TimeSpan expiration)
    {
        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(expiration),
            SigningCredentials = credentials,
            Issuer = _applicationConfiguration.GetAuthenticate.Issuer,
            Audience = _applicationConfiguration.GetAuthenticate.Audience,
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token, out SecurityToken? validatedToken)
    {
        validatedToken = null;
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var principal = tokenHandler.ValidateToken(token, GetTokenValidationParameters(), out var tokenResult);
            validatedToken = tokenResult;
            return principal;
        }
        catch (Exception ex)
        {
            // Log exception if necessary
            Console.WriteLine($"Token validation failed: {ex.Message}");
            return null;
        }
    }

    public bool IsTokenValid(string token)
    {
        return ValidateToken(token, out _) != null;
    }
}