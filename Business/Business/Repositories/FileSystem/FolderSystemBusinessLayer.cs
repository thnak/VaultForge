using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Text;
using Business.Business.Interfaces.FileSystem;
using Business.Business.Interfaces.User;
using Business.Data;
using Business.Data.Interfaces.FileSystem;
using Business.Models;
using Business.Services;
using Business.Utils.Helper;
using Business.Utils.StringExtensions;
using BusinessModels.General.EnumModel;
using BusinessModels.General.Results;
using BusinessModels.General.SettingModels;
using BusinessModels.People;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using BusinessModels.Validator.Folder;
using BusinessModels.WebContent.Drive;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Protector.Utils;

namespace Business.Business.Repositories.FileSystem;

public class FolderSystemBusinessLayer(
    IFolderSystemDatalayer folderSystemService,
    IFileSystemBusinessLayer fileSystemService,
    IUserBusinessLayer userService,
    IOptions<AppSettings> options,
    ILogger<FolderSystemBusinessLayer> logger,
    IMemoryCache memoryCache,
    RedundantArrayOfIndependentDisks raidService)
    : IFolderSystemBusinessLayer, IDisposable
{
    private IFolderSystemDatalayer FolderSystemService { get; set; } = folderSystemService;
    private IFileSystemBusinessLayer FileSystemService { get; set; } = fileSystemService;
    private IUserBusinessLayer UserService { get; set; } = userService;

    private ILogger<FolderSystemBusinessLayer> Logger { get; set; } = logger;
    private readonly string _workingDir = options.Value.FileFolder;
    private readonly CacheKeyManager _cacheKeyManager = new(memoryCache, nameof(FolderSystemBusinessLayer));


    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(nameof(FolderSystemBusinessLayer));
        stringBuilder.Append(nameof(GetDocumentSizeAsync));

        var key = stringBuilder.ToString();
        var value = _cacheKeyManager.GetOrCreateAsync(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            return FolderSystemService.GetDocumentSizeAsync(cancellationToken);
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
            return FolderSystemService.GetDocumentSizeAsync(predicate, cancellationToken);
        });
        return value;
    }

    public IAsyncEnumerable<FolderInfoModel> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        return FolderSystemService.Search(queryString, limit, cancellationToken);
    }

    public IAsyncEnumerable<FolderInfoModel> FindAsync(FilterDefinition<FolderInfoModel> filter, CancellationToken cancellationToken = default)
    {
        return FolderSystemService.FindAsync(filter, cancellationToken);
    }

    public IAsyncEnumerable<FolderInfoModel> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        return FolderSystemService.FindAsync(keyWord, cancellationToken);
    }

    public IAsyncEnumerable<FolderInfoModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<FolderInfoModel, object>>[] fieldsToFetch)
    {
        return FolderSystemService.FindProjectAsync(keyWord, limit, cancellationToken, fieldsToFetch);
    }

    public IAsyncEnumerable<FolderInfoModel> Where(Expression<Func<FolderInfoModel, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<FolderInfoModel, object>>[] fieldsToFetch)
    {
        return FolderSystemService.Where(predicate, cancellationToken, fieldsToFetch);
    }

    public FolderInfoModel? Get(string key)
    {
        return FolderSystemService.Get(key);
    }

    public IAsyncEnumerable<FolderInfoModel?> GetAsync(List<string> keys, CancellationToken cancellationToken = default)
    {
        return FolderSystemService.GetAsync(keys, cancellationToken);
    }

    public Task<(FolderInfoModel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        return FolderSystemService.GetAllAsync(page, size, cancellationToken);
    }

    public IAsyncEnumerable<FolderInfoModel> GetAllAsync(CancellationToken cancellationToken)
    {
        return FolderSystemService.GetAllAsync(cancellationToken);
    }

    public Task<(bool, string)> UpdateAsync(string key, FieldUpdate<FolderInfoModel> updates, CancellationToken cancellationToken = default)
    {
        return FolderSystemService.UpdateAsync(key, updates, cancellationToken);
    }

    public Task<Result<bool>> CreateAsync(FolderInfoModel model, CancellationToken cancellationToken = default)
    {
        return FolderSystemService.CreateAsync(model, cancellationToken);
    }

    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<FolderInfoModel> models, CancellationToken cancellationToken = default)
    {
        return FolderSystemService.CreateAsync(models, cancellationToken);
    }

    public Task<(bool, string)> UpdateAsync(FolderInfoModel model, CancellationToken cancellationToken = default)
    {
        return FolderSystemService.ReplaceAsync(model, cancellationToken);
    }

    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<FolderInfoModel> models, CancellationToken cancellationToken = default)
    {
        return FolderSystemService.ReplaceAsync(models, cancellationToken);
    }

    public async Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        var folder = Get(key);
        if (folder == null) return (false, AppLang.Folder_could_not_be_found);

        if (folder.AbsolutePath == "/root") return (false, AppLang.Could_not_delete_root_folder);
        if (folder.Type == FolderContentType.SystemFolder) return (false, AppLang.Folder_could_not_be_found);

        if (folder.Type != FolderContentType.DeletedFolder)
        {
            await UpdateAsync(key, new FieldUpdate<FolderInfoModel>()
            {
                { model => model.Type, FolderContentType.DeletedFolder },
                { model => model.PreviousType, folder.Type }
            }, cancelToken);
            return (true, AppLang.Delete_successfully);
        }

        var res = await FolderSystemService.DeleteAsync(key, cancelToken);
        if (res.Item1)
        {
            _ = Task.Run(async () =>
            {
                var folderList = FolderSystemService.Where(x => x.RootFolder == key, default, model => model.Id);
                await foreach (var fol in folderList)
                {
                    var folderId = fol.Id.ToString();
                    await DeleteAsync(folderId);
                    await DeleteAsync(folderId);
                }
            });

            _ = Task.Run(async () =>
            {
                var cursor = FileSystemService.Where(x => x.RootFolder == key, default, model => model.Id);
                await foreach (var file in cursor)
                {
                    var fileId = file.Id.ToString();
                    await FileSystemService.DeleteAsync(fileId);
                    await FileSystemService.DeleteAsync(fileId);
                }
            });
        }

        return res;
    }

    public FolderInfoModel? Get(string username, string absoblutePath)
    {
        var user = GetUser(username);
        return FolderSystemService.Get(user?.UserName ?? string.Empty, absoblutePath);
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
                UserService.UpdateAsync(user);
                return Get(folder.Id.ToString());
            }

            return default;
        }

        return folder;
    }

    public IAsyncEnumerable<FolderInfoModel> GetContentFormParentFolderAsync(string id, int pageNumber, int pageSize, CancellationToken cancellationToken = default, params Expression<Func<FolderInfoModel, object>>[] fieldsToFetch)
    {
        return FolderSystemService.GetContentFormParentFolderAsync(id, pageNumber, pageSize, cancellationToken, fieldsToFetch);
    }

    public IAsyncEnumerable<FolderInfoModel> GetContentFormParentFolderAsync(Expression<Func<FolderInfoModel, bool>> predicate, int pageNumber, int pageSize, CancellationToken cancellationToken = default, params Expression<Func<FolderInfoModel, object>>[] fieldsToFetch)
    {
        return FolderSystemService.GetContentFormParentFolderAsync(predicate, pageNumber, pageSize, cancellationToken, fieldsToFetch);
    }

    public IAsyncEnumerable<FolderInfoModel> Search(string queryString, string? username, int limit = 10, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> CreateFileAsync(FolderInfoModel folder, FileInfoModel file, CancellationToken cancellationTokenSource = default)
    {
        var dateString = DateTime.UtcNow.ToString("dd-MM-yyy");

        var filePath = Path.Combine(_workingDir, dateString, Path.GetRandomFileName());
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        file.AbsolutePath = filePath;
        file.RootFolder = folder.Id.ToString();
        file.RelativePath = folder.RelativePath + $"/{file.FileName}";
        var res = await FileSystemService.CreateAsync(file, cancellationTokenSource);
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
        var user = UserService.Get(userName) ?? UserService.GetAnonymous();
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
        return FileSystemService.GetFileSize(predicate, cancellationTokenSource);
    }

    public async Task<long> GetFolderByteSize(Expression<Func<FolderInfoModel, bool>> predicate, CancellationToken cancellationTokenSource = default)
    {
        var folders = FolderSystemService.Where(predicate, cancellationTokenSource);
        long total = 0;
        await foreach (var folder in folders)
        foreach (var content in folder.Contents)
            if (content is { Type: FolderContentType.File or FolderContentType.HiddenFile })
            {
                var file = FileSystemService.Get(content.Id);
                if (file == null)
                {
                    Logger.LogInformation($@"[Error] file by id {content} can not be found");
                    continue;
                }

                total += file.FileSize;
            }
            else
            {
                var id = ObjectId.Parse(content.Id);
                total += await GetFolderByteSize(e => e.Id == id, cancellationTokenSource);
            }

        return total;
    }

    public async Task<(long, long)> GetFolderContentsSize(Expression<Func<FolderInfoModel, bool>> predicate, CancellationToken cancellationTokenSource = default)
    {
        var folders = FolderSystemService.Where(predicate, cancellationTokenSource);
        long totalFolders = 0;
        long totalFiles = 0;
        await foreach (var folder in folders)
        foreach (var content in folder.Contents)
            if (content is { Type: FolderContentType.File or FolderContentType.HiddenFile })
            {
                totalFiles++;
            }
            else
            {
                var id = ObjectId.Parse(content.Id);
                var (numFolders, numFiles) = await GetFolderContentsSize(e => e.Id == id, cancellationTokenSource);
                totalFiles += numFiles;
                totalFolders += numFolders;
            }

        return (totalFolders, totalFiles);
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
                _cacheKeyManager.Set(key, result, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });
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
                _ = _cacheKeyManager.GetOrCreateAsync(key, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10);
                    return await GetFolderRequest(folderId, folderPredicate, filePredicate, pageSize, i1, cancellationToken);
                }).ConfigureAwait(false);
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
                model => model.Thumbnail,
                model => model.Status,
                model => model.RootFolder,
                model => model.ContentType,
                model => model.RelativePath,
                model => model.ModifiedTime,
                model => model.CreatedDate
            };

            var fileCursor = FileSystemService.Where(x => x.Status == FileStatus.DeletedFile && x.RootFolder == rootFolder, cancellationTokenSource, fieldToFetch);
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
            return Result<FolderInfoModel>.Failure("File is in an invalid format", ErrorType.Validation);
        }

        var workDir = path.Replace(Path.GetFileName(path), "");

        var outputDir = Path.Combine(workDir, Path.GetFileNameWithoutExtension(path));

        if (!Directory.Exists(outputDir))
        {
            logger.LogInformation($"Output directory doesn't exist: {outputDir}");
            return Result<FolderInfoModel>.Failure("File is in an invalid format", ErrorType.Validation);
        }

        var m3U8Files = Directory.GetFiles(outputDir, "playlist.m3u8", SearchOption.AllDirectories).ToArray();

        var rootVideoFolder = Get("Anonymous", "/root/Videos");

        if (rootVideoFolder == null)
        {
            logger.LogError($"Root video folder was not found. Skipping {path}.");
            return Result<FolderInfoModel>.Failure("File is in an invalid format", ErrorType.Validation);
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
            var resId = await ReadM3U8Files(storageFolder, file, cancellationToken);

            var fileInfor = new FileInfoModel()
            {
                FileName = path,
                ContentType = "video/mp4",
                ExtendResource =
                [
                    new FileContents()
                    {
                        Id = resId,
                        Classify = FileClassify.M3U8File,
                    }
                ],
                RootFolder = requestNewFolder.RootId,
                ModifiedTime = DateTime.UtcNow,
                CreatedDate = DateTime.Today
            };
            await CreateFileAsync(storageFolder, fileInfor, cancellationToken);
            logger.LogInformation($"Add {fileInfor.Id}");
        }

        return Result<FolderInfoModel?>.Success(storageFolder);
    }


    #region Private Mothods

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
                await using var stream = new FileStream(Path.Combine(dir, lineText), FileMode.Open, FileAccess.Read,
                    FileShare.Read, 4096, FileOptions.SequentialScan);
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
        var totalFileDoc = await FileSystemService.GetDocumentSizeAsync(filePredicate, cancellationToken);
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
            model => model.Thumbnail,
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

        await foreach (var m in FileSystemService.GetContentFormParentFolderAsync(filePredicate, pageNumber, pageSize, cancellationToken, fieldsFileToFetch))
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

    /// <summary>
    ///     Get user. if user by the name that is not found return Anonymous user
    /// </summary>
    /// <param name="username"></param>
    /// <returns></returns>
    public UserModel? GetUser(string? username)
    {
        if (string.IsNullOrEmpty(username)) username = "Anonymous";
        var user = UserService.Get(username);
        return user;
    }

    #endregion

    public void Dispose()
    {
        _cacheKeyManager.Dispose();
    }
}