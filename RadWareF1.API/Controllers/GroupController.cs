using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadWareF1.Application.Abstractions;
using RadWareF1.Application.Contracts.Groups;
using RadWareF1.Application.Contracts.Groups.JoinGroup;

namespace RadWareF1.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly IGroupService _groupService;

    public GroupsController(IGroupService groupService)
    {
        _groupService = groupService;
    }

    [HttpPost]
    public async Task<ActionResult<CreateGroupResponse>> Create(
        CreateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var response = await _groupService.CreateAsync(userId, request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<PublicGroupResponse>>> GetPublicGroups(
        CancellationToken cancellationToken)
    {
        var response = await _groupService.GetPublicGroupsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("{groupId:guid}")]
    public async Task<ActionResult<GroupDetailsResponse>> GetById(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var response = await _groupService.GetByIdAsync(groupId, userId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<MyGroupResponse>>> GetMine(
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var response = await _groupService.GetMineAsync(userId, cancellationToken);
        return Ok(response);
    }


    [HttpPost("{groupId:guid}/join")]
    public async Task<ActionResult<JoinGroupResponse>> Join(
        JoinGroupRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var response = await _groupService.JoinAsync(request, userId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{groupId:guid}/leave")]
    public async Task<IActionResult> Leave(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        await _groupService.LeaveAsync(groupId, userId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{groupId:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<GroupMembersResponse>>> GetMembers(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var response = await _groupService.GetMembersAsync(groupId, userId, cancellationToken);
        return Ok(response);
    }

    [HttpPatch("{groupId:guid}")]
    public async Task<ActionResult<UpdateGroupResponse>> Update(
        Guid groupId,
        UpdateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var response = await _groupService.UpdateAsync(groupId, userId, request, cancellationToken);
        return Ok(response);
    }
}