(() => {
  const API = 'https://vertexcraft-api.vertexcrafts1.workers.dev';
  const $ = selector => document.querySelector(selector);
  const tabs = [...document.querySelectorAll('[data-sort]')];
  let sort = 'balance', items = [], boardRequest, profileRequest;
  const titles = {balance:'Reichste Spieler',playtime:'Meiste Spielzeit',kills:'Meiste Kills',deaths:'Meiste Tode',lastplayed:'Zuletzt aktive Spieler'};
  const labels = {balance:'Coins',playtime:'Spielzeit',kills:'Kills',deaths:'Tode',lastplayed:'Letzte Aktivität'};
  const esc = value => String(value ?? '').replace(/[&<>"']/g, char => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#039;'}[char]));
  const number = value => new Intl.NumberFormat('de-DE',{maximumFractionDigits:2}).format(Number(value)||0);
  const coins = value => number(value) + ' Coins';
  const play = ms => { const minutes = Math.max(0,Math.floor((Number(ms)||0)/60000)); const hours = Math.floor(minutes/60); return hours ? `${number(hours)} Std. ${minutes%60} Min.` : `${minutes} Min.`; };
  const seen = (ts,online) => { if(online)return 'Jetzt online'; if(!Number(ts))return 'Unbekannt'; const minutes = Math.floor(Math.max(0,Date.now()-Number(ts))/60000); if(minutes<60)return `vor ${Math.max(1,minutes)} Min.`; const hours=Math.floor(minutes/60); if(hours<24)return `vor ${hours} Std.`; const days=Math.floor(hours/24); return `vor ${days} Tag${days===1?'':'en'}`; };
  const value = p => sort==='balance'?coins(p.balance):sort==='playtime'?play(p.playtimeMillis):sort==='kills'?number(p.kills):sort==='deaths'?number(p.deaths):seen(p.lastPlayed,p.online);
  async function get(path,signal) {
    const response = await fetch(API+path,{headers:{Accept:'application/json'},credentials:'omit',cache:'no-store',signal});
    if(!response.ok)throw new Error('request_failed');
    return response.json();
  }
  function render() {
    $('#podium').innerHTML = items.slice(0,3).map((p,i) => `<button class="podium-card ${i===0?'first':''}" data-player="${esc(p.name)}" aria-label="Profil von ${esc(p.name)} öffnen"><span class="place">#${i+1}</span><span class="avatar">${esc((p.name||'?')[0].toUpperCase())}</span><h3>${esc(p.name)}</h3><p>${esc(p.rank||'Member')}</p><b>${esc(value(p))}</b></button>`).join('');
    $('#leaderRows').innerHTML = items.length ? items.map((p,i)=>`<tr><td>#${i+1}</td><td><button class="row-player" data-player="${esc(p.name)}"><span class="mini-avatar" aria-hidden="true">${esc((p.name||'?')[0].toUpperCase())}</span>${esc(p.name)}</button></td><td class="rank">${esc(p.rank||'Member')}</td><td><strong>${esc(value(p))}</strong></td><td class="${p.online?'online':'offline'}">${p.online?'Online':'Offline'}</td></tr>`).join('') : '<tr><td colspan="5" class="loading">Keine passenden Spieler gefunden. Versuch es mit einem anderen Namen.</td></tr>';
  }
  async function load() {
    boardRequest?.abort(); boardRequest = new AbortController();
    const signal=boardRequest.signal;
    $('#apiState').textContent='Lädt …'; $('#apiHint').textContent='Spielerdaten werden abgefragt.';
    $('#boardTitle').textContent=titles[sort]; $('#valueHeader').textContent=labels[sort]; $('#currentBoard').textContent=labels[sort];
    $('#leaderRows').innerHTML='<tr><td colspan="5" class="loading">Spielerdaten werden geladen …</td></tr>'; $('#podium').replaceChildren(); $('#playerCount').textContent='–';
    $('#profileStage').hidden=true; $('#profileError').textContent='';
    try {
      const data=await get(`/api/public/stats?sort=${encodeURIComponent(sort)}&limit=100&search=${encodeURIComponent($('#playerSearch').value.trim())}`,signal);
      if(signal.aborted)return;
      if(!Array.isArray(data.items))throw new Error('invalid_response');
      items=data.items; $('#playerCount').textContent=number(items.length); $('#apiState').textContent='Aktualisiert';
      $('#apiHint').textContent=items.length===100?'Die ersten 100 Treffer. Suche nach einem Namen.':'Daten direkt vom Server.'; render();
    } catch(error) {
      if(signal.aborted)return;
      items=[]; $('#apiState').textContent='Gerade nicht verfügbar'; $('#apiHint').textContent='Bitte später noch einmal versuchen.';
      $('#leaderRows').innerHTML='<tr><td colspan="5" class="loading">Wir können die Rangliste gerade nicht laden. Versuch es in ein paar Minuten erneut.</td></tr>';
    }
  }
  async function profile(name) {
    profileRequest?.abort(); profileRequest=new AbortController(); const signal=profileRequest.signal;
    $('#profileError').textContent='Profil wird geladen …'; $('#profileStage').hidden=true;
    try {
      const p=await get(`/api/public/player?name=${encodeURIComponent(name)}`,signal);
      if(signal.aborted)return;
      if(!p || typeof p.name!=='string')throw new Error('invalid_profile');
      $('#profileName').textContent=p.name; $('#profileCoins').textContent=coins(p.balance); $('#profilePlaytime').textContent=play(p.playtimeMillis);
      $('#profileKills').textContent=number(p.kills); $('#profileDeaths').textContent=number(p.deaths); $('#profileRank').textContent=p.rank||'Member'; $('#profileSeen').textContent=seen(p.lastPlayed,p.online);
      $('#profileError').textContent=''; $('#profileStage').hidden=false;
      $('#profileStage').scrollIntoView({behavior:matchMedia('(prefers-reduced-motion: reduce)').matches?'auto':'smooth',block:'start'});
      $('#closeProfile').focus({preventScroll:true});
    } catch(error) { if(!signal.aborted)$('#profileError').textContent='Dieses Profil lässt sich gerade nicht laden. Bitte versuche es erneut.'; }
  }
  tabs.forEach(button=>button.addEventListener('click',()=>{tabs.forEach(tab=>{tab.classList.toggle('active',tab===button);tab.setAttribute('aria-pressed',String(tab===button));});sort=button.dataset.sort;load();}));
  $('#statsSearchForm').addEventListener('submit',event=>{event.preventDefault();load();});
  $('#reload').addEventListener('click',load);
  document.addEventListener('click',event=>{const button=event.target.closest('[data-player]');if(button)profile(button.dataset.player);});
  $('#closeProfile').addEventListener('click',()=>{profileRequest?.abort();$('#profileStage').hidden=true;$('#profileError').textContent='';$('#playerSearch').focus({preventScroll:true});});
  load();
})();
