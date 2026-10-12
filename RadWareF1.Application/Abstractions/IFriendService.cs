using RadWareF1.Application.Contracts.Friends;

namespace RadWareF1.Application.Abstractions;

public interface IFriendService
{
    Task<IReadOnlyList<FriendResponse>> GetFriendsAsync(Guid currentUserId, CancellationToken cancellationToken);
    Task<AddFriendResponse> AddFriendAsync(Guid currentUserId, AddFriendRequest request, CancellationToken cancellationToken);
}
