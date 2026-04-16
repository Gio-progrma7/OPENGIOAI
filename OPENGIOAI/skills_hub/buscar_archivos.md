---
id: buscar_archivos
nombre: Buscar Archivos
categoria: archivos
descripcion: Busca archivos por nombre o extension en una carpeta y sus subcarpetas
activa: true
autor: opengio
version: 1.0.0
ejemplo: skill_run("buscar_archivos", patron="*.pdf", ruta="C:/Documents")
---

## Descripcion
Busca archivos que coincidan con un patron glob dentro de una carpeta.
Devuelve ruta, tamano y fecha de modificacion de cada resultado.
Sin dependencias externas. Limite configurable de resultados.

## Codigo
```python
import os, sys, json, time
from datetime import datetime, timezone
from pathlib import Path

def fmt_bytes(b):
    for u in ["B","KB","MB","GB"]:
        if b < 1024: return f"{b:.1f} {u}"
        b /= 1024
    return f"{b:.1f} TB"

def main():
    inicio = time.time()
    params = json.loads(os.environ.get("SKILL_PARAMS", "{}"))
    patron = params.get("patron", "*.txt")
    ruta   = params.get("ruta",   os.path.expanduser("~"))
    limite = int(params.get("limite", 50))

    try:
        p = Path(ruta)
        if not p.exists():
            raise FileNotFoundError(f"No existe: {ruta}")

        encontrados = []
        for entry in p.rglob(patron):
            if not entry.is_file():
                continue
            try:
                stat = entry.stat()
                encontrados.append({
                    "nombre":     entry.name,
                    "ruta":       str(entry),
                    "tamano":     fmt_bytes(stat.st_size),
                    "bytes":      stat.st_size,
                    "modificado": datetime.fromtimestamp(
                        stat.st_mtime, tz=timezone.utc).strftime("%Y-%m-%d %H:%M")
                })
                if len(encontrados) >= limite:
                    break
            except (PermissionError, OSError):
                pass

        encontrados.sort(key=lambda x: x["bytes"], reverse=True)

        if encontrados:
            lineas = [f"  {e['tamano']:>9}  {e['modificado']}  {e['nombre']}"
                      for e in encontrados]
            resumen = f"Patron '{patron}' en '{ruta}':\n" + "\n".join(lineas)
        else:
            resumen = f"No se encontraron archivos con patron '{patron}' en '{ruta}'"

        resultado = {
            "status":        "ok",
            "timestamp":     datetime.now(timezone.utc).isoformat(),
            "duracion":      round(time.time() - inicio, 3),
            "patron":        patron,
            "ruta":          ruta,
            "total":         len(encontrados),
            "limite_activo": len(encontrados) >= limite,
            "resumen":       resumen,
            "archivos":      encontrados
        }
    except Exception as e:
        resultado = {"status": "error", "detalle": str(e),
                     "timestamp": datetime.now(timezone.utc).isoformat()}

    print(json.dumps(resultado, ensure_ascii=False, indent=2))

if __name__ == "__main__":
    main()
```

## Parametros
- nombre: patron | tipo: string | requerido: false | descripcion: Patron glob ej *.pdf *.py reporte*.xlsx
- nombre: ruta   | tipo: string | requerido: false | descripcion: Carpeta donde buscar
- nombre: limite | tipo: number | requerido: false | descripcion: Maximo de resultados (default 50)
