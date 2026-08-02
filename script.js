const copyButtons=document.querySelectorAll(".copy-ip");
const toast=document.getElementById("toast");
const menuButton=document.getElementById("menuButton");
const mainNav=document.getElementById("mainNav");
const year=document.getElementById("year");
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
