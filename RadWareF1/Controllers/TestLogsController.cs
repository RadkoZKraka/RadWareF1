using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RadWareF1.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class TestLogsController : ControllerBase
{
    private readonly ILogger<TestLogsController> _logger;

    public TestLogsController(ILogger<TestLogsController> logger)
    {
        _logger = logger;
    }

    [HttpGet("ok")]
    public IActionResult OkTest()
    {
        _logger.LogInformation("Test log from endpoint ok");
        return Ok(new { message = "ok" });
    }

    [HttpGet("error")]
    public IActionResult ErrorTest()
    {
        _logger.LogError("Test error log");
        throw new Exception("Test exception");
    }
}