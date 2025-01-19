using BusinessModels.Resources;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.ContentServing;

public partial class FilesController
{
    [HttpDelete("delete-file")]
    public async Task<IActionResult> DeleteFile([FromForm] string fileId, [FromForm] string folderId)
    {
        var file = fileServe.Get(fileId);
        if (file == null) return BadRequest(AppLang.File_not_found_);
        var fileDeleteStatus = await fileServe.DeleteAsync(fileId);
        return BadRequest(fileDeleteStatus.ToJson());
    }

    [HttpDelete("safe-delete-file")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SafeDeleteFile(string code)
    {
        var result = await fileServe.DeleteAsync(code);
        return Ok(result.ToJson());
    }

    [HttpDelete("safe-delete-folder")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SafeDeleteFolder(string code)
    {
        var updateResult = await folderServe.DeleteAsync(code);
        return Ok(updateResult.ToJson());
    }
}