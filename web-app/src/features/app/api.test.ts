import { afterEach, describe, expect, it, vi } from "vitest";
import { ApiError, api } from "@/features/app/api";

describe("api client", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("adds the bearer token header for authenticated requests", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ id: "1" }),
    });

    vi.stubGlobal("fetch", fetchMock);

    await api.get<{ id: string }>("/api/User/me", "token-123");

    expect(fetchMock).toHaveBeenCalledWith(
      "http://localhost:5016/api/User/me",
      expect.objectContaining({
        method: "GET",
        cache: "no-store",
        headers: expect.objectContaining({
          "Content-Type": "application/json",
          Authorization: "Bearer token-123",
        }),
      }),
    );
  });

  it("revives numeric enums returned by the backend", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        status: 200,
        json: async () => ({
          visibility: 2,
          members: [{ role: 1 }],
        }),
      }),
    );

    const response = await api.get<{
      visibility: 0 | 1 | 2;
      members: Array<{ role: 0 | 1 | 2 }>;
    }>("/api/Groups/public");

    expect(response).toEqual({
      visibility: 2,
      members: [{ role: 1 }],
    });
  });

  it("returns undefined for 204 responses", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        status: 204,
      }),
    );

    const response = await api.postWithoutResponse("/api/Groups/1/leave", "token-123");

    expect(response).toBeUndefined();
  });

  it("throws an ApiError with the backend message when the request fails", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: false,
        status: 400,
        json: async () => ({ message: "Join code is invalid." }),
      }),
    );

    await expect(api.post("/api/Groups/join", { joinCode: "BAD" })).rejects.toEqual(
      new ApiError("Join code is invalid."),
    );
  });
});
