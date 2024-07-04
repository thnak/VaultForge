using Business.Data.Interfaces;
using BusinessModels.General;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Compression;
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
            Compressors = new List<CompressorConfiguration>
            {
                new(CompressorType.ZStandard),
                new(CompressorType.Zlib),
                new(CompressorType.Snappy),
                new(CompressorType.Noop)
            },
            Credential = new MongoCredential("SCRAM-SHA-1", identity, evidence)

        };

        var client = new MongoClient(setup);
        var dbContext = client.GetDatabase(dbName);
        MongoDatabase = dbContext ?? throw new Exception();

    }
    public IMongoDatabase MongoDatabase { get; }
}