using Business.Business.Interfaces.FileSystem;
using Business.Business.Interfaces.InternetOfThings;
using Business.Data.StorageSpace;
using Business.Services.Http.CircuitBreakers;
using Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.InternetOfThings.Record;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Route("api/[controller]")]
[ApiController]
public partial class IoTController(
    IoTCircuitBreakerService circuitBreakerService,
    IIoTBusinessLayer businessLayer,
    IIotRequestQueue requestQueueHostedService,
    IFolderSystemBusinessLayer folderServe,
    IFileSystemBusinessLayer fileSystemServe,
    IIoTBusinessLayer ioTBusinessService,
    RedundantArrayOfIndependentDisks raidService,
    IThumbnailService thumbnailService,
    ILogger<IoTController> logger) : ControllerBase;