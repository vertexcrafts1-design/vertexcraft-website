# VertexCraft Website

Die bestehende Website bleibt unter `web.play-vertex.com`, der Shop unter `play-vertex.shop`. Statische HTML-, CSS- und JavaScript-Dateien; keine neuen Analyse- oder Werbedienste und kein neuer Website-Host.

Öffentliche Seiten: Start, Mitspielen, Systeme, PvP, Vertex Pass, Vote, Season/Events, Regeln, Ranglisten, Hilfe, Entbannung und die bestehenden rechtlichen Informationen. Der tatsächliche Team-Bereich bleibt `dashboard.html`; alte Team-URLs führen zur geschützten Anmeldung. Bestehende API-Routen und Rollen bleiben erhalten.

Lokal prüfen: `python3 tests/site-contracts.py . ../shop`. JavaScript zusätzlich mit `node --check <Datei>` prüfen. Der Shop enthält sinnvolle Kauf-/Webhooktests und ein vorbereitetes, noch nicht auf dem Server installiertes Rangumbau-Paket in seinem Ordner `integration`.

`vertex.css` enthält die öffentliche Gestaltung. `style.css` wird für die bestehende Dashboard-Struktur weiter verwendet, `team.css` passt ihre Farben und Anmeldung an. `dashboard-app.js` enthält die ausgelagerte vorhandene Dashboard-Logik. Anmelde- oder Zahlungsschlüssel gehören nicht in diese Dateien.

Die Inhalte geben bestätigte Serverinformationen wieder. Die Website erfindet keine passgenauen Reward-Listen, Spielerwerte oder Event-Termine. API-Ausfälle zeigen unbekannte Werte und verständliche Hinweise. Der angekündigte Season-Start ist der 3. Oktober 2026; eine Datumsanzeige beweist keinen gestarteten Server.
