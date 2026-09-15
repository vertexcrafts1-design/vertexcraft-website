from __future__ import annotations

import json
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Any


def default_config() -> dict[str, Any]:
    return {
        'ram_gb': 4,
        'width': 1280,
        'height': 720,
        'client_id': '',
        'redirect_uri': 'http://localhost',
        'close_launcher_on_game_start': False,
        'selected_instance_id': '',
    }


def safe_instance_id(name: str) -> str:
    value = name.strip().lower()
    value = re.sub(r'[^a-z0-9]+', '-', value)
    value = re.sub(r'-{2,}', '-', value).strip('-')
    return value or 'instance'


class ConfigStore:
    def __init__(self, path: Path):
        self.path = Path(path)

    def load(self) -> dict[str, Any]:
        cfg = default_config()
        if self.path.exists():
            try:
                data = json.loads(self.path.read_text(encoding='utf-8'))
                if isinstance(data, dict):
                    cfg.update(data)
            except (OSError, json.JSONDecodeError):
                pass
        cfg['ram_gb'] = max(2, min(16, int(cfg.get('ram_gb', 4))))
        return cfg

    def save(self, data: dict[str, Any]) -> None:
        self.path.parent.mkdir(parents=True, exist_ok=True)
        self.path.write_text(json.dumps(data, indent=2, ensure_ascii=False), encoding='utf-8')


class InstanceStore:
    def __init__(self, path: Path):
        self.path = Path(path)

    def load(self) -> list[dict[str, Any]]:
        if not self.path.exists():
            return []
        try:
            data = json.loads(self.path.read_text(encoding='utf-8'))
            return data if isinstance(data, list) else []
        except (OSError, json.JSONDecodeError):
            return []

    def save(self, instances: list[dict[str, Any]]) -> None:
        self.path.parent.mkdir(parents=True, exist_ok=True)
        self.path.write_text(json.dumps(instances, indent=2, ensure_ascii=False), encoding='utf-8')

    def add(self, name: str, version: str, loader: str = 'vanilla', *, mrpack_path: str = '') -> dict[str, Any]:
        instances = self.load()
        base = safe_instance_id(name)
        used = {str(item.get('id', '')) for item in instances}
        instance_id = base
        suffix = 2
        while instance_id in used:
            instance_id = f'{base}-{suffix}'
            suffix += 1
        item = {
            'id': instance_id,
            'name': name.strip() or 'Minecraft',
            'version': version.strip(),
            'loader': loader,
            'mrpack_path': mrpack_path,
        }
        instances.append(item)
        self.save(instances)
        return item

    def delete(self, instance_id: str) -> bool:
        instances = self.load()
        filtered = [item for item in instances if item.get('id') != instance_id]
        changed = len(filtered) != len(instances)
        if changed:
            self.save(filtered)
        return changed
