using BusinessModels.Resources;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.ContentServing;

public partial class FilesController
{
    [HttpDelete("delete-file")]
    public async Task<IActionResult> DeleteFile([FromForm] string fileId, [FromForm] string folderId)
    {
        var file = fileServe.Get(fileId);
        if (file == null) return BadRequest(AppLang.File_not_found_);

        var folder = folderServe.Get(folderId);
        if (folder == null) return BadRequest(AppLang.Folder_could_not_be_found);

        var fileDeleteStatus = await fileServe.DeleteAsync(fileId);
        if (fileDeleteStatus.Item1)
        {
            await folderServe.UpdateAsync(folder);
        }

        return BadRequest(fileDeleteStatus.Item2);
    }

    [HttpDelete("safe-delete-file")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SafeDeleteFile(string code)
    {
        var result = await fileServe.DeleteAsync(code);
        return result.Item1 ? Ok(result.Item2) : BadRequest(result.Item2);
    }

    [HttpDelete("safe-delete-folder")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SafeDeleteFolder(string code)
    {
        var updateResult = await folderServe.DeleteAsync(code);
        return updateResult.Item1 ? Ok(updateResult.Item2) : BadRequest(updateResult.Item2);
    }
}