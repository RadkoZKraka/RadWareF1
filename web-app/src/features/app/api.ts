import type {
  ApiMessageResponse,
  GroupRole,
  GroupVisibility,
} from "@/features/app/types";

const apiBaseUrl =
  process.env.NEXT_PUBLIC_API_URL?.replace(/\/$/, "") ?? "http://localhost:5016";

export class ApiError extends Error {}

function visibilityFromValue(value: number): GroupVisibility {
  if (value === 1 || value === 2) return value;
  return 0;
}

function roleFromValue(value: number): GroupRole {
  if (value === 1 || value === 2) return value;
  return 0;
}

function reviveEnums(value: unknown): unknown {
  if (Array.isArray(value)) {
    return value.map(reviveEnums);
  }

  if (value && typeof value === "object") {
    const record = value as Record<string, unknown>;
    const next: Record<string, unknown> = {};

    for (const [key, entry] of Object.entries(record)) {
      if (key === "visibility" && typeof entry === "number") {
        next[key] = visibilityFromValue(entry);
      } else if (key === "role" && typeof entry === "number") {
        next[key] = roleFromValue(entry);
      } else {
        next[key] = reviveEnums(entry);
      }
    }

    return next;
  }

  return value;
}

async function request<T>(
  path: string,
  init: RequestInit = {},
  accessToken?: string,
): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      ...(init.headers ?? {}),
    },
    cache: "no-store",
  });

  if (!response.ok) {
    const maybeJson = (await response
      .json()
      .catch(() => null)) as ApiMessageResponse | null;
    throw new ApiError(
      maybeJson?.message ?? `Serwer zwrócił status ${response.status}.`,
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const json = (await response.json()) as unknown;
  return reviveEnums(json) as T;
}

export const api = {
  baseUrl: apiBaseUrl,
  get: <T>(path: string, accessToken?: string) =>
    request<T>(path, { method: "GET" }, accessToken),
  post: <TRequest, TResponse>(
    path: string,
    body: TRequest,
    accessToken?: string,
  ) =>
    request<TResponse>(
      path,
      { method: "POST", body: JSON.stringify(body) },
      accessToken,
    ),
  patch: <TRequest, TResponse>(
    path: string,
    body: TRequest,
    accessToken?: string,
  ) =>
    request<TResponse>(
      path,
      { method: "PATCH", body: JSON.stringify(body) },
      accessToken,
    ),
  postWithoutResponse: (path: string, accessToken?: string) =>
    request<void>(
      path,
      { method: "POST", body: JSON.stringify({}) },
      accessToken,
    ),
};

export const visibilityLabels: Record<GroupVisibility, string> = {
  0: "Public",
  1: "Private",
  2: "Friends",
};

export const roleLabels: Record<GroupRole, string> = {
  0: "User",
  1: "Admin",
  2: "Owner",
};
