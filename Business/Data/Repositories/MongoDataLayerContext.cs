using System.Text;
using Business.Data.Interfaces;
using Business.Services.Configure;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;

namespace Business.Data.Repositories;

public class MongoDataLayerContext : IMongoDataLayerContext
{
    public MongoDataLayerContext(ApplicationConfiguration settings, ILogger<IMongoDataLayerContext> logger)
    {
        var dbName = settings.GetDbSetting.DatabaseName;
        var user = settings.GetDbSetting.UserName;
        var pass = settings.GetDbSetting.Password;
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("MongoDB configuration:");
        stringBuilder.AppendLine($"- connectionString: {settings.GetDbSetting.ConnectionString}");
        stringBuilder.AppendLine($"- dbName: {dbName}");
        stringBuilder.AppendLine($"- user: {user}");
        stringBuilder.AppendLine($"- pass: {pass}");
        logger.LogInformation(stringBuilder.ToString());

        MongoIdentity identity = new MongoInternalIdentity("admin", user);
        MongoIdentityEvidence evidence = new PasswordEvidence(pass);

        var setup = new MongoClientSettings
        {
            Scheme = ConnectionStringScheme.MongoDB,
            Server = new MongoServerAddress(settings.GetDbSetting.ConnectionString, settings.GetDbSetting.Port),
            Credential = new MongoCredential("SCRAM-SHA-1", identity, evidence),
            MaxConnectionPoolSize = settings.GetDbSetting.MaxConnectionPoolSize,
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

    public void Dispose()
    {
        MongoDatabase.Client.Dispose();
    }
}