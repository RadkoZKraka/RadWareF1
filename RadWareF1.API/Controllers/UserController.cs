using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadWareF1.Application.Abstractions;
using RadWareF1.Application.Contracts.Auth.User;

namespace RadWareF1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken cancellationToken)
    {
        var response = await _userService.GetMeAsync(cancellationToken);
        return Ok(response);
    }
}