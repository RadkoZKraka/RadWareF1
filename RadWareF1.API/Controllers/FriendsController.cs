using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadWareF1.Application.Abstractions;
using RadWareF1.Application.Contracts.Friends;

namespace RadWareF1.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FriendsController : ControllerBase
{
    private readonly IFriendService _friendService;

    public FriendsController(IFriendService friendService)
    {
        _friendService = friendService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FriendResponse>>> GetFriends(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var response = await _friendService.GetFriendsAsync(userId, cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<AddFriendResponse>> AddFriend(
        AddFriendRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _friendService.AddFriendAsync(userId, request, cancellationToken);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        if (Guid.TryParse(userIdClaim, out userId))
        {
            return true;
        }

        userId = Guid.Empty;
        return false;
    }
}

