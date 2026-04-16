---
id: screenshot
nombre: Captura de Pantalla
categoria: sistema
descripcion: Toma una captura de pantalla y la guarda como PNG con nombre y ruta configurables
activa: true
autor: opengio
version: 1.0.0
ejemplo: skill_run("screenshot", nombre="captura_reporte")
---

## Descripcion
Toma una captura de pantalla completa usando Pillow (PIL) e ImageGrab.
Guarda el PNG en la carpeta del script o en la ruta especificada.
Instala Pillow automaticamente si no esta disponible.

## Codigo
```python
import os, sys, json, time, subprocess
from datetime import datetime, timezone

def main():
    inicio = time.time()
    params  = json.loads(os.environ.get("SKILL_PARAMS", "{}"))
    nombre  = params.get("nombre", "")
    destino = params.get("destino", "")

    SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))

    try:
        try:
            from PIL import ImageGrab
        except ImportError:
            subprocess.check_call(
                [sys.executable, "-m", "pip", "install", "Pillow"],
                stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            from PIL import ImageGrab

        ts      = datetime.now().strftime("%Y%m%d_%H%M%S")
        nombre_arch = f"{nombre}_{ts}" if nombre else f"screenshot_{ts}"
        nombre_arch = nombre_arch.replace(" ", "_") + ".png"

        carpeta = destino if destino else SCRIPT_DIR
        os.makedirs(carpeta, exist_ok=True)
        ruta_png = os.path.join(carpeta, nombre_arch)

        img = ImageGrab.grab()
        img.save(ruta_png, "PNG")

        ancho, alto = img.size
        size_kb     = round(os.path.getsize(ruta_png) / 1024, 1)

        resumen = (
            f"Captura guardada: {ruta_png}\n"
            f"Resolucion: {ancho}x{alto} px\n"
            f"Tamano:     {size_kb} KB"
        )

        resultado = {
            "status":     "ok",
            "timestamp":  datetime.now(timezone.utc).isoformat(),
            "duracion":   round(time.time() - inicio, 3),
            "resumen":    resumen,
            "archivo":    ruta_png,
            "resolucion": {"ancho": ancho, "alto": alto},
            "tamano_kb":  size_kb
        }
    except Exception as e:
        resultado = {"status": "error", "detalle": str(e),
                     "timestamp": datetime.now(timezone.utc).isoformat()}

    print(json.dumps(resultado, ensure_ascii=False, indent=2))

if __name__ == "__main__":
    main()
```

## Parametros
- nombre: nombre   | tipo: string | requerido: false | descripcion: Prefijo del nombre del archivo (default: screenshot)
- nombre: destino  | tipo: string | requerido: false | descripcion: Carpeta donde guardar (default: carpeta del script)
