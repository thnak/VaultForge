using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Business.Attribute;
using Business.Business.Interfaces.FileSystem;
using Business.Data;
using Business.Models;
using Business.Services.Interfaces;
using Business.Utils.Helper;
using BusinessModels.General.EnumModel;
using BusinessModels.People;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using BusinessModels.Utils;
using BusinessModels.WebContent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Protector.Utils;

namespace ResApi.Controllers.Files;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Route("api/[controller]")]
[ApiController]
public class FilesController(
    IFileSystemBusinessLayer fileServe,
    IFolderSystemBusinessLayer folderServe,
    IThumbnailService thumbnailService,
    ILogger<FilesController> logger,
    RedundantArrayOfIndependentDisks raidService) : ControllerBase
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


        var webpImageContent = file.ExtendResource.FirstOrDefault(x => x.Type == FileContentType.ThumbnailWebpFile);
        if (webpImageContent != null)
        {
            var webpImage = fileServe.Get(webpImageContent.Id);
            if (webpImage != null)
            {
                file = webpImage;
            }
        }

        if (!global::System.IO.File.Exists(file.AbsolutePath))
        {
            logger.LogError($"File {file.AbsolutePath} not found");
            return NotFound();
        }

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
        Response.StatusCode = 200;
        Response.ContentLength = file.FileSize;

        return PhysicalFile(file.AbsolutePath, file.ContentType, true);
    }

    [HttpGet("get-file")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GetFile(string id)
    {
        var cancelToken = HttpContext.RequestAborted;
        var file = fileServe.Get(id);
        if (file == null) return NotFound();

        var webpImageContent = file.ExtendResource.FirstOrDefault(x => x.Type == FileContentType.ThumbnailWebpFile);
        if (webpImageContent != null)
        {
            var webpImage = fileServe.Get(webpImageContent.Id);
            if (webpImage != null)
            {
                file = webpImage;
            }
        }

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

        MemoryStream ms = new MemoryStream();
        await raidService.ReadGetDataAsync(ms, file.AbsolutePath, cancelToken);
        Response.RegisterForDispose(ms);
        return new FileStreamResult(ms, file.ContentType)
        {
            FileDownloadName = file.FileName,
            LastModified = file.ModifiedTime,
            EnableRangeProcessing = false
        };
        // return PhysicalFile(file.AbsolutePath, file.ContentType, true);
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
        Response.Headers.Append("Content-Disposition", cd.ToString());
        Response.ContentType = file.ContentType;
        Response.Headers.ContentType = file.ContentType;
        Response.StatusCode = 200;
        Response.ContentLength = file.FileSize;
        MemoryStream ms = new MemoryStream();
        await raidService.ReadGetDataAsync(ms, file.AbsolutePath, cancelToken);
        Response.RegisterForDispose(ms);

        using SHA256 sha256 = SHA256.Create();

        int bytesRead1;
        byte[] buffer = new byte[1024];
        while ((bytesRead1 = await ms.ReadAsync(buffer, 0, 1024, cancelToken)) > 0)
        {
            sha256.TransformBlock(buffer, 0, bytesRead1, null, 0);
        }

        ms.Seek(0, SeekOrigin.Begin);

        return new FileStreamResult(ms, file.ContentType)
        {
            FileDownloadName = file.FileName,
            LastModified = file.ModifiedTime,
            EnableRangeProcessing = false
        };
    }

    [HttpGet("stream")]
    public async Task<IActionResult> StreamVideo()
    {
        var filePath = "C:\\Users\\thanh\\Downloads\\test.mp4";
        var fileInfo = new FileInfo(filePath);

        // Check if Range request header exists
        if (Request.Headers.ContainsKey("Range"))
        {
            // Parse the Range header
            var rangeHeader = Request.Headers["Range"].ToString();
            var range = rangeHeader.Replace("bytes=", "").Split('-');

            long from = long.Parse(range[0]);
            long to = range.Length > 1 && long.TryParse(range[1], out var endRange) ? endRange : fileInfo.Length - 1;

            if (from >= fileInfo.Length)
            {
                return BadRequest("Requested range is not satisfiable.");
            }

            var length = to - from + 1;

            // Open the file stream and seek to the requested position
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                stream.Seek(from, SeekOrigin.Begin);

                var responseStream = new MemoryStream();
                await stream.CopyToAsync(responseStream, (int)length);

                var buffer = responseStream.ToArray();
                // Set headers for partial content response
                Response.Headers.Add("Content-Range", $"bytes {from}-{to}/{fileInfo.Length}");
                Response.Headers.Add("Accept-Ranges", "bytes");
                Response.ContentLength = length;
                Response.StatusCode = StatusCodes.Status206PartialContent;

                return File(buffer, "video/mp4");
            }
        }

        // If no range is requested, serve the whole file
        var fullStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(fullStream, "video/mp4", enableRangeProcessing: true);
    }


    [HttpGet("read-file-seek")]
    public async Task<IActionResult> ReadAndSeek(int start, int count)
    {
        var cancelToken = HttpContext.RequestAborted;
        string path = "C:/Users/thanh/Git/CodeWithMe/ResApi/bin/Debug/net9.0/02-10-2024\\zboahrzr.g5w";
        var file = fileServe.Get(path);
        if (file == null) return NotFound();
        var pathArray = await raidService.GetDataBlockPaths(path, cancelToken);
        if (pathArray == default) return NotFound();

        Raid5Stream raid5Stream = new Raid5Stream(pathArray.Files[0], pathArray.Files[1], pathArray.Files[2], pathArray.FileSize, pathArray.StripeSize);
        var filePath = "C:\\Users\\thanh\\Downloads\\Input1.txt";
        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        Response.RegisterForDispose(raid5Stream);
        Response.RegisterForDispose(stream);

        byte[] raidBuffer;
        byte[] normalBuffer;
        if (Request.Headers.ContainsKey("Range"))
        {
            // Parse the Range header
            var rangeHeader = Request.Headers["Range"].ToString();
            var range = rangeHeader.Replace("bytes=", "").Split('-');

            long from = long.Parse(range[0]);
            long to = range.Length > 1 && long.TryParse(range[1], out var endRange) ? endRange : file.FileSize - 1;

            if (from >= file.FileSize)
            {
                return BadRequest("Requested range is not satisfiable.");
            }

            var length = to - from + 1;

            // Open the file stream and seek to the requested position

            raid5Stream.Seek(from, SeekOrigin.Begin);

            raidBuffer = new byte[length];
            var totalRead = await raid5Stream.ReadAsync(raidBuffer, 0, (int)length, cancelToken);

            stream.Seek(from, SeekOrigin.Begin);

            var responseStream = new MemoryStream();
            await stream.CopyToAsync(responseStream, (int)length, cancelToken);
            var buffer = responseStream.ToArray();

            SHA256 fileStreamSha256 = SHA256.Create();
            fileStreamSha256.TransformBlock(buffer, 0, (int)length, null, 0);
            fileStreamSha256.TransformFinalBlock([], 0, 0);
            StringBuilder checksum = new StringBuilder();
            if (fileStreamSha256.Hash != null)
            {
                foreach (byte b in fileStreamSha256.Hash)
                {
                    checksum.Append(b.ToString("x2"));
                }
            }

            SHA256 raidStreamSha256 = SHA256.Create();
            raidStreamSha256.TransformBlock(raidBuffer, 0, (int)length, null, 0);
            raidStreamSha256.TransformFinalBlock([], 0, 0);
            StringBuilder raidChecksum = new StringBuilder();
            if (raidStreamSha256.Hash != null)
            {
                foreach (byte b in raidStreamSha256.Hash)
                {
                    raidChecksum.Append(b.ToString("x2"));
                }
            }

            var fileStreamCheck = checksum.ToString();
            var raidStreamCheck = raidChecksum.ToString();
            if (fileStreamCheck == raidStreamCheck)
            {
                logger.LogInformation("[Checksum] File stream check success.");
            }
            else
            {
                logger.LogInformation("[Checksum] File stream check failed.");
            }

            // Set headers for partial content response
            Response.Headers.Add("Content-Range", $"bytes {from}-{to}/{file.FileSize}");
            Response.Headers.Add("Accept-Ranges", "bytes");
            Response.ContentLength = length;
            Response.StatusCode = StatusCodes.Status206PartialContent;

            return File(raidBuffer, "video/mp4");
        }

        var outStreamNormal = new FileStream("C:\\Users\\thanh\\Downloads\\test-1.txt", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
        var outStreamRaid = new FileStream("C:\\Users\\thanh\\Downloads\\test-2.txt", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
        
        
        
        int seekPosition = start;
        int readLength = count;
        raid5Stream.Seek(seekPosition, SeekOrigin.Begin);
        stream.Seek(seekPosition, SeekOrigin.Begin);
        raidBuffer = new byte[file.FileSize - seekPosition];
        normalBuffer = new byte[file.FileSize - seekPosition];

        var raidStreamReadCount = await raid5Stream.ReadAsync(raidBuffer, 0, readLength, cancelToken);
        var normalStreamReadCount = await stream.ReadAsync(normalBuffer, 0, readLength, cancelToken);

        await outStreamNormal.WriteAsync(normalBuffer, 0, normalStreamReadCount, cancelToken);
        await outStreamRaid.WriteAsync(raidBuffer, 0, raidStreamReadCount, cancelToken);

        Response.RegisterForDispose(outStreamNormal);
        Response.RegisterForDispose(outStreamRaid);


        return File(raidBuffer, "video/mp4");
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

    [HttpPost("test-raid")]
    [IgnoreAntiforgeryToken]
    [DisableFormValueModelBinding]
    public async Task<IActionResult> TestRaid()
    {
        var cancelToken = HttpContext.RequestAborted;
        var name = Path.GetRandomFileName();

        var boundary = MediaTypeHeaderValue.Parse(Request.ContentType).GetBoundary(int.MaxValue);
        var reader = new MultipartReader(boundary, HttpContext.Request.Body);
        var section = await reader.ReadNextSectionAsync(cancelToken);
        while (section != null && HttpContext.RequestAborted is { IsCancellationRequested: false })
        {
            var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);
            if (hasContentDispositionHeader && contentDisposition != null)
            {
                if (!contentDisposition.HasFileContentDisposition())
                {
                    continue;
                }

                // var fileStream = System.IO.File.Open("C:/Users/thanh/Downloads/Update _Nosew Monitoring system_V2  (1).pptx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

                var fileStream = new MemoryStream();
                ;
                await section.Body.CopyToAsync(fileStream, cancelToken);

                await raidService.WriteDataAsync(fileStream, name, cancelToken);
                var outPutStream = new FileStream("C:/Users/thanh/Downloads/output.pptx", FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024);
                await raidService.ReadGetDataAsync(outPutStream, name, cancelToken);

                Response.RegisterForDispose(fileStream);
                Response.RegisterForDispose(outPutStream);
            }

            section = await reader.ReadNextSectionAsync(cancelToken);
        }


        return Ok();
    }

    [HttpPost("get-folder")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [OutputCache(Duration = 10)]
    [ResponseCache(Duration = 50)]
    public async Task<IActionResult> GetSharedFolder([FromForm] string? id, [FromForm] string? password,
        [FromForm] int page, [FromForm] int pageSize, [FromForm] string? contentTypes, [FromForm] bool? forceReLoad)
    {
        var cancelToken = HttpContext.RequestAborted;
        try
        {
            var folderSource = string.IsNullOrEmpty(id) ? folderServe.GetRoot("") : folderServe.Get(id);
            if (folderSource == null) return BadRequest(AppLang.Folder_could_not_be_found);
            if (!string.IsNullOrEmpty(folderSource.Password))
            {
                if (password == null || password.ComputeSha256Hash() != folderSource.Password)
                    return Unauthorized(AppLang.This_resource_is_protected_by_password);
            }

            folderSource.Password = string.Empty;
            folderSource.Username = string.Empty;

            List<FolderContentType> contentFolderTypesList = [];

            if (!string.IsNullOrEmpty(contentTypes))
            {
                var listString = contentTypes.DeSerialize<FolderContentType[]>();
                if (listString != null) contentFolderTypesList = listString.ToList();
            }
            else
            {
                contentFolderTypesList = [FolderContentType.File, FolderContentType.Folder];
            }

            if (!contentFolderTypesList.Any(x => x is FolderContentType.DeletedFile or FolderContentType.DeletedFolder))
                contentFolderTypesList.Add(FolderContentType.SystemFolder);

            var contentFileTypesList = contentFolderTypesList.Select(x => x.MapFileContentType()).Distinct().ToList();
            string rootFolderId = folderSource.Id.ToString();
            var res = await folderServe.GetFolderRequestAsync(model => model.RootFolder == rootFolderId && contentFolderTypesList.Contains(model.Type), model => model.RootFolder == rootFolderId && contentFileTypesList.Contains(model.Type),
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
                                                           (user == null || x.Username == user.UserName) &&
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


    [HttpPost("re-name-file")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ReNameFile([FromForm] string objectId, [FromForm] string newName)
    {
        var file = fileServe.Get(objectId);
        if (file == null) return BadRequest(AppLang.File_not_found_);
        if (string.IsNullOrEmpty(newName))
            return BadRequest(AppLang.ThisFieldIsRequired);
        file.FileName = newName;
        var status = await fileServe.UpdateAsync(file);
        return status.Item1 ? Ok(status.Item2) : BadRequest(status.Item2);
    }

    [HttpPost("re-name-folder")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ReNameFolder([FromForm] string objectId, [FromForm] string newName)
    {
        var folder = folderServe.Get(objectId);
        if (folder == null) return BadRequest(AppLang.Folder_could_not_be_found);
        if (string.IsNullOrEmpty(newName))
            return BadRequest(AppLang.ThisFieldIsRequired);

        var rootFolder = folderServe.GetRoot(folder.RootFolder);
        if (rootFolder == default)
            return BadRequest();

        folder.FolderName = newName;
        folder.RelativePath = rootFolder.RelativePath + "/" + newName;

        var status = await folderServe.UpdateAsync(folder);
        return status.Item1 ? Ok(status.Item2) : BadRequest(status.Item2);
    }

    [HttpPost("restore-content")]
    [IgnoreAntiforgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> RestoreContent([FromForm] string id, [FromForm] bool isFile)
    {
        if (isFile)
        {
            var file = fileServe.Get(id);
            if (file == null) return BadRequest(AppLang.File_not_found_);
            var result = await fileServe.UpdateAsync(id, new FieldUpdate<FileInfoModel>() { { model => model.Type, file.PreviousType } });
            return result.Item1 ? Ok(result.Item2) : BadRequest(result.Item2);
        }

        {
            var folder = folderServe.Get(id);
            if (folder == null) return BadRequest(AppLang.File_not_found_);
            var result = await folderServe.UpdateAsync(id, new FieldUpdate<FolderInfoModel>() { { model => model.Type, folder.PreviousType } });
            return result.Item1 ? Ok(result.Item2) : BadRequest(result.Item2);
        }
    }

    [HttpDelete("delete-file")]
    public async Task<IActionResult> DeleteFile([FromForm] string fileId, [FromForm] string folderId)
    {
        var file = fileServe.Get(fileId);
        if (file == null) return BadRequest(AppLang.File_not_found_);

        var folder = folderServe.Get(folderId);
        if (folder == null) return BadRequest(AppLang.Folder_could_not_be_found);

        var fileDeleteStatus = fileServe.Delete(fileId);
        if (fileDeleteStatus.Item1)
        {
            folder.Contents = folder.Contents.Where(x => x.Id != fileId).ToList();
            await folderServe.UpdateAsync(folder);
        }

        return BadRequest(fileDeleteStatus.Item2);
    }

    [HttpDelete("safe-delete-file")]
    [IgnoreAntiforgeryToken]
    public IActionResult SafeDeleteFile(string code)
    {
        var result = fileServe.Delete(code);
        return result.Item1 ? Ok(result.Item2) : BadRequest(result.Item2);
    }

    [HttpDelete("safe-delete-folder")]
    [IgnoreAntiforgeryToken]
    public IActionResult SafeDeleteFolder(string code)
    {
        var updateResult = folderServe.Delete(code);
        return updateResult.Item1 ? Ok(updateResult.Item2) : BadRequest(updateResult.Item2);
    }


    [HttpPut("create-folder")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CreateSharedFolder([FromBody] RequestNewFolderModel request)
    {
        var result = await folderServe.CreateFolder(request);
        return result.Item1 ? Ok(result.Item2) : BadRequest(result.Item2);
    }


    [HttpPut("move-file-to-folder")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> MoveFile2([FromForm] List<string> fileCodes, [FromForm] string currentFolderCode, [FromForm] string targetFolderCode, [FromForm] string? password)
    {
        var cancelToken = HttpContext.RequestAborted;
        var currentFolder = folderServe.Get(currentFolderCode);
        if (currentFolder == null) return NotFound(AppLang.Current_folder_could_not_have_found);

        var targetFolder = folderServe.Get(targetFolderCode);
        if (targetFolder == null) return NotFound(AppLang.Target_folder_could_not_have_found);


        if (!string.IsNullOrEmpty(currentFolder.Password))
        {
            if (!string.IsNullOrEmpty(password))
            {
                if (currentFolder.Password != password.ComputeSha256Hash())
                    return BadRequest(AppLang.Passwords_do_not_match_);
            }
            else
            {
                return BadRequest(AppLang.Incorrect_password);
            }
        }

        var files = fileCodes.Select(fileServe.Get).Where(x => x != default).ToList();

        Dictionary<string, FolderContent> contentsDict = new();
        foreach (var file in currentFolder.Contents) contentsDict.TryAdd(file.Id, file);


        foreach (var file in files)
        {
            if (file == default)
            {
                ModelState.AddModelError(AppLang.File, AppLang.File_not_found_);
                continue;
            }

            var fileName = file.RelativePath.Split("/").Last();
            file.RelativePath = targetFolder.RelativePath + '/' + fileName;

            var fileId = file.Id.ToString();
            contentsDict.Remove(fileId);
            targetFolder.Contents.Add(new FolderContent
            {
                Id = file.Id.ToString(),
                Type = FolderContentType.File
            });
        }

        currentFolder.Contents = contentsDict.Values.ToList();

        await folderServe.UpdateAsync(targetFolder, cancelToken);
        await folderServe.UpdateAsync(currentFolder, cancelToken);
        await foreach (var x in fileServe.UpdateAsync(files!, cancelToken))
            if (!x.Item1)
                ModelState.AddModelError(AppLang.File, x.Item2);


        return Ok(ModelState.Any() ? ModelState : AppLang.File_moved_successfully);
    }

    [HttpPut("move-folder-to-folder")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> MoveFolder2Folder([FromForm] string folderCode, [FromForm] string targetFolderCode)
    {
        var targetFolder = folderServe.Get(targetFolderCode);
        if (targetFolder == null) return NotFound(AppLang.Target_folder_could_not_have_found);

        var folder = folderServe.Get(folderCode);
        if (folder == default) return NotFound(AppLang.Folder_could_not_be_found);

        folder.RelativePath = targetFolder.RelativePath + '/' + folder.FolderName;
        targetFolder.Contents.Add(new FolderContent
        {
            Id = folderCode,
            Type = FolderContentType.Folder
        });

        await folderServe.UpdateAsync(targetFolder);
        await folderServe.UpdateAsync(folder);
        return Ok(AppLang.Folder_moved_successfully);
    }


    [HttpPost("upload-physical")]
    [DisableFormValueModelBinding]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UploadPhysical()
    {
        var folderKeyString = AppLang.Folder;
        var fileKeyString = AppLang.File;
        var cancelToken = HttpContext.RequestAborted;


        try
        {
            if (string.IsNullOrEmpty(Request.ContentType) || !MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                ModelState.AddModelError(fileKeyString, "The request couldn't be processed (Error 1).");
                return BadRequest(ModelState);
            }

            HttpContext.Request.Headers.TryGetValue("Folder", out var folderValues);

            var folderCodes = folderValues.ToString();

            if (string.IsNullOrEmpty(folderCodes))
            {
                ModelState.AddModelError("Header", "Folder path is required in the request header");
                return BadRequest(ModelState);
            }

            // folderServe.GetRoot(user);
            var folder = folderServe.Get(folderCodes);
            if (folder == null)
            {
                ModelState.AddModelError(folderKeyString, AppLang.Folder_could_not_be_found);
                return BadRequest(ModelState);
            }

            var boundary = MediaTypeHeaderValue.Parse(Request.ContentType).GetBoundary(int.MaxValue);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body, 10 * 1024 * 1024);
            var section = await reader.ReadNextSectionAsync(cancelToken);
            while (section != null && HttpContext.RequestAborted is { IsCancellationRequested: false })
            {
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);
                if (hasContentDispositionHeader && contentDisposition != null)
                {
                    if (!contentDisposition.HasFileContentDisposition())
                    {
                        ModelState.AddModelError(fileKeyString, "The request couldn't be processed (Error 2).");
                    }
                    else
                    {
                        var trustedFileNameForDisplay = contentDisposition.FileName.Value ?? Path.GetRandomFileName();

                        var file = new FileInfoModel
                        {
                            FileName = trustedFileNameForDisplay
                        };

                        folder = folderServe.Get(folderCodes);
                        if (folder == null)
                        {
                            if (ModelState.IsValid)
                                return Ok(AppLang.Successfully_uploaded);
                            return BadRequest(ModelState);
                        }

                        var createFileResult = await folderServe.CreateFileAsync(folder, file, cancelToken);
                        if (createFileResult.Item1)
                        {
                            MemoryStream memoryStream = new MemoryStream();
                            // var memoryStream = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize: 10 * 1024 * 1024, FileOptions.DeleteOnClose);
                            await section.Body.CopyToAsync(memoryStream, cancelToken);
                            var saveResult = await raidService.WriteDataAsync(memoryStream, file.AbsolutePath, cancelToken);
                            await memoryStream.DisposeAsync();
                            (file.FileSize, file.ContentType, file.Checksum) = (saveResult.TotalByteWritten, saveResult.ContentType, saveResult.CheckSum);
                            if (string.IsNullOrEmpty(file.ContentType))
                            {
                                file.ContentType = section.ContentType ?? string.Empty;
                            }
                            else if (file.ContentType == "application/octet-stream")
                            {
                                file.ContentType = Path.GetExtension(trustedFileNameForDisplay).GetMimeTypeFromExtension();
                            }

                            var fileId = file.Id.ToString();

                            if (file.FileSize > 0)
                            {
                                var field2Update = new FieldUpdate<FileInfoModel>()
                                {
                                    { x => x.FileSize, file.FileSize },
                                    { x => x.ContentType, file.ContentType },
                                    { x => x.Checksum, file.Checksum },
                                };
                                var updateResult = await fileServe.UpdateAsync(fileId, field2Update, cancelToken);
                                if (updateResult.Item1)
                                {
                                    await thumbnailService.AddThumbnailRequest(fileId);
                                }
                                else
                                {
                                    logger.LogError(updateResult.Item2);
                                }
                            }
                            else
                            {
                                logger.LogWarning($"File empty. deleting {file.FileName}");
                                fileServe.Delete(fileId);
                            }
                        }
                    }
                }

                section = await reader.ReadNextSectionAsync(cancelToken);
            }

            if (ModelState.IsValid)
                return Ok(AppLang.Successfully_uploaded);

            return Ok(ModelState);
        }
        catch (OperationCanceledException ex)
        {
            return Ok(ex.Message);
        }
        catch (Exception e)
        {
            ModelState.AddModelError(AppLang.Exception, e.Message);
            logger.LogError(e, null);
            return StatusCode(500, ModelState);
        }
    }
}