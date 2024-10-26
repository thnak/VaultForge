using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Business.Data.Interfaces;
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

namespace Business.Data;

public class RedundantArrayOfIndependentDisks(IMongoDataLayerContext context, ILogger<RedundantArrayOfIndependentDisks> logger, IOptions<AppSettings> options)
{
    private readonly IMongoCollection<FileRaidModel> _fileDataDb = context.MongoDatabase.GetCollection<FileRaidModel>("FileRaid");
    private readonly IMongoCollection<FileRaidDataBlockModel> _fileMetaDataDataDb = context.MongoDatabase.GetCollection<FileRaidDataBlockModel>("FileRaidDataBlock");
    private readonly SemaphoreSlim _semaphore = new(100, 1000);
    private readonly int _stripSize = options.Value.StripeSize;
    private readonly int _readWriteBufferSize = options.Value.ReadWriteBufferSize;

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

            if (options.Value.FileFolders.Length < 3)
            {
                logger.LogError("FileFolders count is less than 3");
                return (false, "Folder count is less than 3");
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

            int index = 0;
            string[] arrayDisk = [..options.Value.FileFolders];
            arrayDisk.Shuffle();
            List<FileRaidDataBlockModel> disks = arrayDisk.Select(x => new FileRaidDataBlockModel()
            {
                AbsolutePath = Path.Combine(x, $"_{DateTime.UtcNow:yyMMdd}", raidModel.Id + Path.GetRandomFileName()),
                CreationTime = DateTime.UtcNow,
                ModificationTime = DateTime.UtcNow,
                RelativePath = raidModel.Id.ToString(),
                Index = index++
            }).ToList();

            foreach (var disk in disks)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(disk.AbsolutePath)!);
            }

            var result = await WriteDataAsync(stream, raidModel.StripSize, disks[0].AbsolutePath, disks[1].AbsolutePath, disks[2].AbsolutePath, cancellationToken);
            // result.TotalByteWritten = byteWrite;
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

    public async Task<long> ReadAndSeek(Stream outputStream, string path, long seekPosition, CancellationToken cancellationToken = default)
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
        return await ReadDataWithRecoveryAndSeekAsync(outputStream, seekPosition, raidData.StripSize, raidData.Size, dataBlocks[0].AbsolutePath, dataBlocks[1].AbsolutePath, dataBlocks[2].AbsolutePath, cancellationToken);
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
            return;

        var id = raidModel.Id.ToString();
        _ = Task.Run(async () =>
        {
            await foreach (var model in GetDataBlocks(id, cancellationToken: default))
            {
                try
                {
                    File.Delete(model.AbsolutePath);
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"[{model.AbsolutePath}] Failed to delete file");
                }
            }

            await _fileDataDb.DeleteOneAsync(x => x.Id == raidModel.Id);
            await _fileMetaDataDataDb.DeleteManyAsync(x => x.RelativePath == id);
        });
    }

    public bool Exists(string key)
    {
        FilterDefinition<FileRaidModel> filter = ObjectId.TryParse(key, out var id) ? Builders<FileRaidModel>.Filter.Eq(x => x.Id, id) : Builders<FileRaidModel>.Filter.Eq(x => x.RelativePath, key);
        return _fileDataDb.Find(filter).Any();
    }

    private async Task<bool> IsAvailableDataBlock(string key)
    {
        Expression<Func<FileRaidDataBlockModel, object>>[] fieldsToFetch =
        [
            x => x.Id,
            x => x.Status
        ];
        var findOption = new FindOptions<FileRaidDataBlockModel, FileRaidDataBlockModel>()
        {
            Projection = fieldsToFetch.ProjectionBuilder()
        };
        var filter = Builders<FileRaidDataBlockModel>.Filter.Eq(x => x.AbsolutePath, key);
        var dataModel = await _fileMetaDataDataDb.FindAsync(filter, options: findOption);
        return dataModel.FirstOrDefault()?.Status == FileRaidStatus.Normal;
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

    async Task<long> ReadDataWithRecoveryAndSeekAsync(Stream outputStream, long seekPosition, int stripeSize, long originalFileSize, string file1Path, string file2Path, string file3Path, CancellationToken cancellationToken = default)
    {
        // Check if any of the files are corrupted or missing
        bool isFile1Corrupted = !File.Exists(file1Path);
        bool isFile2Corrupted = !File.Exists(file2Path);
        bool isFile3Corrupted = !File.Exists(file3Path);

        if (isFile1Corrupted && isFile2Corrupted || isFile3Corrupted && isFile1Corrupted || isFile2Corrupted && isFile3Corrupted)
        {
            throw new Exception("More than 2 disks are failed. Data recovery is impossible.");
        }

        // Open streams for files that exist
        await using FileStream? file1 = isFile1Corrupted ? null : new FileStream(file1Path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: stripeSize, useAsync: true);
        await using FileStream? file2 = isFile2Corrupted ? null : new FileStream(file2Path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: stripeSize, useAsync: true);
        await using FileStream? file3 = isFile3Corrupted ? null : new FileStream(file3Path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: stripeSize, useAsync: true);

        byte[] buffer1 = new byte[stripeSize];
        byte[] buffer2 = new byte[stripeSize];
        byte[] parityBuffer = new byte[stripeSize];

        long totalBytesWritten = 0;
        int stripeIndex = 0;

        // Seek to the correct stripe
        long stripeToSeek = seekPosition / stripeSize; // Calculate the stripe index to seek to
        long bytesToSkip = seekPosition % stripeSize; // Calculate remaining bytes to skip

        // Move to the required stripe
        while (stripeIndex < stripeToSeek)
        {
            // Skip the stripes that we're not reading
            switch (stripeIndex % 3)
            {
                case 0:
                    totalBytesWritten = await ReadStripeAsync(file1, file2, file3, buffer1, buffer2, parityBuffer, stripeSize, cancellationToken);
                    break;
                case 1:
                    totalBytesWritten = await ReadStripeAsync(file1, file3, file2, buffer1, buffer2, parityBuffer, stripeSize, cancellationToken);
                    break;
                case 2:
                    totalBytesWritten = await ReadStripeAsync(file2, file3, file1, buffer2, buffer1, parityBuffer, stripeSize, cancellationToken);
                    break;
            }

            stripeIndex++;
        }

        // Now we are at the required stripe
        switch (stripeIndex % 3)
        {
            case 0:
                totalBytesWritten = await ReadStripeAsync(file1, file2, file3, buffer1, buffer2, parityBuffer, stripeSize, cancellationToken);
                break;
            case 1:
                totalBytesWritten = await ReadStripeAsync(file1, file3, file2, buffer1, buffer2, parityBuffer, stripeSize, cancellationToken);
                break;
            case 2:
                totalBytesWritten = await ReadStripeAsync(file2, file3, file1, buffer2, buffer1, parityBuffer, stripeSize, cancellationToken);
                break;
        }

        // Write the remaining bytes

        await outputStream.WriteAsync(buffer1, 0, (int)Math.Min(bytesToSkip, stripeSize), cancellationToken);
        await outputStream.FlushAsync(cancellationToken);
        return totalBytesWritten;
    }

    // Helper method to read a single stripe
    private async Task<long> ReadStripeAsync(FileStream? file1, FileStream? file2, FileStream? file3, byte[] buffer1, byte[] buffer2, byte[] parityBuffer, int stripeSize, CancellationToken cancellationToken = default)
    {
        // Read data from the files based on availability and handle recovery
        // Similar logic to what you've implemented previously

        // For example:
        if (file1 != null)
        {
            return await file1.ReadAsync(buffer1, 0, stripeSize, cancellationToken);
        }

        if (file2 != null)
        {
            return await file2.ReadAsync(buffer2, 0, stripeSize, cancellationToken);
        }

        if (file3 != null)
        {
            return await file3.ReadAsync(parityBuffer, 0, stripeSize, cancellationToken);
        }

        return 0;
    }

    private FileStream? OpenFileWrite(string path, int bufferSize)
    {
        FileStream? file1 = null;
        try
        {
            file1 = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: bufferSize, useAsync: true);
        }
        catch (Exception)
        {
            //
        }

        return file1;
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

    async Task<WriteDataResult> WriteDataAsync(Stream inputStream, int stripeSize, string file1Path, string file2Path, string file3Path, CancellationToken cancellationToken = default)
    {
        try
        {
            inputStream.SeekBeginOrigin();
            long totalByteWrite = 0;
            long totalByteWritten1 = 0;
            long totalByteWritten2 = 0;
            long totalByteWritten3 = 0;

            FileStream? file1 = OpenFileWrite(file1Path, _readWriteBufferSize);
            FileStream? file2 = OpenFileWrite(file2Path, _readWriteBufferSize);
            FileStream? file3 = OpenFileWrite(file3Path, _readWriteBufferSize);


            byte[] buffer1 = new byte[stripeSize];
            byte[] buffer2 = new byte[stripeSize];
            int bytesRead1;
            int stripeIndex = 0;
            string? contentType = null;

            using MD5 sha256 = MD5.Create();

            bool bufferIsAlive = true;

            while (bufferIsAlive)
            {
                using MemoryStream bufferStream = await ReadStreamWithLimitAsync(inputStream);
                bufferIsAlive = bufferStream.Length > 0;
                while ((bytesRead1 = await bufferStream.ReadAsync(buffer1, 0, stripeSize, cancellationToken)) > 0)
                {
                    var bytesRead2 = await bufferStream.ReadAsync(buffer2, 0, stripeSize, cancellationToken);
                    if (contentType == null)
                    {
                        byte[] temp = [..buffer1, ..buffer1];
                        contentType = temp.GetCorrectExtension("");
                    }

                    sha256.TransformBlock(buffer1, 0, bytesRead1, null, 0);
                    sha256.TransformBlock(buffer2, 0, bytesRead2, null, 0);

                    totalByteWrite += bytesRead1 + bytesRead2;
                    totalByteWritten1 += bytesRead1;
                    totalByteWritten2 += bytesRead2;
                    totalByteWritten3 += stripeSize;

                    var parityBuffer = buffer1.XorParity(buffer2);

                    // Create tasks for writing data and parity in parallel
                    Task[] writeTasks = [];

                    switch (stripeIndex % 3)
                    {
                        case 0:
                            // Parity goes to file 3
                            writeTasks =
                            [
                                file1?.WriteAsync(buffer1, 0, stripeSize, cancellationToken) ?? Task.CompletedTask,
                                file2?.WriteAsync(buffer2, 0, stripeSize, cancellationToken) ?? Task.CompletedTask,
                                file3?.WriteAsync(parityBuffer, 0, stripeSize, cancellationToken) ?? Task.CompletedTask
                            ];
                            break;

                        case 1:
                            // Parity goes to file 2
                            writeTasks =
                            [
                                file1?.WriteAsync(buffer1, 0, stripeSize, cancellationToken) ?? Task.CompletedTask,
                                file2?.WriteAsync(parityBuffer, 0, stripeSize, cancellationToken) ?? Task.CompletedTask,
                                file3?.WriteAsync(buffer2, 0, stripeSize, cancellationToken) ?? Task.CompletedTask,
                            ];
                            break;

                        case 2:
                            // Parity goes to file 1
                            writeTasks =
                            [
                                file1?.WriteAsync(parityBuffer, 0, stripeSize, cancellationToken) ?? Task.CompletedTask,
                                file2?.WriteAsync(buffer1, 0, stripeSize, cancellationToken) ?? Task.CompletedTask,
                                file3?.WriteAsync(buffer2, 0, stripeSize, cancellationToken) ?? Task.CompletedTask,
                            ];
                            break;
                    }

                    // Wait for all tasks (writes) to complete in parallel
                    await Task.WhenAll(writeTasks);
                    stripeIndex++;
                }
            }


            List<FileStream?> streams = [file1, file2, file3];

            await FlushAndDisposeAsync(streams);

            sha256.TransformFinalBlock([], 0, 0);
            StringBuilder checksum = new StringBuilder();
            if (sha256.Hash != null)
            {
                foreach (byte b in sha256.Hash)
                {
                    checksum.Append(b.ToString("x2"));
                }
            }

            contentType = contentType?.GetMimeTypeFromExtension() ?? string.Empty;
            return new WriteDataResult()
            {
                CheckSum = checksum.ToString(),
                TotalByteWritten = totalByteWrite,
                TotalByteWritten1 = totalByteWritten1,
                TotalByteWritten2 = totalByteWritten2,
                TotalByteWritten3 = totalByteWritten3,
                ContentType = contentType,
            };
        }
        catch (OperationCanceledException)
        {
            File.Delete(file1Path);
            File.Delete(file2Path);
            File.Delete(file3Path);
            return new();
        }
    }

    public async Task<MemoryStream> ReadStreamWithLimitAsync(Stream clientStream, int maxBufferSizeInBytes = 4 * 1024 * 1024) // 4MB limit
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
}