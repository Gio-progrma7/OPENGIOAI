---
id: duplicados_carpeta
nombre: Encontrar Archivos Duplicados
categoria: archivos
descripcion: Detecta archivos duplicados en una carpeta comparando contenido por hash MD5
activa: true
autor: opengio
version: 1.0.0
ejemplo: skill_run("duplicados_carpeta", ruta="C:/Fotos")
---

## Descripcion
Escanea una carpeta y encuentra archivos con contenido identico usando
hash MD5. Agrupa los duplicados y calcula cuanto espacio se puede
liberar eliminandolos. Sin dependencias externas.

## Codigo
```python
import os, sys, json, time, hashlib
from datetime import datetime, timezone
from pathlib import Path
from collections import defaultdict

def fmt_bytes(b):
    for u in ["B","KB","MB","GB"]:
        if b < 1024: return f"{b:.1f} {u}"
        b /= 1024
    return f"{b:.1f} TB"

def hash_archivo(ruta, chunk=65536):
    h = hashlib.md5()
    with open(ruta, "rb") as f:
        while bloque := f.read(chunk):
            h.update(bloque)
    return h.hexdigest()

def main():
    inicio = time.time()
    params = json.loads(os.environ.get("SKILL_PARAMS", "{}"))
    ruta   = params.get("ruta", os.path.expanduser("~"))

    try:
        p = Path(ruta)
        if not p.exists():
            raise FileNotFoundError(f"No existe: {ruta}")

        hashes       = defaultdict(list)
        errores      = 0
        analizados   = 0

        for entry in p.rglob("*"):
            if not entry.is_file():
                continue
            try:
                h = hash_archivo(entry)
                hashes[h].append(str(entry))
                analizados += 1
            except (PermissionError, OSError):
                errores += 1

        grupos_dup = {h: rutas for h, rutas in hashes.items() if len(rutas) > 1}

        bytes_liberables = 0
        grupos_info      = []
        for h, rutas in grupos_dup.items():
            try:
                size = Path(rutas[0]).stat().st_size
            except OSError:
                size = 0
            bytes_lib = size * (len(rutas) - 1)
            bytes_liberables += bytes_lib
            grupos_info.append({
                "hash":     h[:12] + "...",
                "tamano":   fmt_bytes(size),
                "copias":   len(rutas),
                "liberable":fmt_bytes(bytes_lib),
                "archivos": rutas
            })

        grupos_info.sort(key=lambda x: x["copias"], reverse=True)

        if grupos_dup:
            lineas = [f"  {g['copias']} copias  {g['tamano']:>9}  {Path(g['archivos'][0]).name}"
                      for g in grupos_info[:10]]
            resumen = (
                f"Carpeta: {ruta}\n"
                f"Analizados: {analizados:,} archivos\n"
                f"Grupos duplicados: {len(grupos_dup)}\n"
                f"Espacio liberable: {fmt_bytes(bytes_liberables)}\n\n"
                f"Top duplicados:\n" + "\n".join(lineas)
            )
        else:
            resumen = f"No se encontraron duplicados en {ruta} ({analizados:,} archivos analizados)"

        resultado = {
            "status":    "ok",
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "duracion":  round(time.time() - inicio, 3),
            "resumen":   resumen,
            "stats": {
                "archivos_analizados":   analizados,
                "grupos_duplicados":     len(grupos_dup),
                "bytes_liberables":      bytes_liberables,
                "espacio_liberable":     fmt_bytes(bytes_liberables),
                "errores_acceso":        errores
            },
            "duplicados": grupos_info[:20]
        }
    except Exception as e:
        resultado = {"status": "error", "detalle": str(e),
                     "timestamp": datetime.now(timezone.utc).isoformat()}

    print(json.dumps(resultado, ensure_ascii=False, indent=2))

if __name__ == "__main__":
    main()
```

## Parametros
- nombre: ruta | tipo: string | requerido: false | descripcion: Carpeta donde buscar duplicados (default: carpeta del usuario)
