"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { AppFrame } from "@/components/layout/app-frame";
import { InfoCard } from "@/components/ui/info-card";
import { SectionHeading } from "@/components/ui/section-heading";
import { api, roleLabels, visibilityLabels } from "@/features/app/api";
import type {
  AuthMode,
  CreateGroupResponse,
  GroupDetailsResponse,
  GroupMemberResponse,
  GroupVisibility,
  LoginResponse,
  MeResponse,
  MyGroupResponse,
  PublicGroupResponse,
  RegisterResponse,
  UpdateGroupResponse,
  UserSession,
} from "@/features/app/types";

export function F1ShotsWebApp() {
  const [mode, setMode] = useState<AuthMode>("register");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [userName, setUserName] = useState("");
  const [isAuthenticating, setIsAuthenticating] = useState(false);
  const [statusMessage, setStatusMessage] = useState(
    "Wpisz dane i utwórz konto w lokalnym API.",
  );
  const [authResult, setAuthResult] = useState<LoginResponse | null>(null);
  const [registerResult, setRegisterResult] = useState<RegisterResponse | null>(
    null,
  );
  const [session, setSession] = useState<UserSession | null>(null);

  const [currentUser, setCurrentUser] = useState<MeResponse | null>(null);
  const [myGroups, setMyGroups] = useState<MyGroupResponse[]>([]);
  const [publicGroups, setPublicGroups] = useState<PublicGroupResponse[]>([]);
  const [selectedGroupDetails, setSelectedGroupDetails] =
    useState<GroupDetailsResponse | null>(null);
  const [selectedGroupMembers, setSelectedGroupMembers] = useState<
    GroupMemberResponse[]
  >([]);
  const [latestJoinCodeByGroupId, setLatestJoinCodeByGroupId] = useState<
    Record<string, string>
  >({});

  const [createGroupName, setCreateGroupName] = useState("");
  const [createVisibility, setCreateVisibility] =
    useState<GroupVisibility>(0);
  const [joinCode, setJoinCode] = useState("");
  const [updateGroupName, setUpdateGroupName] = useState("");
  const [updateVisibility, setUpdateVisibility] =
    useState<GroupVisibility>(0);
  const [isLoadingHome, setIsLoadingHome] = useState(false);
  const [homeStatusMessage, setHomeStatusMessage] = useState(
    "Po zalogowaniu odświeżymy dane użytkownika i grup.",
  );

  const canSubmitAuth = useMemo(() => {
    const commonFieldsFilled = email.trim().length > 0 && password.length > 0;
    return mode === "register"
      ? commonFieldsFilled && userName.trim().length > 0
      : commonFieldsFilled;
  }, [email, mode, password, userName]);

  const loadSelectedGroup = useCallback(
    async (groupId: string, accessToken?: string) => {
      const token = accessToken ?? session?.accessToken;
      if (!token) return;

      try {
        const details = await api.get<GroupDetailsResponse>(
          `/api/Groups/${groupId}`,
          token,
        );

        setSelectedGroupDetails(details);
        setUpdateGroupName(details.name);
        setUpdateVisibility(details.visibility);

        if (details.isMember) {
          const members = await api.get<GroupMemberResponse[]>(
            `/api/Groups/${groupId}/members`,
            token,
          );
          setSelectedGroupMembers(members);
        } else {
          setSelectedGroupMembers([]);
        }
      } catch (error) {
        setHomeStatusMessage(
          error instanceof Error
            ? error.message
            : "Nie udało się pobrać szczegółów grupy.",
        );
      }
    },
    [session?.accessToken],
  );

  const refreshHome = useCallback(async () => {
    if (!session) return;

    setIsLoadingHome(true);

    try {
      const [me, mine, publicList] = await Promise.all([
        api.get<MeResponse>("/api/User/me", session.accessToken),
        api.get<MyGroupResponse[]>("/api/Groups/mine", session.accessToken),
        api.get<PublicGroupResponse[]>("/api/Groups/public", session.accessToken),
      ]);

      setCurrentUser(me);
      setMyGroups(mine);
      setPublicGroups(publicList);
      setHomeStatusMessage("Dane zostały odświeżone.");

      if (selectedGroupDetails) {
        await loadSelectedGroup(selectedGroupDetails.id, session.accessToken);
      }
    } catch (error) {
      setHomeStatusMessage(
        error instanceof Error ? error.message : "Nie udało się pobrać danych.",
      );
    } finally {
      setIsLoadingHome(false);
    }
  }, [loadSelectedGroup, selectedGroupDetails, session]);

  useEffect(() => {
    if (session && !currentUser && !isLoadingHome) {
      void refreshHome();
    }
  }, [currentUser, isLoadingHome, refreshHome, session]);

  async function submitAuth() {
    if (!canSubmitAuth) return;

    setIsAuthenticating(true);
    setAuthResult(null);
    setRegisterResult(null);

    try {
      if (mode === "register") {
        const response = await api.post<
          { email: string; userName: string; password: string },
          RegisterResponse
        >("/api/Auth/register", {
          email: email.trim(),
          userName: userName.trim(),
          password,
        });

        setRegisterResult(response);
        setStatusMessage(
          "Rejestracja zakończona powodzeniem. Teraz możesz przełączyć się na logowanie.",
        );
      } else {
        const response = await api.post<
          { email: string; password: string },
          LoginResponse
        >("/api/Auth/login", {
          email: email.trim(),
          password,
        });

        setAuthResult(response);
        setSession({
          email: email.trim(),
          accessToken: response.accessToken,
          refreshToken: response.refreshToken,
          accessTokenExpiresAtUtc: response.accessTokenExpiresAtUtc,
        });
        setStatusMessage("Logowanie zakończone powodzeniem.");
      }
    } catch (error) {
      setStatusMessage(
        error instanceof Error ? error.message : "Wystąpił nieznany błąd.",
      );
    } finally {
      setIsAuthenticating(false);
    }
  }

  async function createGroup() {
    if (!session || !createGroupName.trim()) return;

    setIsLoadingHome(true);

    try {
      const response = await api.post<
        { name: string; visibility: GroupVisibility },
        CreateGroupResponse
      >(
        "/api/Groups",
        {
          name: createGroupName.trim(),
          visibility: createVisibility,
        },
        session.accessToken,
      );

      setLatestJoinCodeByGroupId((current) => ({
        ...current,
        [response.id]: response.joinCode,
      }));
      setCreateGroupName("");
      setUpdateGroupName(response.name);
      setUpdateVisibility(response.visibility);
      setHomeStatusMessage(`Grupa ${response.name} została utworzona.`);
      await refreshHome();
      await loadSelectedGroup(response.id, session.accessToken);
    } catch (error) {
      setHomeStatusMessage(
        error instanceof Error
          ? error.message
          : "Nie udało się utworzyć grupy.",
      );
    } finally {
      setIsLoadingHome(false);
    }
  }

  async function joinGroup() {
    if (!session || !joinCode.trim()) return;

    setIsLoadingHome(true);

    try {
      const response = await api.post<
        { joinCode: string },
        { groupId: string }
      >(
        "/api/Groups/00000000-0000-0000-0000-000000000000/join",
        { joinCode: joinCode.trim().toUpperCase() },
        session.accessToken,
      );

      setJoinCode("");
      setHomeStatusMessage("Dołączono do grupy.");
      await refreshHome();
      await loadSelectedGroup(response.groupId, session.accessToken);
    } catch (error) {
      setHomeStatusMessage(
        error instanceof Error
          ? error.message
          : "Nie udało się dołączyć do grupy.",
      );
    } finally {
      setIsLoadingHome(false);
    }
  }

  async function updateSelectedGroup() {
    if (!session || !selectedGroupDetails) return;

    setIsLoadingHome(true);

    try {
      const response = await api.patch<
        { name?: string; visibility?: GroupVisibility },
        UpdateGroupResponse
      >(
        `/api/Groups/${selectedGroupDetails.id}`,
        {
          name: updateGroupName.trim() || undefined,
          visibility: updateVisibility,
        },
        session.accessToken,
      );

      setLatestJoinCodeByGroupId((current) => ({
        ...current,
        [response.id]: response.joinCode,
      }));
      setHomeStatusMessage("Zmiany grupy zostały zapisane.");
      await refreshHome();
      await loadSelectedGroup(response.id, session.accessToken);
    } catch (error) {
      setHomeStatusMessage(
        error instanceof Error
          ? error.message
          : "Nie udało się zaktualizować grupy.",
      );
    } finally {
      setIsLoadingHome(false);
    }
  }

  async function leaveSelectedGroup() {
    if (!session || !selectedGroupDetails) return;

    setIsLoadingHome(true);

    try {
      await api.postWithoutResponse(
        `/api/Groups/${selectedGroupDetails.id}/leave`,
        session.accessToken,
      );
      setSelectedGroupDetails(null);
      setSelectedGroupMembers([]);
      setHomeStatusMessage("Opuściłeś grupę.");
      await refreshHome();
    } catch (error) {
      setHomeStatusMessage(
        error instanceof Error
          ? error.message
          : "Nie udało się opuścić grupy.",
      );
    } finally {
      setIsLoadingHome(false);
    }
  }

  function logout() {
    setSession(null);
    setAuthResult(null);
    setRegisterResult(null);
    setCurrentUser(null);
    setMyGroups([]);
    setPublicGroups([]);
    setSelectedGroupDetails(null);
    setSelectedGroupMembers([]);
    setLatestJoinCodeByGroupId({});
    setPassword("");
    setStatusMessage("Sesja została wyczyszczona lokalnie.");
    setHomeStatusMessage("Sesja została wyczyszczona lokalnie.");
  }

  return (
    <AppFrame>
      <main className="flex flex-1 flex-col gap-8">
        <section className="relative overflow-hidden rounded-[36px] border border-[color:var(--card-border)] bg-[radial-gradient(circle_at_top_left,_rgba(221,28,26,0.18),_transparent_32%),linear-gradient(135deg,_rgba(255,255,255,0.9),_rgba(255,255,255,0.66))] px-6 py-8 shadow-[0_24px_80px_rgba(15,23,42,0.12)] backdrop-blur xl:px-10 xl:py-10 dark:bg-[radial-gradient(circle_at_top_left,_rgba(255,77,79,0.25),_transparent_32%),linear-gradient(135deg,_rgba(17,24,39,0.94),_rgba(17,24,39,0.78))]">
          <div className="absolute right-0 top-0 h-48 w-48 rounded-full bg-[color:var(--gold)]/20 blur-3xl" />
          <div className="relative grid gap-8 lg:grid-cols-[1.2fr_0.8fr]">
            <div className="space-y-4">
              <p className="text-xs font-semibold uppercase tracking-[0.28em] text-[var(--accent)]">
                F1Shots Web Client
              </p>
              <h1 className="max-w-3xl text-4xl font-semibold tracking-tight text-[var(--foreground)] sm:text-5xl">
                Same backend flows as iOS and Android, now on the web.
              </h1>
              <p className="max-w-2xl text-base leading-8 text-[var(--muted)] sm:text-lg">
                Register, login, manage groups, join with code, inspect members,
                and edit the selected group from one dashboard talking directly
                to your RadWareF1 API.
              </p>
            </div>

            <InfoCard
              title={session ? "Session active" : "Auth status"}
              eyebrow="Runtime"
              accent={session ? "gold" : "red"}
            >
              <p>API base URL: {api.baseUrl}</p>
              <p>
                {session
                  ? `Logged in as ${session.email}`
                  : "No active session yet. Use register or login to continue."}
              </p>
            </InfoCard>
          </div>
        </section>

        {!session ? (
          <section className="grid gap-8 lg:grid-cols-[0.92fr_1.08fr]">
            <InfoCard title="Auth flow" eyebrow="Register or Login" accent="red">
              <div className="space-y-4">
                <div className="flex flex-wrap gap-3">
                  {(["register", "login"] as const).map((option) => (
                    <button
                      key={option}
                      type="button"
                      onClick={() => {
                        setMode(option);
                        setStatusMessage(
                          option === "register"
                            ? "Wpisz dane i utwórz konto w lokalnym API."
                            : "Wpisz dane istniejącego konta i pobierz token JWT.",
                        );
                      }}
                      className={`rounded-full px-4 py-2 text-sm font-semibold transition ${
                        mode === option
                          ? "bg-[var(--accent)] text-white"
                          : "border border-[color:var(--card-border)] bg-white/70 text-[var(--foreground)] dark:bg-white/5"
                      }`}
                    >
                      {option === "register" ? "Register" : "Login"}
                    </button>
                  ))}
                </div>

                <div className="grid gap-4">
                  {mode === "register" ? (
                    <Input
                      label="User name"
                      value={userName}
                      onChange={setUserName}
                    />
                  ) : null}
                  <Input label="Email" value={email} onChange={setEmail} />
                  <Input
                    label="Password"
                    value={password}
                    onChange={setPassword}
                    type="password"
                  />
                </div>

                <button
                  type="button"
                  onClick={() => void submitAuth()}
                  disabled={isAuthenticating || !canSubmitAuth}
                  className="rounded-full bg-[var(--accent)] px-5 py-3 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)] disabled:cursor-not-allowed disabled:opacity-55"
                >
                  {isAuthenticating
                    ? "Working..."
                    : mode === "register"
                      ? "Create account"
                      : "Sign in"}
                </button>
              </div>
            </InfoCard>

            <div className="grid gap-4">
              <InfoCard title="Status" eyebrow="Feedback" accent="slate">
                <p>{statusMessage}</p>
              </InfoCard>

              {authResult ? (
                <InfoCard title="Tokens" eyebrow="Login response" accent="gold">
                  <p>Access token: {authResult.accessToken}</p>
                  <p>Refresh token: {authResult.refreshToken}</p>
                  <p>Expires at: {authResult.accessTokenExpiresAtUtc}</p>
                </InfoCard>
              ) : null}

              {registerResult ? (
                <InfoCard
                  title="User created"
                  eyebrow="Register response"
                  accent="gold"
                >
                  <p>User ID: {registerResult.userId}</p>
                </InfoCard>
              ) : null}
            </div>
          </section>
        ) : (
          <>
            <section className="grid gap-8 lg:grid-cols-[1.05fr_0.95fr]">
              <div className="grid gap-4">
                <SectionHeading
                  eyebrow="Dashboard"
                  title="Groups, members, and session state in one place."
                  description="This mirrors the mobile clients and uses the same API endpoints, so web can evolve in lockstep with iOS and Android."
                />

                <div className="grid gap-4 md:grid-cols-3">
                  <StatCard label="My groups" value={String(myGroups.length)} />
                  <StatCard
                    label="Public groups"
                    value={String(publicGroups.length)}
                  />
                  <StatCard
                    label="Selected members"
                    value={String(selectedGroupMembers.length)}
                  />
                </div>
              </div>

              <InfoCard title="Session controls" eyebrow="Actions" accent="gold">
                <div className="flex flex-wrap gap-3">
                  <ActionButton
                    label="Refresh"
                    onClick={() => void refreshHome()}
                    disabled={isLoadingHome}
                  />
                  <ActionButton label="Log out" onClick={logout} variant="dark" />
                </div>
                <p>{currentUser ? `Logged in as ${currentUser.email}` : "Loading user info..."}</p>
                <p>{homeStatusMessage}</p>
              </InfoCard>
            </section>

            <section className="grid gap-8 xl:grid-cols-[0.9fr_1.1fr]">
              <div className="grid gap-4">
                <InfoCard title="Create group" eyebrow="POST /api/Groups" accent="red">
                  <div className="grid gap-4">
                    <Input
                      label="Group name"
                      value={createGroupName}
                      onChange={setCreateGroupName}
                    />
                    <VisibilitySelector
                      value={createVisibility}
                      onChange={setCreateVisibility}
                    />
                    <ActionButton
                      label="Create"
                      onClick={() => void createGroup()}
                      disabled={isLoadingHome || !createGroupName.trim()}
                    />
                  </div>
                </InfoCard>

                <InfoCard
                  title="Join public group"
                  eyebrow="POST /api/Groups/{groupId}/join"
                  accent="slate"
                >
                  <div className="grid gap-4">
                    <Input
                      label="Join code"
                      value={joinCode}
                      onChange={setJoinCode}
                    />
                    <ActionButton
                      label="Join with code"
                      onClick={() => void joinGroup()}
                      disabled={isLoadingHome || !joinCode.trim()}
                    />
                  </div>
                </InfoCard>
              </div>

              <div className="grid gap-4">
                <InfoCard title="My groups" eyebrow="GET /api/Groups/mine" accent="gold">
                  <GroupList
                    items={myGroups.map((group) => ({
                      key: `my-${group.id}`,
                      title: group.name,
                      subtitle: `${visibilityLabels[group.visibility]} • ${group.membersCount} members`,
                      actionLabel:
                        selectedGroupDetails?.id === group.id ? "Selected" : "Open",
                      onAction: () => void loadSelectedGroup(group.id),
                    }))}
                    emptyText="Nie masz jeszcze żadnej grupy. Stwórz własną albo dołącz kodem."
                  />
                </InfoCard>

                <InfoCard
                  title="Public groups"
                  eyebrow="GET /api/Groups/public"
                  accent="slate"
                >
                  <GroupList
                    items={publicGroups.map((group) => ({
                      key: `public-${group.id}`,
                      title: group.name,
                      subtitle: `${group.membersCount} members`,
                      actionLabel: "Details",
                      onAction: () => void loadSelectedGroup(group.id),
                    }))}
                    emptyText="Brak publicznych grup do wyświetlenia."
                  />
                </InfoCard>
              </div>
            </section>

            {selectedGroupDetails ? (
              <section className="grid gap-8 lg:grid-cols-[0.95fr_1.05fr]">
                <InfoCard
                  title={selectedGroupDetails.name}
                  eyebrow="Selected group"
                  accent="red"
                >
                  <p>
                    {visibilityLabels[selectedGroupDetails.visibility]} •{" "}
                    {selectedGroupDetails.membersCount} members
                  </p>
                  <p>Created: {selectedGroupDetails.createdAtUtc}</p>
                  {latestJoinCodeByGroupId[selectedGroupDetails.id] ? (
                    <p>
                      Join code: {latestJoinCodeByGroupId[selectedGroupDetails.id]}
                    </p>
                  ) : null}
                  {selectedGroupDetails.isMember ? (
                    <div className="pt-2">
                      <ActionButton
                        label="Leave group"
                        onClick={() => void leaveSelectedGroup()}
                        disabled={isLoadingHome}
                        variant="dark"
                      />
                    </div>
                  ) : null}
                </InfoCard>

                <div className="grid gap-4">
                  <InfoCard
                    title="Update group"
                    eyebrow="PATCH /api/Groups/{groupId}"
                    accent="gold"
                  >
                    <div className="grid gap-4">
                      <Input
                        label="New group name"
                        value={updateGroupName}
                        onChange={setUpdateGroupName}
                      />
                      <VisibilitySelector
                        value={updateVisibility}
                        onChange={setUpdateVisibility}
                      />
                      <ActionButton
                        label="Save changes"
                        onClick={() => void updateSelectedGroup()}
                        disabled={isLoadingHome}
                      />
                    </div>
                  </InfoCard>

                  <InfoCard
                    title="Members"
                    eyebrow="GET /api/Groups/{groupId}/members"
                    accent="slate"
                  >
                    {selectedGroupMembers.length === 0 ? (
                      <p>
                        Brak członków do wyświetlenia albo lista jeszcze się
                        ładuje.
                      </p>
                    ) : (
                      <div className="space-y-3">
                        {selectedGroupMembers.map((member) => (
                          <div
                            key={`member-${member.userId}`}
                            className="flex flex-col justify-between gap-2 rounded-2xl border border-[color:var(--card-border)] bg-white/60 px-4 py-3 dark:bg-white/4 sm:flex-row sm:items-center"
                          >
                            <p className="font-mono text-xs text-[var(--foreground)]">
                              {member.userId}
                            </p>
                            <p className="text-sm font-semibold text-[var(--muted)]">
                              {roleLabels[member.role]}
                            </p>
                          </div>
                        ))}
                      </div>
                    )}
                  </InfoCard>
                </div>
              </section>
            ) : null}
          </>
        )}
      </main>
    </AppFrame>
  );
}

function Input({
  label,
  value,
  onChange,
  type = "text",
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
}) {
  return (
    <label className="grid gap-2">
      <span className="text-sm font-semibold text-[var(--foreground)]">
        {label}
      </span>
      <input
        type={type}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="rounded-2xl border border-[color:var(--card-border)] bg-white/80 px-4 py-3 text-sm text-[var(--foreground)] outline-none transition focus:border-[var(--accent)] focus:ring-4 focus:ring-[color:var(--accent)]/10 dark:bg-white/5"
      />
    </label>
  );
}

function VisibilitySelector({
  value,
  onChange,
}: {
  value: GroupVisibility;
  onChange: (value: GroupVisibility) => void;
}) {
  return (
    <div className="grid gap-2">
      <span className="text-sm font-semibold text-[var(--foreground)]">
        Visibility
      </span>
      <div className="flex flex-wrap gap-3">
        {([0, 1, 2] as GroupVisibility[]).map((option) => (
          <button
            key={option}
            type="button"
            onClick={() => onChange(option)}
            className={`rounded-full px-4 py-2 text-sm font-semibold transition ${
              value === option
                ? "bg-[var(--accent)] text-white"
                : "border border-[color:var(--card-border)] bg-white/70 text-[var(--foreground)] dark:bg-white/5"
            }`}
          >
            {visibilityLabels[option]}
          </button>
        ))}
      </div>
    </div>
  );
}

function ActionButton({
  label,
  onClick,
  disabled,
  variant = "accent",
}: {
  label: string;
  onClick: () => void;
  disabled?: boolean;
  variant?: "accent" | "dark";
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className={`rounded-full px-5 py-3 text-sm font-semibold transition disabled:cursor-not-allowed disabled:opacity-55 ${
        variant === "accent"
          ? "bg-[var(--accent)] text-white hover:bg-[var(--accent-strong)]"
          : "bg-[#101828] text-white hover:bg-[#1f2937]"
      }`}
    >
      {label}
    </button>
  );
}

function StatCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-[24px] border border-[color:var(--card-border)] bg-[var(--card)] px-5 py-6 shadow-[0_18px_50px_rgba(15,23,42,0.08)] backdrop-blur">
      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--muted)]">
        {label}
      </p>
      <p className="mt-3 text-3xl font-semibold tracking-tight text-[var(--foreground)]">
        {value}
      </p>
    </div>
  );
}

function GroupList({
  items,
  emptyText,
}: {
  items: Array<{
    key: string;
    title: string;
    subtitle: string;
    actionLabel: string;
    onAction: () => void;
  }>;
  emptyText: string;
}) {
  if (items.length === 0) {
    return <p>{emptyText}</p>;
  }

  return (
    <div className="space-y-3">
      {items.map((item) => (
        <div
          key={item.key}
          className="flex flex-col justify-between gap-3 rounded-2xl border border-[color:var(--card-border)] bg-white/60 px-4 py-4 dark:bg-white/4 sm:flex-row sm:items-center"
        >
          <div>
            <p className="text-base font-semibold text-[var(--foreground)]">
              {item.title}
            </p>
            <p className="text-sm text-[var(--muted)]">{item.subtitle}</p>
          </div>
          <ActionButton label={item.actionLabel} onClick={item.onAction} />
        </div>
      ))}
    </div>
  );
}
