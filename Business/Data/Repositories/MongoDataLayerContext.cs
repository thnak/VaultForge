using Business.Data.Interfaces;
using BusinessModels.General;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;

namespace Business.Data.Repositories;

public class MongoDataLayerContext : IMongoDataLayerContext
{
    public MongoDataLayerContext(IOptions<DbSettingModel> settings)
    {
        var dbName = settings.Value.DatabaseName;
        var user = settings.Value.UserName;
        var pass = settings.Value.Password;

        MongoIdentity identity = new MongoInternalIdentity("admin", user);
        MongoIdentityEvidence evidence = new PasswordEvidence(pass);


        var setup = new MongoClientSettings
        {
            Scheme = ConnectionStringScheme.MongoDB,
            Server = new MongoServerAddress(settings.Value.ConnectionString, settings.Value.Port),
            Credential = new MongoCredential("SCRAM-SHA-1", identity, evidence)
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