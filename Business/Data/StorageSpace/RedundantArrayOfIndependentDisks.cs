using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using Business.Data.Interfaces;
using Business.Data.StorageSpace.Utils;
using Business.Services.Configure;
using Business.Utils;
using Business.Utils.StringExtensions;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Business.Data.StorageSpace;

public class RedundantArrayOfIndependentDisks(IMongoDataLayerContext context, ILogger<RedundantArrayOfIndependentDisks> logger, ApplicationConfiguration options) : IMongoDataInitializer
{
    private readonly IMongoCollection<FileRaidModel> _fileDataDb = context.MongoDatabase.GetCollection<FileRaidModel>("FileRaid");
    private readonly IMongoCollection<FileRaidDataBlockModel> _fileMetaDataDataDb = context.MongoDatabase.GetCollection<FileRaidDataBlockModel>("FileRaidDataBlock");
    private readonly SemaphoreSlim _semaphore = new(100, 1000);
    private readonly int _stripSize = options.GetStorage.StripSize;

    public async Task<(bool, string)> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
            var relativePathKey = Builders<FileRaidModel>.IndexKeys.Ascending(x => x.RelativePath);
            var relativePathIndexModel = new CreateIndexModel<FileRaidModel>(relativePathKey, new CreateIndexOptions { Unique = true });

            var dataBlockPathKey = Builders<FileRaidDataBlockModel>.IndexKeys.Ascending(x => x.RelativePath);
            var dataBlockPathIndexModel = new CreateIndexModel<FileRaidDataBlockModel>(dataBlockPathKey, new CreateIndexOptions { Unique = false });

            var absoluteDataBlockPathKey = Builders<FileRaidDataBlockModel>.IndexKeys.Ascending(x => x.AbsolutePath);
            var absoluteDataBlockPathIndexModel = new CreateIndexModel<FileRaidDataBlockModel>(absoluteDataBlockPathKey, new CreateIndexOptions { Unique = true });

            List<CreateIndexModel<FileRaidModel>> indexRaidModels = [relativePathIndexModel];
            await _fileDataDb.Indexes.CreateManyAsync(indexRaidModels, cancellationToken);

            List<CreateIndexModel<FileRaidDataBlockModel>> indexDataBlockModels = [dataBlockPathIndexModel, absoluteDataBlockPathIndexModel];
            await _fileMetaDataDataDb.Indexes.CreateManyAsync(indexDataBlockModels, cancellationToken);

            CheckDiskSpace(options.GetStorage.Disks);
            if (!options.GetStorage.Disks.ValidateStorageFormat(options.GetStorage.DefaultRaidType))
            {
                logger.LogError("Invalid Storage Format");
                return (false, "Invalid Storage Format");
            }

            logger.LogInformation($"Using RAID option: {options.GetStorage.DefaultRaidType}");
            return (true, AppLang.Success);
        }
        catch (OperationCanceledException e)
        {
            logger.LogError(e, "Operation cancelled");
            return (false, "Cancelled");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void CheckDiskSpace(IEnumerable<string> diskSpaces)
    {
        StringBuilder stringBuilder = new StringBuilder();
        foreach (var disk in diskSpaces)
        {
            if (!Directory.Exists(disk))
                logger.LogError($"Disk Space {disk} does not exist");
            else
            {
                try
                {
                    DriveInfo driveInfo = new DriveInfo(disk);
                    long availableSpace = driveInfo.AvailableFreeSpace / 1024 / 1024;
                    long totalSpace = driveInfo.TotalSize / 1024 / 1024;
                    long usedSpace = totalSpace - availableSpace;
                    stringBuilder.AppendLine($"Disk {disk} information:");
                    stringBuilder.AppendLine($"- Total Space: {totalSpace:N0} MB");
                    stringBuilder.AppendLine($"- Used Space: {usedSpace:N0} MB");
                    stringBuilder.AppendLine($"- Available Space: {availableSpace:N0} MB\n");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"An error occurred while retrieving information for disk {disk}");
                }
            }
        }

        logger.LogInformation(stringBuilder.ToString());
    }

    public async Task<WriteDataResult> WriteDataAsync(Stream stream, string path, CancellationToken cancellationToken = default)
    {
        if (Exists(path)) throw new Exception($"File {path} already exists");
        try
        {
            FileRaidModel raidModel = new()
            {
                RelativePath = path,
                CreationTime = DateTime.UtcNow,
                ModificationTime = DateTime.UtcNow,
                StripSize = _stripSize
            };

            string[] arrayDisk = [..options.GetStorage.Disks];
            arrayDisk.Shuffle();
            List<FileRaidDataBlockModel> disks = arrayDisk.GetShuffledDisks(raidModel.Id);

            disks.InitPhysicFolder();
            await using Raid5Stream streamDisk = new Raid5Stream(disks.Select(x => x.AbsolutePath), 0, raidModel.StripSize, FileMode.Create, FileAccess.Write, FileShare.Read);
            await streamDisk.CopyFromAsync(stream, cancellationToken: cancellationToken);
            await streamDisk.FlushAsync(cancellationToken);
            WriteDataResult result = new WriteDataResult()
            {
                CheckSum = string.Empty,
                TotalByteWritten = streamDisk.Length,
                TotalByteWritePerDisk = streamDisk.FileStreams.Select(x => x?.Length ?? 0).ToArray(),
            };
            raidModel.Size = result.TotalByteWritten;
            raidModel.CheckSum = result.CheckSum;
            for (int i = 0; i < result.TotalByteWritePerDisk.Length; i++)
            {
                disks[i].Size = streamDisk.FileStreams[i]?.Length ?? 0;
            }

            await _fileDataDb.InsertOneAsync(raidModel, null, cancellationToken);
            await _fileMetaDataDataDb.InsertManyAsync(disks, null, cancellationToken);
            return result;
        }
        catch (OperationCanceledException)
        {
            return new();
        }
    }

    public async Task ReadGetDataAsync(Stream outputStream, string path, CancellationToken cancellationToken = default)
    {
        var raidData = Get(path);
        if (raidData == null)
        {
            throw new Exception($"File {path} already exists");
        }

        var dataBlocksFilter = Builders<FileRaidDataBlockModel>.Filter.Eq(x => x.RelativePath, raidData.Id.ToString());

        using var fileData = await _fileMetaDataDataDb.FindAsync(dataBlocksFilter, cancellationToken: cancellationToken);
        List<FileRaidDataBlockModel> dataBlocks = fileData.ToList();
        await foreach (var model in GetDataBlocks(raidData.Id.ToString(), cancellationToken, m => m.AbsolutePath, m => m.Status, model => model.Index))
        {
            if (model.Status != FileRaidStatus.Normal)
                model.AbsolutePath = string.Empty;
            dataBlocks.Add(model);
        }

        dataBlocks = dataBlocks.OrderBy(x => x.Index).DistinctBy(x => x.AbsolutePath).ToList();
        await using Raid5Stream stream = new Raid5Stream(dataBlocks.Select(x => x.AbsolutePath), raidData.Size, raidData.StripSize, FileMode.Open, FileAccess.Read, FileShare.Read);
        await stream.CopyToAsync(outputStream, cancellationToken: cancellationToken);
    }

    public async Task<RaidFileInfo?> GetDataBlockPaths(string path, CancellationToken cancellationToken = default)
    {
        var raidData = Get(path);
        if (raidData == null)
        {
            logger.LogError("Not found");
            return null;
        }

        var dataBlocksFilter = Builders<FileRaidDataBlockModel>.Filter.Eq(x => x.RelativePath, raidData.Id.ToString());

        using var fileData = await _fileMetaDataDataDb.FindAsync(dataBlocksFilter, cancellationToken: cancellationToken);
        List<FileRaidDataBlockModel> dataBlocks = fileData.ToList();
        await foreach (var model in GetDataBlocks(raidData.Id.ToString(), cancellationToken, m => m.AbsolutePath, m => m.Status, model => model.Index))
        {
            if (model.Status != FileRaidStatus.Normal)
                model.AbsolutePath = string.Empty;
            dataBlocks.Add(model);
        }

        dataBlocks = dataBlocks.OrderBy(x => x.Index).DistinctBy(x => x.AbsolutePath).ToList();
        return new RaidFileInfo()
        {
            Path = path,
            Files = dataBlocks.Select(x => x.AbsolutePath).ToArray(),
            FileSize = raidData.Size,
            StripeSize = raidData.StripSize,
        };
    }

    public async Task DeleteAsync(string path)
    {
        var raidModel = Get(path);
        if (raidModel == null)
        {
            logger.LogError($"{path} Not found. Delete failed");
            return;
        }

        var id = raidModel.Id.ToString();
        await DeleteAllDataBlocks(id);
    }

    private async Task DeleteAllDataBlocks(string id)
    {
        await foreach (var model in GetDataBlocks(id, cancellationToken: default))
        {
            try
            {
                if (File.Exists(model.AbsolutePath))
                    File.Delete(model.AbsolutePath);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"[{model.AbsolutePath}] Failed to delete file");
            }
        }

        await _fileDataDb.DeleteOneAsync(x => x.Id == ObjectId.Parse(id));
        await _fileMetaDataDataDb.DeleteManyAsync(x => x.RelativePath == id);
    }

    public bool Exists(string key)
    {
        FilterDefinition<FileRaidModel> filter = ObjectId.TryParse(key, out var id) ? Builders<FileRaidModel>.Filter.Eq(x => x.Id, id) : Builders<FileRaidModel>.Filter.Eq(x => x.RelativePath, key);
        return _fileDataDb.Find(filter).Any();
    }

    public async IAsyncEnumerable<FileRaidDataBlockModel> GetDataBlocks(string path, [EnumeratorCancellation] CancellationToken cancellationToken = default, params Expression<Func<FileRaidDataBlockModel, object>>[] fieldsToFetch)
    {
        var findOption = new FindOptions<FileRaidDataBlockModel, FileRaidDataBlockModel>()
        {
            Projection = fieldsToFetch.ProjectionBuilder()
        };
        var filter = Builders<FileRaidDataBlockModel>.Filter.Eq(x => x.RelativePath, path);
        using var dataModel = await _fileMetaDataDataDb.FindAsync(filter, options: findOption, cancellationToken: cancellationToken);
        while (await dataModel.MoveNextAsync(cancellationToken))
        {
            foreach (var model in dataModel.Current)
            {
                yield return model;
            }
        }
    }

    public FileRaidModel? Get(string key)
    {
        FilterDefinition<FileRaidModel> filter = ObjectId.TryParse(key, out var id) ? Builders<FileRaidModel>.Filter.Eq(x => x.Id, id) : Builders<FileRaidModel>.Filter.Eq(x => x.RelativePath, key);
        return _fileDataDb.Find(filter).Limit(1).FirstOrDefault();
    }

    public class RaidFileInfo
    {
        public string Path { get; set; } = string.Empty;
        public string[] Files { get; set; } = [];
        public int StripeSize { get; set; }
        public long FileSize { get; set; }
        public int TotalFiles => Files.Length;
    }

    public class WriteDataResult
    {
        public long TotalByteWritten { get; set; }
        public long[] TotalByteWritePerDisk { get; set; } = [];
        public string CheckSum { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}