---
id: convertir_moneda
nombre: Convertir Moneda
categoria: web
descripcion: Convierte entre cualquier par de monedas usando frankfurter.app, gratis y sin API key
activa: true
autor: opengio
version: 1.0.0
ejemplo: skill_run("convertir_moneda", monto=100, de="USD", a="EUR")
---

## Descripcion
Convierte un monto entre dos monedas usando la API de frankfurter.app
(Banco Central Europeo). Tipos de cambio actualizados diariamente.
Sin API key. Soporta USD, EUR, MXN, COP, ARS, GBP, JPY y mas.

## Codigo
```python
import os, sys, json, time, urllib.request
from datetime import datetime, timezone

def main():
    inicio = time.time()
    params = json.loads(os.environ.get("SKILL_PARAMS", "{}"))
    monto  = float(params.get("monto", 1))
    de     = str(params.get("de", "USD")).upper()
    a      = str(params.get("a",  "EUR")).upper()

    try:
        url = f"https://api.frankfurter.app/latest?amount={monto}&from={de}&to={a}"
        with urllib.request.urlopen(url, timeout=10) as r:
            data = json.loads(r.read().decode())

        resultado_monto = data["rates"][a]
        tasa  = round(resultado_monto / monto, 6)
        fecha = data["date"]

        resumen = (
            f"{monto} {de} = {resultado_monto:.4f} {a}\n"
            f"Tasa: 1 {de} = {tasa} {a}\n"
            f"Fecha tipo de cambio: {fecha}"
        )

        resultado = {
            "status":          "ok",
            "timestamp":       datetime.now(timezone.utc).isoformat(),
            "duracion":        round(time.time() - inicio, 3),
            "resumen":         resumen,
            "monto_origen":    monto,
            "moneda_origen":   de,
            "moneda_destino":  a,
            "monto_resultado": resultado_monto,
            "tasa_cambio":     tasa,
            "fecha_tasa":      fecha
        }
    except Exception as e:
        resultado = {"status": "error", "detalle": str(e),
                     "timestamp": datetime.now(timezone.utc).isoformat()}

    print(json.dumps(resultado, ensure_ascii=False, indent=2))

if __name__ == "__main__":
    main()
```

## Parametros
- nombre: monto | tipo: number | requerido: false | descripcion: Cantidad a convertir (default: 1)
- nombre: de    | tipo: string | requerido: false | descripcion: Moneda origen ej USD EUR MXN COP
- nombre: a     | tipo: string | requerido: false | descripcion: Moneda destino ej EUR MXN COP ARS
