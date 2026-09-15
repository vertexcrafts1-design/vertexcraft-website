import json
import tempfile
import unittest
from pathlib import Path

from core import ConfigStore, InstanceStore, safe_instance_id, default_config

class CoreTests(unittest.TestCase):
    def test_safe_instance_id_normalizes_name(self):
        self.assertEqual(safe_instance_id('Fabric Performance 1.21.1!'), 'fabric-performance-1-21-1')
    def test_config_store_merges_defaults(self):
        with tempfile.TemporaryDirectory() as tmp:
            path=Path(tmp)/'config.json'; path.write_text(json.dumps({'ram_gb':8}),encoding='utf-8')
            cfg=ConfigStore(path).load(); self.assertEqual(cfg['ram_gb'],8); self.assertEqual(cfg['redirect_uri'],'http://localhost:53682'); self.assertIn('client_id',cfg)
    def test_instance_store_creates_unique_ids(self):
        with tempfile.TemporaryDirectory() as tmp:
            store=InstanceStore(Path(tmp)/'instances.json'); first=store.add('Vanilla','1.21.1','vanilla'); second=store.add('Vanilla','1.21.1','fabric')
            self.assertNotEqual(first['id'],second['id']); self.assertEqual(len(store.load()),2)
    def test_default_config_has_sane_ram(self):
        cfg=default_config(); self.assertGreaterEqual(cfg['ram_gb'],2); self.assertLessEqual(cfg['ram_gb'],16)

if __name__=='__main__': unittest.main()
