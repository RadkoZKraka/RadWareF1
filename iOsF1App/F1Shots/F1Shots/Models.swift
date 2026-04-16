import Foundation

enum AuthMode: String, CaseIterable, Identifiable {
    case register
    case login

    var id: Self { self }

    var title: String {
        switch self {
        case .register: "Register"
        case .login: "Login"
        }
    }

    var buttonTitle: String {
        switch self {
        case .register: "Create account"
        case .login: "Sign in"
        }
    }
}

enum GroupVisibility: Int, Codable, CaseIterable, Identifiable {
    case `public` = 0
    case `private` = 1
    case friendsOnly = 2

    var id: Int { rawValue }

    var title: String {
        switch self {
        case .public: "Public"
        case .private: "Private"
        case .friendsOnly: "Friends"
        }
    }
}

enum GroupRole: Int, Codable {
    case user = 0
    case admin = 1
    case owner = 2

    var title: String {
        switch self {
        case .user: "User"
        case .admin: "Admin"
        case .owner: "Owner"
        }
    }
}

struct RegisterRequest: Encodable {
    let email: String
    let userName: String
    let password: String
}

struct LoginRequest: Encodable {
    let email: String
    let password: String
}

struct RegisterResponse: Decodable {
    let userId: UUID
}

struct LoginResponse: Decodable {
    let accessToken: String
    let refreshToken: String
    let accessTokenExpiresAtUtc: Date
}

struct UserSession {
    let email: String
    let accessToken: String
    let refreshToken: String
    let accessTokenExpiresAtUtc: Date
}

struct MeResponse: Decodable {
    let id: UUID
    let email: String
}

struct MyGroupResponse: Decodable, Identifiable {
    let id: UUID
    let name: String
    let visibility: GroupVisibility
    let createdAtUtc: Date
    let membersCount: Int
}

struct PublicGroupResponse: Decodable, Identifiable {
    let id: UUID
    let name: String
    let membersCount: Int
}

struct GroupDetailsResponse: Decodable {
    let id: UUID
    let name: String
    let visibility: GroupVisibility
    let createdAtUtc: Date
    let membersCount: Int
    let isMember: Bool
}

struct GroupMemberResponse: Decodable, Identifiable {
    let userId: UUID
    let role: GroupRole

    var id: UUID { userId }
}

struct CreateGroupRequest: Encodable {
    let name: String
    let visibility: GroupVisibility
}

struct CreateGroupResponse: Decodable {
    let id: UUID
    let name: String
    let joinCode: String
    let visibility: GroupVisibility
    let createdAtUtc: Date
}

struct JoinGroupRequest: Encodable {
    let joinCode: String
}

struct JoinGroupResponse: Decodable {
    let groupId: UUID
    let userId: UUID
    let joinedAtUtc: Date
}

struct UpdateGroupRequest: Encodable {
    let name: String?
    let visibility: GroupVisibility?
}

struct UpdateGroupResponse: Decodable {
    let id: UUID
    let name: String
    let joinCode: String
    let visibility: GroupVisibility
    let createdAtUtc: Date
}

struct APIMessageResponse: Decodable {
    let message: String
}

struct EmptyResponse: Decodable {}
