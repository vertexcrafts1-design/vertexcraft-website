const API='https://vertexcraft-api.vertexcrafts1.workers.dev';
const $=s=>document.querySelector(s),$$=s=>[...document.querySelectorAll(s)];
let sort='balance',items=[];
const titles={balance:'Reichste Spieler',playtime:'Meiste Spielzeit',kills:'Meiste Kills',deaths:'Meiste Tode',lastplayed:'Zuletzt aktive Spieler'};
const labels={balance:'Coins',playtime:'Spielzeit',kills:'Kills',deaths:'Tode',lastplayed:'Letzte Aktivität'};
function esc(v=''){return String(v).replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;').replaceAll('"','&quot;').replaceAll("'",'&#039;')}
function coins(v){return new Intl.NumberFormat('de-DE',{maximumFractionDigits:2}).format(Number(v||0))+' Coins'}
function play(ms){let s=Math.floor(Number(ms||0)/1000),h=Math.floor(s/3600),m=Math.floor((s%3600)/60);return h>0?`${h} Std. ${m} Min.`:`${m} Min.`}
function seen(ts,on){if(on)return'Jetzt online';if(!ts)return'Unbekannt';const d=Math.max(0,Date.now()-Number(ts)),m=Math.floor(d/60000);if(m<60)return`vor ${Math.max(1,m)} Min.`;const h=Math.floor(m/60);if(h<24)return`vor ${h} Std.`;const days=Math.floor(h/24);return`vor ${days} Tag${days===1?'':'en'}`}
function value(p){if(sort==='balance')return coins(p.balance);if(sort==='playtime')return play(p.playtimeMillis);if(sort==='kills')return String(p.kills||0);if(sort==='deaths')return String(p.deaths||0);return seen(p.lastPlayed,p.online)}
async function get(path){const r=await fetch(API+path,{headers:{Accept:'application/json'}});let d={};try{d=await r.json()}catch{}if(!r.ok)throw new Error(d.error||'request_failed');return d}
async function load(){
 $('#apiState').textContent='Lädt …';$('#apiHint').textContent='Live-Daten werden abgefragt';
 try{
   const q=$('#playerSearch').value.trim();
   const d=await get(`/api/public/stats?sort=${encodeURIComponent(sort)}&limit=100&search=${encodeURIComponent(q)}`);
   items=Array.isArray(d.items)?d.items:[];
   $('#playerCount').textContent=d.total??items.length;$('#currentBoard').textContent=labels[sort];$('#apiState').textContent='Online';$('#apiHint').textContent='Minecraft-API verbunden';
   render();
 }catch(e){
   items=[];$('#apiState').textContent='Nicht erreichbar';$('#apiHint').textContent='Public-Stats-API oder Worker noch nicht aktiv';
   $('#leaderRows').innerHTML='<tr><td colspan="5" class="loading">Die öffentliche API ist noch nicht erreichbar. Installiere zuerst VertexPublicStatsWeb auf dem Server und stelle sicher, dass der Worker /api/public/* weiterleitet.</td></tr>';$('#podium').innerHTML='';
 }
}
function render(){
 $('#boardTitle').textContent=titles[sort];$('#valueHeader').textContent=labels[sort];
 const top=items.slice(0,3);$('#podium').innerHTML=top.map((p,i)=>`<article class="podium-card ${i===0?'first':''}" data-player="${esc(p.name)}"><span class="place">${i+1}</span><div class="avatar">${esc((p.name||'?')[0].toUpperCase())}</div><h3>${esc(p.name)}</h3><p>${esc(p.rank||'Member')}</p><b>${esc(value(p))}</b></article>`).join('');
 $('#leaderRows').innerHTML=items.length?items.map((p,i)=>`<tr><td><b>#${i+1}</b></td><td><div class="row-player" data-player="${esc(p.name)}"><span class="mini-avatar">${esc((p.name||'?')[0].toUpperCase())}</span>${esc(p.name)}</div></td><td class="rank">${esc(p.rank||'Member')}</td><td><strong>${esc(value(p))}</strong></td><td class="${p.online?'online':'offline'}">${p.online?'● Online':'Offline'}</td></tr>`).join(''):'<tr><td colspan="5" class="loading">Keine Spieler gefunden.</td></tr>';
}
async function profile(name){
 try{const p=await get(`/api/public/player?name=${encodeURIComponent(name)}`);$('#profileName').textContent=p.name||name;$('#profileCoins').textContent=coins(p.balance);$('#profilePlaytime').textContent=play(p.playtimeMillis);$('#profileKills').textContent=p.kills||0;$('#profileDeaths').textContent=p.deaths||0;$('#profileRank').textContent=p.rank||'Member';$('#profileSeen').textContent=seen(p.lastPlayed,p.online);$('#profileStage').hidden=false;$('#profileStage').scrollIntoView({behavior:'smooth',block:'start'});}catch{}
}
$$('[data-sort]').forEach(b=>b.addEventListener('click',()=>{$$('[data-sort]').forEach(x=>x.classList.toggle('active',x===b));sort=b.dataset.sort;load()}));
$('#reload').addEventListener('click',load);$('#searchButton').addEventListener('click',load);$('#playerSearch').addEventListener('keydown',e=>{if(e.key==='Enter')load()});document.addEventListener('click',e=>{const p=e.target.closest('[data-player]');if(p)profile(p.dataset.player)});$('#closeProfile').addEventListener('click',()=>$('#profileStage').hidden=true);$('#year').textContent=new Date().getFullYear();load();