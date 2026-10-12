namespace RadWareF1.Application.Contracts.Friends;

public class AddFriendResponse
{
    public FriendResponse Friend { get; set; } = null!;
    public bool IsNewFriend { get; set; }
}

