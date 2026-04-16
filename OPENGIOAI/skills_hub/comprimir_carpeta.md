---
id: comprimir_carpeta
nombre: Comprimir Carpeta a ZIP
categoria: archivos
descripcion: Comprime una carpeta completa en un archivo ZIP con nombre y destino configurables
activa: true
autor: opengio
version: 1.0.0
ejemplo: skill_run("comprimir_carpeta", ruta="C:/Documentos/Proyecto", destino="C:/Backups")
---

## Descripcion
Comprime una carpeta y todo su contenido en un archivo ZIP.
Muestra progreso por archivo, tamano original vs comprimido y
ratio de compresion. 100% stdlib de Python, sin dependencias.

## Codigo
```python
import os, sys, json, time, zipfile
from datetime import datetime, timezone
from pathlib import Path

def fmt_bytes(b):
    for u in ["B","KB","MB","GB"]:
        if b < 1024: return f"{b:.1f} {u}"
        b /= 1024
    return f"{b:.1f} TB"

def main():
    inicio = time.time()
    params  = json.loads(os.environ.get("SKILL_PARAMS", "{}"))
    ruta    = params.get("ruta", "")
    destino = params.get("destino", "")
    nombre  = params.get("nombre_zip", "")

    try:
        if not ruta:
            raise ValueError("Parametro 'ruta' es requerido.")

        p = Path(ruta)
        if not p.exists():
            raise FileNotFoundError(f"No existe: {ruta}")

        # Destino: misma carpeta padre si no se especifica
        dest_dir = Path(destino) if destino else p.parent
        dest_dir.mkdir(parents=True, exist_ok=True)

        # Nombre del ZIP
        ts = datetime.now().strftime("%Y%m%d_%H%M%S")
        zip_nombre = nombre if nombre else f"{p.name}_{ts}.zip"
        if not zip_nombre.endswith(".zip"):
            zip_nombre += ".zip"
        zip_path = dest_dir / zip_nombre

        archivos_ok  = 0
        archivos_err = 0
        bytes_orig   = 0

        with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zf:
            for entry in p.rglob("*"):
                if entry.is_file():
                    try:
                        arcname = entry.relative_to(p.parent)
                        zf.write(entry, arcname)
                        bytes_orig += entry.stat().st_size
                        archivos_ok += 1
                    except (PermissionError, OSError):
                        archivos_err += 1

        bytes_zip = zip_path.stat().st_size
        ratio     = round((1 - bytes_zip / bytes_orig) * 100, 1) if bytes_orig > 0 else 0

        resumen = (
            f"ZIP creado: {zip_path}\n"
            f"Archivos:   {archivos_ok:,} comprimidos"
            + (f", {archivos_err} con error" if archivos_err else "") + "\n"
            f"Tamano original:  {fmt_bytes(bytes_orig)}\n"
            f"Tamano ZIP:       {fmt_bytes(bytes_zip)}\n"
            f"Ratio compresion: {ratio}% ahorrado"
        )

        resultado = {
            "status":    "ok",
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "duracion":  round(time.time() - inicio, 3),
            "resumen":   resumen,
            "zip":       str(zip_path),
            "stats": {
                "archivos_comprimidos": archivos_ok,
                "archivos_error":       archivos_err,
                "bytes_original":       bytes_orig,
                "bytes_zip":            bytes_zip,
                "tamano_original":      fmt_bytes(bytes_orig),
                "tamano_zip":           fmt_bytes(bytes_zip),
                "ratio_pct":            ratio
            }
        }
    except Exception as e:
        resultado = {"status": "error", "detalle": str(e),
                     "timestamp": datetime.now(timezone.utc).isoformat()}

    print(json.dumps(resultado, ensure_ascii=False, indent=2))

if __name__ == "__main__":
    main()
```

## Parametros
- nombre: ruta        | tipo: string | requerido: true  | descripcion: Carpeta a comprimir
- nombre: destino     | tipo: string | requerido: false | descripcion: Donde guardar el ZIP (default: carpeta padre)
- nombre: nombre_zip  | tipo: string | requerido: false | descripcion: Nombre del archivo ZIP (default: nombre_carpeta_fecha.zip)
