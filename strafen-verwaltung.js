
const STORAGE_KEY = "vertexcraft_punishments_v1";

const form = document.getElementById("punishmentForm");
const entriesContainer = document.getElementById("adminEntries");
const emptyState = document.getElementById("adminEmptyState");
const countEl = document.getElementById("localEntryCount");
const activeCountEl = document.getElementById("localActiveCount");
const searchEl = document.getElementById("adminSearch");
const filterEl = document.getElementById("adminFilter");
const exportBtn = document.getElementById("exportData");
const importInput = document.getElementById("importData");
const clearBtn = document.getElementById("clearAllData");

const labels = {
  ban: "Bann",
  warn: "Verwarnung",
  mute: "Mute",
  kick: "Kick",
  unban: "Unban",
  other: "Sonstige"
};

function getEntries() {
  try {
    return JSON.parse(localStorage.getItem(STORAGE_KEY) || "[]");
  } catch {
    return [];
  }
}

function setEntries(entries) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(entries));
}

function nowForInput() {
  const d = new Date();
  d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
  return d.toISOString().slice(0, 16);
}

document.getElementById("createdAt").value = nowForInput();

function makeId() {
  return "VC-" + Date.now().toString(36).toUpperCase();
}

function escapeHtml(value = "") {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function formatDate(value) {
  if (!value) return "–";
  return new Intl.DateTimeFormat("de-DE", {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

function showToast(message) {
  const toast = document.getElementById("toast");
  if (!toast) return;
  toast.textContent = message;
  toast.classList.add("show");
  setTimeout(() => toast.classList.remove("show"), 1800);
}

function renderEntries() {
  const all = getEntries();
  const query = (searchEl.value || "").trim().toLowerCase();
  const filter = filterEl.value;

  const filtered = all
    .filter(entry => filter === "all" || entry.type === filter)
    .filter(entry =>
      !query ||
      entry.player.toLowerCase().includes(query) ||
      entry.reason.toLowerCase().includes(query) ||
      (entry.staff || "").toLowerCase().includes(query)
    )
    .sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));

  countEl.textContent = all.length;
  activeCountEl.textContent = all.filter(e => e.status === "active").length;

  entriesContainer.innerHTML = "";
  emptyState.hidden = filtered.length > 0;

  filtered.forEach(entry => {
    const card = document.createElement("article");
    card.className = `admin-entry type-${entry.type}`;
    card.innerHTML = `
      <div class="admin-entry-head">
        <div>
          <span class="sanction-type-label">${labels[entry.type] || "Maßnahme"}</span>
          <h3>${escapeHtml(entry.player)}</h3>
        </div>
        <span class="sanction-status ${entry.status === "active" ? "is-active" : "is-closed"}">
          ${entry.status === "active" ? "Aktiv" : "Abgeschlossen"}
        </span>
      </div>

      <p class="admin-entry-reason">${escapeHtml(entry.reason)}</p>

      <div class="admin-entry-meta">
        <span><b>ID</b>${escapeHtml(entry.id)}</span>
        <span><b>Teammitglied</b>${escapeHtml(entry.staff)}</span>
        <span><b>Beginn</b>${formatDate(entry.createdAt)}</span>
        <span><b>Dauer</b>${escapeHtml(entry.duration || "–")}</span>
      </div>

      ${entry.internalNote ? `<details><summary>Interne Notiz</summary><p>${escapeHtml(entry.internalNote)}</p></details>` : ""}

      <div class="admin-entry-actions">
        <button data-action="toggle" data-id="${entry.id}">
          ${entry.status === "active" ? "Als abgeschlossen markieren" : "Wieder aktivieren"}
        </button>
        <button class="delete-entry" data-action="delete" data-id="${entry.id}">Löschen</button>
      </div>
    `;
    entriesContainer.appendChild(card);
  });
}

form.addEventListener("submit", event => {
  event.preventDefault();
  const data = new FormData(form);

  const entry = {
    id: makeId(),
    player: String(data.get("player") || "").trim(),
    uuid: String(data.get("uuid") || "").trim(),
    type: String(data.get("type") || "other"),
    status: String(data.get("status") || "active"),
    reason: String(data.get("reason") || "").trim(),
    staff: String(data.get("staff") || "").trim(),
    duration: String(data.get("duration") || "").trim(),
    createdAt: String(data.get("createdAt") || ""),
    expiresAt: String(data.get("expiresAt") || ""),
    internalNote: String(data.get("internalNote") || "").trim()
  };

  const entries = getEntries();
  entries.push(entry);
  setEntries(entries);

  form.reset();
  document.getElementById("createdAt").value = nowForInput();
  document.getElementById("type").value = "ban";
  document.getElementById("status").value = "active";

  renderEntries();
  showToast("Maßnahme gespeichert");
});

entriesContainer.addEventListener("click", event => {
  const button = event.target.closest("button[data-action]");
  if (!button) return;

  const entries = getEntries();
  const index = entries.findIndex(entry => entry.id === button.dataset.id);
  if (index < 0) return;

  if (button.dataset.action === "delete") {
    if (!confirm("Diesen Eintrag wirklich löschen?")) return;
    entries.splice(index, 1);
    showToast("Eintrag gelöscht");
  } else if (button.dataset.action === "toggle") {
    entries[index].status = entries[index].status === "active" ? "closed" : "active";
    showToast("Status geändert");
  }

  setEntries(entries);
  renderEntries();
});

searchEl.addEventListener("input", renderEntries);
filterEl.addEventListener("change", renderEntries);

clearBtn.addEventListener("click", () => {
  if (!confirm("Wirklich alle lokal gespeicherten Einträge löschen?")) return;
  localStorage.removeItem(STORAGE_KEY);
  renderEntries();
  showToast("Alle Daten gelöscht");
});

exportBtn.addEventListener("click", () => {
  const blob = new Blob([JSON.stringify(getEntries(), null, 2)], { type: "application/json" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = "vertexcraft-strafen-export.json";
  link.click();
  URL.revokeObjectURL(url);
});

importInput.addEventListener("change", async () => {
  const file = importInput.files?.[0];
  if (!file) return;

  try {
    const imported = JSON.parse(await file.text());
    if (!Array.isArray(imported)) throw new Error("Ungültiges Format");
    setEntries(imported);
    renderEntries();
    showToast("Daten importiert");
  } catch {
    alert("Die Datei konnte nicht importiert werden.");
  }

  importInput.value = "";
});

renderEntries();
