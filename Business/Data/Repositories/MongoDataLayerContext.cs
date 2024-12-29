using Business.Data.Interfaces;
using Business.Services.Configure;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;

namespace Business.Data.Repositories;

public class MongoDataLayerContext : IMongoDataLayerContext
{
    public MongoDataLayerContext(ApplicationConfiguration settings)
    {
        var dbName = settings.GetDbSetting.DatabaseName;
        var user = settings.GetDbSetting.UserName;
        var pass = settings.GetDbSetting.Password;

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

        try
        {
            var client = new MongoClient(setup);
            var dbContext = client.GetDatabase(dbName);
            MongoDatabase = dbContext ?? throw new Exception();
        }

        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public IMongoDatabase MongoDatabase { get; } = null!;

    public void Dispose()
    {
        MongoDatabase.Client.Dispose();
    }
}