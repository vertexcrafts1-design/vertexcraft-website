from __future__ import annotations
import logging, subprocess
from pathlib import Path
import minecraft_launcher_lib

class GameService:
    def __init__(self,data_dir:Path): self.data_dir=data_dir
    def callbacks(self,status,progress):
        state={'max':1}
        return {'setStatus':lambda s:status(str(s)), 'setMax':lambda m:state.update(max=max(1,int(m))), 'setProgress':lambda v:progress(min(1,float(v)/state['max']))}
    def resolve(self,v,status):
        if v=='latest-release': status('Ermittle aktuelle Minecraft-Version…'); return minecraft_launcher_lib.utils.get_latest_version()['release']
        return v
    def install(self,inst,status,progress):
        mc=self.data_dir/'instances'/inst['id']/'.minecraft'; mc.mkdir(parents=True,exist_ok=True); cb=self.callbacks(status,progress); loader=inst.get('loader','vanilla')
        if loader=='mrpack':
            pack=inst.get('mrpack_path','')
            if not pack or not Path(pack).exists(): raise FileNotFoundError('.mrpack-Datei nicht gefunden.')
            status('Installiere Modpack…'); minecraft_launcher_lib.mrpack.install_mrpack(pack,str(mc),modpack_directory=str(mc),callback=cb)
            return mc,minecraft_launcher_lib.mrpack.get_mrpack_launch_version(pack)
        v=self.resolve(inst.get('version','latest-release'),status); status(f'Installiere Minecraft {v}…')
        minecraft_launcher_lib.install.install_minecraft_version(v,str(mc),callback=cb)
        if loader=='fabric':
            status('Installiere Fabric…'); minecraft_launcher_lib.fabric.install_fabric(v,str(mc),callback=cb); needle='fabric-loader'
        elif loader=='quilt':
            status('Installiere Quilt…'); minecraft_launcher_lib.quilt.install_quilt(v,str(mc),callback=cb); needle='quilt-loader'
        else: return mc,v
        ids=[x['id'] for x in minecraft_launcher_lib.utils.get_installed_versions(str(mc))]; found=[x for x in ids if needle in x and v in x]
        if not found: raise RuntimeError(f'{loader.title()}-Startversion nicht gefunden.')
        return mc,found[-1]
    def launch(self,inst,settings,account,status,progress):
        mc,version=self.install(inst,status,progress); status('Bereite Java und Start vor…')
        opts={'username':account['name'],'uuid':account['id'],'token':account['access_token'],'launcherName':'LUMINA','launcherVersion':'0.5.0','gameDirectory':str(mc),'jvmArguments':['-Xms1G',f"-Xmx{int(settings.get('ram_gb',4))}G"], 'customResolution':True,'resolutionWidth':str(settings.get('width',1280)),'resolutionHeight':str(settings.get('height',720))}
        try:
            info=minecraft_launcher_lib.runtime.get_version_runtime_information(version,str(mc))
            if info:
                java=minecraft_launcher_lib.runtime.get_executable_path(info['name'],str(mc))
                if java: opts['executablePath']=java; opts['defaultExecutablePath']=java
        except Exception: logging.exception('Java runtime lookup failed')
        cmd=minecraft_launcher_lib.command.get_minecraft_command(version,str(mc),opts); status('Minecraft wird gestartet…'); progress(1)
        subprocess.Popen(cmd,cwd=str(mc),creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0)); return version
