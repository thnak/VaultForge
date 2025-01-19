using BrainNet.Service.Font.Interfaces;
using Business.Services.TaskQueueServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.Inference;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Route("api/yolo")]
[ApiController]
public partial class YoloInferenceController(IYoloSessionManager yoloSessionManager, IFontServiceProvider fontServiceProvider) : ControllerBase;