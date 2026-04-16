---
id: clima_ciudad
nombre: Clima de una Ciudad
categoria: web
descripcion: Obtiene el clima actual de cualquier ciudad sin API key usando wttr.in
activa: true
autor: opengio
version: 1.0.0
ejemplo: skill_run("clima_ciudad", ciudad="Madrid")
---

## Descripcion
Consulta el clima actual de cualquier ciudad del mundo usando wttr.in
(servicio gratuito, sin API key). Devuelve temperatura, sensacion termica,
condicion, humedad, viento y visibilidad.

## Codigo
```python
import os, sys, json, time, urllib.request, urllib.parse
from datetime import datetime, timezone

def main():
    inicio = time.time()
    params = json.loads(os.environ.get("SKILL_PARAMS", "{}"))
    ciudad = params.get("ciudad", "Madrid")

    try:
        url = f"https://wttr.in/{urllib.parse.quote(ciudad)}?format=j1"
        req = urllib.request.Request(url, headers={"User-Agent": "curl/7.68.0"})
        with urllib.request.urlopen(req, timeout=10) as r:
            data = json.loads(r.read().decode("utf-8"))

        cc   = data["current_condition"][0]
        area = data["nearest_area"][0]
        nombre_area = area["areaName"][0]["value"]
        pais        = area["country"][0]["value"]

        temp_c    = cc["temp_C"]
        sensacion = cc["FeelsLikeC"]
        condicion = cc["weatherDesc"][0]["value"]
        humedad   = cc["humidity"]
        viento_km = cc["windspeedKmph"]
        viento_dir= cc["winddir16Point"]
        visib_km  = cc["visibility"]

        resumen = (
            f"Ciudad:       {nombre_area}, {pais}\n"
            f"Temperatura:  {temp_c} C (sensacion {sensacion} C)\n"
            f"Condicion:    {condicion}\n"
            f"Humedad:      {humedad}%\n"
            f"Viento:       {viento_km} km/h {viento_dir}\n"
            f"Visibilidad:  {visib_km} km"
        )

        resultado = {
            "status": "ok",
            "ciudad": nombre_area,
            "pais":   pais,
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "duracion":  round(time.time() - inicio, 3),
            "resumen":   resumen,
            "datos": {
                "temp_c":         int(temp_c),
                "sensacion_c":    int(sensacion),
                "condicion":      condicion,
                "humedad_pct":    int(humedad),
                "viento_kmh":     int(viento_km),
                "viento_dir":     viento_dir,
                "visibilidad_km": int(visib_km)
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
- nombre: ciudad | tipo: string | requerido: false | descripcion: Nombre de la ciudad (default: Madrid)
