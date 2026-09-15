import { describe, expect, it } from "vitest";
import { handleRequest } from "../src/index";
import type { CollectionEntry, Store } from "../src/store";

class MemoryStore implements Store {
  verified = new Set<string>();
  collection: CollectionEntry[] = [];

  async isVerified(uuid: string) { return this.verified.has(uuid); }
  async listCollection() { return this.collection; }
  async addCollection(entry: CollectionEntry) {
    if (this.collection.some(x => x.projectId === entry.projectId)) return "duplicate" as const;
    this.collection.push(entry);
    return "created" as const;
  }
}

const profileFetch = async (token: string) => token === "valid-token"
  ? { id: "verified-uuid", name: "Player" }
  : null;

const modrinthFetch = async (projectId: string) => projectId === "known-project";

describe("LUMINA collection API", () => {
  it("rejects an unverified curator", async () => {
    const store = new MemoryStore();
    const request = new Request("https://lumina.test/v1/collection", {
      method: "POST",
      headers: { "content-type": "application/json", authorization: "Bearer valid-token" },
      body: JSON.stringify({ projectId: "known-project", category: "mod", note: "Fast" })
    });

    const response = await handleRequest(request, store, { profileFetch, modrinthFetch });
    expect(response.status).toBe(403);
  });

  it("accepts a verified Minecraft session and rejects duplicates", async () => {
    const store = new MemoryStore();
    store.verified.add("verified-uuid");
    const makeRequest = () => new Request("https://lumina.test/v1/collection", {
      method: "POST",
      headers: { "content-type": "application/json", authorization: "Bearer valid-token" },
      body: JSON.stringify({ projectId: "known-project", category: "mod" })
    });

    const first = await handleRequest(makeRequest(), store, { profileFetch, modrinthFetch });
    const duplicate = await handleRequest(makeRequest(), store, { profileFetch, modrinthFetch });
    expect(first.status).toBe(201);
    expect(duplicate.status).toBe(409);
  });

  it("lists collection entries for normal users", async () => {
    const store = new MemoryStore();
    store.collection.push({
      projectId: "known-project",
      category: "mod",
      curatorUuid: "verified-uuid",
      addedAt: "2026-09-15T18:00:00.000Z"
    });

    const response = await handleRequest(new Request("https://lumina.test/v1/collection"), store, { profileFetch, modrinthFetch });
    expect(response.status).toBe(200);
    expect((await response.json()).items).toHaveLength(1);
  });
});
