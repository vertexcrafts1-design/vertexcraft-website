export type CollectionCategory = "mod" | "resourcepack" | "shader";

export type CollectionEntry = {
  projectId: string;
  category: CollectionCategory;
  curatorUuid: string;
  addedAt: string;
  note?: string;
};

export interface Store {
  isVerified(uuid: string): Promise<boolean>;
  listCollection(): Promise<CollectionEntry[]>;
  addCollection(entry: CollectionEntry): Promise<"created" | "duplicate">;
}

export interface KVLike {
  get(key: string): Promise<string | null>;
  put(key: string, value: string): Promise<void>;
}

export class KvStore implements Store {
  constructor(private readonly kv: KVLike) {}

  async isVerified(uuid: string): Promise<boolean> {
    const raw = await this.kv.get("lumina:verified");
    if (!raw) return false;
    const uuids = JSON.parse(raw) as string[];
    const normalized = normalizeUuid(uuid);
    return uuids.some(item => normalizeUuid(item) === normalized);
  }

  async listCollection(): Promise<CollectionEntry[]> {
    const raw = await this.kv.get("lumina:collection");
    if (!raw) return [];
    const value = JSON.parse(raw) as CollectionEntry[];
    return Array.isArray(value) ? value : [];
  }

  async addCollection(entry: CollectionEntry): Promise<"created" | "duplicate"> {
    const items = await this.listCollection();
    if (items.some(item => item.projectId.toLowerCase() === entry.projectId.toLowerCase()))
      return "duplicate";
    items.push(entry);
    await this.kv.put("lumina:collection", JSON.stringify(items));
    return "created";
  }
}

export function normalizeUuid(value: string): string {
  return value.replaceAll("-", "").trim().toLowerCase();
}
