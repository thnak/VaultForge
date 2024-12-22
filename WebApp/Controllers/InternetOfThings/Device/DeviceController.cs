using Business.Business.Interfaces.InternetOfThings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.InternetOfThings.Device;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Route("api/[controller]")]
[ApiController]
public partial class DeviceController(IIotDeviceBusinessLayer deviceBusinessLayer, IIoTSensorBusinessLayer sensorBusinessLayer) : ControllerBase;