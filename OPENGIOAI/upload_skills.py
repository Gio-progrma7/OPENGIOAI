import urllib.request, urllib.parse, time, os

BASE = os.path.join(os.path.dirname(__file__), "skills_hub")

files = [
    "precio_cripto",
    "ip_publica",
    "procesos_activos",
    "generar_contrasena",
    "comprimir_carpeta",
    "duplicados_carpeta",
    "noticias_rss",
    "extraer_links",
    "calculadora",
    "screenshot",
    "limpiar_temporales",
    "convertir_csv_json",
]

for skill_id in files:
    content = open(os.path.join(BASE, f"{skill_id}.md"), encoding="utf-8").read()
    payload = urllib.parse.urlencode({
        "content":     content,
        "syntax":      "text",
        "expiry_days": 365
    }).encode("utf-8")
    req = urllib.request.Request(
        "https://dpaste.com/api/v2/",
        data=payload,
        headers={
            "Content-Type": "application/x-www-form-urlencoded",
            "User-Agent":   "OPENGIOAI/1.0"
        },
        method="POST"
    )
    with urllib.request.urlopen(req, timeout=15) as r:
        url = r.read().decode().strip().strip('"') + ".txt"
        print(f"{skill_id}|{url}")
    time.sleep(0.6)
