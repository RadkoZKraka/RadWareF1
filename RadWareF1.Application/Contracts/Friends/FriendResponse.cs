namespace RadWareF1.Application.Contracts.Friends;

public class FriendResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = null!;
    public DateTime AddedAtUtc { get; set; }
}

