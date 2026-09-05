const copyButtons=document.querySelectorAll(".copy-ip");
const toast=document.getElementById("toast");
const menuButton=document.getElementById("menuButton");
const mainNav=document.getElementById("mainNav");
const year=document.getElementById("year");
const imageMap={'assets/vertex-hero-v2.png':'assets/vertex-hero-fixed.jpg','assets/vertex-build-v2.png':'assets/vertex-build-fixed.jpg','assets/vertex-explore-v2.png':'assets/vertex-explore-fixed.jpg','assets/battlepass-v2.png':'assets/battlepass-fixed.jpg'};
document.querySelectorAll('img[src]').forEach(img=>{const fixed=imageMap[img.getAttribute('src')];if(fixed)img.src=fixed});
const fixCss=document.createElement('link');fixCss.rel='stylesheet';fixCss.href='image-fix.css?v=20260905-complete';document.head.append(fixCss);
copyButtons.forEach(button=>button.addEventListener("click",async()=>{
  const ip=button.dataset.ip;
  try{await navigator.clipboard.writeText(ip);toast.textContent="Server-IP kopiert!";}
  catch{toast.textContent=`IP: ${ip}`;}
  toast.classList.add("show");setTimeout(()=>toast.classList.remove("show"),1800);
}));
if(menuButton&&mainNav){
  menuButton.addEventListener("click",()=>{const open=mainNav.classList.toggle("open");menuButton.setAttribute("aria-expanded",String(open));});
  mainNav.querySelectorAll("a").forEach(link=>link.addEventListener("click",()=>{mainNav.classList.remove("open");menuButton.setAttribute("aria-expanded","false");}));
}
if(year)year.textContent=new Date().getFullYear();


// ===== Battle Pass demo data =====
const rewards = [
  {level:1,type:"free",icon:"🪙",title:"500 Coins",description:"Ein kleiner Startbonus für deine Season-Reise."},
  {level:2,type:"premium",icon:"❖",title:"15 Shards",description:"Premium-Belohnung für besondere Käufe und Systeme."},
  {level:3,type:"free",icon:"🥖",title:"Versorgungspaket",description:"Ein nützliches Paket für dein nächstes Abenteuer."},
  {level:4,type:"premium",icon:"🔑",title:"Beach Key",description:"Ein virtueller Schlüssel für eine Beach Crate."},
  {level:5,type:"free",icon:"🪙",title:"1.000 Coins",description:"Mehr Coins für Handel, Orders und Auktionen."},
  {level:6,type:"premium",icon:"✨",title:"Namens-Ticket",description:"Ein Ticket für besondere Anpassungen."},
  {level:7,type:"free",icon:"❖",title:"20 Shards",description:"Zusätzliche Shards als kostenloser Reward."},
  {level:8,type:"premium",icon:"🪽",title:"Fly-Potion",description:"Zeitlich begrenztes Fliegen als Premium-Reward."},
  {level:9,type:"free",icon:"📦",title:"Quest-Paket",description:"Ein gemischtes Paket mit nützlichen Ressourcen."},
  {level:10,type:"premium",icon:"👑",title:"Season-Tag",description:"Ein exklusiver kosmetischer Season-Tag."},
  {level:11,type:"free",icon:"🪙",title:"1.500 Coins",description:"Ein weiterer Fortschrittsbonus."},
  {level:12,type:"premium",icon:"✧",title:"10 Kristalle",description:"Kristalle für besondere Inhalte und Shops."},
  {level:13,type:"free",icon:"🧪",title:"XP-Flasche",description:"Sammle Erfahrung für Verzauberungen."},
  {level:14,type:"premium",icon:"🔑",title:"Honey Key",description:"Virtueller Schlüssel für eine Honey Crate."},
  {level:15,type:"free",icon:"❖",title:"25 Shards",description:"Kostenloser Shard-Reward in der Mitte des Passes."},
  {level:16,type:"premium",icon:"🛡",title:"Cosmetic Shield",description:"Kosmetische Belohnung für dein Profil."},
  {level:17,type:"free",icon:"🪙",title:"2.000 Coins",description:"Großer Coinschub für die zweite Season-Hälfte."},
  {level:18,type:"premium",icon:"⚡",title:"God-Potion",description:"Zeitlich begrenzter besonderer Effekt."},
  {level:19,type:"free",icon:"📜",title:"Quest-Reroll",description:"Tausche später eine Quest gegen eine neue aus."},
  {level:20,type:"premium",icon:"🔑",title:"Blood Key",description:"Virtueller Schlüssel für eine Blood Crate."},
  {level:21,type:"free",icon:"✧",title:"5 Kristalle",description:"Kostenlose Kristalle als besonderer Meilenstein."},
  {level:22,type:"premium",icon:"🎭",title:"Cosmetic Pack",description:"Ein Paket mit rein kosmetischen Inhalten."},
  {level:23,type:"free",icon:"❖",title:"30 Shards",description:"Mehr Shards für deine nächsten Käufe."},
  {level:24,type:"premium",icon:"🔑",title:"Lotus Key",description:"Virtueller Schlüssel für eine Lotus Crate."},
  {level:25,type:"free",icon:"🪙",title:"3.000 Coins",description:"Ein starker Coinsbonus kurz vor dem Finale."},
  {level:26,type:"premium",icon:"🌌",title:"Aura Cosmetic",description:"Ein exklusiver visueller Effekt."},
  {level:27,type:"free",icon:"📦",title:"Mega-Paket",description:"Ein großes gemischtes Versorgungspaket."},
  {level:28,type:"premium",icon:"🔑",title:"Kristall Key",description:"Virtueller Schlüssel für eine hochwertige Crate."},
  {level:29,type:"free",icon:"✧",title:"15 Kristalle",description:"Letzter kostenloser Kristall-Reward."},
  {level:30,type:"premium",icon:"❄",title:"Frosty Finale",description:"Finale Season-Belohnung mit exklusivem Cosmetic und Key."}
];

const track = document.getElementById("battlepassTrack");
const rewardModal = document.getElementById("rewardModal");
if(track){
  const renderRewards = (filter="all") => {
    track.innerHTML = "";
    rewards.filter(r => filter==="all" || r.type===filter).forEach(r => {
      const card = document.createElement("article");
      card.className = `reward-card ${r.type}`;
      card.innerHTML = `<div class="reward-level"><span>STUFE ${r.level}</span><span class="reward-type">${r.type==="premium"?"PREMIUM":"FREE"}</span></div><div class="reward-icon">${r.icon}</div><h3>${r.title}</h3><p>${r.description}</p>`;
      card.addEventListener("click",()=>openReward(r));
      track.appendChild(card);
    });
  };
  renderRewards();
  document.querySelectorAll("[data-pass-filter]").forEach(btn=>{
    btn.addEventListener("click",()=>{
      document.querySelectorAll("[data-pass-filter]").forEach(b=>b.classList.remove("active"));
      btn.classList.add("active");
      renderRewards(btn.dataset.passFilter);
    });
  });
  const allBtn=document.getElementById("showAllRewards");
  if(allBtn) allBtn.addEventListener("click",()=>track.scrollIntoView({behavior:"smooth"}));
}
function openReward(r){
  if(!rewardModal)return;
  document.getElementById("modalIcon").textContent=r.icon;
  document.getElementById("modalType").textContent=r.type==="premium"?"PREMIUM REWARD":"KOSTENLOSER REWARD";
  document.getElementById("modalTitle").textContent=r.title;
  document.getElementById("modalDescription").textContent=r.description;
  document.getElementById("modalLevel").textContent=r.level;
  rewardModal.classList.add("open");
  rewardModal.setAttribute("aria-hidden","false");
}
document.querySelectorAll("[data-close-modal]").forEach(el=>el.addEventListener("click",()=>{
  rewardModal?.classList.remove("open"); rewardModal?.setAttribute("aria-hidden","true");
}));

// ===== Event filters and modal =====
document.querySelectorAll("[data-event-filter]").forEach(btn=>{
  btn.addEventListener("click",()=>{
    document.querySelectorAll("[data-event-filter]").forEach(b=>b.classList.remove("active"));
    btn.classList.add("active");
    const filter=btn.dataset.eventFilter;
    document.querySelectorAll(".event-card").forEach(card=>{
      card.classList.toggle("hidden",filter!=="all" && card.dataset.category!==filter);
    });
  });
});
const eventModal=document.getElementById("eventModal");
document.querySelectorAll(".event-details").forEach(btn=>btn.addEventListener("click",()=>{
  document.getElementById("eventModalTitle").textContent=btn.dataset.eventTitle;
  eventModal?.classList.add("open");
  eventModal?.setAttribute("aria-hidden","false");
}));
document.querySelectorAll("[data-close-event]").forEach(el=>el.addEventListener("click",()=>{
  eventModal?.classList.remove("open"); eventModal?.setAttribute("aria-hidden","true");
}));
document.addEventListener("keydown",e=>{
  if(e.key==="Escape"){
    rewardModal?.classList.remove("open");
    eventModal?.classList.remove("open");
  }
});
