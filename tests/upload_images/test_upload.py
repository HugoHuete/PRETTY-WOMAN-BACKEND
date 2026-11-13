import importlib.util
import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
from PIL import Image

p = Path(__file__).resolve().parents[2] / 'UploadImages/subir_imagenes_productos.py'
spec = importlib.util.spec_from_file_location('upload', p)
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)

class UploadTests(unittest.TestCase):
    def test_multiple_images_use_presentation_and_resume_by_filename(self):
        with tempfile.TemporaryDirectory() as d:
            root = Path(d)
            mapping = root / 'products.csv'
            mapping.write_text('code;product_id;presentation;presentation_id\n10;15;Negro;20\n')
            for n in (1, 2):
                Image.new('RGB', (2, 2)).save(root / f'10_Negro_{n}.webp', format='JPEG')
            items = m.prepare_images(root, m.load_mapping(mapping))
            self.assertEqual(len(items), 2)
            self.assertEqual(m.image_payload(items[0])[2], 'image/jpeg')
            report = root / 'log.jsonl'
            with patch.object(m, 'http_request', side_effect=[{'id': 1}, {'id': 2}]) as send:
                self.assertEqual(m.execute(items, 'http://localhost', 'token', report, 0), 0)
                self.assertIn('productPresentationId=20', send.call_args.args[1])
            with patch.object(m, 'http_request', side_effect=AssertionError('repetida')):
                self.assertEqual(m.execute(items, 'http://localhost', 'token', report, 0), 0)
            self.assertEqual([e['status'] for e in map(json.loads, report.read_text().splitlines())], ['pending', 'created', 'pending', 'created'])

    def test_unknown_upload_blocks_retry(self):
        with tempfile.TemporaryDirectory() as d:
            root = Path(d)
            Image.new('RGB', (2, 2)).save(root / '10_Negro_1.png')
            items = m.prepare_images(root, {('10', 'negro'): (15, 20)})
            report = root / 'log.jsonl'
            with patch.object(m, 'http_request', side_effect=OSError('offline')):
                self.assertEqual(m.execute(items, 'http://localhost', 'token', report, 0), 1)
            with self.assertRaises(ValueError):
                m.execute(items, 'http://localhost', 'token', report, 0)

if __name__ == '__main__': unittest.main()
