---
id: generar_contrasena
nombre: Generar Contrasena Segura
categoria: datos
descripcion: Genera contrasenas criptograficamente seguras con opciones de longitud y complejidad
activa: true
autor: opengio
version: 1.0.0
ejemplo: skill_run("generar_contrasena", longitud=20, cantidad=5)
---

## Descripcion
Genera contrasenas aleatorias usando el modulo secrets de Python
(criptograficamente seguro). Permite configurar longitud, cantidad,
y si incluir simbolos, numeros y mayusculas. Sin dependencias externas.

## Codigo
```python
import os, sys, json, time, secrets, string
from datetime import datetime, timezone

def generar(longitud, usar_mayus, usar_numeros, usar_simbolos):
    pool = string.ascii_lowercase
    obligatorios = []

    if usar_mayus:
        pool += string.ascii_uppercase
        obligatorios.append(secrets.choice(string.ascii_uppercase))
    if usar_numeros:
        pool += string.digits
        obligatorios.append(secrets.choice(string.digits))
    if usar_simbolos:
        simbolos = "!@#$%^&*()-_=+[]{}|;:,.<>?"
        pool += simbolos
        obligatorios.append(secrets.choice(simbolos))

    restante = longitud - len(obligatorios)
    pwd = obligatorios + [secrets.choice(pool) for _ in range(max(restante, 0))]
    secrets.SystemRandom().shuffle(pwd)
    return "".join(pwd)

def entropia_bits(longitud, pool_size):
    import math
    return round(math.log2(pool_size) * longitud, 1)

def main():
    inicio = time.time()
    params  = json.loads(os.environ.get("SKILL_PARAMS", "{}"))
    longitud   = int(params.get("longitud", 16))
    cantidad   = int(params.get("cantidad", 3))
    mayus      = bool(params.get("mayusculas", True))
    numeros    = bool(params.get("numeros", True))
    simbolos   = bool(params.get("simbolos", True))

    longitud = max(8, min(128, longitud))
    cantidad = max(1, min(20, cantidad))

    pool_size = 26
    if mayus:    pool_size += 26
    if numeros:  pool_size += 10
    if simbolos: pool_size += 24

    try:
        contrasenas = [generar(longitud, mayus, numeros, simbolos) for _ in range(cantidad)]
        entropia    = entropia_bits(longitud, pool_size)
        nivel       = ("Muy alta" if entropia > 100 else
                       "Alta"     if entropia > 70  else
                       "Media"    if entropia > 50  else "Baja")

        resumen = (
            f"Contrasenas generadas ({longitud} chars, entropia ~{entropia} bits — {nivel}):\n" +
            "\n".join(f"  {i+1}. {p}" for i, p in enumerate(contrasenas))
        )

        resultado = {
            "status":     "ok",
            "timestamp":  datetime.now(timezone.utc).isoformat(),
            "duracion":   round(time.time() - inicio, 3),
            "resumen":    resumen,
            "contrasenas": contrasenas,
            "config": {
                "longitud":       longitud,
                "mayusculas":     mayus,
                "numeros":        numeros,
                "simbolos":       simbolos,
                "entropia_bits":  entropia,
                "nivel_seguridad":nivel
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
- nombre: longitud   | tipo: number  | requerido: false | descripcion: Longitud de la contrasena (default 16, max 128)
- nombre: cantidad   | tipo: number  | requerido: false | descripcion: Cuantas contrasenas generar (default 3)
- nombre: mayusculas | tipo: boolean | requerido: false | descripcion: Incluir mayusculas (default true)
- nombre: numeros    | tipo: boolean | requerido: false | descripcion: Incluir numeros (default true)
- nombre: simbolos   | tipo: boolean | requerido: false | descripcion: Incluir simbolos (default true)
