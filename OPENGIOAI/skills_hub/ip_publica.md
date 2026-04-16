---
id: ip_publica
nombre: IP Publica y Geolocalizacion
categoria: web
descripcion: Obtiene la IP publica del equipo con pais, ciudad, ISP y coordenadas
activa: true
autor: opengio
version: 1.0.0
ejemplo: skill_run("ip_publica")
---

## Descripcion
Consulta la IP publica del equipo y su geolocalizacion usando ipapi.co
(gratuito, sin API key, hasta 1000 consultas/dia). Devuelve pais,
region, ciudad, ISP, zona horaria y coordenadas GPS.

## Codigo
```python
import os, sys, json, time, urllib.request
from datetime import datetime, timezone

def main():
    inicio = time.time()

    try:
        url = "https://ipapi.co/json/"
        req = urllib.request.Request(url, headers={"User-Agent": "OPENGIOAI/1.0"})
        with urllib.request.urlopen(req, timeout=10) as r:
            data = json.loads(r.read().decode())

        if data.get("error"):
            raise ValueError(data.get("reason", "Error de API"))

        ip       = data.get("ip", "")
        pais     = data.get("country_name", "")
        codigo   = data.get("country_code", "")
        region   = data.get("region", "")
        ciudad   = data.get("city", "")
        isp      = data.get("org", "")
        tz       = data.get("timezone", "")
        lat      = data.get("latitude", 0)
        lon      = data.get("longitude", 0)
        moneda   = data.get("currency", "")

        resumen = (
            f"IP Publica:   {ip}\n"
            f"Ubicacion:    {ciudad}, {region}, {pais} ({codigo})\n"
            f"ISP:          {isp}\n"
            f"Zona horaria: {tz}\n"
            f"Coordenadas:  {lat}, {lon}\n"
            f"Moneda local: {moneda}"
        )

        resultado = {
            "status":    "ok",
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "duracion":  round(time.time() - inicio, 3),
            "resumen":   resumen,
            "ip":        ip,
            "geo": {
                "pais":     pais,
                "codigo":   codigo,
                "region":   region,
                "ciudad":   ciudad,
                "lat":      lat,
                "lon":      lon,
                "timezone": tz,
                "isp":      isp,
                "moneda":   moneda
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
(Sin parametros - detecta automaticamente la IP publica del equipo)
