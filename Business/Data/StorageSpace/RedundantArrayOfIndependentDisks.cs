using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Business.Data.Interfaces;
using Business.Data.StorageSpace.Utils;
using Business.Services.TaskQueueServices.Base.Interfaces;
using Business.Utils;
using Business.Utils.Helper;
using Business.Utils.StringExtensions;
using BusinessModels.General.SettingModels;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using BusinessModels.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Business.Data.StorageSpace;

public class RedundantArrayOfIndependentDisks(IMongoDataLayerContext context, IParallelBackgroundTaskQueue queue, ILogger<RedundantArrayOfIndependentDisks> logger, IOptions<AppSettings> options) : IMongoDataInitializer
{
    private readonly IMongoCollection<FileRaidModel> _fileDataDb = context.MongoDatabase.GetCollection<FileRaidModel>("FileRaid");
    private readonly IMongoCollection<FileRaidDataBlockModel> _fileMetaDataDataDb = context.MongoDatabase.GetCollection<FileRaidDataBlockModel>("FileRaidDataBlock");
    private readonly SemaphoreSlim _semaphore = new(100, 1000);
    private readonly int _stripSize = options.Value.Storage.StripSize;
    private readonly int _readWriteBufferSize = options.Value.Storage.BufferSize;

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

            CheckDiskSpace(options.Value.Storage.Disks);
            if (!options.Value.Storage.Disks.ValidateStorageFormat(options.Value.Storage.DefaultRaidType))
            {
                logger.LogError("Invalid Storage Format");
                return (false, "Invalid Storage Format");
            }

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

            string[] arrayDisk = [..options.Value.Storage.Disks];
            arrayDisk.Shuffle();
            List<FileRaidDataBlockModel> disks = arrayDisk.GetShuffledDisks(raidModel.Id);

            disks.InitPhysicFolder();

            var result = await WriteDataAsync(stream, raidModel.StripSize, cancellationToken, disks.Select(x => x.AbsolutePath));
            raidModel.Size = result.TotalByteWritten;
            raidModel.CheckSum = result.CheckSum;
            disks[0].Size = result.TotalByteWritten1;
            disks[1].Size = result.TotalByteWritten2;
            disks[2].Size = result.TotalByteWritten3;

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

        var fileData = await _fileMetaDataDataDb.FindAsync(dataBlocksFilter, cancellationToken: cancellationToken);
        List<FileRaidDataBlockModel> dataBlocks = fileData.ToList();
        await foreach (var model in GetDataBlocks(raidData.Id.ToString(), cancellationToken, m => m.AbsolutePath, m => m.Status, model => model.Index))
        {
            if (model.Status != FileRaidStatus.Normal)
                model.AbsolutePath = string.Empty;
            dataBlocks.Add(model);
        }

        dataBlocks = dataBlocks.OrderBy(x => x.Index).DistinctBy(x => x.AbsolutePath).ToList();
        await ReadDataWithRecoveryAsync(outputStream, raidData.StripSize, raidData.Size, dataBlocks[0].AbsolutePath, dataBlocks[1].AbsolutePath, dataBlocks[2].AbsolutePath);
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

        var fileData = await _fileMetaDataDataDb.FindAsync(dataBlocksFilter, cancellationToken: cancellationToken);
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

    public void Delete(string path)
    {
        var raidModel = Get(path);
        if (raidModel == null)
        {
            logger.LogError($"{path} Not found. Delete failed");
            return;
        }

        var id = raidModel.Id.ToString();
        queue.QueueBackgroundWorkItemAsync(async _ => await DeleteAllDataBlocks(id));
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
        var dataModel = await _fileMetaDataDataDb.FindAsync(filter, options: findOption, cancellationToken: cancellationToken);
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

    private async Task ReadDataWithRecoveryAsync(Stream outputStream, int stripeSize, long originalFileSize, string file1Path, string file2Path, string file3Path, long seekPosition = 0)
    {
        // Check if any of the files are corrupted or missing
        bool isFile1Corrupted = !File.Exists(file1Path) || string.IsNullOrEmpty(file1Path);
        bool isFile2Corrupted = !File.Exists(file2Path) || string.IsNullOrEmpty(file2Path);
        bool isFile3Corrupted = !File.Exists(file3Path) || string.IsNullOrEmpty(file3Path);


        long totalBytesWritten = seekPosition;
        if (isFile1Corrupted && isFile2Corrupted || isFile3Corrupted && isFile1Corrupted || isFile2Corrupted && isFile3Corrupted)
        {
            throw new Exception("More than 2 disk are failure. Data recovery is impossible.");
        }

        // Open streams for files that exist
        await using FileStream? file1 = isFile1Corrupted ? null : new FileStream(file1Path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: _readWriteBufferSize, useAsync: true);
        await using FileStream? file2 = isFile2Corrupted ? null : new FileStream(file2Path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: _readWriteBufferSize, useAsync: true);
        await using FileStream? file3 = isFile3Corrupted ? null : new FileStream(file3Path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: _readWriteBufferSize, useAsync: true);

        file1?.Seek(seekPosition, SeekOrigin.Begin);
        file2?.Seek(seekPosition, SeekOrigin.Begin);
        file3?.Seek(seekPosition, SeekOrigin.Begin);

        byte[] buffer1 = new byte[stripeSize];
        byte[] buffer2 = new byte[stripeSize];
        byte[] parityBuffer = new byte[stripeSize];

        int stripeIndex = 0;

        while (totalBytesWritten < originalFileSize)
        {
            Task<int> readTask1 = Task.FromResult(0);
            Task<int> readTask2 = Task.FromResult(0);
            Task<int> readTask3;

            // Determine the current stripe pattern and read from available files
            switch (stripeIndex % 3)
            {
                case 0:
                    // Parity in file 3, data in file 1 and file 2
                    if (isFile1Corrupted)
                    {
                        // Recover data1 using parity and data2
                        readTask2 = file2!.ReadAsync(buffer2, 0, stripeSize);
                        readTask1 = file3!.ReadAsync(parityBuffer, 0, stripeSize);
                        await Task.WhenAll(readTask1, readTask2);
                        buffer1 = parityBuffer.XorParity(buffer2);
                    }
                    else if (isFile2Corrupted)
                    {
                        // Recover data2 using parity and data1
                        readTask1 = file1!.ReadAsync(buffer1, 0, stripeSize);
                        readTask2 = file3!.ReadAsync(parityBuffer, 0, stripeSize);
                        await Task.WhenAll(readTask1, readTask2);
                        buffer2 = parityBuffer.XorParity(buffer1);
                    }
                    else if (isFile3Corrupted)
                    {
                        // Read data, calculate parity to verify correctness
                        readTask1 = file1!.ReadAsync(buffer1, 0, stripeSize);
                        readTask2 = file2!.ReadAsync(buffer2, 0, stripeSize);
                        await Task.WhenAll(readTask1, readTask2);
                    }
                    else
                    {
                        readTask1 = file1!.ReadAsync(buffer1, 0, stripeSize);
                        readTask2 = file2!.ReadAsync(buffer2, 0, stripeSize);
                        readTask3 = file3!.ReadAsync(parityBuffer, 0, stripeSize);
                        await Task.WhenAll(readTask1, readTask2, readTask3);
                    }

                    break;

                case 1:
                    // Parity in file 2, data in file 1 and file 3
                    if (isFile1Corrupted)
                    {
                        readTask2 = file3!.ReadAsync(buffer2, 0, stripeSize);
                        readTask1 = file2!.ReadAsync(parityBuffer, 0, stripeSize);
                        await Task.WhenAll(readTask1, readTask2);
                        buffer1 = parityBuffer.XorParity(buffer2);
                    }
                    else if (isFile3Corrupted)
                    {
                        readTask1 = file1!.ReadAsync(buffer1, 0, stripeSize);
                        readTask2 = file2!.ReadAsync(parityBuffer, 0, stripeSize);
                        await Task.WhenAll(readTask1, readTask2);
                        buffer2 = parityBuffer.XorParity(buffer1);
                    }
                    else if (isFile2Corrupted)
                    {
                        readTask1 = file1!.ReadAsync(buffer1, 0, stripeSize);
                        readTask2 = file3!.ReadAsync(buffer2, 0, stripeSize);
                        await Task.WhenAll(readTask1, readTask2);
                    }
                    else
                    {
                        readTask1 = file1!.ReadAsync(buffer1, 0, stripeSize);
                        readTask2 = file3!.ReadAsync(buffer2, 0, stripeSize);
                        readTask3 = file2!.ReadAsync(parityBuffer, 0, stripeSize);
                        await Task.WhenAll(readTask1, readTask2, readTask3);
                    }

                    break;

                case 2:
                    // Parity in file 1, data in file 2 and file 3
                    if (isFile2Corrupted)
                    {
                        readTask2 = file3!.ReadAsync(buffer2, 0, stripeSize);
                        readTask1 = file1!.ReadAsync(parityBuffer, 0, stripeSize);
                        await Task.WhenAll(readTask1, readTask2);
                        buffer1 = parityBuffer.XorParity(buffer2);
                    }
                    else if (isFile3Corrupted)
                    {
                        readTask1 = file2!.ReadAsync(buffer1, 0, stripeSize);
                        readTask2 = file1!.ReadAsync(parityBuffer, 0, stripeSize);
                        await Task.WhenAll(readTask1, readTask2);
                        buffer2 = parityBuffer.XorParity(buffer1);
                    }
                    else if (isFile1Corrupted)
                    {
                        readTask1 = file2!.ReadAsync(buffer1, 0, stripeSize);
                        readTask2 = file3!.ReadAsync(buffer2, 0, stripeSize);
                        await Task.WhenAll(readTask1, readTask2);
                    }
                    else
                    {
                        readTask1 = file2!.ReadAsync(buffer1, 0, stripeSize);
                        readTask2 = file3!.ReadAsync(buffer2, 0, stripeSize);
                        readTask3 = file1!.ReadAsync(parityBuffer, 0, stripeSize);
                        await Task.WhenAll(readTask1, readTask2, readTask3);
                    }

                    break;
            }

            var bytesRead1 = await readTask1;
            var bytesRead2 = await readTask2;

            if (bytesRead1 == 0 && bytesRead2 == 0)
            {
                break; // End of stream
            }

            var writeSize1 = (int)Math.Min(originalFileSize - totalBytesWritten, bytesRead1);
            await outputStream.WriteAsync(buffer1, 0, writeSize1);
            totalBytesWritten += writeSize1;

            var writeSize2 = (int)Math.Min(originalFileSize - totalBytesWritten, bytesRead2);
            await outputStream.WriteAsync(buffer2, 0, writeSize2);
            totalBytesWritten += writeSize2;

            stripeIndex++;
        }

        await outputStream.FlushAsync();
        outputStream.SeekBeginOrigin();
    }


    private async Task FlushAndDisposeAsync(params IEnumerable<FileStream?> files)
    {
        List<Task> tasksFlush = files.Where(x => x != default).Select(async x =>
        {
            await x!.FlushAsync(CancellationToken.None);
            await x.DisposeAsync();
        }).ToList();
        await Task.WhenAll(tasksFlush);
    }

    public async Task<WriteDataResult> WriteDataAsync(Stream inputStream, int stripeSize, CancellationToken cancellationToken = default, params IEnumerable<string> filePaths)
    {
        var pathsArray = filePaths.ToArray();
        List<FileStream?> fileStreams = pathsArray.Select(path => path.OpenFileWrite(_readWriteBufferSize).Value).ToList();

        try
        {
            inputStream.SeekBeginOrigin();
            var writeDataResult = await ProcessInputStreamAsync(inputStream, stripeSize, cancellationToken, fileStreams);

            return writeDataResult;
        }
        catch (OperationCanceledException)
        {
            pathsArray.DeletePhysicFiles();
            return new WriteDataResult();
        }
        finally
        {
            await FlushAndDisposeAsync(fileStreams);
        }
    }

    private async Task<WriteDataResult> ProcessInputStreamAsync(Stream inputStream, int stripeSize, CancellationToken cancellationToken, List<FileStream?> fileStreams)
    {
        long totalBytesWritten = 0;
        long[] fileBytesWritten = new long[fileStreams.Count];
        byte[][] buffers = Enumerable.Range(0, fileStreams.Count).Select(_ => new byte[stripeSize]).ToArray();

        int[] bytesRead = new int[2];
        int stripeCount = 0;
        string? detectedContentType = null;

        using MD5 md5Hasher = MD5.Create();
        bool hasMoreData = true;

        while (hasMoreData)
        {
            using MemoryStream bufferStream = await ReadStreamWithLimitAsync(inputStream);
            hasMoreData = bufferStream.Length > 0;

            while ((bytesRead[0] = await bufferStream.ReadAsync(buffers[0], 0, stripeSize, cancellationToken)) > 0)
            {
                bytesRead[1] = await bufferStream.ReadAsync(buffers[1], 0, stripeSize, cancellationToken);

                if (detectedContentType == null)
                {
                    detectedContentType = DetectContentType(buffers[0], buffers[2]);
                }

                md5Hasher.TransformBlock(buffers[0], 0, bytesRead[0], null, 0);
                md5Hasher.TransformBlock(buffers[1], 0, bytesRead[1], null, 0);

                totalBytesWritten += bytesRead.Sum();
                UpdateFileBytesWritten(fileBytesWritten, bytesRead, stripeSize);

                buffers[2] = buffers[0].XorParity(buffers[1]);

                var writeTasks = CreateWriteTasks(stripeCount, stripeSize, cancellationToken, buffers, fileStreams);
                await Task.WhenAll(writeTasks);

                stripeCount++;
            }
        }

        md5Hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        return new WriteDataResult
        {
            CheckSum = ConvertChecksumToHex(md5Hasher.Hash),
            TotalByteWritten = totalBytesWritten,
            TotalByteWritten1 = fileBytesWritten[0],
            TotalByteWritten2 = fileBytesWritten[1],
            TotalByteWritten3 = fileBytesWritten[2],
            ContentType = detectedContentType?.GetMimeTypeFromExtension() ?? string.Empty,
        };
    }

    private string DetectContentType(byte[] buffer0, byte[] buffer2)
    {
        return buffer0.Concat(buffer2).ToArray().GetCorrectExtension("");
    }

    private void UpdateFileBytesWritten(long[] fileBytesWritten, int[] bytesRead, int stripeSize)
    {
        fileBytesWritten[0] += bytesRead[0];
        fileBytesWritten[1] += bytesRead[1];
        fileBytesWritten[2] += stripeSize;
    }

    private static List<Task> CreateWriteTasks(int stripeCount, int stripeSize, CancellationToken cancellationToken, byte[][] buffers, List<FileStream?> fileStreams)
    {
        int stripeIndex = stripeCount % fileStreams.Count;
        int lastIndex = fileStreams.Count - 1;
        int fileStripeIndex = lastIndex - stripeIndex;
        int[] sortIndex = new int[fileStreams.Count];
        sortIndex[fileStripeIndex] = lastIndex;
        int index = 0;

        List<Task> tasks = [];
        for (int i = 0; i < sortIndex.Length; i++)
        {
            if (fileStripeIndex == i)
            {
                tasks.Add(fileStreams[i]?.WriteAsync(buffers[fileStripeIndex], 0, stripeSize, cancellationToken) ?? Task.CompletedTask);
                continue;
            }

            tasks.Add(fileStreams[i]?.WriteAsync(buffers[index++], 0, stripeSize, cancellationToken) ?? Task.CompletedTask);
        }

        return tasks;
    }

    private string ConvertChecksumToHex(byte[]? hash)
    {
        StringBuilder checksumBuilder = new();
        if (hash != null)
        {
            foreach (byte b in hash)
            {
                checksumBuilder.Append(b.ToString("x2"));
            }
        }

        return checksumBuilder.ToString();
    }

    private async Task<MemoryStream> ReadStreamWithLimitAsync(Stream clientStream, int maxBufferSizeInBytes = 4 * 1024 * 1024) // 4MB limit
    {
        const int bufferSize = 8192; // 8 KB buffer size
        byte[] buffer = new byte[bufferSize];
        int bytesRead;
        int remainingSize = maxBufferSizeInBytes;

        // Create a MemoryStream to hold the stream data with a capacity of maxBufferSizeInBytes
        MemoryStream memoryStream = new MemoryStream();

        // Read the client stream in chunks and write to the memory stream until the limit is reached
        while ((bytesRead = await clientStream.ReadAsync(buffer, 0, Math.Min(remainingSize, bufferSize))) > 0)
        {
            await memoryStream.WriteAsync(buffer, 0, bytesRead);
            remainingSize -= bytesRead;
        }

        // Optionally, reset the memory stream's position to the beginning if you plan to read from it later
        memoryStream.Position = 0;

        return memoryStream;
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
        public long TotalByteWritten1 { get; set; }
        public long TotalByteWritten2 { get; set; }
        public long TotalByteWritten3 { get; set; }
        public string CheckSum { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}