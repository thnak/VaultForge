using Business.Data.Interfaces.ComputeVision;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.ComputeVision;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Route("api/[controller]")]
[ApiController]
public partial class YoloLabelController(IYoloLabelDataLayer yoloLabelDataLayer) : ControllerBase;