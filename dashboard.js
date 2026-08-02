const API_BASE = "https://vertexcraft-api.vertexcrafts1.workers.dev";
let currentUser = null;
let punishments = [];
const $=s=>document.querySelector(s), $$=s=>[...document.querySelectorAll(s)];
const labels={ban:"Bann",warn:"Verwarnung",kick:"Kick",mute:"Mute",unban:"Unban",unwarn:"Unwarn",other:"Sonstige"};
const icons={ban:"⛔",warn:"⚠",kick:"↪",mute:"🔇",unban:"✓",unwarn:"✓",other:"•"};
function esc(v=""){return String(v).replaceAll("&","&amp;").replaceAll("<","&lt;").replaceAll(">","&gt;").replaceAll('"',"&quot;").replaceAll("'","&#039;")}
function date(v){if(!v)return"–";const d=new Date(Number.isFinite(Number(v))?Number(v):v);return isNaN(d)?"–":new Intl.DateTimeFormat("de-DE",{dateStyle:"medium",timeStyle:"short"}).format(d)}
async function call(path,options={}){const r=await fetch(API_BASE+path,{...options,credentials:"include",headers:{"Content-Type":"application/json",...(options.headers||{})}});let data={};try{data=await r.json()}catch{}if(!r.ok)throw Object.assign(new Error(data.error||"request_failed"),{status:r.status,data});return data}
function isAdmin(){return currentUser&&["owner","dev"].includes(currentUser.role)}
function applyRole(){const admin=isAdmin();$$('[data-role="admin"]').forEach(el=>el.hidden=!admin);$('#dashboardRolePill').textContent=currentUser.role==="owner"?"Owner · Vollzugriff":currentUser.role==="dev"?"Developer · Vollzugriff":"Team · Leserechte";$('#dashboardRolePill').className='role-pill '+currentUser.role;$('#loginGate').classList.add('hidden');document.body.classList.add('authenticated');}
async function checkSession(){try{currentUser=await call('/auth/me');applyRole();await refreshAll()}catch{$('#loginGate').classList.remove('hidden')}}
$('#dashboardLoginForm').addEventListener('submit',async e=>{e.preventDefault();$('#loginError').textContent='';try{currentUser=await call('/auth/login',{method:'POST',body:JSON.stringify({username:$('#loginUsername').value.trim(),password:$('#loginPassword').value})});applyRole();await refreshAll()}catch{$('#loginError').textContent='Benutzername oder Passwort ist falsch.'}})
$('#logoutDashboard').addEventListener('click',async()=>{try{await call('/auth/logout',{method:'POST',body:'{}'})}catch{}location.reload()})
async function refreshAll(){await Promise.all([loadPunishments(),testApi()])}
async function testApi(){try{await call('/auth/me');setApi('online','Online')}catch{setApi('offline','Nicht erreichbar')}}
function setApi(state,text){$('#sidebarApiDot').className=state;$('#sidebarApiText').textContent=text;$('#apiStatusValue').textContent=text;$('#sidebarApiUrl').textContent='Secure Worker API';$('#systemApiUrl').textContent=API_BASE}
function normalize(p){return Array.isArray(p)?p:Array.isArray(p?.items)?p.items:[]}
async function loadPunishments(){try{punishments=normalize(await call('/api/punishments?limit=250&offset=0&type=all'));renderStats();renderRecent();renderList()}catch(e){if(e.status===401)return location.reload();punishments=[];renderStats();$('#dashboardRecentPunishments').innerHTML='<div class="dashboard-loading">Daten konnten nicht geladen werden.</div>'}}
function renderStats(){$('#dashActiveBans').textContent=punishments.filter(x=>x.type==='ban'&&x.active).length;$('#dashWarnings').textContent=punishments.filter(x=>x.type==='warn').length;$('#dashTotalPunishments').textContent=punishments.length}
function card(x,compact=false){const admin=isAdmin();return `<article class="${compact?'dash-recent-item':'dash-full-punishment'} type-${esc(x.type||'other')}"><div class="dash-punishment-icon">${icons[x.type]||'•'}</div><div class="dash-punishment-copy"><div><span>${labels[x.type]||'Maßnahme'}</span><strong>${esc(x.player||'Unbekannt')}</strong></div><p>${esc(x.reason||'Kein Grund')}</p><small>${date(x.createdAt)} · ${esc(x.staff||'Console')} · ${esc(x.id||'')}</small></div><span class="dash-punishment-status ${x.active?'active':'closed'}">${x.active?'Aktiv':'Beendet'}</span>${!compact&&admin?`<div class="punishment-actions">${x.type==='ban'&&x.active?`<button data-manage="unban" data-id="${esc(x.id)}">Unban</button>`:''}<button class="danger" data-manage="delete" data-id="${esc(x.id)}">Löschen</button></div>`:''}</article>`}
function renderRecent(){const rows=[...punishments].sort((a,b)=>(b.createdAt||0)-(a.createdAt||0)).slice(0,5);$('#dashboardRecentPunishments').innerHTML=rows.length?rows.map(x=>card(x,true)).join(''):'<div class="dashboard-empty-panel"><h3>Keine Einträge</h3></div>'}
function filtered(){const q=($('#dashboardPunishmentSearch')?.value||'').toLowerCase(),t=$('#dashboardPunishmentType')?.value||'all';return punishments.filter(x=>(t==='all'||x.type===t)&&(!q||[x.player,x.reason,x.staff,x.uuid].some(v=>String(v||'').toLowerCase().includes(q)))).sort((a,b)=>(b.createdAt||0)-(a.createdAt||0))}
function renderList(){const rows=filtered();$('#dashboardPunishmentList').innerHTML=rows.length?rows.map(x=>card(x)).join(''):'<div class="dashboard-empty-panel"><h3>Keine Einträge gefunden</h3></div>'}
$('#dashboardPunishmentList').addEventListener('click',async e=>{const b=e.target.closest('[data-manage]');if(!b||!isAdmin())return;const action=b.dataset.manage,id=b.dataset.id;const question=action==='delete'?`Eintrag ${id} endgültig löschen?`:`Bann ${id} aufheben?`;if(!confirm(question))return;try{await call('/manage/'+action,{method:'POST',body:JSON.stringify({id,staff:currentUser.user})});await loadPunishments();toast(action==='delete'?'Eintrag gelöscht':'Bann aufgehoben')}catch(err){alert('Aktion fehlgeschlagen: '+(err.data?.error||err.message))}})
let playerSearchTimer = null;
let playerSearchItems = [];

function money(value){
  const number=Number(value||0);
  return new Intl.NumberFormat('de-DE',{maximumFractionDigits:2}).format(number)+' Coins';
}

function relativeSeen(timestamp, online){
  if(online)return 'Jetzt online';
  if(!timestamp)return 'Noch nie gesehen';
  const diff=Math.max(0,Date.now()-Number(timestamp));
  const minutes=Math.floor(diff/60000);
  if(minutes<1)return 'Gerade eben';
  if(minutes<60)return `Vor ${minutes} Min.`;
  const hours=Math.floor(minutes/60);
  if(hours<24)return `Vor ${hours} Std.`;
  const days=Math.floor(hours/24);
  return `Vor ${days} Tag${days===1?'':'en'}`;
}

function renderPlayerSuggestions(items){
  const box=$('#dashboardPlayerSuggestions');
  playerSearchItems=items;
  if(!items.length){
    box.innerHTML='<div class="player-search-empty"><strong>Keine Spieler gefunden</strong><span>Prüfe den Namen oder versuche einen anderen Buchstaben.</span></div>';
    return;
  }
  box.innerHTML=items.map((p,index)=>`<button class="player-suggestion-card" data-player-index="${index}">
    <div class="player-suggestion-avatar">${esc((p.name||'?')[0].toUpperCase())}<span class="player-online-dot ${p.online?'online':'offline'}"></span></div>
    <div class="player-suggestion-main"><strong>${esc(p.name||'Unbekannt')}</strong><span>${esc(p.rank||'Member')} · ${money(p.balance)}</span></div>
    <div class="player-suggestion-side"><b>${p.activePunishments||0} aktiv</b><small>${relativeSeen(p.lastPlayed,p.online)}</small></div>
  </button>`).join('');
}

async function searchPlayerDirectory(query, recent=false){
  const status=$('#playerSearchStatus');
  status.textContent='Spieler werden geladen …';
  try{
    const search=recent?'':query;
    const payload=await call(`/api/players?search=${encodeURIComponent(search)}&limit=30`);
    const items=normalize(payload);
    if(recent)items.sort((a,b)=>(b.lastPlayed||0)-(a.lastPlayed||0));
    status.textContent=`${items.length} Spieler gefunden`;
    renderPlayerSuggestions(items);
  }catch(error){
    status.textContent='Spielerverzeichnis konnte nicht geladen werden.';
    renderPlayerSuggestions([]);
  }
}

async function openPlayerProfile(lookup){
  const result=$('#dashboardPlayerResult');
  result.innerHTML='<div class="dashboard-loading">Spielerprofil wird geladen …</div>';
  try{
    const p=await call('/api/profile/'+encodeURIComponent(lookup));
    const history=Array.isArray(p.history)?p.history:[];
    const counts=p.punishments||{};
    result.innerHTML=`
      <div class="player-profile-hero">
        <div class="dashboard-player-avatar profile-avatar">${esc((p.name||'?')[0].toUpperCase())}<span class="profile-status-dot ${p.online?'online':'offline'}"></span></div>
        <div class="player-profile-title"><span class="portal-badge">${p.online?'ONLINE':'OFFLINE'}</span><h3>${esc(p.name||lookup)}</h3><code>${esc(p.uuid||'Keine UUID')}</code></div>
        <div class="player-profile-rank"><span>Rang</span><strong>${esc(p.rank||'Member')}</strong></div>
      </div>
      <div class="player-profile-stat-grid">
        <article><span>Kontostand</span><strong>${money(p.balance)}</strong><small>Vault / VertexEconomy</small></article>
        <article><span>Letzter Besuch</span><strong>${relativeSeen(p.lastPlayed,p.online)}</strong><small>${p.lastPlayed?date(p.lastPlayed):'Keine Daten'}</small></article>
        <article><span>Strafen gesamt</span><strong>${counts.total||0}</strong><small>${counts.active||0} aktuell aktiv</small></article>
        <article><span>Bans / Warns</span><strong>${counts.bans||0} / ${counts.warns||0}</strong><small>Gespeicherte Historie</small></article>
      </div>
      <div class="player-profile-sections">
        <section><div class="player-section-head"><div><span>ACCOUNT</span><h4>Spielerinformationen</h4></div></div>
          <div class="player-info-list"><div><span>Erstmals gespielt</span><strong>${p.firstPlayed?date(p.firstPlayed):'Unbekannt'}</strong></div><div><span>Zuletzt gespielt</span><strong>${p.lastPlayed?date(p.lastPlayed):'Unbekannt'}</strong></div><div><span>Status</span><strong class="${p.online?'text-online':''}">${p.online?'Online':'Offline'}</strong></div></div>
        </section>
        <section><div class="player-section-head"><div><span>HISTORY</span><h4>Strafhistorie</h4></div><b>${history.length} Einträge</b></div>
          <div class="dashboard-player-history">${history.length?history.map(x=>card(x,true)).join(''):'<div class="clean-player-card"><span>✓</span><div><strong>Keine Strafen vorhanden</strong><p>Für diesen Spieler gibt es aktuell keine Bans, Warns oder Kicks.</p></div></div>'}</div>
        </section>
      </div>`;
    result.scrollIntoView({behavior:'smooth',block:'start'});
  }catch(error){
    result.innerHTML='<div class="dashboard-empty-illustration">×</div><h3>Spieler nicht gefunden</h3><p>Der Spieler war möglicherweise noch nie auf dem Server.</p>';
  }
}

async function searchPlayer(){
  const name=$('#dashboardPlayerSearch').value.trim();
  if(!name)return searchPlayerDirectory('',true);
  await searchPlayerDirectory(name);
  if(playerSearchItems.length===1)openPlayerProfile(playerSearchItems[0].uuid||playerSearchItems[0].name);
}

function openView(v){$$('.dashboard-nav a').forEach(a=>a.classList.toggle('active',a.dataset.view===v));$$('.dashboard-view').forEach(s=>s.classList.toggle('active',s.dataset.dashboardView===v));const t={overview:['VERTEXCRAFT DASHBOARD','Übersicht'],players:['SPIELERVERWALTUNG','Spieler'],punishments:['VERTEXCORE LIVE-DATEN','Strafen'],battlepass:['SEASON-SYSTEM','Battle Pass'],events:['COMMUNITY & TURNIERE','Events'],system:['VERBINDUNG','System']}[v]||['VERTEXCRAFT DASHBOARD','Übersicht'];$('#viewEyebrow').textContent=t[0];$('#viewTitle').textContent=t[1];$('#dashboardSidebar').classList.remove('open')}
$$('.dashboard-nav a[data-view]').forEach(a=>a.addEventListener('click',e=>{e.preventDefault();if(a.hidden)return;openView(a.dataset.view)}));$$('[data-jump-view]').forEach(a=>a.addEventListener('click',e=>{e.preventDefault();openView(a.dataset.jumpView)}));$('#dashboardMenuButton').addEventListener('click',()=>$('#dashboardSidebar').classList.toggle('open'));$('#refreshDashboard').addEventListener('click',refreshAll);$('#dashboardPlayerSearchButton').addEventListener('click',searchPlayer);
$('#dashboardPlayerSearch').addEventListener('keydown',e=>e.key==='Enter'&&searchPlayer());
$('#dashboardPlayerSearch').addEventListener('input',e=>{clearTimeout(playerSearchTimer);const q=e.target.value.trim();if(!q){$('#playerSearchStatus').textContent='Gib mindestens einen Buchstaben ein.';$('#dashboardPlayerSuggestions').innerHTML='';return;}playerSearchTimer=setTimeout(()=>searchPlayerDirectory(q),180)});
$('#dashboardPlayerSuggestions').addEventListener('click',e=>{const button=e.target.closest('[data-player-index]');if(!button)return;const p=playerSearchItems[Number(button.dataset.playerIndex)];if(p)openPlayerProfile(p.uuid||p.name)});
$('#showRecentPlayers').addEventListener('click',()=>searchPlayerDirectory('',true));$('#dashboardPunishmentSearch').addEventListener('input',renderList);$('#dashboardPunishmentType').addEventListener('change',renderList);$('#dashboardPunishmentReload').addEventListener('click',loadPunishments);$('#openApiSettings').addEventListener('click',()=>{alert('Die API-Adresse ist fest und nur für Owner/Developer sichtbar. Änderungen erfolgen direkt im Worker.')});$('#systemTestApi').addEventListener('click',testApi);
function toast(m){const t=$('#toast');t.textContent=m;t.classList.add('show');setTimeout(()=>t.classList.remove('show'),1800)}
openView('overview');checkSession();
