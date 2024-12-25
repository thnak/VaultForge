using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Business.Data.StorageSpace;
using Business.Utils.Protector;
using BusinessModels.General.EnumModel;
using BusinessModels.People;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using BusinessModels.Utils;
using BusinessModels.WebContent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace WebApp.Controllers.ContentServing;

public partial class FilesController
{
    [HttpGet("get-file-wall-paper")]
    [IgnoreAntiforgeryToken]
    [OutputCache(NoStore = true)]
    [ResponseCache(NoStore = true)]
    public async Task<IActionResult> GetWallPaper()
    {
        var cancelToken = HttpContext.RequestAborted;

        var anonymousUser = "Anonymous".ComputeSha256Hash();

        var rootWallpaperFolder = folderServe.Get(anonymousUser, "/root/wallpaper");
        if (rootWallpaperFolder == null)
            return NotFound();


        var file = await fileServe.GetRandomFileAsync(rootWallpaperFolder.Id.ToString(), cancelToken);
        if (file == null) return NotFound();

        var fileThumbnail = await fileServe.GetSubFileByClassifyAsync(file.Id.ToString(), FileClassify.ThumbnailWebpFile, cancelToken);
        if (fileThumbnail != null)
        {
            file = fileThumbnail;
        }

        var memoryStream = new MemoryStream();
        await raidService.ReadGetDataAsync(memoryStream, file.AbsolutePath, cancelToken);
        memoryStream.SeekBeginOrigin();


        var now = DateTime.UtcNow;
        var cd = new ContentDisposition
        {
            FileName = HttpUtility.UrlEncode(file.FileName),
            Inline = true, // false = prompt the user for downloading;  true = browser to try to show the file inline,
            CreationDate = now,
            ModificationDate = now,
            ReadDate = now
        };

        Response.Headers.Append("Content-Disposition", cd.ToString());
        Response.RegisterForDispose(memoryStream);
        Response.StatusCode = 200;
        Response.ContentLength = file.FileSize;

        return File(memoryStream, file.ContentType);
    }

    [HttpGet("get-file")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GetFile(string id, string? type)
    {
        if (!Enum.TryParse<FileClassify>(type, out var fileClassify))
        {
            fileClassify = FileClassify.ThumbnailFile;
        }

        var cancelToken = HttpContext.RequestAborted;
        // id = id.Split(".").First();
        var fileList = await fileServe.GetSubFileByClassifyAsync(id, cancelToken, [fileClassify]);
        if (fileList.Count == 0)
        {
            return NotFound("Not found sub file");
        }

        var file = fileList.First();

        var now = DateTime.UtcNow;
        var cd = new ContentDisposition
        {
            FileName = HttpUtility.UrlEncode(file.FileName),
            Inline = true, // false = prompt the user for downloading;  true = browser to try to show the file inline,
            CreationDate = now,
            ModificationDate = now,
            ReadDate = now
        };
        Response.Headers.Append("Content-Disposition", cd.ToString());
        Response.ContentType = file.ContentType;
        Response.Headers.ContentType = file.ContentType;
        Response.StatusCode = 200;
        Response.ContentLength = file.FileSize;

        var pathArray = await raidService.GetDataBlockPaths(file.AbsolutePath, cancelToken);
        if (pathArray == null)
        {
            logger.LogError("File exists but raid not found. can't download file.");
            return NotFound();
        }

        Raid5Stream raid5Stream = new Raid5Stream(pathArray.Files, pathArray.FileSize, pathArray.StripeSize, FileMode.Open, FileAccess.Read, FileShare.Read);
        Response.RegisterForDispose(raid5Stream);


        if (file is { Classify: FileClassify.M3U8File })
        {
            var streamReader = new StreamReader(raid5Stream);
            Response.RegisterForDispose(streamReader);
            string[] lines = (await streamReader.ReadToEndAsync(cancelToken)).Split("\n");
            raid5Stream.Seek(0, SeekOrigin.Begin);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Contains(".m3u8") || line.Contains(".ts") || line.Contains(".vtt"))
                {
                    lines[i] = $"{HttpContext.Request.Scheme}://" + HttpContext.Request.Host.Value + HttpContext.Request.Path + "?id=" + line;
                    lines[i] = lines[i].Trim();
                }
            }

            var stringContent = string.Join("\n", lines);
            return Content(stringContent, file.ContentType);
        }

        return new FileStreamResult(raid5Stream, file.ContentType)
        {
            FileDownloadName = file.FileName,
            LastModified = file.ModifiedTime,
            EnableRangeProcessing = true
        };
    }

    [HttpGet("download-file")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> DownloadFile(string id)
    {
        var cancelToken = HttpContext.RequestAborted;
        var file = fileServe.Get(id);
        if (file == null) return NotFound(AppLang.File_not_found_);
        var now = DateTime.UtcNow;
        var cd = new ContentDisposition
        {
            FileName = HttpUtility.UrlEncode(file.FileName),
            Inline = false, // false = prompt the user for downloading;  true = browser to try to show the file inline,
            CreationDate = now,
            ModificationDate = now,
            ReadDate = now
        };

        var pathArray = await raidService.GetDataBlockPaths(file.AbsolutePath, cancelToken);
        if (pathArray == null)
        {
            logger.LogError("File exists but raid not found. can't download file.");
            return NotFound();
        }

        Raid5Stream raid5Stream = new Raid5Stream(pathArray.Files, pathArray.FileSize, pathArray.StripeSize, FileMode.Open, FileAccess.Read, FileShare.Read);

        Response.RegisterForDisposeAsync(raid5Stream);

        Response.Headers.Append("Content-Disposition", cd.ToString());
        Response.Headers.Append("Content-Length", file.FileSize.ToString());

        return new FileStreamResult(raid5Stream, file.ContentType)
        {
            FileDownloadName = file.FileName,
            LastModified = file.ModifiedTime,
            EnableRangeProcessing = true
        };
    }

    [HttpGet("stream-raid")]
    public async Task<IActionResult> StreamRaidVideo(string path)
    {
        var cancelToken = HttpContext.RequestAborted;
        var file = fileServe.Get(path);
        if (file == null) return NotFound(AppLang.File_not_found_);

        var pathArray = await raidService.GetDataBlockPaths(file.AbsolutePath, cancelToken);
        if (pathArray == null) return NotFound();

        // Check if Range request header exists
        if (Request.Headers.ContainsKey("Range"))
        {
            // Parse the Range header
            var rangeHeader = Request.Headers["Range"].ToString();
            var range = rangeHeader.Replace("bytes=", "").Split('-');

            long from = long.Parse(range[0]);
            long to = range.Length > 1 && long.TryParse(range[1], out var endRange) ? endRange : file.FileSize - 1;
            if (to - from > 1024 * 1024 * 4)
            {
                to = from + 1024 * 1024 * 4;
            }

            if (from >= file.FileSize)
            {
                return BadRequest("Requested range is not satisfiable.");
            }

            var length = (int)(to - from + 1);

            byte[] buffer = new byte[length];

            Raid5Stream raid5Stream = new Raid5Stream(pathArray.Files, pathArray.FileSize, pathArray.StripeSize, FileMode.Open, FileAccess.Read, FileShare.Read);
            raid5Stream.Seek(from, SeekOrigin.Begin);

            _ = await raid5Stream.ReadAsync(buffer, 0, length, cancelToken);
            await raid5Stream.DisposeAsync();
            // Set headers for partial content response
            Response.Headers.Append("Content-Range", $"bytes {from}-{to}/{file.FileSize}");
            Response.Headers.Append("Accept-Ranges", "bytes");
            Response.ContentLength = length;
            Response.StatusCode = StatusCodes.Status206PartialContent;

            return File(buffer, "video/mp4");
        }

        return NotFound("Unsupported");
    }


    [HttpPost("get-file-list")]
    [IgnoreAntiforgeryToken]
    public IActionResult GetFiles([FromBody] List<string> listFiles)
    {
        List<FileInfoModel> files = [];
        var cancelToken = HttpContext.RequestAborted;

        foreach (var id in listFiles.TakeWhile(_ => cancelToken is not { IsCancellationRequested: true }))
        {
            var file = fileServe.Get(id);
            if (file == null) continue;
            file.AbsolutePath = string.Empty;
            files.Add(file);
        }

        return Content(files.ToJson(), MediaTypeNames.Application.Json);
    }

    [HttpPost("get-folder-list")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public IActionResult GetFolderList([FromBody] List<string> listFolders)
    {
        List<FolderInfoModel> files = [];
        var cancelToken = HttpContext.RequestAborted;
        files.AddRange(listFolders.TakeWhile(_ => cancelToken is not { IsCancellationRequested: true }).Select(folderServe.Get).OfType<FolderInfoModel>());

        return Content(files.ToJson(), MediaTypeNames.Application.Json);
    }

    [HttpGet("get-file-v2")]
    [IgnoreAntiforgeryToken]
    public IActionResult GetFile_v2(string id)
    {
        var file = fileServe.Get(id);
        if (file == null) return NotFound();
        var now = DateTime.UtcNow;
        var cd = new ContentDisposition
        {
            FileName = file.FileName,
            Inline = true, // false = prompt the user for downloading;  true = browser to try to show the file inline,
            CreationDate = now,
            ModificationDate = now,
            ReadDate = now
        };
        Response.Headers.Append("Content-Disposition", cd.ToString());
        Response.ContentType = file.ContentType;
        Response.Headers.ContentType = file.ContentType;
        Response.StatusCode = 200;
        Response.ContentLength = file.FileSize;
        return PhysicalFile(file.AbsolutePath, file.ContentType, true);
    }

    [HttpPost("{username}/get-folder")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [OutputCache(Duration = 10)]
    [ResponseCache(Duration = 50)]
    public async Task<IActionResult> GetSharedFolder(string username, [FromForm] string? id, [FromForm] string? password,
        [FromForm] int page, [FromForm] int pageSize, [FromForm] string? contentTypes, [FromForm] bool? forceReLoad)
    {
        var cancelToken = HttpContext.RequestAborted;
        try
        {
            var folderSource = string.IsNullOrEmpty(id) ? folderServe.GetRoot(username) : folderServe.Get(id);
            if (folderSource == null) return BadRequest(AppLang.Folder_could_not_be_found);
            if (!string.IsNullOrEmpty(folderSource.Password))
            {
                if (password == null || password.ComputeSha256Hash() != folderSource.Password)
                    return Unauthorized(AppLang.This_resource_is_protected_by_password);
            }

            folderSource.Password = string.Empty;
            folderSource.OwnerUsername = string.Empty;

            List<FolderContentType> contentFolderTypesList = [];

            if (!string.IsNullOrEmpty(contentTypes))
            {
                var listString = contentTypes.DeSerialize<FolderContentType[]>();
                if (listString != null) contentFolderTypesList = listString.ToList();
            }
            else
            {
                contentFolderTypesList = [FolderContentType.Folder];
            }

            if (!contentFolderTypesList.Any(x => x is FolderContentType.DeletedFolder))
                contentFolderTypesList.Add(FolderContentType.SystemFolder);

            var contentFileTypesList = contentFolderTypesList.Select(x => x.MapFileContentType()).Distinct().ToList();

            FileClassify[] fileClassify = [FileClassify.Normal];

            string rootFolderId = folderSource.Id.ToString();
            var res = await folderServe.GetFolderRequestAsync(rootFolderId,
                folderInfoModel => folderInfoModel.RootFolder == rootFolderId && contentFolderTypesList.Contains(folderInfoModel.Type),
                fileInfoModel => fileInfoModel.RootFolder == rootFolderId && contentFileTypesList.Contains(fileInfoModel.Status) && fileClassify.Contains(fileInfoModel.Classify),
                pageSize, page, forceReLoad is true, cancelToken);

            res.Folder = folderSource;
            return Content(res.ToJson(), MediaTypeNames.Application.Json);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Request cancelled");
        }

        return Ok();
    }

    [HttpPost("get-deleted-content")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [OutputCache(Duration = 10)]
    [ResponseCache(Duration = 50)]
    public async Task<IActionResult> GetDeletedContent([FromForm] string? userName, [FromForm] int pageSize, [FromForm] int page)
    {
        try
        {
            var cancelToken = HttpContext.RequestAborted;
            var content = await folderServe.GetDeletedContentAsync(userName, pageSize, page, cancellationToken: cancelToken);
            return Content(content.ToJson(), MediaTypeNames.Application.Json);
        }
        catch (OperationCanceledException)
        {
            return Ok();
        }
    }

    [HttpPost("search-folder")]
    [IgnoreAntiforgeryToken]
    [OutputCache(Duration = 10, NoStore = true)]
    public async Task<IActionResult> SearchFolder([FromForm] string? username, [FromForm] string searchString)
    {
        List<FolderInfoModel> folderList = [];
        var cancelToken = HttpContext.RequestAborted;

        var user = string.IsNullOrEmpty(username) ? folderServe.GetUser(string.Empty) : new UserModel();

        try
        {
            await foreach (var x in folderServe.Where(x => x.FolderName.Contains(searchString) ||
                                                           x.RelativePath.Contains(searchString) &&
                                                           (user == null || x.OwnerUsername == user.UserName) &&
                                                           x.Type == FolderContentType.Folder, cancelToken,
                               model => model.FolderName, model => model.Type, model => model.Icon, model => model.ModifiedTime, model => model.Id))
            {
                folderList.Add(x);
                if (folderList.Count == 10)
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            //
        }
        catch (Exception)
        {
            //
        }

        return Content(folderList.ToJson(), MimeTypeNames.Application.Json);
    }

    [HttpGet("get-folder-blood-line")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public IActionResult GetFolderBloodLine(string id)
    {
        var folders = folderServe.GetFolderBloodLine(id);
        return Content(folders.ToJson(), MimeTypeNames.Application.Json);
    }
}