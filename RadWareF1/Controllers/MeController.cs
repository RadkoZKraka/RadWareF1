namespace RadWareF1.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("me")]
public class MeController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public IActionResult Get()
    {
        return Ok(new
        {
            user = User.Identity?.Name,
            claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }
}