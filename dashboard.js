
const DEFAULT_API_BASE = "http://176.9.113.40:25798";
const API_STORAGE_KEY = "vertexcraft_dashboard_api_base";

let apiBase = localStorage.getItem(API_STORAGE_KEY) || DEFAULT_API_BASE;
let dashboardPunishments = [];

const $ = selector => document.querySelector(selector);
const $$ = selector => [...document.querySelectorAll(selector)];

function normalizedApiBase(value) {
  return String(value || "").trim().replace(/\/+$/, "");
}

function escapeDashboardHtml(value = "") {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function dashboardDate(value) {
  if (!value) return "–";
  const date = new Date(Number.isFinite(Number(value)) ? Number(value) : value);
  if (Number.isNaN(date.getTime())) return "–";
  return new Intl.DateTimeFormat("de-DE", {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(date);
}

function setApiUi(state, text) {
  const dot = $("#sidebarApiDot");
  const status = $("#sidebarApiText");
  const value = $("#apiStatusValue");

  dot.className = state;
  status.textContent = text;
  value.textContent = text;

  $("#sidebarApiUrl").textContent = apiBase.replace(/^https?:\/\//, "");
  $("#systemApiUrl").textContent = apiBase;
}

function showDashboardToast(message) {
  const toast = $("#toast");
  if (!toast) return;
  toast.textContent = message;
  toast.classList.add("show");
  setTimeout(() => toast.classList.remove("show"), 1800);
}

function detectMixedContent() {
  const blocked = location.protocol === "https:" && apiBase.startsWith("http://");
  $("#mixedContentAlert").hidden = !blocked;
  return blocked;
}

async function apiFetch(path, options = {}) {
  const url = `${apiBase}${path}`;
  const response = await fetch(url, {
    ...options,
    headers: {
      Accept: "application/json",
      ...(options.headers || {})
    }
  });

  if (!response.ok) {
    throw new Error(`API ${response.status}`);
  }

  return response.json();
}

async function testApi(showResult = false) {
  apiBase = normalizedApiBase(apiBase);
  detectMixedContent();
  setApiUi("checking", "Verbindung wird geprüft");

  try {
    const data = await apiFetch("/health");
    setApiUi("online", "Online");
    if (showResult) {
      $("#apiSettingsResult").className = "api-test-result success";
      $("#apiSettingsResult").textContent = "Verbindung erfolgreich.";
    }
    return data;
  } catch (error) {
    const mixed = location.protocol === "https:" && apiBase.startsWith("http://");
    setApiUi("offline", mixed ? "HTTPS erforderlich" : "Nicht erreichbar");
    if (showResult) {
      $("#apiSettingsResult").className = "api-test-result error";
      $("#apiSettingsResult").textContent = mixed
        ? "Der Browser blockiert HTTP-Daten auf einer HTTPS-Seite."
        : "Die API konnte nicht erreicht werden.";
    }
    return null;
  }
}

function normalizePunishmentPayload(payload) {
  if (Array.isArray(payload)) return payload;
  if (Array.isArray(payload?.items)) return payload.items;
  return [];
}

async function loadPunishments() {
  const container = $("#dashboardRecentPunishments");
  const fullList = $("#dashboardPunishmentList");

  try {
    const payload = await apiFetch("/api/punishments?limit=250&offset=0&type=all");
    dashboardPunishments = normalizePunishmentPayload(payload);
    updatePunishmentStats();
    renderRecentPunishments();
    renderFullPunishmentList();
  } catch (error) {
    dashboardPunishments = [];
    updatePunishmentStats();

    const message = location.protocol === "https:" && apiBase.startsWith("http://")
      ? "Live-Daten werden nach der HTTPS-Einrichtung angezeigt."
      : "Die Strafen-API konnte nicht geladen werden.";

    if (container) container.innerHTML = `<div class="dashboard-loading">${message}</div>`;
    if (fullList) fullList.innerHTML = `<div class="dashboard-empty-panel"><h3>Keine Verbindung</h3><p>${message}</p></div>`;
  }
}

function updatePunishmentStats() {
  const activeBans = dashboardPunishments.filter(item => item.type === "ban" && item.active).length;
  const warnings = dashboardPunishments.filter(item => item.type === "warn").length;

  $("#dashActiveBans").textContent = activeBans;
  $("#dashWarnings").textContent = warnings;
  $("#dashTotalPunishments").textContent = dashboardPunishments.length;
}

const punishmentLabels = {
  ban: "Bann",
  warn: "Verwarnung",
  kick: "Kick",
  mute: "Mute",
  unban: "Unban",
  unwarn: "Unwarn",
  other: "Sonstige"
};

const punishmentIcons = {
  ban: "⛔",
  warn: "⚠",
  kick: "↪",
  mute: "🔇",
  unban: "✓",
  unwarn: "✓",
  other: "•"
};

function punishmentCard(item, compact = false) {
  const type = item.type || "other";
  const active = Boolean(item.active);
  return `
    <article class="${compact ? "dash-recent-item" : "dash-full-punishment"} type-${escapeDashboardHtml(type)}">
      <div class="dash-punishment-icon">${punishmentIcons[type] || "•"}</div>
      <div class="dash-punishment-copy">
        <div>
          <span>${punishmentLabels[type] || "Maßnahme"}</span>
          <strong>${escapeDashboardHtml(item.player || "Unbekannt")}</strong>
        </div>
        <p>${escapeDashboardHtml(item.reason || "Kein Grund angegeben")}</p>
        <small>${dashboardDate(item.createdAt)} · ${escapeDashboardHtml(item.staff || "Console")}</small>
      </div>
      <span class="dash-punishment-status ${active ? "active" : "closed"}">${active ? "Aktiv" : "Beendet"}</span>
    </article>
  `;
}

function renderRecentPunishments() {
  const container = $("#dashboardRecentPunishments");
  if (!container) return;

  const rows = [...dashboardPunishments]
    .sort((a, b) => Number(b.createdAt || 0) - Number(a.createdAt || 0))
    .slice(0, 5);

  container.innerHTML = rows.length
    ? rows.map(row => punishmentCard(row, true)).join("")
    : `<div class="dashboard-empty-panel"><h3>Noch keine Maßnahmen</h3><p>Die API hat keine Einträge zurückgegeben.</p></div>`;
}

function filteredDashboardPunishments() {
  const query = ($("#dashboardPunishmentSearch")?.value || "").toLowerCase().trim();
  const type = $("#dashboardPunishmentType")?.value || "all";

  return dashboardPunishments
    .filter(item => type === "all" || item.type === type)
    .filter(item => {
      if (!query) return true;
      return [item.player, item.reason, item.staff, item.uuid]
        .some(value => String(value || "").toLowerCase().includes(query));
    })
    .sort((a, b) => Number(b.createdAt || 0) - Number(a.createdAt || 0));
}

function renderFullPunishmentList() {
  const container = $("#dashboardPunishmentList");
  if (!container) return;

  const rows = filteredDashboardPunishments();
  container.innerHTML = rows.length
    ? rows.map(row => punishmentCard(row)).join("")
    : `<div class="dashboard-empty-panel"><h3>Keine Einträge gefunden</h3><p>Ändere Suche oder Filter.</p></div>`;
}

async function searchDashboardPlayer() {
  const name = $("#dashboardPlayerSearch").value.trim();
  const result = $("#dashboardPlayerResult");

  if (!name) {
    result.innerHTML = `<div class="dashboard-empty-illustration">!</div><h3>Spielername fehlt</h3><p>Gib einen Namen ein.</p>`;
    return;
  }

  result.innerHTML = `<div class="dashboard-loading">Spieler wird gesucht …</div>`;

  try {
    const payload = await apiFetch(`/api/player/${encodeURIComponent(name)}`);
    const items = normalizePunishmentPayload(payload);
    const player = payload.player || items[0]?.player || name;
    const uuid = payload.uuid || items[0]?.uuid || "Keine UUID verfügbar";
    const active = items.filter(item => item.active).length;

    result.innerHTML = `
      <div class="dashboard-player-head">
        <div class="dashboard-player-avatar">${escapeDashboardHtml(player.slice(0, 1).toUpperCase())}</div>
        <div><span class="portal-badge">SPIELERPROFIL</span><h3>${escapeDashboardHtml(player)}</h3><code>${escapeDashboardHtml(uuid)}</code></div>
      </div>
      <div class="dashboard-player-stats">
        <div><span>Maßnahmen</span><strong>${items.length}</strong></div>
        <div><span>Aktiv</span><strong>${active}</strong></div>
        <div><span>Letzter Eintrag</span><strong>${items.length ? dashboardDate(items[0].createdAt) : "–"}</strong></div>
      </div>
      <div class="dashboard-player-history">
        ${items.length ? items.map(item => punishmentCard(item, true)).join("") : "<p>Keine öffentliche Strafhistorie vorhanden.</p>"}
      </div>
    `;
  } catch (error) {
    result.innerHTML = `<div class="dashboard-empty-illustration">×</div><h3>Spieler nicht gefunden</h3><p>Die API konnte keine Daten liefern.</p>`;
  }
}

function openDashboardView(view) {
  $$(".dashboard-nav a").forEach(link => link.classList.toggle("active", link.dataset.view === view));
  $$(".dashboard-view").forEach(section => section.classList.toggle("active", section.dataset.dashboardView === view));

  const titles = {
    overview: ["VERTEXCRAFT DASHBOARD", "Übersicht"],
    players: ["SPIELERVERWALTUNG", "Spieler"],
    punishments: ["VERTEXCORE LIVE-DATEN", "Strafen"],
    battlepass: ["SEASON-SYSTEM", "Battle Pass"],
    events: ["COMMUNITY & TURNIERE", "Events"],
    system: ["VERBINDUNG", "System"]
  };

  const [eyebrow, title] = titles[view] || titles.overview;
  $("#viewEyebrow").textContent = eyebrow;
  $("#viewTitle").textContent = title;
  $("#dashboardSidebar").classList.remove("open");
}

$$(".dashboard-nav a[data-view]").forEach(link => {
  link.addEventListener("click", event => {
    event.preventDefault();
    const view = link.dataset.view;
    history.replaceState(null, "", `#${view}`);
    openDashboardView(view);
  });
});

$$("[data-jump-view]").forEach(link => {
  link.addEventListener("click", event => {
    event.preventDefault();
    const view = link.dataset.jumpView;
    history.replaceState(null, "", `#${view}`);
    openDashboardView(view);
  });
});

$("#dashboardMenuButton")?.addEventListener("click", () => {
  $("#dashboardSidebar").classList.toggle("open");
});

$("#refreshDashboard")?.addEventListener("click", async () => {
  await testApi();
  await loadPunishments();
  showDashboardToast("Dashboard aktualisiert");
});

$("#dashboardPlayerSearchButton")?.addEventListener("click", searchDashboardPlayer);
$("#dashboardPlayerSearch")?.addEventListener("keydown", event => {
  if (event.key === "Enter") searchDashboardPlayer();
});

$("#dashboardPunishmentSearch")?.addEventListener("input", renderFullPunishmentList);
$("#dashboardPunishmentType")?.addEventListener("change", renderFullPunishmentList);
$("#dashboardPunishmentReload")?.addEventListener("click", loadPunishments);

const apiModal = $("#apiSettingsModal");
$("#openApiSettings")?.addEventListener("click", () => {
  $("#apiBaseInput").value = apiBase;
  $("#apiSettingsResult").textContent = "";
  apiModal.classList.add("open");
  apiModal.setAttribute("aria-hidden", "false");
});
$$("[data-close-api-settings]").forEach(element => {
  element.addEventListener("click", () => {
    apiModal.classList.remove("open");
    apiModal.setAttribute("aria-hidden", "true");
  });
});
$("#saveApiSettings")?.addEventListener("click", async () => {
  apiBase = normalizedApiBase($("#apiBaseInput").value);
  localStorage.setItem(API_STORAGE_KEY, apiBase);
  await testApi(true);
  await loadPunishments();
});
$("#useFutureHttps")?.addEventListener("click", () => {
  $("#apiBaseInput").value = "https://api.play-vertex.com";
});
$("#systemTestApi")?.addEventListener("click", async () => {
  const ok = await testApi();
  showDashboardToast(ok ? "API ist erreichbar" : "API nicht erreichbar");
});

const initialView = location.hash.slice(1);
openDashboardView(["overview", "players", "punishments", "battlepass", "events", "system"].includes(initialView) ? initialView : "overview");
apiBase = normalizedApiBase(apiBase);
$("#apiBaseInput").value = apiBase;
detectMixedContent();
testApi().then(loadPunishments);
