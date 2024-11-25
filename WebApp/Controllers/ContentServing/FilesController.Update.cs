﻿using Business.Models;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Protector.Utils;

namespace WebApp.Controllers.ContentServing;

public partial class FilesController
{
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
            var result = await fileServe.UpdateAsync(id, new FieldUpdate<FileInfoModel>() { { model => model.Status, file.PreviousStatus } });
            return result.Item1 ? Ok(result.Item2) : BadRequest(result.Item2);
        }

        {
            var folder = folderServe.Get(id);
            if (folder == null) return BadRequest(AppLang.File_not_found_);
            var result = await folderServe.UpdateAsync(id, new FieldUpdate<FolderInfoModel>() { { model => model.Type, folder.PreviousType } });
            return result.Item1 ? Ok(result.Item2) : BadRequest(result.Item2);
        }
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


        foreach (var file in files)
        {
            if (file == default)
            {
                ModelState.AddModelError(AppLang.File, AppLang.File_not_found_);
                continue;
            }

            var fileName = file.RelativePath.Split("/").Last();
            file.RelativePath = targetFolder.RelativePath + '/' + fileName;
        }

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

        await folderServe.UpdateAsync(targetFolder);
        await folderServe.UpdateAsync(folder);
        return Ok(AppLang.Folder_moved_successfully);
    }

    [HttpGet("fix-content-status")]
    public async Task<IActionResult> FixFile()
    {
        var cancelToken = HttpContext.RequestAborted;
        var totalFiles = await fileServe.GetDocumentSizeAsync(cancelToken);
        var files = fileServe.Where(x => true, cancelToken, model => model.Id, model => model.PreviousStatus, model => model.Status);

        long index = 0;
        await foreach (var x in files)
        {
            await fileServe.UpdateAsync(x.Id.ToString(), new FieldUpdate<FileInfoModel>()
            {
                { z => z.Status, x.PreviousStatus }
            }, cancelToken);
            index += 1;
            if (index == totalFiles)
                break;
        }

        return Ok();
    }
}