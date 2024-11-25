using Business.Business.Interfaces.FileSystem;
using Business.Data.StorageSpace;
using Business.Services.Configure;
using Business.Services.Interfaces;
using Business.Services.TaskQueueServices.Base.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.ContentServing;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Route("api/[controller]")]
[ApiController]
public partial class FilesController(
    IFileSystemBusinessLayer fileServe,
    IFolderSystemBusinessLayer folderServe,
    IThumbnailService thumbnailService,
    ILogger<FilesController> logger,
    RedundantArrayOfIndependentDisks raidService,
    IParallelBackgroundTaskQueue parallelBackgroundTaskQueue,
    ApplicationConfiguration options) : ControllerBase
{
}