#!/usr/bin/env python3
"""Valida y sube imágenes generales de productos a la API.

Por defecto solo valida. Use --execute --report REPORTE.jsonl para subir.
Configure API_TOKEN al inicio del archivo o use PRETTYWOMAN_TOKEN.
Requiere Pillow: python -m pip install Pillow
"""
import argparse
import csv
import hashlib
import io
import json
import math
import os
from pathlib import Path
import sys
import tempfile
import time
import urllib.error
import urllib.request
import uuid

from PIL import Image, ImageOps, UnidentifiedImageError

MAX_BYTES = 4 * 1024 * 1024
MAX_SIDE = 6000
MAX_PIXELS = 25_000_000
FORMATS = {'.jpg': 'image/jpeg', '.jpeg': 'image/jpeg', '.png': 'image/png', '.webp': 'image/webp'}
USER_AGENT = 'PrettyWoman-Migracion/1.0'
API_TOKEN = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJmODk5ODc1Zi05ZTFiLTQ5OTctOTMzYi01YzQyODg0NDNkNmYiLCJqdGkiOiIyOTg2N2E4NS1jYjllLTQ0YTctODc5OC03YzhmMGMzMTkyNDAiLCJlbWFpbCI6Imh1Z28uaHVldGUwNDAyQGdtYWlsLmNvbSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWVpZGVudGlmaWVyIjoiZjg5OTg3NWYtOWUxYi00OTk3LTkzM2ItNWM0Mjg4NDQzZDZmIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvZW1haWxhZGRyZXNzIjoiaHVnby5odWV0ZTA0MDJAZ21haWwuY29tIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZSI6Ikh1Z28iLCJzZWN1cml0eV9zdGFtcCI6IkFNVklQN0JDQk1FQk9NTDRBQlRHN0dTQkVXVjdDS1czIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiQWRtaW4iLCJleHAiOjE3OTQ0MzgxNDYsImlzcyI6IlByZXR0eVdvbWFuLkFwaSIsImF1ZCI6IlByZXR0eVdvbWFuLkNsaWVudCJ9.S4Fkp83Y8eDn7-9rdeLQ2hWSu2IlK7zQgXX4DFloFJ4'  # Pegue aquí el token de la API.


def load_mapping(path):
    try:
        path.read_text(encoding='utf-8-sig')
        encoding = 'utf-8-sig'
    except UnicodeDecodeError:
        encoding = 'cp1252'
    with path.open(encoding=encoding, newline='') as file:
        sample = file.read(8192)
        file.seek(0)
        reader = csv.DictReader(file, dialect=csv.Sniffer().sniff(sample, delimiters=';,\t'))
        required = {'code', 'product_id', 'presentation', 'presentation_id'}
        if not reader.fieldnames or not required.issubset(reader.fieldnames):
            raise ValueError('El CSV requiere code, product_id, presentation y presentation_id.')
        mapping = {}
        for line, row in enumerate(reader, 2):
            code, product, presentation_id = ((row.get(key) or '').strip() for key in ('code', 'product_id', 'presentation_id'))
            presentation = (row.get('presentation') or '').strip()
            if any(not value.isascii() or not value.isdigit() or int(value) <= 0 for value in (code, product, presentation_id)) or not presentation:
                raise ValueError(f'Fila {line}: IDs positivos y presentación obligatoria.')
            key = (str(int(code)), presentation.casefold())
            if key in mapping:
                raise ValueError(f'Fila {line}: código y presentación repetidos.')
            mapping[key] = (int(product), int(presentation_id))
        return mapping


def prepare_images(directory, mapping):
    files = sorted((p for p in directory.iterdir() if p.is_file() and p.suffix.lower() in FORMATS), key=lambda p: p.name.casefold())
    if not files:
        raise ValueError('No se encontraron imágenes JPG, PNG o WebP.')
    result, seen = [], set()
    for path in files:
        parts = path.stem.split('_')
        if len(parts) != 3 or not parts[0].isascii() or not parts[0].isdigit() or int(parts[0]) <= 0 or not parts[1] or not parts[2].isascii() or not parts[2].isdigit() or int(parts[2]) <= 0:
            raise ValueError(f'Nombre inválido, use codigo_presentacion_#.ext: {path.name}')
        code = str(int(parts[0]))
        key = (code, parts[1].casefold())
        image_key = (key, int(parts[2]))
        if image_key in seen:
            raise ValueError(f'Número de imagen duplicado: {path.name}')
        seen.add(image_key)
        if key not in mapping:
            raise ValueError(f'No existe el código y presentación de {path.name} en el CSV.')
        raw = path.read_bytes()
        if not raw:
            raise ValueError(f'La imagen {path.name} está vacía.')
        try:
            with Image.open(io.BytesIO(raw)) as image:
                actual_format = image.format
                width, height = image.size
                image.verify()
        except (UnidentifiedImageError, OSError, Image.DecompressionBombError) as exc:
            raise ValueError(f'Imagen inválida: {path.name}') from exc
        if actual_format not in ('JPEG', 'PNG', 'WEBP'):
            raise ValueError(f'Formato de imagen no compatible: {path.name}')
        needs_conversion = len(raw) > MAX_BYTES or width > MAX_SIDE or height > MAX_SIDE or width * height > MAX_PIXELS
        result.append({'code': code, 'product_id': mapping[key][0], 'presentation_id': mapping[key][1], 'path': path,
                       'sha256': hashlib.sha256(raw).hexdigest(), 'format': actual_format, 'convert': needs_conversion})
    return result


def image_payload(item):
    raw = item['path'].read_bytes()
    if hashlib.sha256(raw).hexdigest() != item['sha256']:
        raise ValueError(f'La imagen {item["path"].name} cambió después de la validación.')
    if not item['convert']:
        extensions = {'JPEG': ('.jpg', 'image/jpeg'), 'PNG': ('.png', 'image/png'), 'WEBP': ('.webp', 'image/webp')}
        suffix, content_type = extensions[item['format']]
        return raw, item['path'].stem + suffix, content_type
    with Image.open(io.BytesIO(raw)) as original:
        image = ImageOps.exif_transpose(original)
        image.load()
        if image.mode not in ('RGB', 'RGBA'):
            image = image.convert('RGBA' if 'transparency' in image.info else 'RGB')
        scale = min(1.0, MAX_SIDE / image.width, MAX_SIDE / image.height,
                    math.sqrt(MAX_PIXELS / (image.width * image.height)))
        if scale < 1:
            image = image.resize((max(1, int(image.width * scale)), max(1, int(image.height * scale))), Image.Resampling.LANCZOS)
        for _ in range(8):
            for quality in (90, 80, 70, 60, 50):
                output = io.BytesIO()
                image.save(output, format='WEBP', quality=quality, method=4)
                if output.tell() <= MAX_BYTES:
                    return output.getvalue(), f'{item["path"].stem}.webp', 'image/webp'
            image = image.resize((max(1, int(image.width * 0.8)), max(1, int(image.height * 0.8))), Image.Resampling.LANCZOS)
    raise ValueError(f'No se pudo reducir {item["path"].name} a 4 MB.')


def http_request(method, url, token, data=None, content_type=None):
    headers = {'Authorization': f'Bearer {token}', 'Accept': 'application/json', 'User-Agent': USER_AGENT}
    if content_type:
        headers['Content-Type'] = content_type
    request = urllib.request.Request(url, data=data, headers=headers, method=method)
    with urllib.request.urlopen(request, timeout=90) as response:
        body = response.read()
        return json.loads(body.decode('utf-8')) if body else None


def multipart_image(content, filename, content_type):
    boundary = 'prettywoman-' + uuid.uuid4().hex
    prefix = (f'--{boundary}\r\nContent-Disposition: form-data; name="file"; filename="{filename}"\r\n'
              f'Content-Type: {content_type}\r\n\r\n').encode('utf-8')
    return prefix + content + f'\r\n--{boundary}--\r\n'.encode('ascii'), f'multipart/form-data; boundary={boundary}'


def load_report(path, base_url):
    states = {}
    if not path.exists():
        return states
    with path.open(encoding='utf-8') as file:
        for line_no, line in enumerate(file, 1):
            try:
                event = json.loads(line)
                if event['base_url'] != base_url:
                    raise ValueError('El reporte pertenece a otra API.')
                name = event['file']
                if name in states and any(event[key] != states[name][key] for key in ('product_id', 'presentation_id', 'sha256')):
                    raise ValueError('El reporte contiene datos contradictorios.')
                states[name] = event
            except (json.JSONDecodeError, KeyError, TypeError) as exc:
                raise ValueError(f'Reporte inválido en línea {line_no}; use un reporte de este script.') from exc
    return states


def execute(items, base_url, token, report_path, interval):
    states = load_report(report_path, base_url)
    for item in items:
        state = states.get(item['path'].name)
        if state and any(state[key] != item[key] for key in ('product_id', 'presentation_id', 'sha256')):
            raise ValueError(f'La imagen o sus IDs cambiaron: {item["path"].name}')
        if state and state['status'] != 'created':
            raise ValueError(f'Resultado desconocido para {item["path"].name}. Revise la API antes de reintentar.')
    with report_path.open('a', encoding='utf-8') as report:
        def record(status, item, **extra):
            event = {'status': status, 'base_url': base_url, 'code': item['code'], 'product_id': item['product_id'],
                     'presentation_id': item['presentation_id'], 'file': item['path'].name, 'sha256': item['sha256'], **extra}
            report.write(json.dumps(event, ensure_ascii=False) + '\n')
            report.flush()
            os.fsync(report.fileno())
        for item in items:
            if item['path'].name in states:
                continue
            url = f'{base_url}/api/v1/products/{item["product_id"]}/images?productPresentationId={item["presentation_id"]}'
            try:
                content, filename, content_type = image_payload(item)
                data, multipart_type = multipart_image(content, filename, content_type)
            except (OSError, ValueError) as exc:
                print(f'{item["path"].name}: {exc}', file=sys.stderr)
                return 1
            record('pending', item)
            try:
                response = http_request('POST', url, token, data, multipart_type)
                if not isinstance(response, dict) or type(response.get('id')) is not int or response['id'] <= 0:
                    raise ValueError('La API no devolvió un ID de imagen válido.')
            except (urllib.error.URLError, OSError, ValueError, TimeoutError) as exc:
                detail = str(exc)
                if isinstance(exc, urllib.error.HTTPError):
                    detail += ' ' + exc.read().decode('utf-8', errors='replace')[:2000]
                record('unknown', item, error=detail)
                print(f'{item["path"].name}: resultado desconocido o error: {detail}. Revise la API antes de continuar.', file=sys.stderr)
                return 1
            record('created', item, image_id=response['id'], converted=item['convert'])
            print(f'{item["path"].name}: imagen {response["id"]} subida.')
            if interval:
                time.sleep(interval)
    return 0


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--images', type=Path, default=Path(__file__).resolve().parent / 'Images', help='Carpeta de imágenes.')
    parser.add_argument('--csv', type=Path, help='CSV con columnas code e id (por defecto, products.csv en la carpeta de imágenes).')
    parser.add_argument('--base-url', default='http://localhost:5064')
    parser.add_argument('--report', type=Path, help='Reporte JSONL obligatorio al ejecutar; se puede reutilizar para reanudar.')
    parser.add_argument('--interval', type=float, default=6.5, help='Segundos entre subidas; por defecto 6.5.')
    parser.add_argument('--execute', action='store_true', help='Subir imágenes. Sin esta opción solo se valida.')
    args = parser.parse_args(argv)
    try:
        if not math.isfinite(args.interval) or args.interval < 0:
            raise ValueError('El intervalo debe ser un número no negativo.')
        if not args.base_url.startswith(('https://', 'http://')):
            raise ValueError('La URL base debe comenzar con https:// o http://.')
        csv_path = args.csv or Path(__file__).resolve().parent / 'products.csv'
        items = prepare_images(args.images, load_mapping(csv_path))
        print(f'{len(items)} imágenes validadas; {sum(item["convert"] for item in items)} se convertirán en memoria para cumplir los límites.')
        if not args.execute:
            print('Simulación: no se realizaron llamadas a la API.')
            return 0
        if not args.report:
            raise ValueError('Indique --report para registrar los resultados.')
        token = os.environ.get('PRETTYWOMAN_TOKEN', '').strip() or API_TOKEN.strip()
        if not token:
            raise ValueError('Configure API_TOKEN al inicio del script o la variable PRETTYWOMAN_TOKEN.')
        return execute(items, args.base_url.rstrip('/'), token, args.report, args.interval)
    except (OSError, ValueError, csv.Error) as exc:
        print(f'Error: {exc}', file=sys.stderr)
        return 1


if __name__ == '__main__':
    sys.exit(main())
