const copyButtons = document.querySelectorAll(".copy-ip");
const toast = document.getElementById("toast");
const menuButton = document.getElementById("menuButton");
const mainNav = document.getElementById("mainNav");
const year = document.getElementById("year");
const discordLink = document.getElementById("discordLink");

// HIER später deinen echten Discord-Link eintragen:
discordLink.href = "https://discord.gg/DEIN-LINK";

copyButtons.forEach((button) => {
  button.addEventListener("click", async () => {
    const ip = button.dataset.ip;

    try {
      await navigator.clipboard.writeText(ip);
      toast.textContent = "Server-IP kopiert!";
    } catch {
      toast.textContent = `IP: ${ip}`;
    }

    toast.classList.add("show");
    setTimeout(() => toast.classList.remove("show"), 1800);
  });
});

menuButton.addEventListener("click", () => {
  const isOpen = mainNav.classList.toggle("open");
  menuButton.setAttribute("aria-expanded", String(isOpen));
});

mainNav.querySelectorAll("a").forEach((link) => {
  link.addEventListener("click", () => {
    mainNav.classList.remove("open");
    menuButton.setAttribute("aria-expanded", "false");
  });
});

year.textContent = new Date().getFullYear();
