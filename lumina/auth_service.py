from __future__ import annotations
import json, threading, webbrowser
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path
from urllib.parse import urlparse
import keyring, minecraft_launcher_lib

SERVICE='LUMINA Minecraft Launcher'; TOKEN='microsoft_refresh_token'

class Callback(BaseHTTPRequestHandler):
    url=None
    def do_GET(self):
        self.__class__.url=f"http://{self.headers.get('Host','localhost')}{self.path}"
        body=("<html><body style='background:#09090e;color:#eee;font-family:Segoe UI;display:grid;place-items:center;height:100vh'>"
              "<div><h1 style='color:#8b5cf6'>LUMINA</h1><h2>Anmeldung abgeschlossen</h2><p>Du kannst dieses Fenster schließen.</p></div></body></html>").encode()
        self.send_response(200); self.send_header('Content-Type','text/html; charset=utf-8'); self.send_header('Content-Length',str(len(body))); self.end_headers(); self.wfile.write(body)
    def log_message(self,*_): pass

class AccountService:
    def __init__(self, account_file:Path): self.account_file=account_file
    def name(self):
        try: return json.loads(self.account_file.read_text(encoding='utf-8')).get('name','Nicht angemeldet')
        except Exception: return 'Nicht angemeldet'
    def _save(self,data):
        keyring.set_password(SERVICE,TOKEN,data['refresh_token'])
        self.account_file.parent.mkdir(parents=True,exist_ok=True)
        self.account_file.write_text(json.dumps({'id':data['id'],'name':data['name']},indent=2),encoding='utf-8')
    def login(self,client_id,redirect_uri):
        p=urlparse(redirect_uri)
        if p.scheme!='http' or p.hostname not in {'localhost','127.0.0.1'}: raise RuntimeError('Redirect URI muss lokal sein, z. B. http://localhost:53682')
        Callback.url=None; server=HTTPServer(('127.0.0.1',p.port or 80),Callback); server.timeout=180
        login_url,state,verifier=minecraft_launcher_lib.microsoft_account.get_secure_login_data(client_id,redirect_uri)
        webbrowser.open(login_url); server.handle_request(); server.server_close()
        if not Callback.url: raise TimeoutError('Microsoft-Anmeldung wurde nicht abgeschlossen.')
        code=minecraft_launcher_lib.microsoft_account.parse_auth_code_url(Callback.url,state)
        data=minecraft_launcher_lib.microsoft_account.complete_login(client_id,None,redirect_uri,code,verifier); self._save(data); return data
    def refresh(self,client_id,redirect_uri):
        token=keyring.get_password(SERVICE,TOKEN)
        if not client_id: raise RuntimeError('Microsoft Client-ID fehlt.')
        if not token: raise RuntimeError('Bitte zuerst mit Microsoft anmelden.')
        data=minecraft_launcher_lib.microsoft_account.complete_refresh(client_id,None,redirect_uri,token); self._save(data); return data
    def logout(self):
        try: keyring.delete_password(SERVICE,TOKEN)
        except Exception: pass
        self.account_file.unlink(missing_ok=True)
