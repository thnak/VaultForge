using System.Text;
using Business.Data.Interfaces;
using BusinessModels.General.SettingModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;

namespace Business.Data.Repositories;

public class MongoDataLayerContext : IMongoDataLayerContext
{
    public MongoDataLayerContext(IOptions<AppSettings> settings, ILogger<IMongoDataLayerContext> logger)
    {
        var dbName = settings.Value.DbSetting.DatabaseName;
        var user = settings.Value.DbSetting.UserName;
        var pass = settings.Value.DbSetting.Password;
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("MongoDB configuration:");
        stringBuilder.AppendLine($"- connectionString: {settings.Value.DbSetting.ConnectionString}");
        stringBuilder.AppendLine($"- dbName: {dbName}");
        stringBuilder.AppendLine($"- user: {user}");
        stringBuilder.AppendLine($"- pass: {pass}");
        logger.LogInformation(stringBuilder.ToString());

        MongoIdentity identity = new MongoInternalIdentity("admin", user);
        MongoIdentityEvidence evidence = new PasswordEvidence(pass);

        var setup = new MongoClientSettings
        {
            Scheme = ConnectionStringScheme.MongoDB,
            Server = new MongoServerAddress(settings.Value.DbSetting.ConnectionString, settings.Value.DbSetting.Port),
            Credential = new MongoCredential("SCRAM-SHA-1", identity, evidence),
            MaxConnectionPoolSize = settings.Value.DbSetting.MaxConnectionPoolSize,
            WaitQueueTimeout = TimeSpan.FromMinutes(1)
        };

// #if DEBUG
//         setup.Compressors =
//         [
//             new CompressorConfiguration(CompressorType.ZStandard),
//             new CompressorConfiguration(CompressorType.Zlib),
//             new CompressorConfiguration(CompressorType.Snappy),
//             new CompressorConfiguration(CompressorType.Noop)
//         ];
// #else
//         setup.Compressors =
//         [
//             new CompressorConfiguration(CompressorType.Noop)
//         ];
// #endif

        var client = new MongoClient(setup);
        var dbContext = client.GetDatabase(dbName);
        MongoDatabase = dbContext ?? throw new Exception();
    }

    public IMongoDatabase MongoDatabase { get; }
}