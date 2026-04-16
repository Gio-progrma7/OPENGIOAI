# OPENGIOAI

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![.NET 9](https://img.shields.io/badge/.NET%209-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Windows Forms](https://img.shields.io/badge/Windows%20Forms-0078D4?style=for-the-badge&logo=windows&logoColor=white)
![Python](https://img.shields.io/badge/Python%203.8+-3776AB?style=for-the-badge&logo=python&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-blue)

**OPENGIOAI** es una plataforma de escritorio desarrollada en C# con .NET 9 que orquesta múltiples agentes de inteligencia artificial, ejecuta scripts Python generados dinámicamente, gestiona un sistema de Skills extensible y permite crear automatizaciones visuales con nodos. Compatible con OpenAI, Google Gemini, Anthropic Claude, DeepSeek, OpenRouter, Ollama y Google Vertex AI.

---

## Tabla de Contenidos

1. [Arquitectura General](#arquitectura-general)
2. [Pipeline ARIA — El Motor de Agentes](#pipeline-aria)
3. [Sistema de Skills y Skills Hub](#sistema-de-skills-y-skills-hub)
4. [Automatizaciones con Nodos](#automatizaciones-con-nodos)
5. [Sistema de Credenciales Seguras](#sistema-de-credenciales-seguras)
6. [Multi-Proveedor de LLMs](#multi-proveedor-de-llms)
7. [Integración Multi-Canal](#integracion-multi-canal)
8. [Cómo Empezar](#como-empezar)
9. [Extensión y Desarrollo](#extension-y-desarrollo)
10. [Troubleshooting](#troubleshooting)

---

## Arquitectura General

OPENGIOAI se organiza en capas bien definidas. El punto de entrada es siempre `AIModelConector.cs`, el orquestador central que coordina todos los agentes, providers y herramientas.

```
┌─────────────────────────────────────────────────────────────────┐
│                     CAPA DE INTERFAZ                            │
│  FrmMandos (chat)  •  FrmAutomatizaciones  •  FrmPipelineAgente │
│  Telegram Bot      •  Slack Bot                                 │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│               CAPA DE ORQUESTACIÓN DE AGENTES                   │
│  OrquestadorARIA (pipeline 4 fases)                             │
│  PipelineMultiAgente (Planificador → Ejecutor → Verificador)    │
│  AgentePlanificador (ReAct / Chain-of-Thought)                  │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│               AIModelConector  (hub central)                    │
│  AgentContext.BuildAsync()  •  RetryPolicy  •  Streaming        │
│  Enrutamiento por proveedor  •  Ejecución Python                │
└──────────────┬──────────────────────────┬───────────────────────┘
               │                          │
       ┌───────▼───────┐          ┌───────▼───────┐
       │  LLM EXTERNOS │          │ EJECUCIÓN LOCAL│
       │  OpenAI       │          │  Ollama        │
       │  Claude       │          │  Python proc   │
       │  Gemini       │          │  File I/O      │
       │  DeepSeek     │          │  Skills runner │
       │  Vertex AI    │          │  Herramientas  │
       └───────────────┘          └───────────────┘
```

### Estructura de carpetas

```
OPENGIOAI/
├── Agentes/                    # Motores de orquestación
│   ├── OrquestadorARIA.cs      # Pipeline ARIA de 4 fases
│   ├── PipelineMultiAgente.cs  # Pipeline paralelo / verificador
│   ├── AgentePlanificador.cs   # Planificación ReAct/CoT
│   └── PasoDelPlan.cs          # Modelo de paso de plan
│
├── Data/                       # Núcleo lógico
│   ├── AIModelConector.cs      # Orquestador principal (hub)
│   ├── AgentContext.cs         # Contexto inmutable de ejecución
│   ├── RetryPolicy.cs          # Backoff exponencial con jitter
│   └── ConversationWindow.cs   # Ventana deslizante de historial
│
├── Skills/                     # Motor de Skills
│   ├── SkillHubManager.cs      # Instalación/actualización remota
│   ├── SkillMdParser.cs        # Parser de archivos .md de skill
│   ├── SkillLoader.cs          # Carga skills activas desde disco
│   ├── SkillRunnerHelper.cs    # Genera skill_runner.py dinámico
│   ├── HerramientaSkill.cs     # Skill como herramienta (tool use)
│   └── SkillManifestBuilder.cs # Manifiesto dinámico para el LLM
│
├── skills_hub/                 # Librería de skills preinstaladas
│   ├── clima_ciudad.md
│   ├── precio_cripto.md
│   ├── calculadora.md
│   ├── generar_qr.md
│   └── [15+ skills más]
│
├── Herramientas/               # Sistema de herramientas (function calling)
│   ├── IHerramienta.cs
│   ├── MotorHerramientas.cs
│   ├── RegistroHerramientas.cs
│   └── [HTTP, archivos, comandos, búsqueda]
│
├── Entidades/                  # Modelos de dominio
│   ├── Servicios.cs            # Enum de proveedores LLM
│   ├── Skill.cs                # Entidad skill con metadata Hub
│   ├── Automatizacion.cs       # Definición de automatización
│   ├── NodoAutomatizacion.cs   # Nodo (Trigger/Condición/Acción/Fin)
│   └── [Modelo, Api, ChatMessage, ConsumoTokens…]
│
├── Vistas/                     # UI Windows Forms
│   ├── FrmPrincipal.cs         # Ventana principal
│   ├── FrmMandos.cs            # Chat con burbujas y display ARIA
│   ├── FrmAutomatizaciones.cs  # Editor visual de nodos
│   ├── FrmApis.cs              # Gestión de API keys
│   ├── FrmModelos.cs           # Selección de modelos
│   └── FrmPipelineAgente.cs    # Pipeline multi-agente UI
│
├── ServiciosTelegram/          # Bot Telegram
├── ServiciosSlack/             # Bot Slack
├── Utilerias/                  # RutasProyecto, MarkdownFileManager…
└── Themas/                     # EmeraldTheme, BurbujaChat…
```

---

## Pipeline ARIA

ARIA (Analista · Reconstructor · Inteligencia · Agente) es el corazón de OPENGIOAI. Cada instrucción del usuario pasa por cuatro fases ejecutadas en secuencia, con autocorrección automática integrada.

### Diagrama de flujo

```
Instrucción del usuario
        │
        ▼
┌───────────────┐
│   ANALISTA    │  Interpreta la instrucción. Genera un plan amigable
│               │  y lo muestra al usuario en tiempo real.
└───────┬───────┘
        │
        ▼
┌───────────────┐
│  CONSTRUCTOR  │  Llama a AIModelConector para que el LLM genere
│               │  un script Python. Lo guarda y lo ejecuta.
│               │  La salida queda en respuesta.txt
└───────┬───────┘
        │
        ▼
┌───────────────────────┐
│  ANALIZADOR RÁPIDO   │  Lee respuesta.txt. Pregunta al LLM:
│  (0.5 s aprox.)       │  "¿La tarea está completa?"
└──────┬───────┬────────┘
       │ SÍ   │ NO
       │       ▼
       │  ┌───────────────┐
       │  │   GUARDIÁN    │  Lee el código generado + la salida
       │  │  (0–3 reintentos) con errores. Pide al LLM una corrección.
       │  │               │  Re-ejecuta el Constructor con el fix.
       │  └───────┬───────┘
       │          │ éxito / max reintentos
       ▼          ▼
┌───────────────┐
│ COMUNICADOR   │  Convierte el resultado técnico a lenguaje natural.
│               │  Hace streaming token-a-token al usuario.
└───────────────┘
        │
        ▼
  Respuesta final + log en ARIALog.md
```

### Fases en detalle

**Analista**
Recibe la instrucción completa. Usa el LLM para generar una explicación en lenguaje natural de lo que se va a hacer. El texto se muestra inmediatamente en la UI mientras el Constructor trabaja en paralelo.

**Constructor**
Invoca `AIModelConector.EjecutarInstruccionIAAsync()`. El LLM genera el script Python necesario, se guarda como `script_ia.py` en el directorio de trabajo y se ejecuta en un proceso aislado. `stdout` y `stderr` se capturan y se guardan en `respuesta.txt`.

**Guardián (agente de corrección)**
Si el Analizador Rápido detecta que la respuesta tiene errores o está incompleta, el Guardián entra en acción. Lee el script generado y la salida con errores y construye un prompt de corrección contextualizado. Reintenta el ciclo Constructor hasta 3 veces con contexto acumulado. Cada reintento dispara el evento `OnReintentoGuardian` para que la UI lo muestre al usuario.

**Comunicador**
Toma el resultado final (sea éxito o el mejor intento del Guardián) y lo transforma en una respuesta conversacional, amigable y sin jerga técnica. Hace streaming token-a-token vía callback `OnToken` para que la UI renderice en tiempo real.

### Eventos de la UI

```csharp
orquestador.OnFaseIniciada     += (fase) => MostrarFaseEnPanel(fase);
orquestador.OnToken            += (tok)  => AgregarTokenAlChat(tok);
orquestador.OnFaseCompletada   += (fase) => MarcarFaseCompletada(fase);
orquestador.OnReintentoGuardian+= (n)    => MostrarReintentoN(n);
orquestador.OnInicioScript     += ()     => MostrarIconoEjecucion();
orquestador.OnLineaScript      += (line) => AgregarLineaConsola(line);
```

---

## Sistema de Skills y Skills Hub

Las Skills son capacidades Python empaquetadas en archivos `.md` que el agente puede invocar como herramientas. El sistema tiene dos partes: el **Skills Hub** (repositorio remoto) y el **motor local** que las carga, parsea y ejecuta.

### Formato de una Skill (.md)

```markdown
---
id: clima_ciudad
nombre: Clima de una Ciudad
categoria: web
descripcion: Obtiene el clima actual de cualquier ciudad
activa: true
autor: opengio
version: 1.0.0
ejemplo: skill_run("clima_ciudad", ciudad="Madrid")
source_url: https://hub.opengioai.com/skills/clima_ciudad.md
---

## Descripcion
Consulta una API meteorológica y devuelve temperatura,
humedad y condición del tiempo para la ciudad solicitada.

## Codigo
```python
import json, os, requests

params = json.loads(os.environ.get("SKILL_PARAMS", "{}"))
ciudad = params.get("ciudad", "Madrid")

resp = requests.get(f"https://wttr.in/{ciudad}?format=j1")
data = resp.json()
temp = data["current_condition"][0]["temp_C"]
desc = data["current_condition"][0]["weatherDesc"][0]["value"]

print(json.dumps({"ciudad": ciudad, "temperatura_C": temp, "condicion": desc}))
```

## Parametros
- nombre: ciudad | tipo: string | requerido: false | default: Madrid
```

### Ciclo de vida de una Skill

```
1. DISCOVERY
   SkillLoader.CargarActivas()
   └─ Lee todos los archivos Skills/*.md del directorio de trabajo

2. PARSE
   SkillMdParser.Parsear(contenido)
   └─ Extrae: id, nombre, categoría, código Python, parámetros, metadata Hub

3. GENERACIÓN DEL RUNNER
   SkillRunnerHelper.GenerarAsync(skills)
   └─ Crea skill_runner.py dinámico con dispatch tabla
   └─ El LLM puede llamar: skill_run("id", param1="val1")
   └─ Parámetros pasados via variable de entorno SKILL_PARAMS (JSON)

4. MANIFIESTO PARA EL LLM
   SkillManifestBuilder.Construir(skills)
   └─ Genera texto que se inyecta en el PromptEfectivo
   └─ El LLM "sabe" qué skills hay y cómo llamarlas

5. EJECUCIÓN
   HerramientaSkill → MotorHerramientas.Ejecutar()
   └─ Proceso Python aislado, SKILL_PARAMS=JSON, captura stdout
```

### Skills Hub — instalación remota

El `SkillHubManager` permite instalar y actualizar skills desde una URL remota:

```csharp
// Instalar desde URL
var skill = await SkillHubManager.InstalarDesdeUrlAsync(
    url: "https://hub.opengioai.com/skills/generar_qr.md",
    rutaBase: rutaDirectorioTrabajo,
    ct: cancellationToken
);

// Actualizar a la última versión
await SkillHubManager.ActualizarAsync(skill, rutaBase, ct);

// Exportar para compartir
string mdListo = SkillHubManager.GenerarMdParaExportar(skill);
```

El campo `source_url` en el frontmatter de la skill registra de dónde se instaló, permitiendo actualizaciones automáticas.

### Skills incluidas por defecto

| ID | Descripción |
|----|-------------|
| `clima_ciudad` | Clima actual de cualquier ciudad |
| `precio_cripto` | Precio en tiempo real de criptomonedas |
| `calculadora` | Operaciones matemáticas |
| `generar_qr` | Genera imágenes QR |
| `procesos_activos` | Lista procesos del sistema |
| `ip_publica` | Obtiene la IP pública |
| `convertir_csv_json` | Conversión CSV ↔ JSON |
| + 11 más | Incluidas en `/skills_hub/` |

### Creador de Skills (agente)

Desde la UI se puede describir una skill en lenguaje natural. El agente genera automáticamente el archivo `.md` completo con código Python, parámetros y metadata, listo para activar.

---

## Automatizaciones con Nodos

El editor visual de automatizaciones (`FrmAutomatizaciones.cs`) permite construir flujos de trabajo complejos conectando nodos en un canvas. Cada nodo representa una instrucción en lenguaje natural que el agente convierte en un script Python.

### Tipos de nodo

| Tipo | Función |
|------|---------|
| `Disparador` | Punto de entrada. Inicia la cadena cuando se activa. |
| `Condicion` | Evalúa una expresión. Ramifica el flujo (Sí / No). |
| `Accion` | Ejecuta una instrucción. Puede usar el resultado del nodo anterior. |
| `Fin` | Termina la cadena y consolida el resultado. |

### Modelo de datos de un nodo

```csharp
public class NodoAutomatizacion
{
    public string Id { get; set; }                           // GUID único
    public TipoNodo TipoNodo { get; set; }                   // Tipo
    public string InstruccionNatural { get; set; }           // Lo que escribe el usuario
    public string ScriptGenerado { get; set; }               // Python generado por el LLM
    public string UltimaRespuesta { get; set; }              // Última salida de ejecución
    public string NombreScript { get; set; }                 // nodo_01_captura.py
    public Dictionary<string, string> Parametros { get; set; }
    public int OrdenEjecucion { get; set; }
    public List<string> ConexionesSalida { get; set; }       // IDs de nodos destino
    public EstadoNodo Estado { get; set; }                   // Pendiente/Ejecutando/OK/Error
    public int CanvasX, CanvasY { get; set; }                // Posición en el canvas
}
```

### Flujo de ejecución de una automatización

```
Usuario activa la automatización
        │
        ▼
[Disparador] — evalúa condición de arranque
        │
        ▼
[Accion 1] — InstruccionNatural → LLM → nodo_01_*.py → ejecuta
        │  resultado disponible en contexto del siguiente nodo
        ▼
[Condicion] — evalúa resultado de Accion 1
        ├─ SÍ ─▶ [Accion 2]
        └─ NO ─▶ [Accion 3]
        │
        ▼
[Fin] — consolida y devuelve respuesta final
```

### Almacenamiento en disco

```
ListAutomatizaciones.json              ← definición de todas las automatizaciones
Automatizaciones/
└── {GUID}/
    ├── nodo_01_captura.py
    ├── nodo_02_procesa.py
    ├── nodo_03_envia.py
    └── messages.json                  ← historial de ejecución de esta automatización
```

### RAG y contexto entre nodos

Cada nodo tiene acceso al historial completo de ejecución de la cadena a través de `ConversationWindow`, que mantiene una ventana deslizante de hasta 6 turnos y 3000 tokens. El agente de cada nodo recibe:

- El resultado del nodo anterior
- El resumen del plan completo (Analista)
- Las skills disponibles para ese contexto
- Las credenciales permitidas (ver sección siguiente)

Esto permite que instrucciones como *"filtra los datos que obtuviste en el paso anterior"* funcionen sin tener que repetir información.

---

## Sistema de Credenciales Seguras

OPENGIOAI gestiona las API keys de forma centralizada. Los agentes **no tienen acceso directo** a las claves; en cambio, el sistema inyecta una sección de credenciales en el contexto del agente de forma controlada.

### Cómo funciona

**1. Almacenamiento**

Las API keys se guardan en `ListApis.json`, en el directorio del binario:

```json
[
  {
    "Id": "openai",
    "Nombre": "OpenAI",
    "ApiKey": "sk-proj-...",
    "ModeloPorDefecto": "gpt-4"
  },
  {
    "Id": "openweather",
    "Nombre": "OpenWeather",
    "ApiKey": "abc123...",
    "ModeloPorDefecto": ""
  }
]
```

**2. Inyección en el contexto del agente**

`AgentContext.BuildAsync()` lee `ListApis.json` y construye la sección `ClavesDisponibles`, que se inserta en el `PromptEfectivo` enviado al LLM:

```
================= SISTEMA DE CREDENCIALES =================
RUTA DEL JSON: C:\...\ListApis.json
NOMBRES DISPONIBLES:
openai, openweather, telegram, slack, gemini, deepseek

REGLAS PARA EL AGENTE:
* Solo usa una credencial si es estrictamente necesaria para la tarea.
* Para obtener una clave: lee ListApis.json, busca por el campo "Id",
  extrae el campo "ApiKey".
* Nunca inventes claves. Si no existe la credencial solicitada → informa al usuario.
* Nunca imprimas ni expongas las claves en la respuesta al usuario.
===========================================================
```

**3. Qué ve el agente**

El agente LLM **solo ve los nombres** de las credenciales disponibles, no los valores. Cuando necesita una clave para su script Python, genera código que lee `ListApis.json` en tiempo de ejecución dentro del proceso aislado.

```python
# Ejemplo de código generado por el agente
import json

with open(r"C:\...\ListApis.json") as f:
    apis = json.load(f)

api_key = next(a["ApiKey"] for a in apis if a["Id"] == "openweather")
# Usa api_key en la petición HTTP
```

**4. Aislamiento del proceso**

Los scripts Python se ejecutan en un proceso separado (`Process.Start`). El proceso:
- No tiene acceso a variables de entorno del proceso padre
- Solo puede leer los archivos a los que el usuario ha dado acceso mediante la ruta de trabajo configurada
- `stdout` y `stderr` se capturan completamente antes de devolverlos a la UI

### Recomendaciones de seguridad

| Escenario | Recomendación |
|-----------|---------------|
| Desarrollo local | `ListApis.json` en el directorio de compilación (no subir a git) |
| CI/CD | Usar variables de entorno; el archivo se genera en tiempo de build |
| Producción | Implementar cifrado AES-256 sobre `ListApis.json` (mejora pendiente) |
| Auditoría | Revisar `ARIALog.md` — registra cada instrucción ejecutada con timestamp |
| Scripts en producción | Revisar `script_ia.py` antes de ejecutar en sistemas críticos |

**Importante:** Agregar `ListApis.json` al `.gitignore` para no subir credenciales al repositorio.

```gitignore
# Credenciales — nunca subir al repositorio
ListApis.json
Configuracion.json
*.env
```

---

## Multi-Proveedor de LLMs

| Proveedor | Modelos | Autenticación |
|-----------|---------|---------------|
| **OpenAI** | GPT-3.5 Turbo, GPT-4, GPT-4 Turbo | API Key (Bearer) |
| **Anthropic Claude** | Claude 3 Haiku / Sonnet / Opus | API Key (x-api-key) |
| **Google Gemini** | Gemini 1.5 Flash, Gemini Pro | API Key (querystring) |
| **DeepSeek** | DeepSeek Chat, DeepSeek Code | API Key (Bearer) |
| **OpenRouter** | Todos los modelos disponibles | API Key (Bearer) |
| **Ollama** | Llama, Mistral, Phi, Gemma… | Sin clave (local) |
| **Google Vertex AI** | Gemini en Vertex | gcloud ADC (`Antigravity`) |

### Resiliencia — RetryPolicy

Todos los proveedores externos pasan por `RetryPolicy.EjecutarAsync()`:

```
Intento 1  →  falla  →  espera 1s ± jitter
Intento 2  →  falla  →  espera 2s ± jitter
Intento 3  →  falla  →  espera 4s ± jitter
Intento 4  →  falla  →  lanza excepción al caller
```

Errores que activan reintento: `429 Rate Limit`, `5xx Server Error`, `HttpRequestException`, `TaskCanceledException` (timeout interno — no cancelación del usuario).

Errores que **no** reintentan: `400 Bad Request`, `401 Unauthorized`, `OperationCanceledException` (usuario presionó Stop).

### Streaming en tiempo real

Los providers que soportan SSE/streaming (`OpenAI`, `Claude`, `DeepSeek`, `Ollama`) envían tokens vía callback:

```csharp
await AIModelConector.ObtenerRespuestaStreamingAsync(
    instruccion: "Explica la relatividad",
    ctx: contexto,
    onToken: token => txtChat.AppendText(token),  // UI actualiza en tiempo real
    ct: cancellationToken
);
```

---

## Integracion Multi-Canal

### Telegram

- `TelegramPollingService` sondea la API de Telegram cada N segundos
- `TelegramListener` procesa mensajes entrantes y los envía a `AIModelConector`
- `TelegramSender` responde al usuario con el resultado del agente
- Token configurado en `Configuracion.json` → campo `telegramToken`

### Slack

- `SlackPollingService` sondea el workspace
- `SlackListener` despacha mensajes al motor de agentes
- `SlackService` gestiona operaciones de la API Slack
- OAuth token en `Configuracion.json` → campo `slackToken`

### UI Desktop

- `FrmMandos` — chat principal con burbujas, panel de fases ARIA, consola de scripts
- `FrmAutomatizaciones` — editor de nodos en canvas con conexiones visuales
- `FrmApis` — gestión de API keys por proveedor
- `FrmModelos` — selección de modelo por proveedor
- `FrmPipelineAgente` — ejecución del pipeline multi-agente con resultados por fase

---

## Como Empezar

### Requisitos

- **.NET 9 Runtime** — [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Windows 10+**
- **Python 3.8+** en el PATH del sistema
- **API Key** de al menos un proveedor LLM

### Instalación

```bash
git clone https://github.com/Gio-progrma7/OPENGIOAI.git
cd OPENGIOAI
dotnet restore
dotnet build
dotnet run --project OPENGIOAI
```

### Configuración inicial

1. Abrir la aplicación → `FrmPrincipal`
2. Ir a **APIs** → Agregar la API key de cada proveedor
3. Ir a **Modelos** → Seleccionar el modelo por defecto
4. Ir a **Rutas** → Establecer el directorio de trabajo donde se guardarán scripts y logs
5. (Opcional) Ir a **Skills** → Activar o instalar skills desde el Hub

### Archivos de configuración en tiempo de ejecución

```
[DirectorioTrabajo]/
├── promtMaestro.md           ← Prompt maestro del agente (editable)
├── promtAgente.md            ← Prompt de manejo de errores
├── respuesta.txt             ← Salida del último script (usada por Guardián)
├── ARIALog.md                ← Log de todas las ejecuciones ARIA
├── script_ia.py              ← Último script generado
├── skill_runner.py           ← Runner dinámico de skills (autogenerado)
├── Skills/
│   ├── clima_ciudad.md       ← Definición de skill
│   └── clima_ciudad.py       ← Python extraído (autogenerado)
└── Automatizaciones/
    └── {GUID}/
        ├── nodo_01_*.py
        └── messages.json

[DirectorioBinario]/
├── ListApis.json             ← API keys (NO subir a git)
├── Configuracion.json        ← Configuración general
├── ListModelos.json          ← Modelos por proveedor
└── ListAutomatizaciones.json ← Automatizaciones guardadas
```

---

## Extension y Desarrollo

### Agregar un nuevo proveedor LLM

1. Añadir valor al enum `Servicios.cs`
2. Extender `ConstruirRequest()` en `AIModelConector.cs` con endpoint y body
3. Extender `AgregarHeaders()` con la autenticación del provider
4. Extender `ExtraerContenido()` con el path JSON de la respuesta
5. (Opcional) Extender `ObtenerRespuestaStreamingAsync()` si soporta SSE

### Crear una Skill personalizada

Crear un archivo `.md` en el directorio `Skills/` del directorio de trabajo siguiendo el formato de skill (ver [Sistema de Skills](#sistema-de-skills-y-skills-hub)). Al reiniciar o recargar skills, el agente la detecta automáticamente y la incluye en su manifiesto.

### Agregar un nuevo tipo de nodo de automatización

1. Añadir valor al enum `TipoNodo` en `NodoAutomatizacion.cs`
2. Implementar la lógica de evaluación en `FrmAutomatizaciones.cs`
3. Agregar el ícono y propiedades en el panel de configuración del nodo

---

## Troubleshooting

| Problema | Causa probable | Solución |
|----------|---------------|----------|
| Script Python no ejecuta | Python no está en PATH | Agregar Python al PATH del sistema o especificar ruta absoluta en Rutas |
| Error 429 frecuente | Rate limit del proveedor | RetryPolicy lo maneja; considerar modelo más económico o reducir frecuencia |
| Guardián en bucle | Tarea imposible para el modelo | Revisar `ARIALog.md`; simplificar instrucción o cambiar a modelo más capaz |
| Skill no aparece en el agente | `activa: false` en el .md | Editar el .md y cambiar a `activa: true`; recargar skills |
| Credencial no encontrada | Id incorrecto en ListApis.json | Verificar que el campo `Id` en JSON coincide exactamente con el nombre que pide el agente |
| Telegram no responde | Token inválido o bot no iniciado | Verificar token en `Configuracion.json`; comprobar que el bot está activo en BotFather |
| Contexto perdido entre turnos | Ventana de contexto llena | Aumentar `MaxTurnosContexto` o `MaxTokensContexto` en la configuración |
| Socket exhaustion | HttpClient mal instanciado | Ya resuelto — AIModelConector usa singleton por proveedor |

---

## Dependencias

```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
<PackageReference Include="SlackAPI" Version="1.1.14" />
<PackageReference Include="System.Management" Version="10.0.3" />
<PackageReference Include="Telegram.Bot" Version="22.9.0" />
```

---

## Mejoras Futuras

- [ ] Cifrado AES-256 para `ListApis.json`
- [ ] Historial persistente de conversaciones con búsqueda
- [ ] Dashboard de analytics de tokens y coste por proveedor
- [ ] Integración con Discord
- [ ] Fine-tuning de modelos locales (Ollama)
- [ ] Caché inteligente de respuestas para reducir coste
- [ ] Exportación/importación de automatizaciones en formato portátil
- [ ] Skills Hub con búsqueda y categorías en la UI

---

## Autor

**Giovanni Sanchez** — [GitHub @Gio-progrma7](https://github.com/Gio-progrma7)

---

## Licencia

MIT — ver archivo `LICENSE` para detalles.

---

## Contribuciones

1. Fork el repositorio
2. Crea una rama: `git checkout -b feature/mi-feature`
3. Commit: `git commit -m 'feat: descripción del cambio'`
4. Push: `git push origin feature/mi-feature`
5. Abre un Pull Request

---

## Disclaimer

OPENGIOAI ejecuta código Python generado dinámicamente por LLMs. El proceso de ejecución está aislado, pero siempre es recomendable:

- Revisar `script_ia.py` antes de ejecutar en sistemas críticos
- No ejecutar scripts en entornos de producción sin validación previa
- Mantener `ListApis.json` fuera del control de versiones

---

**Hecho en C# y .NET 9**
