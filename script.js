(() => {
  const menuButton = document.getElementById('menuButton');
  const nav = document.getElementById('mainNav');
  const toast = document.getElementById('toast');
  let toastTimer;
  const closeMenu = () => { nav?.classList.remove('open'); menuButton?.setAttribute('aria-expanded', 'false'); menuButton?.setAttribute('aria-label', 'Menü öffnen'); };
  menuButton?.addEventListener('click', () => {
    const open = nav.classList.toggle('open');
    menuButton.setAttribute('aria-expanded', String(open));
    menuButton.setAttribute('aria-label', open ? 'Menü schließen' : 'Menü öffnen');
  });
  nav?.querySelectorAll('a').forEach(link => link.addEventListener('click', closeMenu));
  document.addEventListener('keydown', event => {
    if (event.key === 'Escape' && nav?.classList.contains('open')) {
      const focusedInside = nav.contains(document.activeElement);
      closeMenu(); if (focusedInside) menuButton.focus();
    }
  });
  document.querySelectorAll('.copy-ip').forEach(button => button.addEventListener('click', async () => {
    let message;
    try { await navigator.clipboard.writeText(button.dataset.ip); message = 'Serveradresse kopiert. Wir sehen uns am Spawn!'; }
    catch { message = 'Serveradresse: ' + button.dataset.ip; }
    if (!toast) return;
    toast.textContent = message; toast.classList.add('show');
    clearTimeout(toastTimer); toastTimer = setTimeout(() => toast.classList.remove('show'), 3000);
  }));
  const year = document.getElementById('year');
  if (year) year.textContent = new Date().getFullYear();
  // The announcement is a date, not a verified live server status.
  if (Date.now() >= Date.parse('2026-10-03T00:00:00+02:00')) {
    document.querySelectorAll('[data-season-label]').forEach(label => { label.textContent = 'Season 2 · angekündigter Start: 3. Oktober'; });
  }
})();
