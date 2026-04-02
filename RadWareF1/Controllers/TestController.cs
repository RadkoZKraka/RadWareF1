namespace RadWareF1.Controllers;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("test")]
public class TestController : ControllerBase
{
    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<IActionResult> Public()
    {
        var authResult = await HttpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);

        return Ok(new
        {
            request = new
            {
                path = HttpContext.Request.Path.Value,
                method = HttpContext.Request.Method,
                host = HttpContext.Request.Host.Value,
                scheme = HttpContext.Request.Scheme,
                authorizationHeader = Request.Headers.Authorization.ToString()
            },
            authenticateResult = new
            {
                succeeded = authResult.Succeeded,
                failure = authResult.Failure?.ToString(),
                none = authResult.None,
                ticketAuthenticationScheme = authResult.Ticket?.AuthenticationScheme,
                principalIdentityAuthenticated = authResult.Principal?.Identity?.IsAuthenticated,
                principalAuthenticationType = authResult.Principal?.Identity?.AuthenticationType
            },
            httpContextUser = new
            {
                isAuthenticated = User.Identity?.IsAuthenticated,
                authenticationType = User.Identity?.AuthenticationType,
                name = User.Identity?.Name
            },
            claims = User.Claims.Select(c => new
            {
                c.Type,
                c.Value
            }).ToList()
        });
    }

    [HttpGet("manual-auth")]
    [AllowAnonymous]
    public async Task<IActionResult> ManualAuth()
    {
        var authResult = await HttpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);

        if (!authResult.Succeeded)
        {
            return Unauthorized(new
            {
                message = "AuthenticateAsync failed",
                failure = authResult.Failure?.ToString(),
                none = authResult.None
            });
        }

        return Ok(new
        {
            message = "AuthenticateAsync succeeded",
            isAuthenticated = authResult.Principal?.Identity?.IsAuthenticated,
            authenticationType = authResult.Principal?.Identity?.AuthenticationType,
            claims = authResult.Principal?.Claims.Select(c => new
            {
                c.Type,
                c.Value
            }).ToList()
        });
    }

    [HttpGet("secure")]
    [Authorize]
    public IActionResult Secure()
    {
        return Ok(new
        {
            message = "Secure endpoint reached",
            isAuthenticated = User.Identity?.IsAuthenticated,
            authenticationType = User.Identity?.AuthenticationType,
            name = User.FindFirst(ClaimTypes.Name)?.Value,
            email = User.FindFirst(ClaimTypes.Email)?.Value,
            userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            claims = User.Claims.Select(c => new
            {
                c.Type,
                c.Value
            }).ToList()
        });
    }
}