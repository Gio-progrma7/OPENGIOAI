---
id: precio_cripto
nombre: Precio de Criptomonedas
categoria: web
descripcion: Consulta precios en tiempo real de Bitcoin, Ethereum y mas usando CoinGecko (sin API key)
activa: true
autor: opengio
version: 1.0.0
ejemplo: skill_run("precio_cripto", monedas="bitcoin,ethereum,solana")
---

## Descripcion
Obtiene precios actuales, cambio en 24h y capitalizacion de mercado
de cualquier criptomoneda usando la API gratuita de CoinGecko.
Sin API key. Soporta cualquier moneda listada en CoinGecko.

## Codigo
```python
import os, sys, json, time, urllib.request
from datetime import datetime, timezone

def main():
    inicio  = time.time()
    params  = json.loads(os.environ.get("SKILL_PARAMS", "{}"))
    monedas = params.get("monedas", "bitcoin,ethereum,solana")
    vs      = params.get("divisa", "usd").lower()

    ids = ",".join(m.strip().lower() for m in monedas.split(","))

    try:
        url = (f"https://api.coingecko.com/api/v3/simple/price"
               f"?ids={ids}&vs_currencies={vs}"
               f"&include_24hr_change=true&include_market_cap=true")
        req = urllib.request.Request(url, headers={"User-Agent": "OPENGIOAI/1.0"})
        with urllib.request.urlopen(req, timeout=12) as r:
            data = json.loads(r.read().decode())

        if not data:
            raise ValueError(f"No se encontraron datos para: {ids}")

        lineas = []
        detalle = []
        for coin_id, vals in data.items():
            precio  = vals.get(vs, 0)
            cambio  = vals.get(f"{vs}_24h_change", 0)
            mcap    = vals.get(f"{vs}_market_cap", 0)
            signo   = "+" if cambio >= 0 else ""
            flecha  = "▲" if cambio >= 0 else "▼"
            lineas.append(
                f"  {coin_id:<15} ${precio:>12,.2f} {vs.upper()}"
                f"  {flecha} {signo}{cambio:.2f}%"
                f"  Cap: ${mcap/1e9:.2f}B"
            )
            detalle.append({
                "id":          coin_id,
                "precio":      precio,
                "divisa":      vs.upper(),
                "cambio_24h":  round(cambio, 4),
                "market_cap":  mcap
            })

        resumen = f"Precios ({vs.upper()}) — {datetime.now().strftime('%H:%M:%S')}:\n" + "\n".join(lineas)

        resultado = {
            "status":    "ok",
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "duracion":  round(time.time() - inicio, 3),
            "resumen":   resumen,
            "precios":   detalle
        }
    except Exception as e:
        resultado = {"status": "error", "detalle": str(e),
                     "timestamp": datetime.now(timezone.utc).isoformat()}

    print(json.dumps(resultado, ensure_ascii=False, indent=2))

if __name__ == "__main__":
    main()
```

## Parametros
- nombre: monedas | tipo: string | requerido: false | descripcion: IDs separados por coma ej bitcoin,ethereum,solana,cardano
- nombre: divisa  | tipo: string | requerido: false | descripcion: Divisa de referencia ej usd eur mxn cop (default usd)
