---
id: procesos_activos
nombre: Procesos Activos del Sistema
categoria: sistema
descripcion: Lista los procesos que mas CPU y RAM consumen en el equipo en tiempo real
activa: true
autor: opengio
version: 1.0.0
ejemplo: skill_run("procesos_activos", top=10)
---

## Descripcion
Obtiene los N procesos que mas CPU y RAM consumen usando psutil.
Devuelve PID, nombre, CPU%, RAM en MB, estado y tiempo de inicio.
Util para diagnostico de rendimiento y deteccion de procesos pesados.

## Codigo
```python
import os, sys, json, time, subprocess
from datetime import datetime, timezone

def main():
    inicio = time.time()
    params = json.loads(os.environ.get("SKILL_PARAMS", "{}"))
    top    = int(params.get("top", 10))
    orden  = params.get("orden", "cpu").lower()   # "cpu" o "ram"

    try:
        try:
            import psutil
        except ImportError:
            subprocess.check_call([sys.executable, "-m", "pip", "install", "psutil"],
                                  stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            import psutil

        procs = []
        for p in psutil.process_iter(["pid","name","cpu_percent","memory_info","status","create_time"]):
            try:
                info = p.info
                ram_mb = round(info["memory_info"].rss / (1024**2), 2) if info["memory_info"] else 0
                procs.append({
                    "pid":      info["pid"],
                    "nombre":   info["name"] or "?",
                    "cpu_pct":  info["cpu_percent"] or 0,
                    "ram_mb":   ram_mb,
                    "estado":   info["status"],
                    "inicio":   datetime.fromtimestamp(
                        info["create_time"], tz=timezone.utc
                    ).strftime("%H:%M:%S") if info["create_time"] else "?"
                })
            except (psutil.NoSuchProcess, psutil.AccessDenied):
                pass

        clave = "cpu_pct" if orden == "cpu" else "ram_mb"
        procs.sort(key=lambda x: x[clave], reverse=True)
        top_procs = procs[:top]

        lineas = [f"  {'PID':>6}  {'CPU%':>6}  {'RAM MB':>8}  {'Estado':<12}  Nombre"]
        lineas.append("  " + "-"*60)
        for p in top_procs:
            lineas.append(
                f"  {p['pid']:>6}  {p['cpu_pct']:>5.1f}%  {p['ram_mb']:>7.1f}  "
                f"{p['estado']:<12}  {p['nombre']}"
            )

        resumen = f"Top {top} procesos por {orden.upper()}:\n" + "\n".join(lineas)

        resultado = {
            "status":    "ok",
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "duracion":  round(time.time() - inicio, 3),
            "resumen":   resumen,
            "total_procesos": len(procs),
            "procesos":  top_procs
        }
    except Exception as e:
        resultado = {"status": "error", "detalle": str(e),
                     "timestamp": datetime.now(timezone.utc).isoformat()}

    print(json.dumps(resultado, ensure_ascii=False, indent=2))

if __name__ == "__main__":
    main()
```

## Parametros
- nombre: top   | tipo: number | requerido: false | descripcion: Cuantos procesos mostrar (default 10)
- nombre: orden | tipo: string | requerido: false | descripcion: Ordenar por cpu o ram (default cpu)
