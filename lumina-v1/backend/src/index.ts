import { KvStore, type CollectionCategory, type Store, type KVLike } from "./store";

type MinecraftProfile = { id: string; name: string };

type Dependencies = {
  profileFetch: (accessToken: string) => Promise<MinecraftProfile | null>;
  modrinthFetch: (projectId: string) => Promise<boolean>;
};

type WorkerEnv = {
  LUMINA_KV: KVLike;
};

const jsonHeaders = {
  "content-type": "application/json; charset=utf-8",
  "cache-control": "no-store"
};

const defaultDependencies: Dependencies = {
  profileFetch: async accessToken => {
    try {
      const response = await fetch("https://api.minecraftservices.com/minecraft/profile", {
        headers: { authorization: `Bearer ${accessToken}` }
      });
      if (!response.ok) return null;
      const data = await response.json() as Partial<MinecraftProfile>;
      if (!data.id || !data.name) return null;
      return { id: data.id, name: data.name };
    } catch {
      return null;
    }
  },
  modrinthFetch: async projectId => {
    try {
      const response = await fetch(`https://api.modrinth.com/v2/project/${encodeURIComponent(projectId)}`, {
        headers: { "user-agent": "LUMINA/1.1 (Minecraft Client)" }
      });
      return response.ok;
    } catch {
      return false;
    }
  }
};

function response(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), { status, headers: jsonHeaders });
}

function bearerToken(request: Request): string | null {
  const header = request.headers.get("authorization")?.trim() ?? "";
  if (!header.toLowerCase().startsWith("bearer ")) return null;
  const token = header.slice(7).trim();
  return token.length > 0 ? token : null;
}

function category(value: unknown): CollectionCategory | null {
  return value === "mod" || value === "resourcepack" || value === "shader" ? value : null;
}

function cleanNote(value: unknown): string | undefined {
  if (typeof value !== "string") return undefined;
  const note = value.trim();
  return note.length === 0 ? undefined : note.slice(0, 240);
}

export async function handleRequest(
  request: Request,
  store: Store,
  dependencies: Dependencies = defaultDependencies
): Promise<Response> {
  const url = new URL(request.url);
  const path = url.pathname.replace(/\/+$/, "") || "/";

  if (request.method === "GET" && path.startsWith("/v1/verification/")) {
    const uuid = decodeURIComponent(path.slice("/v1/verification/".length)).trim();
    if (!uuid) return response(400, { error: "missing_uuid" });
    return response(200, { verified: await store.isVerified(uuid) });
  }

  if (request.method === "GET" && path === "/v1/collection") {
    const items = await store.listCollection();
    return response(200, {
      items: [...items].sort((a, b) => b.addedAt.localeCompare(a.addedAt))
    });
  }

  if (request.method === "POST" && path === "/v1/collection") {
    const token = bearerToken(request);
    if (!token) return response(401, { error: "missing_minecraft_session" });

    const profile = await dependencies.profileFetch(token);
    if (!profile) return response(401, { error: "invalid_minecraft_session" });
    if (!await store.isVerified(profile.id)) return response(403, { error: "curator_not_verified" });

    let body: Record<string, unknown>;
    try {
      body = await request.json() as Record<string, unknown>;
    } catch {
      return response(400, { error: "invalid_json" });
    }

    const projectId = typeof body.projectId === "string" ? body.projectId.trim() : "";
    const projectCategory = category(body.category);
    if (!projectId || !projectCategory)
      return response(400, { error: "invalid_collection_entry" });

    if (!await dependencies.modrinthFetch(projectId))
      return response(404, { error: "modrinth_project_not_found" });

    const entry = {
      projectId,
      category: projectCategory,
      curatorUuid: profile.id,
      addedAt: new Date().toISOString(),
      note: cleanNote(body.note)
    };

    const result = await store.addCollection(entry);
    if (result === "duplicate") return response(409, { error: "duplicate_project" });
    return response(201, { item: entry });
  }

  return response(404, { error: "not_found" });
}

export default {
  async fetch(request: Request, env: WorkerEnv): Promise<Response> {
    if (!env.LUMINA_KV) return response(503, { error: "storage_not_configured" });
    return handleRequest(request, new KvStore(env.LUMINA_KV));
  }
};
