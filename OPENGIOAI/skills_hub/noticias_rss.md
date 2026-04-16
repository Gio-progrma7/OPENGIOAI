---
id: noticias_rss
nombre: Leer Noticias RSS
categoria: web
descripcion: Obtiene los titulares mas recientes de cualquier feed RSS sin API key
activa: true
autor: opengio
version: 1.0.0
ejemplo: skill_run("noticias_rss", url="https://feeds.bbci.co.uk/mundo/rss.xml", cantidad=5)
---

## Descripcion
Lee cualquier feed RSS/Atom y devuelve los ultimos N titulares con
titulo, descripcion, fecha y link. Funciona con cualquier feed publico:
noticias, blogs, YouTube, podcasts, etc. Sin dependencias externas.

## Codigo
```python
import os, sys, json, time, urllib.request
import xml.etree.ElementTree as ET
from datetime import datetime, timezone
from email.utils import parsedate_to_datetime

RSS_DEFAULT = "https://feeds.bbci.co.uk/mundo/rss.xml"

def limpiar(texto):
    if not texto: return ""
    import re
    texto = re.sub(r"<[^>]+>", "", texto)
    return texto.strip()[:300]

def parsear_fecha(s):
    if not s: return ""
    try:
        return parsedate_to_datetime(s).strftime("%Y-%m-%d %H:%M")
    except Exception:
        return s[:16] if s else ""

def main():
    inicio   = time.time()
    params   = json.loads(os.environ.get("SKILL_PARAMS", "{}"))
    url      = params.get("url", RSS_DEFAULT)
    cantidad = int(params.get("cantidad", 5))

    try:
        req = urllib.request.Request(url, headers={"User-Agent": "OPENGIOAI/1.0"})
        with urllib.request.urlopen(req, timeout=12) as r:
            xml_bytes = r.read()

        root = ET.fromstring(xml_bytes)
        ns   = {"atom": "http://www.w3.org/2005/Atom"}

        # Detectar RSS vs Atom
        items = root.findall(".//item")
        if not items:
            items = root.findall(".//atom:entry", ns)

        noticias = []
        for item in items[:cantidad]:
            def txt(tag, alt=""):
                el = item.find(tag) or item.find(f"atom:{tag}", ns)
                return limpiar(el.text if el is not None else "") or alt

            titulo = txt("title")
            link_el = item.find("link") or item.find("atom:link", ns)
            link = ""
            if link_el is not None:
                link = link_el.text or link_el.get("href","")

            desc   = txt("description") or txt("summary")
            fecha  = parsear_fecha(txt("pubDate") or txt("published") or txt("updated"))

            noticias.append({
                "titulo":      titulo,
                "descripcion": desc,
                "fecha":       fecha,
                "link":        link.strip()
            })

        canal = root.find(".//channel/title")
        nombre_canal = canal.text if canal is not None else url

        lineas = []
        for i, n in enumerate(noticias, 1):
            lineas.append(f"  {i}. [{n['fecha']}] {n['titulo']}")
            if n["descripcion"]:
                lineas.append(f"     {n['descripcion'][:120]}...")

        resumen = f"Feed: {nombre_canal}\n" + "\n".join(lineas)

        resultado = {
            "status":    "ok",
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "duracion":  round(time.time() - inicio, 3),
            "canal":     nombre_canal,
            "resumen":   resumen,
            "noticias":  noticias
        }
    except Exception as e:
        resultado = {"status": "error", "detalle": str(e),
                     "timestamp": datetime.now(timezone.utc).isoformat()}

    print(json.dumps(resultado, ensure_ascii=False, indent=2))

if __name__ == "__main__":
    main()
```

## Parametros
- nombre: url      | tipo: string | requerido: false | descripcion: URL del feed RSS o Atom (default BBC Mundo)
- nombre: cantidad | tipo: number | requerido: false | descripcion: Numero de noticias a obtener (default 5)
