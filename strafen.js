
const sanctionEntries = [
  {
    id: "VC-1048",
    player: "Gamefly",
    uuid: "2f94d885-3e8f-4a80-9043-10b978d46c21",
    type: "ban",
    reason: "Unerlaubte Client-Modifikation",
    staff: "Console",
    createdAt: "2026-08-02T18:40:00+02:00",
    expiresAt: "2026-08-09T18:40:00+02:00",
    active: true,
    duration: "7 Tage"
  },
  {
    id: "VC-1047",
    player: "Player123",
    uuid: "b47e58a2-41c1-4cda-b316-c7447f8fe349",
    type: "warn",
    reason: "Respektloses Verhalten im Chat",
    staff: "Moderator",
    createdAt: "2026-08-02T17:15:00+02:00",
    expiresAt: null,
    active: true,
    duration: "Permanent gespeichert"
  },
  {
    id: "VC-1046",
    player: "BuilderMax",
    uuid: "a52102ef-3976-41e6-93c1-5b1a3f6f1957",
    type: "kick",
    reason: "AFK während eines Events",
    staff: "EventTeam",
    createdAt: "2026-08-02T16:21:00+02:00",
    expiresAt: null,
    active: false,
    duration: "Sofortmaßnahme"
  },
  {
    id: "VC-1045",
    player: "Gamefly",
    uuid: "2f94d885-3e8f-4a80-9043-10b978d46c21",
    type: "warn",
    reason: "Spam im globalen Chat",
    staff: "Helper",
    createdAt: "2026-07-28T20:12:00+02:00",
    expiresAt: null,
    active: false,
    duration: "Erledigt"
  },
  {
    id: "VC-1044",
    player: "MiningFox",
    uuid: "9d620b98-0305-4638-933c-6b2d927d7a28",
    type: "mute",
    reason: "Beleidigung anderer Spieler",
    staff: "Moderator",
    createdAt: "2026-07-27T13:00:00+02:00",
    expiresAt: "2026-07-28T13:00:00+02:00",
    active: false,
    duration: "24 Stunden"
  },
  {
    id: "VC-1043",
    player: "Gamefly",
    uuid: "2f94d885-3e8f-4a80-9043-10b978d46c21",
    type: "kick",
    reason: "Unangemessener Skin",
    staff: "Moderator",
    createdAt: "2026-07-18T14:40:00+02:00",
    expiresAt: null,
    active: false,
    duration: "Sofortmaßnahme"
  },
  {
    id: "VC-1042",
    player: "RedstonePro",
    uuid: "108ea439-d917-47b8-b78c-8f8e67c6209a",
    type: "ban",
    reason: "Ausnutzen eines Duplication-Bugs",
    staff: "Admin",
    createdAt: "2026-07-12T22:35:00+02:00",
    expiresAt: null,
    active: true,
    duration: "Permanent"
  },
  {
    id: "VC-1041",
    player: "OldPlayer",
    uuid: "8d13b5d4-f5cc-488f-a2f6-130702a09f05",
    type: "unban",
    reason: "Bann nach erfolgreichem Entbannungsantrag aufgehoben",
    staff: "Admin",
    createdAt: "2026-07-08T12:00:00+02:00",
    expiresAt: null,
    active: false,
    duration: "Aufgehoben"
  },
  {
    id: "VC-1040",
    player: "OldPlayer",
    uuid: "8d13b5d4-f5cc-488f-a2f6-130702a09f05",
    type: "ban",
    reason: "Griefing in einem fremden Claim",
    staff: "Moderator",
    createdAt: "2026-07-01T18:20:00+02:00",
    expiresAt: null,
    active: false,
    duration: "Ursprünglich permanent"
  }
];

const typeLabels = {
  ban: "Bann",
  warn: "Verwarnung",
  mute: "Mute",
  kick: "Kick",
  unban: "Aufhebung"
};

const typeIcons = {
  ban: "⛔",
  warn: "⚠",
  mute: "🔇",
  kick: "↪",
  unban: "✓"
};

const list = document.getElementById("sanctionsList");
const search = document.getElementById("playerSearch");
const clearSearch = document.getElementById("clearSearch");
const sortSelect = document.getElementById("sortSelect");
const resultCount = document.getElementById("resultCount");
const emptyState = document.getElementById("emptyState");
const historyModal = document.getElementById("playerHistoryModal");

let currentFilter = "all";
let currentSearch = "";
let currentSort = "newest";

function formatDate(value) {
  if (!value) return "–";
  return new Intl.DateTimeFormat("de-DE", {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function filteredEntries() {
  let rows = sanctionEntries.filter(entry => {
    const matchesFilter = currentFilter === "all" || entry.type === currentFilter;
    const needle = currentSearch.trim().toLowerCase();
    const matchesSearch =
      !needle ||
      entry.player.toLowerCase().includes(needle) ||
      entry.uuid.toLowerCase().includes(needle) ||
      entry.reason.toLowerCase().includes(needle);

    return matchesFilter && matchesSearch;
  });

  rows.sort((a, b) => {
    if (currentSort === "oldest") {
      return new Date(a.createdAt) - new Date(b.createdAt);
    }
    if (currentSort === "player") {
      return a.player.localeCompare(b.player, "de");
    }
    return new Date(b.createdAt) - new Date(a.createdAt);
  });

  return rows;
}

function renderStats() {
  document.getElementById("activeBanCount").textContent =
    sanctionEntries.filter(e => e.type === "ban" && e.active).length;

  document.getElementById("warningCount").textContent =
    sanctionEntries.filter(e => e.type === "warn").length;

  document.getElementById("otherCount").textContent =
    sanctionEntries.filter(e => !["ban", "warn"].includes(e.type)).length;
}

function renderSanctions() {
  if (!list) return;

  const rows = filteredEntries();
  resultCount.textContent = rows.length;
  emptyState.hidden = rows.length !== 0;
  list.innerHTML = "";

  rows.forEach(entry => {
    const card = document.createElement("article");
    card.className = `sanction-entry type-${entry.type}`;
    card.tabIndex = 0;

    const status = entry.active ? "Aktiv" : "Abgeschlossen";

    card.innerHTML = `
      <div class="sanction-type-icon">${typeIcons[entry.type]}</div>
      <div class="sanction-main">
        <div class="sanction-entry-head">
          <div>
            <span class="sanction-type-label">${typeLabels[entry.type]}</span>
            <h3>${escapeHtml(entry.player)}</h3>
          </div>
          <span class="sanction-status ${entry.active ? "is-active" : "is-closed"}">${status}</span>
        </div>
        <p class="sanction-reason">${escapeHtml(entry.reason)}</p>
        <div class="sanction-meta">
          <span><b>ID</b>${escapeHtml(entry.id)}</span>
          <span><b>Teammitglied</b>${escapeHtml(entry.staff)}</span>
          <span><b>Datum</b>${formatDate(entry.createdAt)}</span>
          <span><b>Dauer</b>${escapeHtml(entry.duration)}</span>
        </div>
      </div>
      <button class="history-button" type="button" aria-label="Historie von ${escapeHtml(entry.player)} öffnen">Historie</button>
    `;

    const open = () => openPlayerHistory(entry.player);
    card.querySelector(".history-button").addEventListener("click", event => {
      event.stopPropagation();
      open();
    });
    card.addEventListener("click", open);
    card.addEventListener("keydown", event => {
      if (event.key === "Enter" || event.key === " ") open();
    });

    list.appendChild(card);
  });
}

function openPlayerHistory(player) {
  const entries = sanctionEntries
    .filter(entry => entry.player.toLowerCase() === player.toLowerCase())
    .sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));

  if (!entries.length || !historyModal) return;

  const first = entries[0];
  document.getElementById("historyAvatar").textContent = player.slice(0, 1).toUpperCase();
  document.getElementById("historyPlayerName").textContent = player;
  document.getElementById("historyPlayerUuid").textContent = first.uuid;
  document.getElementById("historyTotal").textContent = entries.length;
  document.getElementById("historyActive").textContent = entries.filter(e => e.active).length;
  document.getElementById("historyLatest").textContent = formatDate(first.createdAt);

  const timeline = document.getElementById("historyTimeline");
  timeline.innerHTML = "";

  entries.forEach(entry => {
    const item = document.createElement("article");
    item.className = `history-item type-${entry.type}`;
    item.innerHTML = `
      <div class="history-dot">${typeIcons[entry.type]}</div>
      <div>
        <div class="history-item-head">
          <strong>${typeLabels[entry.type]}</strong>
          <span>${formatDate(entry.createdAt)}</span>
        </div>
        <p>${escapeHtml(entry.reason)}</p>
        <small>${escapeHtml(entry.id)} · ${escapeHtml(entry.staff)} · ${escapeHtml(entry.duration)}</small>
      </div>
    `;
    timeline.appendChild(item);
  });

  historyModal.classList.add("open");
  historyModal.setAttribute("aria-hidden", "false");
}

document.querySelectorAll("[data-sanction-filter]").forEach(button => {
  button.addEventListener("click", () => {
    document.querySelectorAll("[data-sanction-filter]").forEach(tab => tab.classList.remove("active"));
    button.classList.add("active");
    currentFilter = button.dataset.sanctionFilter;
    renderSanctions();
  });
});

search?.addEventListener("input", () => {
  currentSearch = search.value;
  renderSanctions();
});

clearSearch?.addEventListener("click", () => {
  search.value = "";
  currentSearch = "";
  search.focus();
  renderSanctions();
});

sortSelect?.addEventListener("change", () => {
  currentSort = sortSelect.value;
  renderSanctions();
});

document.querySelectorAll("[data-close-history]").forEach(element => {
  element.addEventListener("click", () => {
    historyModal?.classList.remove("open");
    historyModal?.setAttribute("aria-hidden", "true");
  });
});

document.addEventListener("keydown", event => {
  if (event.key === "Escape") {
    historyModal?.classList.remove("open");
    historyModal?.setAttribute("aria-hidden", "true");
  }
});

renderStats();
renderSanctions();
