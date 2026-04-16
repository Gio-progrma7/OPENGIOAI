---
id: fecha_hora_mundial
nombre: Fecha y Hora Mundial
categoria: sistema
descripcion: Muestra la fecha y hora actual en multiples zonas horarias
activa: true
autor: opengio
version: 1.0.0
ejemplo: skill_run("fecha_hora_mundial")
---

## Descripcion
Devuelve la fecha y hora actual en UTC y en las zonas horarias mas usadas
en Latinoamerica y el mundo. Acepta zonas personalizadas adicionales.

## Codigo
```python
import os, sys, json, time
from datetime import datetime, timezone
from zoneinfo import ZoneInfo

ZONAS_DEFAULT = [
    ("UTC",              "UTC"),
    ("Ciudad de Mexico", "America/Mexico_City"),
    ("Bogota",           "America/Bogota"),
    ("Lima",             "America/Lima"),
    ("Buenos Aires",     "America/Argentina/Buenos_Aires"),
    ("Santiago",         "America/Santiago"),
    ("Madrid",           "Europe/Madrid"),
    ("Nueva York",       "America/New_York"),
    ("Los Angeles",      "America/Los_Angeles"),
    ("Londres",          "Europe/London"),
    ("Tokyo",            "Asia/Tokyo"),
]

def main():
    inicio    = time.time()
    params    = json.loads(os.environ.get("SKILL_PARAMS", "{}"))
    extras    = params.get("zonas", [])
    zonas     = list(ZONAS_DEFAULT)
    for tz_str in extras:
        zonas.append((tz_str, tz_str))

    ahora_utc = datetime.now(timezone.utc)
    zonas_res = []
    lineas    = []

    for nombre, tz_id in zonas:
        try:
            tz  = ZoneInfo(tz_id)
            dt  = ahora_utc.astimezone(tz)
            fmt = dt.strftime("%Y-%m-%d  %H:%M:%S  %Z")
            lineas.append(f"  {nombre:<22} {fmt}")
            zonas_res.append({"zona": nombre, "tz_id": tz_id,
                               "iso": dt.isoformat(), "legible": fmt})
        except Exception:
            lineas.append(f"  {nombre:<22} (zona invalida: {tz_id})")

    resumen = "Fecha y hora actual:\n" + "\n".join(lineas)

    resultado = {
        "status":    "ok",
        "timestamp": ahora_utc.isoformat(),
        "duracion":  round(time.time() - inicio, 3),
        "resumen":   resumen,
        "zonas":     zonas_res,
        "unix_ts":   int(ahora_utc.timestamp())
    }
    print(json.dumps(resultado, ensure_ascii=False, indent=2))

if __name__ == "__main__":
    main()
```

## Parametros
- nombre: zonas | tipo: array | requerido: false | descripcion: Zonas horarias extra ej ["Asia/Dubai","Pacific/Auckland"]
