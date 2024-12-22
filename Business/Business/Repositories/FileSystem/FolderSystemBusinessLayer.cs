using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text;
using BrainNet.Database;
using BrainNet.Models.Result;
using BrainNet.Models.Setting;
using BrainNet.Models.Vector;
using Business.Business.Interfaces.FileSystem;
using Business.Business.Interfaces.User;
using Business.Business.Utils;
using Business.Data.Interfaces.FileSystem;
using Business.Data.StorageSpace;
using Business.Models;
using Business.Services;
using Business.Services.Configure;
using Business.Services.TaskQueueServices.Base.Interfaces;
using Business.Utils.Helper;
using Business.Utils.Protector;
using Business.Utils.StringExtensions;
using BusinessModels.General.EnumModel;
using BusinessModels.General.Results;
using BusinessModels.People;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using BusinessModels.Validator.Folder;
using BusinessModels.WebContent.Drive;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.IO.Compression;
using Business.Services.Interfaces;
using BusinessModels.General.Update;

namespace Business.Business.Repositories.FileSystem;

internal class FolderSystemBusinessLayer(
    IFolderSystemDatalayer folderSystemService,
    IFileSystemBusinessLayer fileSystemService,
    IUserBusinessLayer userService,
    ILogger<FolderSystemBusinessLayer> logger,
    IMemoryCache memoryCache,
    RedundantArrayOfIndependentDisks raidService,
    IParallelBackgroundTaskQueue parallelBackgroundTaskQueue,
    ISequenceBackgroundTaskQueue sequenceBackgroundTaskQueue,
    IThumbnailService thumbnailService,
    ApplicationConfiguration options)
    : IFolderSystemBusinessLayer
{
    private readonly CacheKeyManager _cacheKeyManager = new(memoryCache, nameof(FolderSystemBusinessLayer));
    private readonly ConcurrentDictionary<string, IInMemoryVectorDb> _vectorDbs = new();

    [Experimental("SKEXP0020")]
    public async Task<Result<bool>> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await foreach (var user in userService.GetAllAsync([model => model.UserName], cancellationToken))
            {
                await GetOrInitCollection(user.UserName);
            }

            return Result<bool>.SuccessWithMessage(true, AppLang.Success);
        }
        catch (OperationCanceledException e)
        {
            return Result<bool>.Failure(e.Message, ErrorType.Cancelled);
        }
        catch (Exception e)
        {
            return Result<bool>.Failure(e.Message, ErrorType.Unknown);
        }
    }

    public Task<Result<List<SearchScore<VectorRecord>>?>> SearchVectorAsync(float[] vector, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    [Experimental("SKEXP0020")]
    private async Task<IInMemoryVectorDb> GetOrInitCollection(string userName)
    {
        var collectionName = userName.GetVectorName(nameof(FolderSystemBusinessLayer));
        var success = _vectorDbs.TryAdd(collectionName, new InMemoryIInMemoryVectorDb(new VectorDbConfig()
        {
            Name = collectionName,
            OllamaConnectionString = options.GetOllamaConfig.ConnectionString,
            OllamaTextEmbeddingModelName = options.GetOllamaConfig.TextEmbeddingModel,
            OllamaImage2TextModelName = options.GetOllamaConfig.Image2TextModel
        }, logger));
        if (success)
        {
            await _vectorDbs[collectionName].Init();
            var rootFolder = Get(userName, "/root");
            if (rootFolder != null)
            {
                await InitVectorDbData(rootFolder);
            }
        }

        return _vectorDbs[collectionName];
    }

    [Experimental("SKEXP0020")]
    private async Task InitVectorDbData(FolderInfoModel folderRoot)
    {
        var rootFolderId = folderRoot.Id.ToString();
        var files = fileSystemService.Where(x => x.RootFolder == rootFolderId && x.Classify == FileClassify.Normal && x.Status == FileStatus.File);
        await foreach (var file in files)
        {
            await RequestIndexAsync(file.Id.ToString());
        }

        FolderContentType[] folderContentTypes = [FolderContentType.Folder, FolderContentType.SystemFolder, FolderContentType.Folder];
        var folders = Where(x => x.RootFolder == rootFolderId && folderContentTypes.Contains(x.Type));
        await foreach (var folder in folders)
        {
            await InitVectorDbData(folder);
        }
    }

    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(nameof(FolderSystemBusinessLayer));
        stringBuilder.Append(nameof(GetDocumentSizeAsync));

        var key = stringBuilder.ToString();
        var value = _cacheKeyManager.GetOrCreateAsync(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            return folderSystemService.GetDocumentSizeAsync(cancellationToken);
        });
        return value;
    }

    public Task<long> GetDocumentSizeAsync(Expression<Func<FolderInfoModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(nameof(FolderSystemBusinessLayer));
        stringBuilder.Append(nameof(GetDocumentSizeAsync));
        stringBuilder.Append(predicate.GetCacheKey());

        var key = stringBuilder.ToString();
        var value = _cacheKeyManager.GetOrCreateAsync(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            return folderSystemService.GetDocumentSizeAsync(predicate, cancellationToken);
        });
        return value;
    }

    public IAsyncEnumerable<FolderInfoModel> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        return folderSystemService.Search(queryString, limit, cancellationToken);
    }

    public IAsyncEnumerable<FolderInfoModel> FindAsync(FilterDefinition<FolderInfoModel> filter, CancellationToken cancellationToken = default)
    {
        return folderSystemService.FindAsync(filter, cancellationToken);
    }

    public IAsyncEnumerable<FolderInfoModel> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        return folderSystemService.FindAsync(keyWord, cancellationToken);
    }

    public IAsyncEnumerable<FolderInfoModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<FolderInfoModel, object>>[] fieldsToFetch)
    {
        return folderSystemService.FindProjectAsync(keyWord, limit, cancellationToken, fieldsToFetch);
    }

    public IAsyncEnumerable<FolderInfoModel> Where(Expression<Func<FolderInfoModel, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<FolderInfoModel, object>>[] fieldsToFetch)
    {
        return folderSystemService.WhereAsync(predicate, cancellationToken, fieldsToFetch);
    }

    public FolderInfoModel? Get(string key)
    {
        return folderSystemService.Get(key);
    }

    public Task<Result<FolderInfoModel?>> Get(string key, params Expression<Func<FolderInfoModel, object>>[] fieldsToFetch)
    {
        return folderSystemService.Get(key, fieldsToFetch);
    }

    public IAsyncEnumerable<FolderInfoModel?> GetAsync(List<string> keys, CancellationToken cancellationToken = default)
    {
        return folderSystemService.GetAsync(keys, cancellationToken);
    }

    public Task<(FolderInfoModel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        return folderSystemService.GetAllAsync(page, size, cancellationToken);
    }

    public IAsyncEnumerable<FolderInfoModel> GetAllAsync(Expression<Func<FolderInfoModel, object>>[] field2Fetch, CancellationToken cancellationToken)
    {
        return folderSystemService.GetAllAsync(field2Fetch, cancellationToken);
    }

    public Task<(bool, string)> UpdateAsync(string key, FieldUpdate<FolderInfoModel> updates, CancellationToken cancellationToken = default)
    {
        return folderSystemService.UpdateAsync(key, updates, cancellationToken);
    }

    public Task<Result<bool>> CreateAsync(FolderInfoModel model, CancellationToken cancellationToken = default)
    {
        return folderSystemService.CreateAsync(model, cancellationToken);
    }

    public Task<Result<bool>> CreateAsync(IReadOnlyCollection<FolderInfoModel> models, CancellationToken cancellationToken = default)
    {
        return folderSystemService.CreateAsync(models, cancellationToken);
    }

    public Task<(bool, string)> UpdateAsync(FolderInfoModel model, CancellationToken cancellationToken = default)
    {
        return folderSystemService.ReplaceAsync(model, cancellationToken);
    }

    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<FolderInfoModel> models, CancellationToken cancellationToken = default)
    {
        return folderSystemService.ReplaceAsync(models, cancellationToken);
    }

    public async Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        var folder = Get(key);
        if (folder == null) return (false, AppLang.Folder_could_not_be_found);

        if (folder.AbsolutePath == "/root") return (false, AppLang.Could_not_delete_root_folder);

        if (folder.Type != FolderContentType.DeletedFolder)
        {
            await UpdateAsync(key, new FieldUpdate<FolderInfoModel>()
            {
                { model => model.Type, FolderContentType.DeletedFolder },
                { model => model.PreviousType, folder.Type }
            }, cancelToken);
            return (true, AppLang.Delete_successfully);
        }

        var res = await folderSystemService.DeleteAsync(key, cancelToken);
        if (res.Item1)
        {
            await sequenceBackgroundTaskQueue.QueueBackgroundWorkItemAsync(async serverToken1 =>
            {
                List<FolderInfoModel> folders = new List<FolderInfoModel>();
                var folderList = folderSystemService.WhereAsync(x => x.RootFolder == key, serverToken1, model => model.Id);
                await foreach (var fol in folderList)
                {
                    folders.Add(fol);
                }

                logger.LogInformation($"Deleting {folders.Count:N0} folders");
                foreach (var folderStack in folders)
                {
                    await sequenceBackgroundTaskQueue.QueueBackgroundWorkItemAsync(async (serverToken) =>
                    {
                        var folderId = folderStack.Id.ToString();
                        await DeleteAsync(folderId, serverToken);
                        await DeleteAsync(folderId, serverToken);
                    }, serverToken1);
                }
            }, cancelToken);

            await sequenceBackgroundTaskQueue.QueueBackgroundWorkItemAsync(async serverToken1 =>
            {
                List<FileInfoModel> files = new List<FileInfoModel>();
                var cursor = fileSystemService.Where(x => x.RootFolder == key, serverToken1, model => model.Id);
                await foreach (var file in cursor)
                {
                    files.Add(file);
                }

                logger.LogInformation($"Deleting {files.Count:N0} files");
                foreach (var file in files)
                {
                    await sequenceBackgroundTaskQueue.QueueBackgroundWorkItemAsync(async serverToken =>
                    {
                        var fileId = file.Id.ToString();
                        await fileSystemService.DeleteAsync(fileId, serverToken);
                        await fileSystemService.DeleteAsync(fileId, serverToken);
                    }, serverToken1);
                }
            }, cancelToken);
        }

        return res;
    }

    public FolderInfoModel? Get(string username, string absoblutePath)
    {
        return folderSystemService.Get(username, absoblutePath);
    }

    public List<FolderInfoModel> GetFolderBloodLine(string folderId)
    {
        List<string> allPath = [];

        var rootFolder = Get(folderId);
        if (rootFolder == null)
            return [];

        var allFolderName = rootFolder.RelativePath.Split('/').Where(x => !string.IsNullOrEmpty(x)).ToArray();
        for (var i = 0; i < allFolderName.Length; i++)
        {
            var path = $"/{allFolderName[i]}";
            if (i > 0)
                allPath.Add(allPath[i - 1] + path);
            else
                allPath.Add(path);
        }

        List<FolderInfoModel> folderInfoModels = [];
        foreach (var path in allPath)
        {
            var folder = Get(rootFolder.OwnerUsername, path);
            if (folder != null)
                folderInfoModels.Add(folder);
        }

        return folderInfoModels;
    }

    public FolderInfoModel? GetRoot(string username)
    {
        var user = GetUser(username);
        if (user == null) return default;

        var folder = Get(user.UserName, "/root");
        if (folder == null)
        {
            folder = new FolderInfoModel
            {
                RelativePath = "/root",
                AbsolutePath = "/root",
                FolderName = "Home",
                ModifiedTime = DateTime.UtcNow,
                OwnerUsername = user.UserName
            };
            var res = CreateAsync(folder).Result;
            if (res.IsSuccess)
            {
                userService.UpdateAsync(user);
                return Get(folder.Id.ToString());
            }

            return default;
        }

        return folder;
    }

    public IAsyncEnumerable<FolderInfoModel> GetContentFormParentFolderAsync(string id, int pageNumber, int pageSize, CancellationToken cancellationToken = default, params Expression<Func<FolderInfoModel, object>>[] fieldsToFetch)
    {
        return folderSystemService.GetContentFormParentFolderAsync(id, pageNumber, pageSize, cancellationToken, fieldsToFetch);
    }

    public IAsyncEnumerable<FolderInfoModel> GetContentFormParentFolderAsync(Expression<Func<FolderInfoModel, bool>> predicate, int pageNumber, int pageSize, CancellationToken cancellationToken = default, params Expression<Func<FolderInfoModel, object>>[] fieldsToFetch)
    {
        return folderSystemService.GetContentFormParentFolderAsync(predicate, pageNumber, pageSize, cancellationToken, fieldsToFetch);
    }

    public IAsyncEnumerable<FolderInfoModel> Search(string queryString, string? username, int limit = 10, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> CreateFileAsync(FolderInfoModel folder, FileInfoModel file, CancellationToken cancellationTokenSource = default)
    {
        var dateString = DateTime.UtcNow.ToString("dd-MM-yyy");

        var filePath = Path.Combine(dateString, Path.GetRandomFileName());

        file.AbsolutePath = filePath;
        file.RootFolder = folder.Id.ToString();
        file.RelativePath = Path.Combine(folder.RelativePath, file.FileName);
        var res = await fileSystemService.CreateAsync(file, cancellationTokenSource);
        if (res.IsSuccess)
        {
            var folderUpdateResult = await UpdateAsync(folder, cancellationTokenSource);
            if (!folderUpdateResult.Item1)
                return folderUpdateResult;
        }

        return (res.IsSuccess, res.Message);
    }

    public async Task<(bool, string)> CreateFileAsync(string userName, FileInfoModel file, CancellationToken cancellationToken = default)
    {
        var user = userService.Get(userName) ?? userService.GetAnonymous();
        var folder = GetRoot(user.UserName)!;
        return await CreateFileAsync(folder, file, cancellationToken);
    }

    public async Task<(bool, string)> CreateFolder(string userName, string targetFolderId, string folderName)
    {
        var folder = Get(targetFolderId);
        if (folder == null) return (false, AppLang.Folder_could_not_be_found);
        var user = GetUser(userName);
        if (user == null) return (false, AppLang.User_is_not_exists);

        var newFolder = new FolderInfoModel
        {
            FolderName = folderName,
            RelativePath = folder.RelativePath + $"/{folderName}",
            AbsolutePath = folder.RelativePath + $"/{folderName}",
            OwnerUsername = user.UserName,
            RootFolder = targetFolderId,
            Type = FolderContentType.Folder
        };

        if (Get(user.UserName, newFolder.RelativePath) != null)
            return (false, AppLang.Folder_already_exists);

        var createNewFolderResult = await CreateAsync(newFolder);
        if (createNewFolderResult.IsSuccess)
        {
            await UpdateAsync(folder);
        }

        return (createNewFolderResult.IsSuccess, createNewFolderResult.Message);
    }

    public async Task<(bool, string)> CreateFolder(RequestNewFolderModel request)
    {
        FolderNameFluentValidator validator = new();
        var validationResult = await validator.ValidateAsync(request.NewFolder.FolderName);
        if (!validationResult.IsValid)
            foreach (var error in validationResult.Errors)
                return (false, error?.ErrorMessage ?? string.Empty);

        var folderRoot = Get(request.RootId);
        if (folderRoot == null) return (false, AppLang.Root_folder_could_not_be_found);


        if (!string.IsNullOrEmpty(folderRoot.Password))
            if (folderRoot.Password != request.RootPassWord.ComputeSha256Hash())
                return (false, AppLang.Passwords_do_not_match_);

        request.NewFolder.RelativePath = folderRoot.RelativePath + '/' + request.NewFolder.FolderName;
        request.NewFolder.AbsolutePath = request.NewFolder.RelativePath;
        request.NewFolder.RootFolder = request.RootId;
        request.NewFolder.ModifiedTime = DateTime.Now;
        request.NewFolder.OwnerUsername = folderRoot.OwnerUsername;


        if (string.IsNullOrEmpty(request.NewFolder.ModifiedUserName))
            request.NewFolder.ModifiedUserName = folderRoot.OwnerUsername;

        if (Get(folderRoot.OwnerUsername, request.NewFolder.RelativePath) != null)
            return (false, AppLang.Folder_already_exists);

        var res = await CreateAsync(request.NewFolder);
        if (res.IsSuccess)
        {
            var result = await UpdateAsync(folderRoot);
            if (result.Item1)
                return (true, AppLang.Create_successfully);
        }

        return (false, AppLang.Create_failed);
    }

    public long GetFileSize(Expression<Func<FileInfoModel, bool>> predicate, CancellationToken cancellationTokenSource = default)
    {
        return fileSystemService.GetFileSize(predicate, cancellationTokenSource);
    }

    public Task<long> GetFolderByteSize(Expression<Func<FolderInfoModel, bool>> predicate, CancellationToken cancellationTokenSource = default)
    {
        long total = 0;
        return Task.FromResult(total);
    }

    public Task<(long, long)> GetFolderContentsSize(Expression<Func<FolderInfoModel, bool>> predicate, CancellationToken cancellationTokenSource = default)
    {
        long totalFolders = 0;
        long totalFiles = 0;
        return Task.FromResult((totalFolders, totalFiles));
    }

    public async Task<FolderRequest> GetFolderRequestAsync(string folderId, Expression<Func<FolderInfoModel, bool>> folderPredicate, Expression<Func<FileInfoModel, bool>> filePredicate, int pageSize, int pageNumber, bool forceLoad = false, CancellationToken cancellationToken = default)
    {
        StringBuilder keyBuilder = new StringBuilder();
        keyBuilder.Append(folderPredicate.GetCacheKey());
        keyBuilder.Append(filePredicate.GetCacheKey());
        keyBuilder.Append(pageSize.ToString());
        keyBuilder.Append(pageNumber.ToString());
        var key = keyBuilder.ToString();

        try
        {
            if (forceLoad)
            {
                var result = await GetFolderRequest(folderId, folderPredicate, filePredicate, pageSize, pageNumber, cancellationToken);
                _cacheKeyManager.Set(key, result, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10) });
                return result;
            }
            else
            {
                var result = await _cacheKeyManager.GetOrCreateAsync(key, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10);
                    return await GetFolderRequest(folderId, folderPredicate, filePredicate, pageSize, pageNumber, cancellationToken);
                }) ?? new();
                return result;
            }
        }
        finally
        {
            var minPageIndex = Math.Max(pageNumber - 10, 1);
            var maxPageIndex = minPageIndex + 20;
            for (int i = minPageIndex; i < maxPageIndex; i++)
            {
                keyBuilder.Clear();
                keyBuilder.Append(folderPredicate.GetCacheKey());
                keyBuilder.Append(filePredicate.GetCacheKey());
                keyBuilder.Append(pageSize.ToString());
                keyBuilder.Append(i.ToString());
                key = keyBuilder.ToString();
                var i1 = i;
                await parallelBackgroundTaskQueue.QueueBackgroundWorkItemAsync(async token => await _cacheKeyManager.GetOrCreateAsync(key, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10);
                    return await GetFolderRequest(folderId, folderPredicate, filePredicate, pageSize, i1, token);
                }), cancellationToken);
            }
        }
    }

    public async Task<FolderRequest> GetDeletedContentAsync(string? userName, int pageSize, int page, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        page -= 1;

        var user = GetUser(userName);
        if (user == null) return new FolderRequest();

        var fieldsFolderToFetch = new Expression<Func<FolderInfoModel, object>>[]
        {
            model => model.Id,
            model => model.FolderName,
            model => model.Type,
            model => model.RootFolder,
            model => model.Icon,
            model => model.RelativePath,
            model => model.ModifiedTime,
            model => model.CreateDate
        };

        var folderList = Where(x => x.OwnerUsername == user.UserName, cancellationToken, fieldsFolderToFetch);

        ConcurrentBag<FileInfoModel> files = new ConcurrentBag<FileInfoModel>();
        ConcurrentBag<FolderInfoModel> folders = new ConcurrentBag<FolderInfoModel>();

        await Parallel.ForEachAsync(folderList, cancellationToken, async (folder, cancellationTokenSource) =>
        {
            if (folder.Type == FolderContentType.DeletedFolder)
                folders.Add(folder);

            var rootFolder = folder.Id.ToString();
            var fieldToFetch = new Expression<Func<FileInfoModel, object>>[]
            {
                model => model.Id,
                model => model.FileName,
                model => model.Status,
                model => model.RootFolder,
                model => model.ContentType,
                model => model.RelativePath,
                model => model.ModifiedTime,
                model => model.CreatedDate
            };

            var fileCursor = fileSystemService.Where(x => x.Status == FileStatus.DeletedFile && x.RootFolder == rootFolder, cancellationTokenSource, fieldToFetch);
            await foreach (var file in fileCursor)
            {
                files.Add(file);
            }
        });

        var totalFolder = folders.Count;
        var totalFiles = files.Count;
        var totalFolderPages = (int)Math.Ceiling(totalFolder / (double)pageSize);
        var totalFilePages = (int)Math.Ceiling(totalFiles / (double)pageSize);

        return new FolderRequest()
        {
            Files = files.Skip(page * pageSize).Take(pageSize).ToArray(),
            Folders = folders.Skip(page * pageSize).Take(pageSize).ToArray(),
            TotalFilePages = totalFilePages,
            TotalFolderPages = totalFolderPages,
        };
    }

    public async Task<Result<FolderInfoModel?>> InsertMediaContent(string path, CancellationToken cancellationToken = default)
    {
        await path.CheckFileSizeStable();
        var extension = Path.GetExtension(path);
        string[] allowedExtensions = [".mp4", ".mkv"];
        if (!allowedExtensions.Contains(extension))
        {
#if DEBUG
            logger.LogWarning($"File {path} is in an invalid format");
#endif
            return Result<FolderInfoModel?>.Failure("File is in an invalid format", ErrorType.Validation);
        }

        var workDir = path.Replace(Path.GetFileName(path), "");

        var outputDir = Path.Combine(workDir, Path.GetFileNameWithoutExtension(path));

        if (!Directory.Exists(outputDir))
        {
            logger.LogInformation($"Output directory doesn't exist: {outputDir}");
            return Result<FolderInfoModel?>.Failure("File is in an invalid format", ErrorType.Validation);
        }

        var m3U8Files = Directory.GetFiles(outputDir, "playlist.m3u8", SearchOption.AllDirectories).ToArray();

        var rootVideoFolder = Get("Anonymous", "/root/Videos");

        if (rootVideoFolder == null)
        {
            logger.LogError($"Root video folder was not found. Skipping {path}.");
            return Result<FolderInfoModel?>.Failure("File is in an invalid format", ErrorType.Validation);
        }

        var storageFolder = new FolderInfoModel()
        {
            FolderName = Path.GetFileName(path),
            Type = FolderContentType.SystemFolder,
            RootFolder = rootVideoFolder.Id.ToString()
        };

        var requestNewFolder = new RequestNewFolderModel()
        {
            NewFolder = storageFolder,
            RootId = rootVideoFolder.Id.ToString(),
        };

        await CreateFolder(requestNewFolder);

        foreach (var file in m3U8Files)
        {
            await ReadM3U8Files(storageFolder, file, cancellationToken);

            var fileInfo = new FileInfoModel()
            {
                FileName = path,
                ContentType = "video/mp4",
                RootFolder = requestNewFolder.RootId,
                ModifiedTime = DateTime.UtcNow,
                CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };
            await CreateFileAsync(storageFolder, fileInfo, cancellationToken);
            logger.LogInformation($"Add {fileInfo.Id}");
        }

        return Result<FolderInfoModel?>.Success(storageFolder);
    }

    [Experimental("SKEXP0020")]
    public  Task RequestIndexAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<List<SearchScore<VectorRecord>>> SearchRagFromAllDb(string query, int count, CancellationToken cancellationToken = default)
    {
        return _vectorDbs.RagSearch(query, count, cancellationToken);
    }

    public async Task<Result<string>> Decompress(string fileId)
    {
        var file = fileSystemService.Get(fileId);
        if (file == null) return Result<string>.Failure("File not found", ErrorType.NotFound);

        var raidPath = await raidService.GetDataBlockPaths(file.AbsolutePath);
        if (raidPath == null) return Result<string>.Failure("File not found", ErrorType.NotFound);

        var rootFolder = Get(file.RootFolder)!;

        await using Raid5Stream raid5Stream = new Raid5Stream(raidPath.Files, file.FileSize, raidPath.StripeSize, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var archive = new ZipArchive(raid5Stream, ZipArchiveMode.Read, leaveOpen: true);
        foreach (var entry in archive.Entries)
        {
            string destinationPath = Path.Combine(rootFolder.AbsolutePath, entry.FullName);

            // Ensure the directory exists for this entry
            // if (!Directory.Exists(directoryPath))
            // {
            //     Directory.CreateDirectory(directoryPath);
            // }
            // // Skip directories
            // if (string.IsNullOrEmpty(entry.Name))
            // {
            //     continue;
            // }

            logger.LogInformation($"Extracting: {entry.FullName}");

            // Open the entry as a stream
            await using var entryStream = entry.Open();
            var fileCreateResult = await CreateFileAsync(rootFolder, new FileInfoModel()
            {
                FileName = entry.Name,
                RootFolder = rootFolder.Id.ToString()
            });
            if (fileCreateResult.Item1)
            {
                var writeResult = await raidService.WriteDataAsync(entryStream, file.AbsolutePath);
                await UpdateFilePropertiesAfterUpload(file, writeResult, "", entry.Name);
            }
        }

        return Result<string>.SuccessWithMessage("Successfully extracted file", "");
    }


    #region Private Mothods

    [Experimental("SKEXP0020")]
    private async Task Request(string userName, FileInfoModel file, CancellationToken cancellationToken = default)
    {
        long limit = 2147483648;
        if (file.FileSize > limit)
        {
            logger.LogWarning($"{file.Id} File is too large to be processed. Skipping.");
            return;
        }

        using MemoryStream stream = new();
        await raidService.ReadGetDataAsync(stream, file.AbsolutePath, cancellationToken);
        stream.Seek(0, SeekOrigin.Begin);

        var collection = await GetOrInitCollection(userName);
        var description = await collection.GenerateImageDescription(stream, cancellationToken);
        if (string.IsNullOrEmpty(description))
        {
            logger.LogError($"Description is null for {file.AbsolutePath}");
            return;
        }

        var vector = await collection.GenerateVectorsFromDescription(description, cancellationToken);
        var model = new VectorRecord()
        {
            Key = file.Id.ToString(),
            Vector = vector,
        };
        await collection.AddNewRecordAsync(model, cancellationToken);
        await fileSystemService.UpdateAsync(file.Id.ToString(), new FieldUpdate<FileInfoModel>()
        {
            { x => x.Description, description },
        }, cancellationToken);
    }

    private async Task<string> ReadM3U8Files(FolderInfoModel folderStorage, string path, CancellationToken cancellationToken = default)
    {
        var playListContents = await File.ReadAllLinesAsync(path, cancellationToken);
        var fileName = Path.GetFileName(path);
        var dir = path.Replace(fileName, "");

        for (int i = 0; i < playListContents.Length; i++)
        {
            var lineText = playListContents[i];
            if (lineText.EndsWith(".m3u8"))
            {
                playListContents[i] = await ReadM3U8Files(folderStorage, Path.Combine(dir, lineText), cancellationToken);
            }

            if (lineText.EndsWith(".ts") || lineText.EndsWith(".vtt"))
            {
                var fileInfo = new FileInfoModel()
                {
                    FileName = lineText,
                    ContentType = lineText.EndsWith(".ts") ? "video/MP2T" : "text/vtt",
                    Classify = FileClassify.M3U8FileSegment,
                };
                await CreateFileAsync(folderStorage, fileInfo, cancellationToken);
                playListContents[i] = fileInfo.Id.ToString();
                await using var stream = new FileStream(Path.Combine(dir, lineText), FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
                var writeResult = await raidService.WriteDataAsync(stream, fileInfo.AbsolutePath, cancellationToken);

                await fileSystemService.UpdateAsync(playListContents[i], new FieldUpdate<FileInfoModel>()
                {
                    { x => x.FileSize, writeResult.TotalByteWritten },
                    { x => x.Checksum, writeResult.CheckSum }
                }, cancellationToken);
                playListContents[i] += lineText.EndsWith(".ts") ? ".ts" : ".vtt";
            }
        }

        var m3U8FileInfo = new FileInfoModel()
        {
            FileName = path,
            ContentType = "application/x-mpegURL",
            Classify = FileClassify.M3U8File
        };
        m3U8FileInfo.AbsolutePath = m3U8FileInfo.Id.ToString();
        await CreateFileAsync(folderStorage, m3U8FileInfo, cancellationToken);


        await using MemoryStream ms = new MemoryStream();
        await using StreamWriter sw = new StreamWriter(ms);
        foreach (var line in playListContents)
        {
            await sw.WriteLineAsync(line);
        }

        await sw.FlushAsync(cancellationToken);

        ms.Seek(0, SeekOrigin.Begin);

        var m3U8 = fileSystemService.Get(m3U8FileInfo.Id.ToString());
        if (m3U8 == null)
        {
            logger.LogInformation($"M3U8: {m3U8FileInfo.Id.ToString()} ERROR");
            return string.Empty;
        }

        var m3U8WriteResult = await raidService.WriteDataAsync(ms, m3U8.AbsolutePath, cancellationToken);

        await fileSystemService.UpdateAsync(m3U8FileInfo.Id.ToString(), new FieldUpdate<FileInfoModel>()
        {
            { x => x.FileSize, m3U8WriteResult.TotalByteWritten },
            { x => x.Checksum, m3U8WriteResult.CheckSum }
        }, cancellationToken);

        return m3U8FileInfo.Id + ".m3u8";
    }


    private async Task<FolderRequest> GetFolderRequest(string folderId, Expression<Func<FolderInfoModel, bool>> folderPredicate, Expression<Func<FileInfoModel, bool>> filePredicate, int pageSize, int pageNumber, CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageNumber -= 1;

        var folderList = new List<FolderInfoModel>();
        var fileList = new List<FileInfoModel>();

        var totalFolderDoc = await GetDocumentSizeAsync(folderPredicate, cancellationToken);
        var totalFileDoc = await fileSystemService.GetDocumentSizeAsync(filePredicate, cancellationToken);
        var totalFilePages = Math.Ceiling((double)totalFileDoc / pageSize);
        var totalFolderPages = Math.Ceiling((double)totalFolderDoc / pageSize);

        var fieldsFolderToFetch = new Expression<Func<FolderInfoModel, object>>[]
        {
            model => model.Id,
            model => model.FolderName,
            model => model.Type,
            model => model.RootFolder,
            model => model.Icon,
            model => model.RelativePath,
            model => model.ModifiedTime,
            model => model.CreateDate
        };
        var fieldsFileToFetch = new Expression<Func<FileInfoModel, object>>[]
        {
            model => model.Id,
            model => model.FileName,
            model => model.Status,
            model => model.RootFolder,
            model => model.ContentType,
            model => model.RelativePath,
            model => model.ModifiedTime,
            model => model.CreatedDate
        };

        await foreach (var m in GetContentFormParentFolderAsync(folderPredicate, pageNumber, pageSize, cancellationToken, fieldsFolderToFetch))
        {
            folderList.Add(m);
        }

        await foreach (var m in fileSystemService.GetContentFormParentFolderAsync(filePredicate, pageNumber, pageSize, cancellationToken, fieldsFileToFetch))
        {
            fileList.Add(m);
        }

        return new FolderRequest()
        {
            Files = fileList.ToArray(),
            Folders = folderList.ToArray(),
            TotalFolderPages = (int)totalFolderPages,
            TotalFilePages = (int)totalFilePages,
            BloodLines = GetFolderBloodLine(folderId).ToArray()
        };
    }

    public async Task UpdateFilePropertiesAfterUpload(FileInfoModel file, RedundantArrayOfIndependentDisks.WriteDataResult saveResult, string contentType, string trustedFileNameForDisplay)
    {
        file.FileSize = saveResult.TotalByteWritten;
        file.ContentType = saveResult.ContentType;
        file.Checksum = saveResult.CheckSum;

        if (string.IsNullOrEmpty(file.ContentType))
        {
            file.ContentType = contentType;
        }
        else if (file.ContentType == "application/octet-stream")
        {
            file.ContentType = Path.GetExtension(trustedFileNameForDisplay).GetMimeTypeFromExtension();
        }

        var updateResult = await fileSystemService.UpdateAsync(file.Id.ToString(), GetFileFieldUpdates(file));
        await thumbnailService.AddThumbnailRequest(file.Id.ToString());

        if (!updateResult.Item1)
        {
            logger.LogError(updateResult.Item2);
        }

        static FieldUpdate<FileInfoModel> GetFileFieldUpdates(FileInfoModel file)
        {
            return new FieldUpdate<FileInfoModel>
            {
                { x => x.FileSize, file.FileSize },
                { x => x.ContentType, file.ContentType },
                { x => x.Checksum, file.Checksum }
            };
        }
    }

    /// <summary>
    ///     Get user. if user by the name that is not found return Anonymous user
    /// </summary>
    /// <param name="username"></param>
    /// <returns></returns>
    public UserModel? GetUser(string? username)
    {
        if (string.IsNullOrEmpty(username)) username = "Anonymous";
        var user = userService.Get(username);
        return user;
    }

    #endregion

    public void Dispose()
    {
        foreach (var keyValuePair in _vectorDbs)
        {
            keyValuePair.Value.Dispose();
        }

        _cacheKeyManager.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var keyValuePair in _vectorDbs)
        {
            await keyValuePair.Value.DisposeAsync();
        }
    }
}