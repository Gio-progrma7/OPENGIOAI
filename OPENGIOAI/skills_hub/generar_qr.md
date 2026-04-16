---
id: generar_qr
nombre: Generar Codigo QR
categoria: datos
descripcion: Genera un codigo QR desde cualquier texto o URL y lo guarda como PNG
activa: true
autor: opengio
version: 1.0.0
ejemplo: skill_run("generar_qr", texto="https://opengio.ai", nombre_archivo="mi_qr")
---

## Descripcion
Genera un codigo QR a partir de cualquier texto, URL o dato y lo guarda
como imagen PNG en la carpeta del script. Instala automaticamente qrcode
si no esta disponible.

## Codigo
```python
import os, sys, json, time, subprocess
from datetime import datetime, timezone

def main():
    inicio = time.time()
    params = json.loads(os.environ.get("SKILL_PARAMS", "{}"))
    texto  = params.get("texto", "https://opengio.ai")
    nombre = params.get("nombre_archivo", "qr_code")
    size   = int(params.get("tamano", 10))

    SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))

    try:
        try:
            import qrcode
        except ImportError:
            subprocess.check_call(
                [sys.executable, "-m", "pip", "install", "qrcode[pil]"],
                stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            import qrcode

        qr = qrcode.QRCode(
            version=None,
            error_correction=qrcode.constants.ERROR_CORRECT_H,
            box_size=size,
            border=4
        )
        qr.add_data(texto)
        qr.make(fit=True)
        img = qr.make_image(fill_color="black", back_color="white")

        nombre_limpio = "".join(c for c in nombre if c.isalnum() or c in "-_")
        archivo = os.path.join(SCRIPT_DIR, f"{nombre_limpio}.png")
        img.save(archivo)

        resumen = (
            f"QR generado exitosamente\n"
            f"Texto:      {texto[:80]}{'...' if len(texto)>80 else ''}\n"
            f"Archivo:    {archivo}\n"
            f"Version QR: {qr.version}  |  Tamano: {size}px por cuadrito"
        )

        resultado = {
            "status":     "ok",
            "timestamp":  datetime.now(timezone.utc).isoformat(),
            "duracion":   round(time.time() - inicio, 3),
            "resumen":    resumen,
            "archivo":    archivo,
            "texto":      texto,
            "version_qr": qr.version
        }
    except Exception as e:
        resultado = {"status": "error", "detalle": str(e),
                     "timestamp": datetime.now(timezone.utc).isoformat()}

    print(json.dumps(resultado, ensure_ascii=False, indent=2))

if __name__ == "__main__":
    main()
```

## Parametros
- nombre: texto          | tipo: string | requerido: false | descripcion: Texto o URL a codificar
- nombre: nombre_archivo | tipo: string | requerido: false | descripcion: Nombre del PNG sin extension (default: qr_code)
- nombre: tamano         | tipo: number | requerido: false | descripcion: Pixeles por cuadrito del QR (default: 10)
