package com.radware.f1shots

data class RegisterRequest(
    val email: String,
    val userName: String,
    val password: String
)

data class LoginRequest(
    val email: String,
    val password: String
)

data class RegisterResponse(
    val userId: String
)

data class LoginResponse(
    val accessToken: String,
    val refreshToken: String,
    val accessTokenExpiresAtUtc: String
)

data class UserSession(
    val email: String,
    val accessToken: String,
    val refreshToken: String,
    val accessTokenExpiresAtUtc: String
)

data class MeResponse(
    val id: String,
    val email: String
)

data class MyGroupResponse(
    val id: String,
    val name: String,
    val visibility: GroupVisibility,
    val createdAtUtc: String,
    val membersCount: Int
)

data class PublicGroupResponse(
    val id: String,
    val name: String,
    val membersCount: Int
)

data class GroupDetailsResponse(
    val id: String,
    val name: String,
    val visibility: GroupVisibility,
    val createdAtUtc: String,
    val membersCount: Int,
    val isMember: Boolean
)

data class GroupMemberResponse(
    val userId: String,
    val role: GroupRole
)

data class CreateGroupRequest(
    val name: String,
    val visibility: GroupVisibility
)

data class CreateGroupResponse(
    val id: String,
    val name: String,
    val joinCode: String,
    val visibility: GroupVisibility,
    val createdAtUtc: String
)

data class JoinGroupRequest(
    val joinCode: String
)

data class JoinGroupResponse(
    val groupId: String,
    val userId: String,
    val joinedAtUtc: String
)

data class UpdateGroupRequest(
    val name: String?,
    val visibility: GroupVisibility?
)

data class UpdateGroupResponse(
    val id: String,
    val name: String,
    val joinCode: String,
    val visibility: GroupVisibility,
    val createdAtUtc: String
)

data class ApiMessageResponse(
    val message: String
)

enum class AuthMode(val title: String, val buttonTitle: String) {
    Register("Register", "Create account"),
    Login("Login", "Sign in")
}

enum class GroupVisibility(val title: String) {
    Public("Public"),
    Private("Private"),
    FriendsOnly("Friends");

    companion object {
        fun fromValue(value: Int): GroupVisibility = entries.getOrElse(value) { Public }
    }
}

enum class GroupRole(val title: String) {
    User("User"),
    Admin("Admin"),
    Owner("Owner");

    companion object {
        fun fromValue(value: Int): GroupRole = entries.getOrElse(value) { User }
    }
}
