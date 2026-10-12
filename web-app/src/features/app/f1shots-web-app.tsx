"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { AppFrame } from "@/components/layout/app-frame";
import { InfoCard } from "@/components/ui/info-card";
import { SectionHeading } from "@/components/ui/section-heading";
import { api, roleLabels, visibilityLabels } from "@/features/app/api";
import type {
  AuthMode,
  FriendResponse,
  CreateGroupResponse,
  GroupDetailsResponse,
  GroupMemberResponse,
  GroupVisibility,
  LoginResponse,
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

  const [myGroups, setMyGroups] = useState<MyGroupResponse[]>([]);
  const [publicGroups, setPublicGroups] = useState<PublicGroupResponse[]>([]);
  const [friends, setFriends] = useState<FriendResponse[]>([]);
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
  const [friendIdentifier, setFriendIdentifier] = useState("");
  const [updateGroupName, setUpdateGroupName] = useState("");
  const [updateVisibility, setUpdateVisibility] =
    useState<GroupVisibility>(0);
  const [isLoadingHome, setIsLoadingHome] = useState(false);
  const [isAddingFriend, setIsAddingFriend] = useState(false);
  const [homeStatusMessage, setHomeStatusMessage] = useState(
    "Po zalogowaniu odświeżymy dane użytkownika i grup.",
  );
  const [friendStatusMessage, setFriendStatusMessage] = useState(
    "Dodaj znajomego po user ID albo e-mailu.",
  );
  const lastLoadedSessionTokenRef = useRef<string | null>(null);

  const sessionUserId = useMemo(
    () => getUserIdFromAccessToken(session?.accessToken),
    [session?.accessToken],
  );

  useEffect(() => {
    if (!session) return;

    window.scrollTo({ top: 0, behavior: "smooth" });
  }, [session]);

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
      const [mine, publicList, friendList] = await Promise.all([
        api.get<MyGroupResponse[]>("/api/Groups/mine", session.accessToken),
        api.get<PublicGroupResponse[]>("/api/Groups/public", session.accessToken),
        api.getFriends(session.accessToken),
      ]);

      setMyGroups(mine);
      setPublicGroups(publicList);
      setFriends(friendList);
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

  async function addFriend() {
    if (!session || !friendIdentifier.trim()) return;

    setIsAddingFriend(true);

    try {
      const response = await api.addFriend(
        { identifier: friendIdentifier.trim() },
        session.accessToken,
      );

      setFriendIdentifier("");
      setFriendStatusMessage(
        response.isNewFriend
          ? `Dodano znajomego: ${response.friend.email}.`
          : `${response.friend.email} jest już w znajomych.`,
      );
      await refreshHome();
    } catch (error) {
      setFriendStatusMessage(
        error instanceof Error
          ? error.message
          : "Nie udało się dodać znajomego.",
      );
    } finally {
      setIsAddingFriend(false);
    }
  }

  useEffect(() => {
    if (!session) {
      lastLoadedSessionTokenRef.current = null;
      return;
    }

    if (lastLoadedSessionTokenRef.current === session.accessToken || isLoadingHome) {
      return;
    }

    lastLoadedSessionTokenRef.current = session.accessToken;
    void refreshHome();
  }, [isLoadingHome, refreshHome, session]);

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
        const loginResponse = await api.post<
          { email: string; password: string },
          LoginResponse
        >("/api/Auth/login", {
          email: email.trim(),
          password,
        });

        setAuthResult(loginResponse);
        setSession({
          email: email.trim(),
          accessToken: loginResponse.accessToken,
          refreshToken: loginResponse.refreshToken,
          accessTokenExpiresAtUtc: loginResponse.accessTokenExpiresAtUtc,
        });
        setStatusMessage(
          "Rejestracja zakończona powodzeniem i zalogowano automatycznie.",
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
    setMyGroups([]);
    setPublicGroups([]);
    setFriends([]);
    setSelectedGroupDetails(null);
    setSelectedGroupMembers([]);
    setLatestJoinCodeByGroupId({});
    setPassword("");
    setIsAddingFriend(false);
    setStatusMessage("Sesja została wyczyszczona lokalnie.");
    setHomeStatusMessage("Sesja została wyczyszczona lokalnie.");
    setFriendIdentifier("");
    setFriendStatusMessage("Dodaj znajomego po user ID albo e-mailu.");
    lastLoadedSessionTokenRef.current = null;
  }

  return (
    <AppFrame>
      <main className="flex flex-1 flex-col gap-10 lg:gap-12">
        {!session ? (
          <section className="relative overflow-hidden rounded-[44px] border border-(--card-border) bg-[linear-gradient(180deg,color-mix(in_srgb,var(--card)_92%,white),var(--card))] px-6 py-10 shadow-(--shadow-soft) xl:px-10 xl:py-14">
            <div className="absolute inset-x-0 top-0 h-px bg-[linear-gradient(90deg,transparent,color-mix(in_srgb,var(--accent)_28%,transparent),transparent)]" />
            <div className="absolute -left-24 -top-24 h-56 w-56 rounded-full bg-(--accent)/12 blur-3xl" />
            <div className="absolute right-0 top-0 h-48 w-48 rounded-full bg-(--gold)/12 blur-3xl" />
            <div className="absolute -bottom-32 right-24 h-52 w-52 rounded-full bg-white/18 blur-3xl dark:bg-white/6" />
            <div className="relative space-y-10">
              <div className="mx-auto flex max-w-3xl flex-col items-center space-y-6 text-center">
                <div className="flex flex-wrap justify-center gap-3 text-[11px] font-semibold uppercase tracking-[0.28em] text-(--muted)">
                  <span className="rounded-full border border-(--card-border) bg-white/60 px-3 py-2 text-(--gold) dark:bg-white/5">
                    F1Shots Race Club
                  </span>
                  <span className="rounded-full border border-(--card-border) bg-white/60 px-3 py-2 dark:bg-white/5">
                    Predictions • leagues • race day drama
                  </span>
                </div>
                <h1 className="max-w-4xl text-5xl font-semibold tracking-[-0.06em] text-foreground sm:text-6xl xl:text-7xl">
                  A premium home for Formula 1 prediction leagues and podium calls.
                </h1>
                <p className="max-w-2xl text-base leading-8 text-(--muted) sm:text-xl">
                  Create private leagues, invite friends with a code, and track who
                  called the grid best — from lights out to the chequered flag.
                </p>
                <div className="grid w-full gap-3 sm:grid-cols-3">
                  <HeroMetric label="Race rooms" value="Private & public" />
                  <HeroMetric label="Picks flow" value="Fast, social" />
                  <HeroMetric label="Feel" value="Paddock premium" />
                </div>
              </div>

              <div className="grid gap-4 lg:grid-cols-[1.15fr_0.85fr]">
                <InfoCard
                  title="Built for race-weekend circles"
                  eyebrow="Product idea"
                  accent="slate"
                >
                  <p>
                    Think fantasy league energy without money involved — just bragging
                    rights, perfect podium calls, and chaos when somebody predicts a
                    wild P7 finish.
                  </p>
                  <p>
                    Use the current backend flow to register, create rooms, invite
                    friends, and shape the base of the prediction product.
                  </p>
                </InfoCard>

                <InfoCard
                  title="Ready for sign-in"
                  eyebrow="Runtime"
                  accent="red"
                >
                  <p>API base URL: {api.baseUrl}</p>
                  <p>
                    Create an account or log in to start building race-prediction
                    leagues.
                  </p>
                </InfoCard>
              </div>
            </div>
          </section>
        ) : null}

        {!session ? (
          <section className="grid gap-8 lg:grid-cols-[0.92fr_1.08fr]">
            <InfoCard title="Enter the paddock" eyebrow="Register or Login" accent="red">
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
                            ? "Załóż konto i przygotuj pierwszą ligę typowania."
                            : "Zaloguj się i wróć do swoich lig oraz typów.",
                        );
                      }}
                      className={`rounded-full px-4 py-2 text-sm font-semibold transition ${
                        mode === option
                          ? "bg-[linear-gradient(135deg,var(--accent),var(--accent-strong))] text-white shadow-[0_12px_30px_rgba(255,77,79,0.26)]"
                          : "border border-(--card-border) bg-white/60 text-foreground dark:bg-white/5"
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
                  className="rounded-full bg-[linear-gradient(135deg,var(--accent),var(--accent-strong))] px-5 py-3 text-sm font-semibold text-white shadow-[0_18px_40px_rgba(255,77,79,0.28)] transition hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-55"
                >
                  {isAuthenticating
                    ? "Launching..."
                    : mode === "register"
                      ? "Create account"
                      : "Enter dashboard"}
                </button>
              </div>
            </InfoCard>

            <div className="grid gap-4">
              <InfoCard title="Race weekend status" eyebrow="Feedback" accent="slate">
                <p>{statusMessage}</p>
              </InfoCard>

              <InfoCard title="What this version already nails" eyebrow="Experience" accent="gold">
                <ul className="space-y-3">
                  <li>• fast auth flow for returning players</li>
                  <li>• invite-only leagues powered by join codes</li>
                  <li>• public league discovery for open race weekends</li>
                </ul>
              </InfoCard>

              {authResult ? (
                <InfoCard title="Session payload" eyebrow="Login response" accent="gold">
                  <p>Access token: {authResult.accessToken}</p>
                  <p>Refresh token: {authResult.refreshToken}</p>
                  <p>Expires at: {authResult.accessTokenExpiresAtUtc}</p>
                </InfoCard>
              ) : null}

              {registerResult ? (
                <InfoCard
                  title="Driver profile created"
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
                  eyebrow="League control"
                  title="Manage your prediction rooms like a race engineer."
                  description="This dashboard already covers the essentials: private leagues, discovery, members, and session state — wrapped in a sharper race-day presentation."
                />

                <div className="grid gap-4 md:grid-cols-3">
                  <StatCard label="My leagues" value={String(myGroups.length)} />
                  <StatCard
                    label="Open leagues"
                    value={String(publicGroups.length)}
                  />
                  <StatCard
                    label="Grid size"
                    value={String(selectedGroupMembers.length)}
                  />
                </div>
              </div>

              <InfoCard title="Command panel" eyebrow="Actions" accent="gold">
                <div className="flex flex-wrap gap-3">
                  <ActionButton
                    label="Refresh"
                    onClick={() => void refreshHome()}
                    disabled={isLoadingHome}
                  />
                  <ActionButton label="Log out" onClick={logout} variant="dark" />
                </div>
                <div className="grid gap-3">
                  <p>{session ? `Signed in as ${session.email}` : "Loading user info..."}</p>
                  <CopyableIdentity label="Unique ID" value={sessionUserId} />
                </div>
                <p>{homeStatusMessage}</p>
              </InfoCard>
            </section>

            <section className="grid gap-8 xl:grid-cols-[0.9fr_1.1fr]">
              <div className="grid gap-4">
                <InfoCard title="Create league" eyebrow="POST /api/Groups" accent="red">
                  <div className="grid gap-4">
                    <Input
                      label="League name"
                      value={createGroupName}
                      onChange={setCreateGroupName}
                    />
                    <VisibilitySelector
                      value={createVisibility}
                      onChange={setCreateVisibility}
                    />
                    <ActionButton
                      label="Create league"
                      onClick={() => void createGroup()}
                      disabled={isLoadingHome || !createGroupName.trim()}
                    />
                  </div>
                </InfoCard>

                <InfoCard
                  title="Join with race code"
                  eyebrow="POST /api/Groups/{groupId}/join"
                  accent="slate"
                >
                  <div className="grid gap-4">
                    <Input
                      label="League code"
                      value={joinCode}
                      onChange={setJoinCode}
                    />
                    <ActionButton
                      label="Join league"
                      onClick={() => void joinGroup()}
                      disabled={isLoadingHome || !joinCode.trim()}
                    />
                  </div>
                </InfoCard>

                <InfoCard
                  title="Add friend"
                  eyebrow="POST /api/Friends"
                  accent="gold"
                >
                  <div className="grid gap-4">
                    <Input
                      label="User ID or email"
                      value={friendIdentifier}
                      onChange={setFriendIdentifier}
                    />
                    <ActionButton
                      label={isAddingFriend ? "Adding..." : "Add friend"}
                      onClick={() => void addFriend()}
                      disabled={isAddingFriend || !friendIdentifier.trim()}
                    />
                    <p className="text-sm leading-7 text-(--muted)">
                      {friendStatusMessage}
                    </p>
                  </div>
                </InfoCard>

                <InfoCard title="Friends" eyebrow="GET /api/Friends" accent="slate">
                  {friends.length === 0 ? (
                    <p>Brak znajomych. Dodaj pierwszą osobę po ID albo e-mailu.</p>
                  ) : (
                    <div className="space-y-3">
                      {friends.map((friend) => (
                        <div
                          key={friend.userId}
                          className="rounded-3xl border border-(--card-border) bg-white/70 px-4 py-4 dark:bg-white/5"
                        >
                          <div className="flex items-center justify-between gap-3">
                            <div>
                              <p className="text-base font-semibold text-foreground">
                                {friend.email}
                              </p>
                              <p className="font-mono text-xs text-(--muted)">
                                {friend.userId}
                              </p>
                            </div>
                            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-(--muted)">
                              {friend.addedAtUtc.slice(0, 10)}
                            </p>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </InfoCard>
              </div>

              <div className="grid gap-4">
                <InfoCard title="My leagues" eyebrow="GET /api/Groups/mine" accent="gold">
                  <GroupList
                    items={myGroups.map((group) => ({
                      key: `my-${group.id}`,
                      title: group.name,
                      subtitle: `${visibilityLabels[group.visibility]} • ${group.membersCount} members`,
                      actionLabel:
                        selectedGroupDetails?.id === group.id ? "Selected" : "Race room",
                      onAction: () => void loadSelectedGroup(group.id),
                    }))}
                    emptyText="Nie masz jeszcze żadnej ligi. Załóż własną albo dołącz kodem od znajomych."
                  />
                </InfoCard>

                <InfoCard
                  title="Open leagues"
                  eyebrow="GET /api/Groups/public"
                  accent="slate"
                >
                  <GroupList
                    items={publicGroups.map((group) => ({
                      key: `public-${group.id}`,
                      title: group.name,
                      subtitle: `${group.membersCount} members`,
                       actionLabel: "Preview",
                      onAction: () => void loadSelectedGroup(group.id),
                    }))}
                    emptyText="Brak otwartych lig do wyświetlenia."
                  />
                </InfoCard>
              </div>
            </section>

            {selectedGroupDetails ? (
              <section className="grid gap-8 lg:grid-cols-[0.95fr_1.05fr]">
                <InfoCard
                  title={selectedGroupDetails.name}
                  eyebrow="Selected league"
                  accent="red"
                >
                  <p>
                    {visibilityLabels[selectedGroupDetails.visibility]} •{" "}
                    {selectedGroupDetails.membersCount} members
                  </p>
                  <p>Created: {selectedGroupDetails.createdAtUtc}</p>
                  {latestJoinCodeByGroupId[selectedGroupDetails.id] ? (
                    <p>League code: {latestJoinCodeByGroupId[selectedGroupDetails.id]}</p>
                  ) : null}
                  {selectedGroupDetails.isMember ? (
                    <div className="pt-2">
                      <ActionButton
                        label="Leave league"
                        onClick={() => void leaveSelectedGroup()}
                        disabled={isLoadingHome}
                        variant="dark"
                      />
                    </div>
                  ) : null}
                </InfoCard>

                <div className="grid gap-4">
                  <InfoCard
                    title="Tune league"
                    eyebrow="PATCH /api/Groups/{groupId}"
                    accent="gold"
                  >
                    <div className="grid gap-4">
                      <Input
                        label="League display name"
                        value={updateGroupName}
                        onChange={setUpdateGroupName}
                      />
                      <VisibilitySelector
                        value={updateVisibility}
                        onChange={setUpdateVisibility}
                      />
                      <ActionButton
                        label="Save setup"
                        onClick={() => void updateSelectedGroup()}
                        disabled={isLoadingHome}
                      />
                    </div>
                  </InfoCard>

                  <InfoCard
                    title="Grid & members"
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
                            className="flex flex-col justify-between gap-2 rounded-2xl border border-(--card-border) bg-white/70 px-4 py-3 dark:bg-white/4 sm:flex-row sm:items-center"
                          >
                            <p className="font-mono text-xs text-foreground">
                              {member.userId}
                            </p>
                            <p className="text-sm font-semibold text-(--muted)">
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
      <span className="text-sm font-semibold text-foreground">
        {label}
      </span>
      <input
        type={type}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="rounded-2xl border border-(--card-border) bg-white/75 px-4 py-3 text-sm text-foreground outline-none transition placeholder:text-(--muted)/70 focus:border-(--gold) focus:bg-white focus:ring-4 focus:ring-(--gold)/10 dark:bg-white/6 dark:focus:bg-white/8"
      />
    </label>
  );
}

function HeroMetric({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-3xl border border-(--card-border) bg-white/70 px-4 py-4 shadow-(--shadow-soft) dark:bg-white/5">
      <p className="text-[11px] font-semibold uppercase tracking-[0.24em] text-(--muted)">
        {label}
      </p>
      <p className="mt-2 text-lg font-semibold text-foreground">{value}</p>
    </div>
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
      <span className="text-sm font-semibold text-foreground">Visibility</span>
      <div className="flex flex-wrap gap-3">
        {([0, 1, 2] as GroupVisibility[]).map((option) => (
          <button
            key={option}
            type="button"
            onClick={() => onChange(option)}
            className={`rounded-full px-4 py-2 text-sm font-semibold transition ${
              value === option
                ? "bg-[linear-gradient(135deg,var(--accent),var(--accent-strong))] text-white shadow-[0_12px_30px_rgba(255,77,79,0.35)]"
                : "border border-(--card-border) bg-white/70 text-foreground dark:bg-white/5"
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
          ? "bg-[linear-gradient(135deg,var(--accent),var(--accent-strong))] text-white shadow-[0_18px_40px_rgba(255,77,79,0.28)] hover:brightness-110"
          : "border border-(--card-border) bg-white/70 text-foreground hover:bg-white dark:bg-white/6 dark:text-white dark:hover:bg-white/10"
      }`}
    >
      {label}
    </button>
  );
}

function StatCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-[26px] border border-(--card-border) bg-[linear-gradient(180deg,var(--card-elevated),var(--card))] px-5 py-6 shadow-(--shadow-soft)">
      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-(--muted)">
        {label}
      </p>
      <p className="mt-3 text-3xl font-semibold tracking-tight text-foreground">
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
          className="flex flex-col justify-between gap-3 rounded-3xl border border-(--card-border) bg-white/70 px-4 py-4 shadow-(--shadow-soft) sm:flex-row sm:items-center dark:bg-white/5"
        >
          <div>
            <p className="text-base font-semibold text-foreground">
              {item.title}
            </p>
            <p className="text-sm text-(--muted)">{item.subtitle}</p>
          </div>
          <ActionButton label={item.actionLabel} onClick={item.onAction} />
        </div>
      ))}
    </div>
  );
}

function CopyableIdentity({
  label,
  value,
}: {
  label: string;
  value: string | null;
}) {
  const displayValue = value ?? "N/A";

  async function copyValue() {
    if (!value || typeof navigator === "undefined" || !navigator.clipboard) {
      return;
    }

    await navigator.clipboard.writeText(value);
  }

  return (
    <div className="flex flex-wrap items-center gap-3 rounded-3xl border border-(--card-border) bg-white/70 px-4 py-4 dark:bg-white/5">
      <div className="min-w-0 flex-1">
        <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-(--muted)">
          {label}
        </p>
        <p className="mt-1 break-all font-mono text-sm text-foreground">{displayValue}</p>
      </div>
      <button
        type="button"
        onClick={() => void copyValue()}
        disabled={!value}
        className="rounded-full border border-(--card-border) bg-white/70 px-4 py-2 text-xs font-semibold text-foreground transition hover:bg-white disabled:cursor-not-allowed disabled:opacity-50 dark:bg-white/6 dark:hover:bg-white/10"
      >
        Kopiuj
      </button>
    </div>
  );
}

function getUserIdFromAccessToken(accessToken?: string) {
  if (!accessToken) {
    return null;
  }

  const payload = accessToken.split(".")[1];
  if (!payload) {
    return null;
  }

  try {
    const decoded = JSON.parse(base64UrlDecode(payload)) as Record<string, unknown>;
    const nameIdentifier = decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"];

    if (typeof nameIdentifier === "string" && nameIdentifier.trim().length > 0) {
      return nameIdentifier;
    }

    const subject = decoded.sub;
    if (typeof subject === "string" && subject.trim().length > 0) {
      return subject;
    }

    return null;
  } catch {
    return null;
  }
}

function base64UrlDecode(value: string) {
  const normalized = value.replace(/-/g, "+").replace(/_/g, "/");
  const padding = "=".repeat((4 - (normalized.length % 4)) % 4);
  return atob(`${normalized}${padding}`);
}

