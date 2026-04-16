---
id: calculadora
nombre: Calculadora Cientifica
categoria: datos
descripcion: Evalua expresiones matematicas complejas de forma segura con soporte para funciones cientificas
activa: true
autor: opengio
version: 1.0.0
ejemplo: skill_run("calculadora", expresion="sqrt(144) + sin(pi/2) * 100")
---

## Descripcion
Evalua cualquier expresion matematica de forma segura (sin eval directo).
Soporta operaciones basicas, funciones trigonometricas, logaritmos,
raices, potencias, constantes pi y e. Sin dependencias externas.

## Codigo
```python
import os, sys, json, time, math, operator
from datetime import datetime, timezone

FUNCIONES = {
    "sqrt": math.sqrt, "cbrt": lambda x: x**(1/3),
    "sin":  math.sin,  "cos":  math.cos,  "tan": math.tan,
    "asin": math.asin, "acos": math.acos, "atan": math.atan,
    "log":  math.log,  "log2": math.log2, "log10": math.log10,
    "exp":  math.exp,  "abs":  abs,       "ceil": math.ceil,
    "floor":math.floor,"round":round,     "factorial": math.factorial,
    "gcd":  math.gcd,  "pi":   math.pi,   "e": math.e,
    "inf":  math.inf,  "tau":  math.tau,
    "degrees": math.degrees, "radians": math.radians,
    "pow":  pow,       "max":  max,       "min": min,
    "sum":  sum,
}

def eval_seguro(expr):
    import ast
    expr_clean = expr.strip().replace("^","**")
    tree = ast.parse(expr_clean, mode="eval")

    class Validador(ast.NodeVisitor):
        NODOS_OK = (
            ast.Expression, ast.BinOp, ast.UnaryOp, ast.Call,
            ast.Constant, ast.Name, ast.Load,
            ast.Add, ast.Sub, ast.Mult, ast.Div, ast.Mod,
            ast.Pow, ast.FloorDiv, ast.USub, ast.UAdd,
            ast.Tuple, ast.List,
        )
        def visit(self, node):
            if not isinstance(node, self.NODOS_OK):
                raise ValueError(f"Operacion no permitida: {type(node).__name__}")
            self.generic_visit(node)

    Validador().visit(tree)
    return eval(compile(tree, "<string>", "eval"), {"__builtins__": {}}, FUNCIONES)

def main():
    inicio = time.time()
    params = json.loads(os.environ.get("SKILL_PARAMS", "{}"))
    expr   = params.get("expresion", "2 + 2")

    try:
        resultado_val = eval_seguro(expr)

        if isinstance(resultado_val, float):
            if resultado_val == int(resultado_val):
                resultado_str = str(int(resultado_val))
            else:
                resultado_str = f"{resultado_val:.10g}"
        else:
            resultado_str = str(resultado_val)

        resumen = f"{expr} = {resultado_str}"

        resultado = {
            "status":      "ok",
            "timestamp":   datetime.now(timezone.utc).isoformat(),
            "duracion":    round(time.time() - inicio, 4),
            "expresion":   expr,
            "resultado":   resultado_val if not math.isinf(resultado_val) else str(resultado_val),
            "resultado_str": resultado_str,
            "resumen":     resumen
        }
    except Exception as e:
        resultado = {
            "status":  "error",
            "detalle": str(e),
            "expresion": expr,
            "timestamp": datetime.now(timezone.utc).isoformat()
        }

    print(json.dumps(resultado, ensure_ascii=False, indent=2))

if __name__ == "__main__":
    main()
```

## Parametros
- nombre: expresion | tipo: string | requerido: false | descripcion: Expresion matematica ej sqrt(144) sin(pi/2) log(1000,10)
