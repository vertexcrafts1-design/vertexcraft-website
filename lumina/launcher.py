from __future__ import annotations
import json, logging, os, shutil, subprocess, threading
from pathlib import Path
import customtkinter as ctk, minecraft_launcher_lib
from tkinter import filedialog, messagebox
from core import ConfigStore, InstanceStore
from auth_service import AccountService
from game_service import GameService

APP='LUMINA'; VER='0.5.0'; DATA=Path(os.environ.get('APPDATA',Path.home()))/'LUMINA'; DATA.mkdir(parents=True,exist_ok=True)
logging.basicConfig(filename=DATA/'lumina.log',level=logging.INFO,format='%(asctime)s %(levelname)s %(message)s',encoding='utf-8')
ctk.set_appearance_mode('dark'); ctk.set_default_color_theme('dark-blue')

class Lumina(ctk.CTk):
    A='#8b5cf6'; AH='#a78bfa'; BG='#09090e'; CARD='#12121a'; BORDER='#252536'; TXT='#f1f5f9'; MUT='#94a3b8'
    def __init__(self):
        super().__init__(); self.title(f'LUMINA Launcher {VER}'); self.geometry('1120x700'); self.minsize(980,620); self.configure(fg_color=self.BG)
        self.cfgs=ConfigStore(DATA/'config.json'); self.cfg=self.cfgs.load(); self.store=InstanceStore(DATA/'instances.json'); self.instances=self.store.load(); self.account=AccountService(DATA/'account.json'); self.game=GameService(DATA); self.busy=False
        if not self.instances: self.store.add('Vanilla – Latest','latest-release','vanilla'); self.instances=self.store.load()
        self.selected=self.cfg.get('selected_instance_id') or self.instances[0]['id']; self.build(); self.play_page()
    def build(self):
        side=ctk.CTkFrame(self,width=235,corner_radius=0,fg_color='#0d0d14'); side.pack(side='left',fill='y'); side.pack_propagate(False)
        ctk.CTkLabel(side,text='LUMINA',font=ctk.CTkFont(size=29,weight='bold'),text_color=self.A).pack(anchor='w',padx=22,pady=(28,0)); ctk.CTkLabel(side,text='Minecraft Launcher',text_color=self.MUT).pack(anchor='w',padx=22,pady=(0,22))
        self.nav={}
        for k,t,fn in [('play','▶   Spielen',self.play_page),('instances','▦   Instanzen',self.instances_page),('packs','⬡   Modpacks',self.packs_page),('account','●   Account',self.account_page),('settings','⚙   Einstellungen',self.settings_page)]:
            b=ctk.CTkButton(side,text=t,anchor='w',height=44,fg_color='transparent',hover_color='#1a1a27',command=fn); b.pack(fill='x',padx=12,pady=3); self.nav[k]=b
        self.chip=ctk.CTkLabel(side,text=self.account.name(),text_color=self.TXT,anchor='w'); self.chip.pack(side='bottom',fill='x',padx=22,pady=22)
        self.main=ctk.CTkScrollableFrame(self,fg_color='transparent',corner_radius=0); self.main.pack(side='right',fill='both',expand=True,padx=30,pady=24)
    def clear(self,key,title,sub):
        for w in self.main.winfo_children(): w.destroy()
        for k,b in self.nav.items(): b.configure(fg_color='#1a1a27' if k==key else 'transparent')
        ctk.CTkLabel(self.main,text=title,font=ctk.CTkFont(size=28,weight='bold'),text_color=self.TXT).pack(anchor='w'); ctk.CTkLabel(self.main,text=sub,text_color=self.MUT).pack(anchor='w',pady=(4,20))
    def card(self): return ctk.CTkFrame(self.main,fg_color=self.CARD,border_width=1,border_color=self.BORDER,corner_radius=16)
    def inst(self): return next((x for x in self.instances if x['id']==self.selected),self.instances[0] if self.instances else None)
    def persist(self): self.cfg['selected_instance_id']=self.selected; self.cfgs.save(self.cfg)
    def play_page(self):
        self.instances=self.store.load(); self.clear('play','Bereit zum Spielen','Wähle eine Instanz und starte Minecraft direkt aus LUMINA.'); c=self.card(); c.pack(fill='x'); inner=ctk.CTkFrame(c,fg_color='transparent'); inner.pack(fill='x',padx=26,pady=24)
        names=[x['name'] for x in self.instances]; active=self.inst(); self.pick=ctk.StringVar(value=active['name']); ctk.CTkLabel(inner,text='AKTIVE INSTANZ',text_color=self.A,font=ctk.CTkFont(size=11,weight='bold')).pack(anchor='w'); ctk.CTkOptionMenu(inner,values=names,variable=self.pick,command=self.choose,width=360,fg_color='#1d1d2a',button_color=self.A).pack(anchor='w',pady=(7,12))
        ctk.CTkLabel(inner,text=f"Minecraft {active['version']}  •  {active['loader'].title()}  •  {self.cfg.get('ram_gb',4)} GB RAM",text_color=self.MUT).pack(anchor='w'); self.play=ctk.CTkButton(inner,text='▶  SPIELEN',width=210,height=52,fg_color=self.A,hover_color=self.AH,font=ctk.CTkFont(size=16,weight='bold'),command=self.start); self.play.pack(anchor='e',pady=(15,0))
        s=self.card(); s.pack(fill='x',pady=14); box=ctk.CTkFrame(s,fg_color='transparent'); box.pack(fill='x',padx=22,pady=16); self.status=ctk.CTkLabel(box,text='Bereit',text_color=self.MUT); self.status.pack(anchor='e'); self.bar=ctk.CTkProgressBar(box,progress_color=self.A); self.bar.pack(fill='x',pady=(8,0)); self.bar.set(0)
    def choose(self,name):
        x=next(x for x in self.instances if x['name']==name); self.selected=x['id']; self.persist(); self.play_page()
    def ui_status(self,text): self.after(0,lambda:self.status.configure(text=text) if hasattr(self,'status') and self.status.winfo_exists() else None)
    def ui_progress(self,v): self.after(0,lambda:self.bar.set(v) if hasattr(self,'bar') and self.bar.winfo_exists() else None)
    def start(self):
        if self.busy:return
        if self.account.name()=='Nicht angemeldet': messagebox.showwarning(APP,'Bitte zuerst mit Microsoft anmelden.'); self.account_page(); return
        self.busy=True; self.play.configure(state='disabled',text='WIRD VORBEREITET…'); threading.Thread(target=self._start_worker,daemon=True).start()
    def _start_worker(self):
        try:
            acc=self.account.refresh(self.cfg.get('client_id',''),self.cfg.get('redirect_uri','http://localhost:53682')); self.game.launch(self.inst(),self.cfg,acc,self.ui_status,self.ui_progress); self.after(0,lambda:self.done(None))
        except Exception as e: logging.exception('launch failed'); self.after(0,lambda:self.done(str(e)))
    def done(self,error):
        self.busy=False
        if hasattr(self,'play') and self.play.winfo_exists(): self.play.configure(state='normal',text='▶  SPIELEN')
        if error: messagebox.showerror(APP,f'Start fehlgeschlagen:\n\n{error}')
        elif self.cfg.get('close_launcher_on_game_start'): self.withdraw()
    def instances_page(self):
        self.instances=self.store.load(); self.clear('instances','Instanzen','Eigene Installationen für Vanilla, Fabric und Quilt.'); ctk.CTkButton(self.main,text='＋ Neue Instanz',fg_color=self.A,hover_color=self.AH,command=self.new_instance).pack(anchor='w',pady=(0,10))
        for x in self.instances:
            c=self.card(); c.pack(fill='x',pady=5); ctk.CTkLabel(c,text=x['name'],font=ctk.CTkFont(size=15,weight='bold'),text_color=self.TXT).pack(anchor='w',padx=18,pady=(13,0)); ctk.CTkLabel(c,text=f"Minecraft {x['version']} • {x['loader'].title()}",text_color=self.MUT).pack(anchor='w',padx=18)
            r=ctk.CTkFrame(c,fg_color='transparent'); r.pack(anchor='e',padx=14,pady=(0,12)); ctk.CTkButton(r,text='Auswählen',width=90,fg_color=self.A if x['id']==self.selected else '#1d1d2a',command=lambda i=x['id']:self.select(i)).pack(side='left',padx=3); ctk.CTkButton(r,text='Ordner',width=70,fg_color='#1d1d2a',command=lambda i=x['id']:self.open_inst(i)).pack(side='left',padx=3); ctk.CTkButton(r,text='Löschen',width=70,fg_color='#3a1723',command=lambda i=x['id']:self.delete(i)).pack(side='left',padx=3)
    def new_instance(self):
        d=ctk.CTkToplevel(self); d.title('Neue Instanz'); d.geometry('430x350'); d.grab_set(); d.configure(fg_color=self.BG); ctk.CTkLabel(d,text='Neue Instanz',font=ctk.CTkFont(size=22,weight='bold')).pack(anchor='w',padx=24,pady=(22,12)); n=ctk.CTkEntry(d,width=380,placeholder_text='Name'); n.pack(padx=24,pady=7); v=ctk.CTkEntry(d,width=380); v.insert(0,'latest-release'); v.pack(padx=24,pady=7); l=ctk.CTkOptionMenu(d,values=['vanilla','fabric','quilt'],width=380); l.pack(padx=24,pady=7)
        def add(): x=self.store.add(n.get().strip() or 'Minecraft',v.get().strip() or 'latest-release',l.get()); self.selected=x['id']; self.persist(); d.destroy(); self.instances_page()
        ctk.CTkButton(d,text='Erstellen',fg_color=self.A,command=add).pack(fill='x',padx=24,pady=18)
    def select(self,i): self.selected=i; self.persist(); self.instances_page()
    def open_inst(self,i): p=DATA/'instances'/i/'.minecraft'; p.mkdir(parents=True,exist_ok=True); os.startfile(p)
    def delete(self,i):
        x=next((x for x in self.instances if x['id']==i),None)
        if x and messagebox.askyesno(APP,f"'{x['name']}' inklusive Welten und Mods löschen?"):
            self.store.delete(i); shutil.rmtree(DATA/'instances'/i,ignore_errors=True); self.instances=self.store.load(); self.selected=self.instances[0]['id'] if self.instances else ''; self.persist(); self.instances_page()
    def packs_page(self):
        self.clear('packs','Modpacks','Importiere Modrinth .mrpack-Dateien.'); c=self.card(); c.pack(fill='x'); ctk.CTkLabel(c,text='Modrinth Pack importieren',font=ctk.CTkFont(size=17,weight='bold'),text_color=self.TXT).pack(anchor='w',padx=22,pady=(20,5)); ctk.CTkLabel(c,text='Beim ersten Start lädt LUMINA Pack, Loader und Abhängigkeiten.',text_color=self.MUT).pack(anchor='w',padx=22); ctk.CTkButton(c,text='⬇  .mrpack auswählen',fg_color=self.A,command=self.import_pack).pack(anchor='w',padx=22,pady=18)
    def import_pack(self):
        p=filedialog.askopenfilename(filetypes=[('Modrinth Pack','*.mrpack')]);
        if not p:return
        try:
            info=minecraft_launcher_lib.mrpack.get_mrpack_information(p); x=self.store.add(info.get('name') or Path(p).stem,info.get('minecraftVersion','unknown'),'mrpack',mrpack_path=p); self.selected=x['id']; self.persist(); messagebox.showinfo(APP,'Modpack importiert.'); self.packs_page()
        except Exception as e: messagebox.showerror(APP,str(e))
    def account_page(self):
        self.clear('account','Account','Microsoft-Account für Minecraft: Java Edition.'); c=self.card(); c.pack(fill='x'); name=self.account.name(); ctk.CTkLabel(c,text=name,font=ctk.CTkFont(size=18,weight='bold'),text_color=self.TXT).pack(anchor='w',padx=22,pady=(20,10)); fn=self.logout if name!='Nicht angemeldet' else self.login; text='Abmelden' if name!='Nicht angemeldet' else 'Mit Microsoft anmelden'; ctk.CTkButton(c,text=text,fg_color=self.A,command=fn).pack(anchor='w',padx=22,pady=(0,20)); ctk.CTkLabel(self.main,text='Für einen eigenen Launcher verlangt Microsoft eine eigene Azure Client-ID und Minecraft-API-Freigabe. Die Client-ID trägst du unter Einstellungen ein.',wraplength=760,justify='left',text_color=self.MUT).pack(anchor='w',pady=16)
    def login(self):
        cid=self.cfg.get('client_id',''); red=self.cfg.get('redirect_uri','http://localhost:53682')
        if not cid: messagebox.showwarning(APP,'Client-ID fehlt. Trage sie unter Einstellungen ein.'); self.settings_page(); return
        messagebox.showinfo(APP,'Der Browser öffnet sich jetzt für die Microsoft-Anmeldung.'); threading.Thread(target=self._login,args=(cid,red),daemon=True).start()
    def _login(self,cid,red):
        try: data=self.account.login(cid,red); self.after(0,lambda:self.login_done(data['name'],None))
        except Exception as e: self.after(0,lambda:self.login_done(None,str(e)))
    def login_done(self,name,error):
        if error: messagebox.showerror(APP,error)
        else: self.chip.configure(text=name); messagebox.showinfo(APP,f'Angemeldet als {name}.'); self.account_page()
    def logout(self): self.account.logout(); self.chip.configure(text='Nicht angemeldet'); self.account_page()
    def settings_page(self):
        self.clear('settings','Einstellungen','RAM, Fenstergröße und Microsoft-Login.'); c=self.card(); c.pack(fill='x'); box=ctk.CTkFrame(c,fg_color='transparent'); box.pack(fill='x',padx=22,pady=20); ctk.CTkLabel(box,text='RAM',text_color=self.MUT).pack(anchor='w'); self.ram=ctk.CTkSlider(box,from_=2,to=16,number_of_steps=14,progress_color=self.A); self.ram.set(self.cfg.get('ram_gb',4)); self.ram.pack(anchor='w',fill='x',pady=(5,14)); self.w=ctk.CTkEntry(box,placeholder_text='Breite'); self.w.insert(0,str(self.cfg.get('width',1280))); self.w.pack(anchor='w',pady=4); self.h=ctk.CTkEntry(box,placeholder_text='Höhe'); self.h.insert(0,str(self.cfg.get('height',720))); self.h.pack(anchor='w',pady=4); self.cid=ctk.CTkEntry(box,width=560,placeholder_text='Microsoft Client-ID'); self.cid.insert(0,self.cfg.get('client_id','')); self.cid.pack(anchor='w',pady=(14,4)); self.red=ctk.CTkEntry(box,width=560); self.red.insert(0,self.cfg.get('redirect_uri','http://localhost:53682')); self.red.pack(anchor='w',pady=4); self.close=ctk.CTkSwitch(box,text='Launcher nach Spielstart ausblenden',progress_color=self.A); self.close.pack(anchor='w',pady=12); self.close.select() if self.cfg.get('close_launcher_on_game_start') else None; ctk.CTkButton(box,text='Speichern',fg_color=self.A,command=self.save).pack(anchor='w')
    def save(self):
        try: self.cfg.update(ram_gb=int(self.ram.get()),width=max(640,int(self.w.get())),height=max(480,int(self.h.get())),client_id=self.cid.get().strip(),redirect_uri=self.red.get().strip() or 'http://localhost:53682',close_launcher_on_game_start=bool(self.close.get()),selected_instance_id=self.selected); self.cfgs.save(self.cfg); messagebox.showinfo(APP,'Gespeichert.')
        except ValueError: messagebox.showerror(APP,'Ungültige Auflösung.')

if __name__=='__main__': Lumina().mainloop()
