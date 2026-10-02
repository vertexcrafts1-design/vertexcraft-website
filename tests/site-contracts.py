"""Static release checks for both existing GitHub Pages sites. No dependencies."""
from html.parser import HTMLParser
from pathlib import Path
from urllib.parse import urlsplit, unquote
from collections import Counter
import re
import sys

class Page(HTMLParser):
    def __init__(self, path):
        super().__init__(convert_charrefs=True)
        self.path, self.ids, self.links, self.assets, self.scripts = path, [], [], [], []
        self.h1, self.lang, self.csp = 0, None, None
        self.feed(path.read_text(encoding='utf-8'))
    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if 'id' in attrs: self.ids.append(attrs['id'])
        if tag == 'html': self.lang = attrs.get('lang')
        if tag == 'h1': self.h1 += 1
        if tag == 'a' and attrs.get('href'): self.links.append(attrs['href'])
        if tag in ('script', 'img') and attrs.get('src'): self.assets.append(attrs['src'])
        if tag == 'link' and attrs.get('href'): self.assets.append(attrs['href'])
        if tag == 'script' and attrs.get('src'): self.scripts.append(attrs['src'])
        if tag == 'meta' and attrs.get('http-equiv', '').lower() == 'content-security-policy': self.csp=attrs.get('content')

roots = [Path(arg).resolve() for arg in sys.argv[1:]] or [Path(__file__).resolve().parents[1]]
errors, count = [], 0
for root in roots:
    pages = {path.resolve(): Page(path) for path in root.glob('*.html')}
    for path, page in pages.items():
        count += 1
        if page.lang != 'de': errors.append(f'{path.name}: language missing')
        expected_h1 = 2 if path.name == 'dashboard.html' else 1
        if page.h1 != expected_h1: errors.append(f'{path.name}: expected {expected_h1} h1, found {page.h1}')
        if not page.csp: errors.append(f'{path.name}: CSP missing')
        for identity, amount in Counter(page.ids).items():
            if amount > 1: errors.append(f'{path.name}: duplicate id {identity}')
        for link in page.links + page.assets:
            parsed = urlsplit(link)
            if parsed.scheme or parsed.netloc: continue
            target = root / unquote(parsed.path).lstrip('/') if parsed.path.startswith('/') else path.parent / unquote(parsed.path)
            if not parsed.path: target = path
            if target.is_dir(): target /= 'index.html'
            if not target.exists(): errors.append(f'{path.name}: broken local reference {link}')
            elif parsed.fragment and target.resolve() in pages and parsed.fragment not in pages[target.resolve()].ids:
                errors.append(f'{path.name}: missing anchor {link}')
        for script in page.scripts:
            target = root / urlsplit(script).path.lstrip('/') if script.startswith('/') else path.parent / urlsplit(script).path
            if not target.exists(): continue
            text = target.read_text(encoding='utf-8')
            # Explicit non-nullable ID lookups should refer to real elements.
            if target.name in ('stats.js', 'shop-v3.js', 'dashboard-app.js'):
                dynamic_ids = re.findall(r'id="([A-Za-z][A-Za-z0-9_-]*)"', text)
                for selector in re.findall(r"\$\('(#?[^']+)'\)", text):
                    if re.fullmatch(r'#?[A-Za-z][A-Za-z0-9_-]*',selector) and selector.lstrip('#') not in page.ids + dynamic_ids:
                        errors.append(f'{path.name}: {target.name} references absent id {selector}')
    print(f'Checked {len(pages)} pages in {root.name}')
if errors:
    print('\n'.join(errors));sys.exit(1)
print(f'PASS: {count} pages; local links, anchors, assets, IDs and CSP')
