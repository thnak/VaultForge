using System.Linq.Expressions;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Business.Data.Interfaces;
using Business.Utils;
using Business.Utils.Helper;
using BusinessModels.General;
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
            List<FileRaidDataBlockModel> disks = options.Value.FileFolders.Select(x => new FileRaidDataBlockModel()
            {
                AbsolutePath = Path.Combine(x, $"{DateTime.UtcNow:yy-MM-dd}", raidModel.Id + Path.GetRandomFileName()),
                CreationTime = DateTime.UtcNow,
                ModificationTime = DateTime.UtcNow,
                RelativePath = raidModel.Id.ToString(),
                Index = index++
            }).ToList();

            foreach (var disk in disks)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(disk.AbsolutePath)!);
            }

            WriteDataResult result = new();

            result = await WriteDataAsync(stream, raidModel.StripSize, disks[0].AbsolutePath, disks[1].AbsolutePath, disks[2].AbsolutePath, cancellationToken);
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
        catch (OperationCanceledException e)
        {
            Console.WriteLine(e);
            throw;
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

    public void Delete(string path)
    {
        var raidModel = Get(path);
        if (raidModel == null)
            return;


        _fileDataDb.DeleteOne(x => x.Id == raidModel.Id);
        _fileMetaDataDataDb.DeleteManyAsync(x => x.RelativePath == raidModel.Id.ToString());

        _ = Task.Run(async () =>
        {
            await foreach (var model in GetDataBlocks(raidModel.Id.ToString(), cancellationToken: default))
            {
                try
                {
                    File.Delete(model.AbsolutePath);
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"[{model.AbsolutePath}]Failed to delete file");
                }
            }
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

    private async IAsyncEnumerable<FileRaidDataBlockModel> GetDataBlocks(string path, [EnumeratorCancellation] CancellationToken cancellationToken = default, params Expression<Func<FileRaidDataBlockModel, object>>[] fieldsToFetch)
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

    private FileRaidModel? Get(string key)
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
                        buffer1 = XorParity(parityBuffer, buffer2);
                    }
                    else if (isFile2Corrupted)
                    {
                        // Recover data2 using parity and data1
                        readTask1 = file1!.ReadAsync(buffer1, 0, stripeSize);
                        readTask2 = file3!.ReadAsync(parityBuffer, 0, stripeSize);
                        await Task.WhenAll(readTask1, readTask2);
                        buffer2 = XorParity(parityBuffer, buffer1);
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
                        buffer1 = XorParity(parityBuffer, buffer2);
                    }
                    else if (isFile3Corrupted)
                    {
                        readTask1 = file1!.ReadAsync(buffer1, 0, stripeSize);
                        readTask2 = file2!.ReadAsync(parityBuffer, 0, stripeSize);
                        await Task.WhenAll(readTask1, readTask2);
                        buffer2 = XorParity(parityBuffer, buffer1);
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
                        buffer1 = XorParity(parityBuffer, buffer2);
                    }
                    else if (isFile3Corrupted)
                    {
                        readTask1 = file2!.ReadAsync(buffer1, 0, stripeSize);
                        readTask2 = file1!.ReadAsync(parityBuffer, 0, stripeSize);
                        await Task.WhenAll(readTask1, readTask2);
                        buffer2 = XorParity(parityBuffer, buffer1);
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

    async Task<WriteDataResult> WriteDataAsync(Stream inputStream, int stripeSize, string file1Path, string file2Path, string file3Path, CancellationToken cancellationToken = default)
    {
        inputStream.SeekBeginOrigin();
        long totalByteWrite = 0;
        long totalByteWritten1 = 0;
        long totalByteWritten2 = 0;
        long totalByteWritten3 = 0;

        FileStream? file1 = null;
        try
        {
            file1 = new FileStream(file1Path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: _readWriteBufferSize, useAsync: true);
        }
        catch (Exception)
        {
            //
        }

        FileStream? file2 = null;
        try
        {
            file2 = new FileStream(file2Path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: _readWriteBufferSize, useAsync: true);
        }
        catch (Exception)
        {
            //
        }

        FileStream? file3 = null;
        try
        {
            file3 = new FileStream(file3Path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: _readWriteBufferSize, useAsync: true);
        }
        catch (Exception)
        {
            //
        }

        byte[] buffer1 = new byte[stripeSize];
        byte[] buffer2 = new byte[stripeSize];
        int bytesRead1;
        int stripeIndex = 0;
        string? contentType = null;

        using SHA256 sha256 = SHA256.Create();

        while ((bytesRead1 = await inputStream.ReadAsync(buffer1, 0, stripeSize, cancellationToken)) > 0)
        {
            if (contentType == null)
                contentType = buffer1.GetCorrectExtension("");
            var bytesRead2 = await inputStream.ReadAsync(buffer2, 0, stripeSize, cancellationToken);

            sha256.TransformBlock(buffer1, 0, bytesRead1, null, 0);
            sha256.TransformBlock(buffer2, 0, bytesRead2, null, 0);

            totalByteWrite += bytesRead1 + bytesRead2;
            totalByteWritten1 += bytesRead1;
            totalByteWritten2 += bytesRead2;
            totalByteWritten3 += stripeSize;

            var parityBuffer = XorParity(buffer1, buffer2);

            // Create tasks for writing data and parity in parallel
            Task[] writeTasks = [];

            switch (stripeIndex % 3)
            {
                case 0:
                    // Parity goes to file 3
                    writeTasks =
                    [
                        file1?.WriteAsync(buffer1, 0, bytesRead1, cancellationToken) ?? Task.CompletedTask,
                        file2?.WriteAsync(buffer2, 0, bytesRead2, cancellationToken) ?? Task.CompletedTask,
                        file3?.WriteAsync(parityBuffer, 0, stripeSize, cancellationToken) ?? Task.CompletedTask
                    ];
                    break;

                case 1:
                    // Parity goes to file 2
                    writeTasks =
                    [
                        file1?.WriteAsync(buffer1, 0, bytesRead1, cancellationToken) ?? Task.CompletedTask,
                        file2?.WriteAsync(parityBuffer, 0, stripeSize, cancellationToken) ?? Task.CompletedTask,
                        file3?.WriteAsync(buffer2, 0, bytesRead2, cancellationToken) ?? Task.CompletedTask,
                    ];
                    break;

                case 2:
                    // Parity goes to file 1
                    writeTasks =
                    [
                        file1?.WriteAsync(parityBuffer, 0, stripeSize, cancellationToken) ?? Task.CompletedTask,
                        file2?.WriteAsync(buffer1, 0, bytesRead1, cancellationToken) ?? Task.CompletedTask,
                        file3?.WriteAsync(buffer2, 0, bytesRead2, cancellationToken) ?? Task.CompletedTask,
                    ];
                    break;
            }

            // Wait for all tasks (writes) to complete in parallel
            await Task.WhenAll(writeTasks);
            stripeIndex++;
        }

        if (file1 != null)
        {
            await file1.FlushAsync(cancellationToken);
            await file1.DisposeAsync().ConfigureAwait(false);
        }

        if (file2 != null)
        {
            await file2.FlushAsync(cancellationToken);
            await file2.DisposeAsync().ConfigureAwait(false);
        }

        if (file3 != null)
        {
            await file3.FlushAsync(cancellationToken);
            await file3.DisposeAsync().ConfigureAwait(false);
        }

        sha256.TransformFinalBlock([], 0, 0);
        StringBuilder checksum = new StringBuilder();
        if (sha256.Hash != null)
        {
            foreach (byte b in sha256.Hash)
            {
                checksum.Append(b.ToString("x2"));
            }
        }

        return new WriteDataResult()
        {
            CheckSum = checksum.ToString(),
            TotalByteWritten = totalByteWrite,
            TotalByteWritten1 = totalByteWritten1,
            TotalByteWritten2 = totalByteWritten2,
            TotalByteWritten3 = totalByteWritten3,
            ContentType = contentType?.GetMimeTypeFromExtension() ?? string.Empty
        };
    }

    private byte[] XorParity(byte[] data0, byte[] data1)
    {
        int vectorSize = Vector<byte>.Count;
        int i = 0;

        byte[] parity = new byte[data0.Length];

        // Process in chunks of Vector<byte>.Count (size of SIMD vector)
        if (Vector.IsHardwareAccelerated)
        {
            for (; i <= data1.Length - vectorSize; i += vectorSize)
            {
                // Load the current portion of the parity and data as vectors
                var data0Vector = new Vector<byte>(data0, i);
                var data1Vector = new Vector<byte>(data1, i);

                // XOR the vectors
                var resultVector = Vector.Xor(data0Vector, data1Vector);

                // Store the result back into the parity array
                resultVector.CopyTo(parity, i);
            }

            return parity;
        }

        // Fallback to scalar XOR for the remaining bytes (if any)
        for (; i < data1.Length; i++)
        {
            parity[i] = (byte)(data0[i] ^ data1[i]);
        }

        return parity;
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