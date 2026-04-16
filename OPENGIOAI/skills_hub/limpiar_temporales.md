---
id: limpiar_temporales
nombre: Limpiar Archivos Temporales
categoria: sistema
descripcion: Elimina archivos de la carpeta Temp del sistema y libera espacio en disco
activa: true
autor: opengio
version: 1.0.0
ejemplo: skill_run("limpiar_temporales", simular=true)
---

## Descripcion
Escanea y elimina archivos de la carpeta temporal del sistema.
Primero hace un scan en modo simulacion mostrando cuanto se puede
liberar. Con simular=false los elimina realmente. Sin dependencias.

## Codigo
```python
import os, sys, json, time, tempfile
from datetime import datetime, timezone
from pathlib import Path

def fmt_bytes(b):
    for u in ["B","KB","MB","GB"]:
        if b < 1024: return f"{b:.1f} {u}"
        b /= 1024
    return f"{b:.1f} TB"

def main():
    inicio  = time.time()
    params  = json.loads(os.environ.get("SKILL_PARAMS", "{}"))
    simular = bool(params.get("simular", True))   # True = solo reportar
    max_age = int(params.get("max_dias", 0))       # 0 = todos

    carpeta_temp = Path(tempfile.gettempdir())

    eliminados   = 0
    errores      = 0
    bytes_lib    = 0
    archivos_esc = []

    ahora = time.time()

    for entry in carpeta_temp.rglob("*"):
        if not entry.is_file():
            continue
        try:
            stat = entry.stat()
            edad_dias = (ahora - stat.st_mtime) / 86400
            if max_age > 0 and edad_dias < max_age:
                continue
            size = stat.st_size
            if not simular:
                entry.unlink()
            bytes_lib += size
            eliminados += 1
            if len(archivos_esc) < 10:
                archivos_esc.append({
                    "nombre":    entry.name,
                    "tamano":    fmt_bytes(size),
                    "edad_dias": round(edad_dias, 1)
                })
        except (PermissionError, OSError):
            errores += 1

    accion = "Se puede liberar" if simular else "Liberado"
    resumen = (
        f"Carpeta temp: {carpeta_temp}\n"
        f"Modo:         {'SIMULACION (no borra nada)' if simular else 'REAL (archivos eliminados)'}\n"
        f"Archivos:     {eliminados:,} procesados\n"
        f"{accion}: {fmt_bytes(bytes_lib)}\n"
        f"Errores:      {errores}"
    )

    resultado = {
        "status":     "ok",
        "timestamp":  datetime.now(timezone.utc).isoformat(),
        "duracion":   round(time.time() - inicio, 3),
        "resumen":    resumen,
        "simulacion": simular,
        "stats": {
            "archivos_procesados": eliminados,
            "bytes_liberados":     bytes_lib,
            "espacio_liberado":    fmt_bytes(bytes_lib),
            "errores_acceso":      errores
        },
        "muestra_archivos": archivos_esc
    }

    print(json.dumps(resultado, ensure_ascii=False, indent=2))

if __name__ == "__main__":
    main()
```

## Parametros
- nombre: simular  | tipo: boolean | requerido: false | descripcion: true solo reporta sin borrar (default true), false borra realmente
- nombre: max_dias | tipo: number  | requerido: false | descripcion: Solo borrar archivos mas viejos que N dias (default 0 = todos)
