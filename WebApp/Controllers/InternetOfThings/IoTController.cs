using Business.Business.Interfaces.FileSystem;
using Business.Business.Interfaces.InternetOfThings;
using Business.Data.StorageSpace;
using Business.Services.Http.CircuitBreakers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.InternetOfThings;

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
    ILogger<IoTController> logger) : ControllerBase;