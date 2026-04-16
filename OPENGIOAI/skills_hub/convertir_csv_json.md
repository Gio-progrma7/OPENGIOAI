---
id: convertir_csv_json
nombre: Convertir CSV a JSON
categoria: datos
descripcion: Convierte archivos CSV a JSON con deteccion automatica de separador y encoding
activa: true
autor: opengio
version: 1.0.0
ejemplo: skill_run("convertir_csv_json", archivo="C:/datos/reporte.csv")
---

## Descripcion
Lee un archivo CSV y lo convierte a JSON. Detecta automaticamente
el separador (coma, punto y coma, tabulacion) y el encoding.
Genera estadisticas de columnas y muestra una preview de los datos.

## Codigo
```python
import os, sys, json, time, csv
from datetime import datetime, timezone
from pathlib import Path

def detectar_separador(linea):
    for sep in [",", ";", "\t", "|"]:
        if sep in linea:
            return sep
    return ","

def main():
    inicio  = time.time()
    params  = json.loads(os.environ.get("SKILL_PARAMS", "{}"))
    archivo = params.get("archivo", "")
    limite  = int(params.get("limite_filas", 1000))
    destino = params.get("destino", "")

    try:
        if not archivo:
            raise ValueError("Parametro 'archivo' es requerido.")

        p = Path(archivo)
        if not p.exists():
            raise FileNotFoundError(f"No existe: {archivo}")

        # Detectar encoding
        encodings = ["utf-8-sig", "utf-8", "latin-1", "cp1252"]
        contenido = None
        enc_usado = "utf-8"
        for enc in encodings:
            try:
                contenido = p.read_text(encoding=enc)
                enc_usado = enc
                break
            except UnicodeDecodeError:
                continue

        if not contenido:
            raise ValueError("No se pudo leer el archivo con ninguna codificacion conocida.")

        lineas = contenido.splitlines()
        sep    = detectar_separador(lineas[0] if lineas else "")

        reader   = csv.DictReader(contenido.splitlines(), delimiter=sep)
        columnas = reader.fieldnames or []
        filas    = []

        for fila in reader:
            filas.append(dict(fila))
            if len(filas) >= limite:
                break

        # Guardar JSON
        dest_dir = Path(destino) if destino else p.parent
        dest_dir.mkdir(parents=True, exist_ok=True)
        json_path = dest_dir / (p.stem + ".json")
        json_path.write_text(
            json.dumps(filas, ensure_ascii=False, indent=2), encoding="utf-8")

        # Estadisticas de columnas
        col_stats = {}
        for col in columnas:
            valores = [r.get(col,"") for r in filas if r.get(col,"").strip()]
            col_stats[col] = {"no_vacios": len(valores), "ejemplo": valores[0] if valores else ""}

        resumen = (
            f"Archivo:   {archivo}\n"
            f"Encoding:  {enc_usado} | Separador: '{sep}'\n"
            f"Columnas:  {len(columnas)} — {', '.join(columnas[:8])}"
            + ("..." if len(columnas) > 8 else "") + "\n"
            f"Filas:     {len(filas):,}\n"
            f"JSON:      {json_path}"
        )

        resultado = {
            "status":    "ok",
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "duracion":  round(time.time() - inicio, 3),
            "resumen":   resumen,
            "json_path": str(json_path),
            "stats": {
                "columnas":      len(columnas),
                "filas":         len(filas),
                "encoding":      enc_usado,
                "separador":     sep,
                "limite_activo": len(filas) >= limite
            },
            "columnas":     columnas,
            "col_stats":    col_stats,
            "preview":      filas[:3]
        }
    except Exception as e:
        resultado = {"status": "error", "detalle": str(e),
                     "timestamp": datetime.now(timezone.utc).isoformat()}

    print(json.dumps(resultado, ensure_ascii=False, indent=2))

if __name__ == "__main__":
    main()
```

## Parametros
- nombre: archivo      | tipo: string | requerido: true  | descripcion: Ruta del archivo CSV
- nombre: destino      | tipo: string | requerido: false | descripcion: Carpeta donde guardar el JSON (default: misma carpeta del CSV)
- nombre: limite_filas | tipo: number | requerido: false | descripcion: Maximo de filas a procesar (default 1000)
