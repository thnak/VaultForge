using System.Linq.Expressions;
using Business.Business.Interfaces.FileSystem;
using Business.Business.Interfaces.User;
using Business.Data.Interfaces.FileSystem;
using Business.Models;
using BusinessModels.General;
using BusinessModels.General.EnumModel;
using BusinessModels.People;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using BusinessModels.Validator.Folder;
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
    ILogger<FolderSystemBusinessLayer> logger) : IFolderSystemBusinessLayer
{
    private readonly string _workingDir = options.Value.FileFolder;

    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        return folderSystemService.GetDocumentSizeAsync(cancellationToken);
    }

    public Task<long> GetDocumentSizeAsync(Expression<Func<FolderInfoModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return folderSystemService.GetDocumentSizeAsync(predicate, cancellationToken);
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
        return folderSystemService.Where(predicate, cancellationToken, fieldsToFetch);
    }

    public FolderInfoModel? Get(string key)
    {
        return folderSystemService.Get(key);
    }

    public IAsyncEnumerable<FolderInfoModel?> GetAsync(List<string> keys, CancellationToken cancellationToken = default)
    {
        return folderSystemService.GetAsync(keys, cancellationToken);
    }

    public Task<(FolderInfoModel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        return folderSystemService.GetAllAsync(page, size, cancellationToken);
    }

    public IAsyncEnumerable<FolderInfoModel> GetAllAsync(CancellationToken cancellationToken)
    {
        return folderSystemService.GetAllAsync(cancellationToken);
    }

    public Task<(bool, string)> UpdateAsync(string key, FieldUpdate<FolderInfoModel> updates, CancellationToken cancellationToken = default)
    {
        return folderSystemService.UpdateAsync(key, updates, cancellationToken);
    }

    public Task<(bool, string)> CreateAsync(FolderInfoModel model, CancellationToken cancellationToken = default)
    {
        return folderSystemService.CreateAsync(model, cancellationToken);
    }

    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<FolderInfoModel> models, CancellationToken cancellationToken = default)
    {
        return folderSystemService.CreateAsync(models, cancellationToken);
    }

    public Task<(bool, string)> UpdateAsync(FolderInfoModel model, CancellationToken cancellationToken = default)
    {
        return folderSystemService.UpdateAsync(model, cancellationToken);
    }

    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<FolderInfoModel> models, CancellationToken cancellationToken = default)
    {
        return folderSystemService.UpdateAsync(models, cancellationToken);
    }

    public (bool, string) Delete(string key)
    {
        var folder = Get(key);
        if (folder == null) return (false, AppLang.Folder_could_not_be_found);

        if (folder.RelativePath == "/") return (false, AppLang.Could_not_delete_root_folder);

        var res = folderSystemService.Delete(key);
        if (res.Item1)
            foreach (var content in folder.Contents)
                if (content is { Type: FolderContentType.File or FolderContentType.HiddenFile or FolderContentType.DeletedFile })
                    fileSystemService.Delete(content.Id);
                else
                    Delete(content.Id);

        return res;
    }

    public FolderInfoModel? Get(string username, string relativePath)
    {
        var user = GetUser(username);
        return folderSystemService.Get(user?.UserName ?? string.Empty, relativePath);
    }

    public List<FolderInfoModel> GetFolderBloodLine(string username, string folderId)
    {
        var user = GetUser(username);
        if (user == null) return [];
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
            var folder = Get(user.UserName, path);
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
                FolderName = "Home",
                ModifiedDate = DateTime.UtcNow,
                Username = user.UserName
            };
            var res = CreateAsync(folder).Result;
            if (res.Item1)
            {
                user.Folder = folder.Id.ToString();
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
        var newIndex = folder.Contents.Count + 1;
        var dateString = DateTime.UtcNow.ToString("dd-MM-yyy");

        var filePath = Path.Combine(_workingDir, dateString, folder.FolderName, $"_file_{newIndex}.bin");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        file.AbsolutePath = filePath;
        file.RootFolder = folder.Id.ToString();
        file.RelativePath = folder.RelativePath + $"/{file.FileName}";
        var res = await fileSystemService.CreateAsync(file, cancellationTokenSource);
        if (res.Item1)
        {
            folder.Contents.Add(new FolderContent
            {
                Id = file.Id.ToString(),
                Type = FolderContentType.File
            });
            var folderUpdateResult = await UpdateAsync(folder, cancellationTokenSource);
            if (!folderUpdateResult.Item1)
                return folderUpdateResult;

            var folderInfoModel = Get(folder.Id.ToString())!;
            folder.Contents = folderInfoModel.Contents;
        }

        return res;
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
            Username = user.UserName
        };

        if (Get(user.UserName, newFolder.RelativePath) != null)
            return (false, AppLang.Folder_already_exists);

        var createNewFolderResult = await CreateAsync(newFolder);
        if (createNewFolderResult is { Item1: true })
        {
            folder.Contents.Add(new FolderContent
            {
                Type = FolderContentType.Folder,
                Id = newFolder.Id.ToString()
            });
            await UpdateAsync(folder);
        }

        return createNewFolderResult;
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
        request.NewFolder.RootFolder = request.RootId;
        request.NewFolder.ModifiedDate = DateTime.Now;
        

        if (string.IsNullOrEmpty(request.NewFolder.Username))
            request.NewFolder.Username = folderRoot.Username;

        if (Get(folderRoot.Username, request.NewFolder.RelativePath) != null)
            return (false, AppLang.Folder_already_exists);

        var res = await CreateAsync(request.NewFolder);
        if (res.Item1)
        {
            folderRoot.Contents.Add(new FolderContent
            {
                Id = request.NewFolder.Id.ToString(),
                Type = FolderContentType.Folder,
            });
            res = await UpdateAsync(folderRoot);
            if (res.Item1)
                return (true, AppLang.Create_successfully);
        }

        return (false, AppLang.Create_failed);
    }

    public long GetFileSize(Expression<Func<FileInfoModel, bool>> predicate, CancellationToken cancellationTokenSource = default)
    {
        return fileSystemService.GetFileSize(predicate, cancellationTokenSource);
    }

    public async Task<long> GetFolderByteSize(Expression<Func<FolderInfoModel, bool>> predicate, CancellationToken cancellationTokenSource = default)
    {
        var folders = folderSystemService.Where(predicate, cancellationTokenSource);
        long total = 0;
        await foreach (var folder in folders)
        foreach (var content in folder.Contents)
            if (content is { Type: FolderContentType.File or FolderContentType.HiddenFile })
            {
                var file = fileSystemService.Get(content.Id);
                if (file == null)
                {
                    logger.LogInformation($@"[Error] file by id {content} can not be found");
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
        var folders = folderSystemService.Where(predicate, cancellationTokenSource);
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

    #region Private Mothods

    /// <summary>
    ///     Get user. if user by the name that is not found return Anonymous user
    /// </summary>
    /// <param name="username"></param>
    /// <returns></returns>
    public UserModel? GetUser(string username)
    {
        if (string.IsNullOrEmpty(username)) username = "Anonymous";
        var user = userService.Get(username);
        return user;
    }

    #endregion
}