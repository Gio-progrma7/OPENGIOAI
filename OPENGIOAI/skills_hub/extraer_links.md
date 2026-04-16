---
id: extraer_links
nombre: Extraer Links de una Pagina Web
categoria: web
descripcion: Extrae todos los hipervinculos de cualquier pagina web con sus textos y URLs
activa: true
autor: opengio
version: 1.0.0
ejemplo: skill_run("extraer_links", url="https://example.com", filtro="pdf")
---

## Descripcion
Descarga una pagina web y extrae todos sus hipervinculos usando
html.parser (stdlib). Permite filtrar por extension o texto.
Util para encontrar archivos descargables, listar secciones, etc.

## Codigo
```python
import os, sys, json, time, urllib.request, urllib.parse
import html.parser
from datetime import datetime, timezone

class ExtractorLinks(html.parser.HTMLParser):
    def __init__(self, url_base):
        super().__init__()
        self.url_base = url_base
        self.links    = []
        self._texto   = ""

    def handle_starttag(self, tag, attrs):
        if tag == "a":
            d = dict(attrs)
            href = d.get("href", "").strip()
            if href and not href.startswith(("#","javascript","mailto")):
                abs_url = urllib.parse.urljoin(self.url_base, href)
                self._href = abs_url
            else:
                self._href = None
        else:
            self._href = None
        self._texto = ""

    def handle_data(self, data):
        self._texto += data.strip()

    def handle_endtag(self, tag):
        if tag == "a" and self._href:
            texto = self._texto.strip()[:100] or "(sin texto)"
            self.links.append({"texto": texto, "url": self._href})
            self._href  = None
            self._texto = ""

def main():
    inicio  = time.time()
    params  = json.loads(os.environ.get("SKILL_PARAMS", "{}"))
    url     = params.get("url", "https://example.com")
    filtro  = params.get("filtro", "").lower()
    limite  = int(params.get("limite", 50))

    try:
        req = urllib.request.Request(url, headers={
            "User-Agent": "Mozilla/5.0 (compatible; OPENGIOAI/1.0)"
        })
        with urllib.request.urlopen(req, timeout=12) as r:
            html_bytes = r.read()
            charset    = r.headers.get_content_charset("utf-8")

        html_text = html_bytes.decode(charset, errors="replace")

        parser = ExtractorLinks(url)
        parser.feed(html_text)

        links = parser.links
        if filtro:
            links = [l for l in links if filtro in l["url"].lower() or filtro in l["texto"].lower()]

        links = links[:limite]

        lineas = [f"  {i+1:>3}. {l['texto'][:60]:<62} {l['url']}" for i, l in enumerate(links)]
        resumen = (
            f"URL: {url}\n"
            f"Links encontrados: {len(links)}"
            + (f" (filtro: '{filtro}')" if filtro else "") + "\n\n"
            + "\n".join(lineas[:20])
            + (f"\n  ... y {len(links)-20} mas" if len(links) > 20 else "")
        )

        resultado = {
            "status":    "ok",
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "duracion":  round(time.time() - inicio, 3),
            "url":       url,
            "total":     len(links),
            "resumen":   resumen,
            "links":     links
        }
    except Exception as e:
        resultado = {"status": "error", "detalle": str(e),
                     "timestamp": datetime.now(timezone.utc).isoformat()}

    print(json.dumps(resultado, ensure_ascii=False, indent=2))

if __name__ == "__main__":
    main()
```

## Parametros
- nombre: url    | tipo: string | requerido: true  | descripcion: URL de la pagina web
- nombre: filtro | tipo: string | requerido: false | descripcion: Texto para filtrar links ej pdf docx github
- nombre: limite | tipo: number | requerido: false | descripcion: Maximo de links a devolver (default 50)
