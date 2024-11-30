using Business.Data.Interfaces;
using Business.Services.Configure;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;

namespace Business.Data.Repositories;

public class MongoDataLayerContext : IMongoDataLayerContext
{
    private readonly ILogger<MongoDataLayerContext> _logger;
    public MongoDataLayerContext(ApplicationConfiguration settings, ILogger<MongoDataLayerContext> logger)
    {
        _logger = logger;
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
        catch (OperationCanceledException ex)
        {
            logger.LogInformation(ex.ToString());
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
        }
    }

    public IMongoDatabase MongoDatabase { get; } = null!;

    public void Dispose()
    {
        _logger.LogInformation("Disposing MongoDataLayerContext");
        MongoDatabase.Client.Dispose();
        _logger.LogInformation("Disposed MongoDataLayerContext");
    }
}