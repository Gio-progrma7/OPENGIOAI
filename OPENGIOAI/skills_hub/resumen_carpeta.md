---
id: resumen_carpeta
nombre: Resumen de Carpeta
categoria: archivos
descripcion: Analiza una carpeta y devuelve estadisticas de archivos, extensiones y tamano total
activa: true
autor: opengio
version: 1.0.0
ejemplo: skill_run("resumen_carpeta", ruta="C:/Users/usuario/Documentos")
---

## Descripcion
Escanea una carpeta y sus subcarpetas. Devuelve total de archivos, carpetas,
tamano total, las 5 extensiones mas frecuentes y los 5 archivos mas pesados.
100% Python estandar, sin dependencias externas.

## Codigo
```python
import os, sys, json, time
from datetime import datetime, timezone
from collections import Counter
from pathlib import Path

def fmt_bytes(b):
    for u in ["B","KB","MB","GB","TB"]:
        if b < 1024: return f"{b:.1f} {u}"
        b /= 1024
    return f"{b:.1f} PB"

def main():
    inicio = time.time()
    params = json.loads(os.environ.get("SKILL_PARAMS", "{}"))
    ruta   = params.get("ruta", os.path.expanduser("~"))

    try:
        p = Path(ruta)
        if not p.exists():
            raise FileNotFoundError(f"No existe la ruta: {ruta}")
        if not p.is_dir():
            raise NotADirectoryError(f"No es una carpeta: {ruta}")

        archivos    = []
        carpetas    = 0
        total_bytes = 0
        extensiones = Counter()
        errores     = 0

        for entry in p.rglob("*"):
            try:
                if entry.is_dir():
                    carpetas += 1
                elif entry.is_file():
                    size = entry.stat().st_size
                    archivos.append((size, str(entry)))
                    total_bytes += size
                    ext = entry.suffix.lower() or "(sin ext)"
                    extensiones[ext] += 1
            except (PermissionError, OSError):
                errores += 1

        top_ext     = extensiones.most_common(5)
        top_pesados = sorted(archivos, key=lambda x: x[0], reverse=True)[:5]

        resumen = (
            f"Carpeta:      {ruta}\n"
            f"Archivos:     {len(archivos):,}\n"
            f"Subcarpetas:  {carpetas:,}\n"
            f"Tamano total: {fmt_bytes(total_bytes)}\n"
            f"Top extensiones: {', '.join(f'{e}({n})' for e,n in top_ext)}\n"
            f"Mas pesados:\n" +
            "\n".join(f"  {fmt_bytes(s)} - {Path(f).name}" for s,f in top_pesados)
        )

        resultado = {
            "status":    "ok",
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "duracion":  round(time.time() - inicio, 3),
            "ruta":      ruta,
            "resumen":   resumen,
            "estadisticas": {
                "total_archivos":  len(archivos),
                "total_carpetas":  carpetas,
                "tamano_bytes":    total_bytes,
                "tamano_legible":  fmt_bytes(total_bytes),
                "errores_acceso":  errores
            },
            "top_extensiones": [{"ext": e, "cantidad": n} for e,n in top_ext],
            "archivos_mas_pesados": [{"tamano": fmt_bytes(s), "ruta": f} for s,f in top_pesados]
        }
    except Exception as e:
        resultado = {"status": "error", "detalle": str(e),
                     "timestamp": datetime.now(timezone.utc).isoformat()}

    print(json.dumps(resultado, ensure_ascii=False, indent=2))

if __name__ == "__main__":
    main()
```

## Parametros
- nombre: ruta | tipo: string | requerido: false | descripcion: Ruta de la carpeta a analizar (default: carpeta del usuario)
