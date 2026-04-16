export type AuthMode = "register" | "login";

export type GroupVisibility = 0 | 1 | 2;

export type GroupRole = 0 | 1 | 2;

export type RegisterRequest = {
  email: string;
  userName: string;
  password: string;
};

export type LoginRequest = {
  email: string;
  password: string;
};

export type RegisterResponse = {
  userId: string;
};

export type LoginResponse = {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAtUtc: string;
};

export type UserSession = {
  email: string;
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAtUtc: string;
};

export type MeResponse = {
  id: string;
  email: string;
};

export type MyGroupResponse = {
  id: string;
  name: string;
  visibility: GroupVisibility;
  createdAtUtc: string;
  membersCount: number;
};

export type PublicGroupResponse = {
  id: string;
  name: string;
  membersCount: number;
};

export type GroupDetailsResponse = {
  id: string;
  name: string;
  visibility: GroupVisibility;
  createdAtUtc: string;
  membersCount: number;
  isMember: boolean;
};

export type GroupMemberResponse = {
  userId: string;
  role: GroupRole;
};

export type CreateGroupRequest = {
  name: string;
  visibility: GroupVisibility;
};

export type CreateGroupResponse = {
  id: string;
  name: string;
  joinCode: string;
  visibility: GroupVisibility;
  createdAtUtc: string;
};

export type JoinGroupRequest = {
  joinCode: string;
};

export type JoinGroupResponse = {
  groupId: string;
  userId: string;
  joinedAtUtc: string;
};

export type UpdateGroupRequest = {
  name?: string;
  visibility?: GroupVisibility;
};

export type UpdateGroupResponse = {
  id: string;
  name: string;
  joinCode: string;
  visibility: GroupVisibility;
  createdAtUtc: string;
};

export type ApiMessageResponse = {
  message: string;
};
