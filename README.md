# OPENGIOAI

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![.NET 10](https://img.shields.io/badge/.NET%2010-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Windows Forms](https://img.shields.io/badge/Windows%20Forms-0078D4?style=for-the-badge&logo=windows&logoColor=white)
![Python](https://img.shields.io/badge/Python%203.8+-3776AB?style=for-the-badge&logo=python&logoColor=white)
![RAG](https://img.shields.io/badge/RAG-Semántico-10b981?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-blue)

> **Plataforma de escritorio de IA de clase producción** — orquestación multiagente ARIA, memoria semántica con RAG, automatizaciones visuales DAG, Skills ejecutables, integraciones Telegram/Slack/TTS, telemetría de tokens y tema Emerald dinámico.

**OPENGIOAI** es una plataforma desktop desarrollada en C# con .NET 10 que orquesta agentes de inteligencia artificial especializados, ejecuta código Python generado dinámicamente, gestiona un sistema de Skills extensible, permite crear flujos de automatización visual con nodos, y **optimiza el consumo de tokens** mediante telemetría granular, context slicing declarativo y recuperación semántica (RAG). Compatible con OpenAI, Anthropic Claude, Google Gemini, DeepSeek, Ollama y Google Vertex AI.

---

## Tabla de Contenidos

1. [Arquitectura General](#arquitectura-general)
2. [Pipeline ARIA — El Motor de Agentes](#pipeline-aria)
3. [Memoria Durable del Agente](#memoria-durable-del-agente)
4. [Token-Saving Architecture](#token-saving-architecture)
   - [Fase A — Telemetría de Tokens](#fase-a--telemetría-de-tokens)
   - [Fase B — Context Slicing](#fase-b--context-slicing)
   - [Fase C — RAG Local (Memoria Semántica)](#fase-c--rag-local-memoria-semántica)
5. [Sistema de Skills y Skills Hub](#sistema-de-skills-y-skills-hub)
6. [Automatizaciones con Nodos](#automatizaciones-con-nodos)
7. [Sistema de Comandos `#cmd`](#sistema-de-comandos-cmd)
8. [Sistema de Credenciales](#sistema-de-credenciales)
9. [Multi-Proveedor de LLMs](#multi-proveedor-de-llms)
10. [Integración Multi-Canal](#integración-multi-canal)
11. [UI — Tema Emerald y Controles Personalizados](#ui--tema-emerald-y-controles-personalizados)
12. [Cómo Empezar](#cómo-empezar)
13. [Extensión y Desarrollo](#extensión-y-desarrollo)
14. [Troubleshooting](#troubleshooting)

---

## Arquitectura General

OPENGIOAI se organiza en capas bien definidas. El núcleo es `AIModelConector.cs`, el hub central que coordina todos los proveedores LLM, herramientas y políticas de reintento. Por encima vive el **OrquestadorARIA** (pipeline de 4 fases), y alrededor operan subsistemas independientes — Memoria, Skills, Embeddings, Telemetría, Comandos, TTS — que colaboran para que cada token enviado al LLM esté justificado.

```
┌─────────────────────────────────────────────────────────────────┐
│                       CAPA DE INTERFAZ                          │
│  FrmPrincipal (nav/sidebar)  •  FrmMandos (chat ARIA)           │
│  FrmAutomatizaciones (DAG)   •  Skills (hub local)              │
│  FrmMemoria  •  FrmHabilidades  •  FrmPatrones                  │
│  FrmEmbeddings  •  FrmConsumoTokens  •  FrmTraces               │
│  FrmApis  •  FrmModelos  •  FrmRutas  •  FrmComunicadores       │
│  Telegram Bot  •  Slack Bot                                     │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│              CAPA DE ORQUESTACIÓN — OrquestadorARIA             │
│  Analista → Constructor → Guardián → Comunicador                │
│  AgentContext (inmutable)  •  PerfilContexto (slicing)          │
│  PanelAgentes (eventos UI reactiva)                             │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│          SUBSISTEMAS TRANSVERSALES (context & savings)          │
│  ConsumoTokensTracker  •  PreciosModelos  •  TraceStorage       │
│  HabilidadesRegistry   •  MemoriaManager  •  MemoriaSemantica   │
│  VectorStore  •  EmbeddingsService  •  AudioTTSService          │
│  CommandRegistry  •  CommandExecutor  •  ResultFormatter        │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│              AIModelConector  (hub central)                     │
│  AgentContext.BuildAsync()  •  RetryPolicy  •  Streaming        │
│  Enrutamiento por proveedor  •  Ejecución Python                │
└──────────────┬──────────────────────────┬───────────────────────┘
               │                          │
       ┌───────▼───────┐          ┌───────▼─────────┐
       │  LLM EXTERNOS │          │ EJECUCIÓN LOCAL │
       │  OpenAI       │          │  Ollama         │
       │  Claude       │          │  Python proc    │
       │  Gemini       │          │  File I/O       │
       │  DeepSeek     │          │  Skills runner  │
       │  Vertex AI    │          │  Herramientas   │
       └───────────────┘          └─────────────────┘
```

### Estructura de carpetas

```
OPENGIOAI/
├── Agentes/                    # Motor de orquestación ARIA
│   └── OrquestadorARIA.cs      # Pipeline 4 fases + eventos UI
│
├── Comandos/                   # Sistema profesional de comandos #cmd
│   ├── CommandParser.cs        # Tokenizer shell-lite con comillas y legacy
│   ├── CommandRegistry.cs      # Registro + Levenshtein typo-suggest
│   ├── CommandExecutor.cs      # Despacho parse → resolve → execute
│   ├── ResultFormatter.cs      # Render por canal (Telegram/Slack/UI)
│   ├── IServiciosComandos.cs   # Fachada UI-thread safe
│   └── Handlers/               # 19 comandos visibles + 2 legacy ocultos
│
├── Data/                       # Núcleo lógico
│   ├── AIModelConector.cs      # Hub central (providers + herramientas)
│   ├── AgentContext.cs         # Contexto inmutable (PromptEstable/Variable)
│   ├── RetryPolicy.cs          # Backoff exponencial con jitter
│   └── ConversationWindow.cs   # Ventana deslizante de historial
│
├── Entidades/                  # Modelos de dominio (POCOs)
│   ├── ConfiguracionClient.cs  # Raíz de configuración
│   ├── Skill.cs                # Skill con metadata Hub
│   ├── Automatizacion.cs       # Flujo DAG completo
│   ├── NodoAutomatizacion.cs   # Nodo (Disparador/Condición/Acción/Fin)
│   ├── PerfilContexto.cs       # Flags de context slicing (Fase B)
│   ├── ChunkMemoria.cs         # Chunk de memoria + embedding vector
│   ├── ConsumoTokens.cs        # Registro de consumo por llamada
│   ├── TraceEjecucion.cs       # Trace por fase (nombre, estado, duración)
│   └── [+20 entidades más]
│
├── Herramientas/               # System de herramientas (function calling)
│   ├── IHerramienta.cs
│   ├── MotorHerramientas.cs
│   ├── RegistroHerramientas.cs
│   └── [HTTP, archivos, comandos, búsqueda]
│
├── ServiciosAI/                # Servicios transversales de IA
│   ├── AIServicios.cs          # Listar modelos, validar keys, OAuth Vertex
│   ├── TokenUsageReader.cs     # Extractor multi-proveedor de usage
│   ├── EmbeddingsService.cs    # Embeddings (OpenAI/HuggingFace/Vertex)
│   └── HistorialResumidor.cs   # Compresión inteligente de historial
│
├── ServiciosTelegram/          # Bot Telegram (long-polling)
│   ├── TelegramService.cs
│   └── TelegramListener.cs
│
├── ServiciosSlack/             # Bot Slack (polling)
│   ├── SlackChannelService.cs
│   └── SlackPollingService.cs
│
├── ServiciosTTS/               # Síntesis de voz
│   └── AudioTTSService.cs      # Windows System.Speech + OpenAI TTS
│
├── Skills/                     # Motor de Skills
│   ├── SkillHubManager.cs      # Instalación/actualización remota
│   ├── SkillMdParser.cs        # Parser de archivos .md de skill
│   ├── SkillLoader.cs          # Carga skills activas desde disco
│   ├── SkillRunnerHelper.cs    # Genera skill_runner.py dinámico
│   ├── HerramientaSkill.cs     # Skill como herramienta (tool use)
│   └── SkillManifestBuilder.cs # Manifiesto dinámico para el LLM
│
├── Themas/                     # Sistema de temas + controles custom
│   ├── EmeraldTheme.cs         # Paleta oscuro/claro, ThemeChanged event
│   ├── BurbujaChat.cs          # Burbuja conversación con streaming GDI+
│   └── [controles custom GDI+]
│
├── Utilerias/                  # Subsistemas de soporte
│   ├── RutasProyecto.cs        # Rutas canónicas (AppDir / Workspace)
│   ├── JsonManager.cs          # Persistencia genérica List<T>
│   ├── HabilidadesRegistry.cs  # Singleton con caché en memoria
│   ├── PreciosModelos.cs       # Registry de tarifas LLM
│   ├── ConsumoTokensTracker.cs # Telemetría correlacionada (Fase A)
│   ├── TraceStorage.cs         # Persistencia de traces de ejecución
│   ├── MemoriaManager.cs       # Hechos.md / Episodios.md
│   ├── MemoriaChunker.cs       # Chunking + SHA1 IDs estables
│   ├── MemoriaIndexer.cs       # Indexación incremental (Fase C)
│   ├── MemoriaSemantica.cs     # API alto nivel RAG (Fase C)
│   └── VectorStore.cs          # JSONL vector store + cosine similarity
│
├── Vistas/                     # UI Windows Forms
│   ├── FrmPrincipal.cs         # Ventana principal, sidebar, nav LIFO
│   ├── FrmMandos.cs            # Chat multiagente + streaming + comandos
│   ├── FrmMandos.Comandos.cs   # Fachada IServiciosComandos (partial)
│   ├── FrmAutomatizaciones.cs  # Editor visual DAG + ejecución + logs
│   ├── Skills.cs               # Gestor de skills, test, manifiesto
│   ├── FrmMemoria.cs           # Editor Hechos.md / Episodios.md
│   ├── FrmHabilidades.cs       # Toggles de capacidades cognitivas
│   ├── FrmPatrones.cs          # Detección de patrones → Skills
│   ├── FrmEmbeddings.cs        # Config y operación RAG (Fase C)
│   ├── FrmConsumoTokens.cs     # Panel flotante de telemetría (Fase A)
│   ├── FrmTraces.cs            # Visualización de traces de ejecución
│   ├── FrmApis.cs              # CRUD de credenciales
│   ├── FrmModelos.cs           # Selección de modelos por proveedor
│   ├── FrmRutas.cs             # Gestión de workspace / ruta de trabajo
│   ├── FrmComunicadores.cs     # Config Telegram y Slack
│   ├── FrmPromts.cs            # Editor prompts maestros
│   ├── DialogoParametrosSkill.cs # UI modal parámetros de skill
│   └── SkillResultRenderer.cs  # Renderización resultados con highlighting
│
├── Tests/
│   ├── ComandosTests/          # 67 tests (Parser/Registry/Executor/Handlers)
│   └── ConversationWindowTests/
│
├── Program.cs                  # Entrada, DI, Serilog
└── OPENGIOAI.csproj            # net10.0-windows, nullable enabled
```

---

## Pipeline ARIA

ARIA (**A**nalista · **R**IA-Constructor · Guard**i**án · Comunic**a**dor) es el corazón de OPENGIOAI. Cada instrucción del usuario pasa por cuatro fases ejecutadas en secuencia, con autocorrección automática integrada, streaming de tokens en tiempo real y telemetría por fase.

### Diagrama de flujo

```
Instrucción del usuario
        │
        ▼
┌───────────────┐
│   ANALISTA    │  Interpreta la instrucción. Genera un plan amigable
│               │  y lo muestra al usuario en streaming.
│  Perfil:      │  [Mínimo] — prompt propio, sin disco
└───────┬───────┘
        │
        ▼
┌───────────────┐
│  CONSTRUCTOR  │  El LLM genera un script Python. Se guarda y ejecuta
│               │  en proceso aislado. stdout/stderr → respuesta.txt
│  Perfil:      │  [Completo] — skills + credenciales + memoria (o RAG)
└───────┬───────┘
        │
        ▼
┌──────────────────────┐
│  ANALIZADOR RÁPIDO   │  Lee respuesta.txt. Pregunta al LLM:
│  (~0.5 s)            │  "¿La tarea está completa?"
│  Perfil: [Mínimo]    │
└───┬──────────┬───────┘
  SÍ │          │ NO
     │          ▼
     │   ┌───────────────┐
     │   │   GUARDIÁN    │  Lee código generado + salida con errores.
     │   │ (0–3 intentos)│  Construye prompt de corrección contextualizado.
     │   │  Perfil:      │  Re-ejecuta el Constructor con el fix.
     │   │  [Mínimo]     │  Dispara OnReintentoGuardian para la UI.
     │   └───────┬───────┘
     │           │ éxito / max reintentos alcanzado
     ▼           ▼
┌───────────────┐
│ COMUNICADOR   │  Convierte el resultado técnico a lenguaje natural.
│               │  Hace streaming token-a-token vía callback OnToken.
│  Perfil:      │  [Comunicador] — identidad + memoria, sin skills
└───────────────┘
        │
        ▼
  Respuesta visible al usuario — Trace guardado en TraceStorage
```

### Fases en detalle

**Analista** — Recibe la instrucción completa. Usa el LLM con `PerfilContexto.Minimo` para no cargar skills ni memoria (no los necesita), genera una explicación en lenguaje natural del plan y la muestra en streaming en la UI mientras el Constructor trabaja.

**Constructor** — Invoca `AIModelConector.EjecutarInstruccionIAAsync()`. Es la fase con contexto más completo (`PerfilContexto.Completo`): prompt maestro + skills + credenciales + memoria (completa o RAG top-K según habilidades activas). El LLM genera el script Python, se guarda y se ejecuta en proceso aislado. `stdout` y `stderr` van a `respuesta.txt`. La salida se emite línea a línea vía `OnLineaScript`.

**Guardián** — Si el Analizador Rápido detecta error o tarea incompleta, el Guardián entra en acción. Lee el script generado + la salida con errores y construye un prompt de corrección contextualizado. Reintenta el ciclo Constructor hasta 3 veces (configurable con `#reintentos`). Cada intento dispara el evento `OnReintentoGuardian` para que la UI lo informe al usuario.

**Comunicador** — Toma el resultado final (éxito o mejor intento del Guardián) y lo transforma en respuesta conversacional sin jerga técnica. Hace streaming token-a-token con `PerfilContexto.Comunicador` — lleva identidad + memoria para personalizar, pero nunca expone credenciales ni skills, lo que reduce la latencia del primer token.

### Eventos de la UI

```csharp
orquestador.OnFaseIniciada      += (fase, msg) => MostrarBurbujaFase(fase, msg);
orquestador.OnToken             += (fase, tok)  => AgregarTokenAlChat(tok);
orquestador.OnFaseCompletada    += (fase, ok)   => MarcarFaseCompletada(fase);
orquestador.OnReintentoGuardian += (n, max, r)  => MostrarReintentoN(n);
orquestador.OnInicioScript      += (fase)        => IniciarBurbujaScriptFase(fase);
orquestador.OnLineaScript       += (fase, line)  => AgregarLineaConsola(line);
```

`FrmMandos` suscribe cada evento para actualizar `BurbujaChat` y `PanelAgentes` en tiempo real. El throttle de streaming agrupa los updates a ~8 fps para eliminar parpadeos.

---

## Memoria Durable del Agente

La memoria vive en **archivos Markdown editables a mano**, asociados al directorio de trabajo activo. Cero BD, cero servicios externos — la memoria "viaja" con el workspace y se versiona en git si el usuario lo decide.

### Dos fuentes de memoria

| Archivo | Propósito | Estructura |
|---------|-----------|------------|
| `Memoria/Hechos.md` | Verdades durables sobre el usuario y su entorno. | Lista de bullets — un hecho por línea. |
| `Memoria/Episodios.md` | Timeline append-only de ejecuciones relevantes. | Bloques con timestamp ISO + descripción. |

### Ciclo completo

```
INICIO DE PIPELINE                       FIN DE PIPELINE
AgentContext.BuildAsync()                [Memorista — si implementado]
  │                                            │
  ├─ Lee Hechos.md + Episodios.md              ├─ Lee conversación completa
  ├─ [con RAG ON] → embed instrucción          ├─ Extrae hechos nuevos
  │   → busca top-K chunks similares           └─ Escribe bullets/episodios
  └─ Formatea sección del prompt

                     ▲
                     │ Edición directa desde FrmMemoria
                     │ (autosave, el usuario corrige a mano)
                     ▼
           Memoria/Hechos.md   ← fuente de verdad editable
           Memoria/Episodios.md
```

### Presupuesto de tokens

`MemoriaManager.FormatearParaPromptAsync()` recorta la memoria al presupuesto configurado (~800 tokens / 3200 caracteres). Con **Fase C** activa, este dump completo se reemplaza por recuperación semántica top-K.

### Patrones → Skills

`FrmPatrones` analiza `Episodios.md` en busca de tareas recurrentes (≥3 ocurrencias similares) y propone convertirlas en **Skills ejecutables**. El análisis solo se dispara cuando el usuario entra al módulo — nunca automáticamente — para que el coste sea visible y controlado.

---

## Token-Saving Architecture

OPENGIOAI ataca el coste del agente por tres frentes complementarios e independientes. Objetivo: que cada token enviado al LLM esté justificado.

```
┌──────────────────────────────────────────────────────────────────┐
│                  PROBLEMA: un agente ingenuo                     │
│  — manda prompt maestro enorme en cada fase                      │
│  — inyecta memoria completa aunque sea irrelevante               │
│  — inyecta TODOS los skills aunque solo use uno                  │
│  — el usuario no sabe cuánto gasta ni en qué                     │
└──────────────────────────────────────────────────────────────────┘
                               ▼
┌──────────────────────────────────────────────────────────────────┐
│   FASE A — TELEMETRÍA       │  Ver cuánto gasta por fase y por  │
│   📊 Tokens (flotante)       │  instrucción. Costo USD en vivo.   │
├──────────────────────────────────────────────────────────────────┤
│   FASE B — CONTEXT SLICING  │  Cada fase declara qué secciones  │
│   PerfilContexto             │  necesita. El resto NO se lee NI   │
│                              │  se envía al LLM.                  │
├──────────────────────────────────────────────────────────────────┤
│   FASE C — RAG LOCAL        │  Memoria recuperada por similitud  │
│   🧬 Embeddings              │  semántica (top-K) en lugar del    │
│                              │  dump completo.                    │
└──────────────────────────────────────────────────────────────────┘
                               ▼
              Ahorro típico combinado: 40–70 % de tokens
              Latencia del primer token del Comunicador −15–30 %
```

---

### Fase A — Telemetría de Tokens

Panel flotante `📊 Tokens` (always-on-top, arrastrable desde `FrmConsumoTokens`) que muestra el consumo de **cada llamada** al LLM con desglose por fase y **costo estimado en USD**.

```
📊 Consumo de Tokens — EN VIVO
┌──────────────────────────────────────────────────────────┐
│ Instrucción: "dame el clima de Madrid y envíamelo a TG"  │
├──────────────────────────────────────────────────────────┤
│ Analista     │  1 204 in  │    318 out  │  $0.0013  ✓    │
│ Constructor  │  4 872 in  │  1 103 out  │  $0.0239  ✓    │
│ Guardián     │      0     │      0     │   –       —    │
│ Comunicador  │  2 108 in  │    412 out  │  $0.0073  ✓    │
├──────────────────────────────────────────────────────────┤
│ TOTAL        │  8 184 in  │  1 833 out  │  $0.0325       │
└──────────────────────────────────────────────────────────┘
```

Cada llamada al LLM se etiqueta automáticamente vía `AgentContext.ComoFase(nombre)`. Soporta **todos los proveedores**: OpenAI/Claude/Gemini/DeepSeek parsean el campo `usage` del JSON; Ollama parsea `prompt_eval_count`/`eval_count` del stream NDJSON. Las tarifas USD se configuran en `ListPreciosModelos.json` con valores por defecto embebidos en `PreciosModelos.Defaults()`.

---

### Fase B — Context Slicing

Cada fase del pipeline declara **qué secciones del prompt necesita** mediante un `PerfilContexto`. Las secciones no declaradas ni se leen del disco ni se envían al LLM.

#### Flags de PerfilContexto

```csharp
public sealed class PerfilContexto
{
    // Grupo 1: I/O de disco — si está en false, ni se lee el archivo
    public bool LeerPromptMaestroDeDisco { get; init; }
    public bool LeerSkillsDeDisco         { get; init; }
    public bool LeerMemoriaDeDisco        { get; init; }

    // Grupo 2: qué secciones ensambla ConstruirPromptEfectivo()
    public bool IncluirPromptMaestro      { get; init; }
    public bool IncluirCredenciales       { get; init; }
    public bool IncluirRutaTrabajo        { get; init; }
    public bool IncluirSkills             { get; init; }
    public bool IncluirAutomatizaciones   { get; init; }
    public bool IncluirHistorial          { get; init; }
    public bool IncluirMemoria            { get; init; }
    public bool IncluirUsuario            { get; init; }
}
```

#### Presets disponibles

| Preset | Para quién | Qué incluye |
|--------|-----------|------------|
| `Completo` | Constructor | Todo — prompt maestro + skills + credenciales + memoria + automatizaciones |
| `Minimo` | Analista, Guardián, Analizador Rápido | Nada — recibe prompt propio vía `ConPromptPersonalizado` |
| `Memorista` | Memorista async | Prompt maestro + ruta + usuario (sin credenciales, sin skills) |
| `Comunicador` | Comunicador streaming | Prompt maestro + memoria + usuario (sin credenciales, sin skills) |
| `SoloIdentidad` | Inicio / saludo | Prompt maestro + usuario |

#### Ahorro medido

- **1 700 – 10 500 tokens** ahorrados por instrucción dependiendo del tamaño del prompt maestro y la memoria.
- **3–4 lecturas de disco evitadas** por ejecución.
- **Latencia del primer token del Comunicador** reducida ~15–30 % al no procesar secciones irrelevantes.

---

### Fase C — RAG Local (Memoria Semántica)

Cuando la memoria crece (cientos de hechos, meses de episodios), inyectarla completa se vuelve costoso y el 90 % es irrelevante para la instrucción actual. La **Fase C** sustituye el dump completo por **recuperación semántica top-K**: embebe la instrucción del usuario, busca los chunks más similares de Hechos/Episodios y solo inyecta esos.

#### Arquitectura

```
EmbeddingConfig  ({AppDir}/EmbeddingsConfig.json)
  │  Proveedor: OpenAI | HuggingFace | Vertex AI
  │  Modelo, endpoint, API key, TopK, ChunkSize, ChunkOverlap
  ▼
EmbeddingsService
  │  EmbedAsync(texto) → float[]
  │  EmbedManyAsync(textos) → batch, HttpClient singleton
  ▼
MemoriaChunker + MemoriaIndexer
  │  SHA1 por fuente → skip si sin cambios
  │  Batch embed + Upsert → idempotente
  ▼
VectorStore  ({ruta}/Memoria/embeddings.jsonl)
  │  Brute-force cosine normalizado [0,1]
  │  2–3k chunks → < 50 ms  •  Thread-safe (lock)
  ▼
MemoriaSemantica.ObtenerContextoRelevanteAsync()
  │  Gate: HAB_MEMORIA ∧ HAB_MEMORIA_SEMANTICA
  │  Si falla → "" → fallback a dump completo
  └─ Tolerante a fallos: provider caído no rompe el pipeline
```

#### Proveedores de embeddings soportados

| Proveedor | Modelos | Dimensión | Costo |
|-----------|---------|-----------|-------|
| **OpenAI** | `text-embedding-3-small` | 1 536 | $0.02 / 1M tok |
| **OpenAI** | `text-embedding-3-large` | 3 072 | $0.13 / 1M tok |
| **HuggingFace** | Modelos locales / API | Variable | Gratuito (local) |
| **Google Vertex AI** | `textembedding-gecko` | 768 | Según plan GCP |

Cambiar de proveedor/modelo **invalida el índice** (espacios vectoriales incompatibles). El `MemoriaIndexer` lo detecta por `ManifestEmbeddings` y hace rebuild automático.

#### Ahorro medido

- Memoria de 50 hechos + 200 episodios: dump ≈ 4 200 tokens → RAG (top-5) ≈ 400 tokens. **Ahorro: ~90 %**.

#### Activación paso a paso

```
1. ⚙ Habilidades  → activar 🧠 memoria
                  → activar 🧬 memoria_semantica
2. 🧬 Embeddings  → elegir proveedor → Probar conexión → Guardar
                  → Re-indexar
3. 📊 Tokens      → ejecutar una instrucción → observar el ahorro
```

---

## Sistema de Skills y Skills Hub

Las Skills son capacidades Python empaquetadas en archivos `.md` que el agente puede invocar como herramientas. El sistema tiene dos partes: el **Skills Hub** (instalación remota) y el **motor local** que las carga, parsea y ejecuta.

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
1. DISCOVERY  — SkillLoader.CargarActivas()
               Lee Skills/*.md del directorio de trabajo

2. PARSE      — SkillMdParser.Parsear(contenido)
               Extrae id, nombre, categoría, código Python, parámetros, metadata Hub

3. RUNNER     — SkillRunnerHelper.GenerarAsync(skills)
               Crea skill_runner.py dinámico con dispatch table
               Parámetros pasados vía env var SKILL_PARAMS (JSON)

4. MANIFIESTO — SkillManifestBuilder.Construir(skills)
               Genera texto inyectado en el PromptEfectivo del Constructor

5. EJECUCIÓN  — HerramientaSkill → MotorHerramientas.Ejecutar()
               Proceso Python aislado, captura stdout, valida parámetros
```

### Skills Hub — instalación remota

```csharp
var skill = await SkillHubManager.InstalarDesdeUrlAsync(
    url: "https://hub.opengioai.com/skills/generar_qr.md",
    rutaBase: rutaDirectorioTrabajo,
    ct: cancellationToken
);

await SkillHubManager.ActualizarAsync(skill, rutaBase, ct);
string mdListo = SkillHubManager.GenerarMdParaExportar(skill);
```

El campo `source_url` en el frontmatter registra el origen y permite actualizaciones automáticas.

### Skills incluidas por defecto

| ID | Categoría | Descripción |
|----|-----------|-------------|
| `clima_ciudad` | web | Clima actual de cualquier ciudad |
| `precio_cripto` | web | Precio en tiempo real de criptomonedas |
| `calculadora` | general | Operaciones matemáticas |
| `generar_qr` | sistema | Genera imágenes QR |
| `procesos_activos` | sistema | Lista procesos del sistema |
| `ip_publica` | web | Obtiene la IP pública |
| `convertir_csv_json` | datos | Conversión CSV ↔ JSON |
| + más | varios | Incluidas en `/skills_hub/` |

### Parámetros tipados con validación

Cada skill declara sus parámetros con tipo JSON-Schema, default, enum y flag de requerido. `DialogoParametrosSkill` muestra una UI modal para llenarlos antes de ejecutar, y `ValidarParametros()` verifica el JSON de inputs antes de llamar al runner.

### Creador de Skills (agente)

Desde `Skills.cs` se puede describir una skill en lenguaje natural. El agente genera automáticamente el archivo `.md` completo con código Python, parámetros y metadata, listo para activar.

---

## Automatizaciones con Nodos

`FrmAutomatizaciones.cs` permite construir flujos de trabajo complejos conectando nodos en un canvas interactivo. Cada nodo representa una instrucción en lenguaje natural que el agente convierte en un script Python.

### Tipos de nodo

| Tipo | Función |
|------|---------|
| `Disparador` | Punto de entrada. Inicia la cadena cuando se activa. |
| `Condicion` | Evalúa una expresión. Ramifica el flujo (Sí / No). |
| `Accion` | Ejecuta una instrucción. Puede usar el resultado del nodo anterior. |
| `Fin` | Termina la cadena y consolida el resultado. |

### Modelo de datos de una automatización

```csharp
public class Automatizacion
{
    public string Id { get; set; }                           // GUID único
    public string Titulo { get; set; }
    public List<NodoAutomatizacion> Nodos { get; set; }
    public TipoSchedule TipoSchedule { get; set; }          // Manual/Diaria/Intervalo/Única/Siempre/Rango
    public string HoraEjecucion { get; set; }               // HH:mm para diaria
    public int IntervaloMinutos { get; set; }               // Para tipo Intervalo
    public DateTime? FechaUnica { get; set; }               // Para tipo Única
    public string HoraInicio { get; set; }                  // Para tipo Rango
    public string HoraFin { get; set; }
    public List<int> DiasActivos { get; set; }              // 0=Dom..6=Sab (vacío = todos)
    public int TimeoutMinutos { get; set; }                 // 0 = sin límite
    public Dictionary<string, string> VariablesGlobales { get; set; }
    public string CarpetaScripts { get; set; }
}
```

### Tipos de programación

| Tipo | Descripción |
|------|-------------|
| `manual` | Solo al presionar el botón ejecutar |
| `diaria HH:mm` | Cada día a esa hora exacta |
| `intervalo N` | Cada N minutos |
| `única YYYY-MM-DD HH:mm` | Una sola vez en fecha/hora específica |
| `siempre` | Continuamente sin pausa |
| `rango HH:mm HH:mm` | Solo dentro de la ventana horaria |

Los días activos permiten restringir la ejecución a días específicos de la semana. Las **variables globales** se pasan a cada nodo vía env var `AUTO_VARIABLES` (JSON).

### Validación DAG

- **Ciclos**: algoritmo de Kahn detecta dependencias circulares.
- **Entradas requeridas**: verificación topológica de que cada entrada requerida tiene un predecesor o valor en variables globales.
- Mensajes de error legibles: `"falta entrada X en nodo Y"`.

### Almacenamiento

```
ListAutomatizaciones.json          ← definición de todas las automatizaciones
Automatizaciones/
└── {GUID}/
    ├── nodo_01_captura.py
    ├── nodo_02_procesa.py
    └── messages.json              ← historial de ejecución
```

### Modo segundo plano

El `AutomatizacionScheduler` persiste y ejecuta automatizaciones programadas incluso cuando la UI está cerrada, gracias al **tray icon** (NotifyIcon) que mantiene vivo el proceso en la bandeja del sistema.

---

## Sistema de Comandos `#cmd`

Cualquier mensaje que empiece con `#` se interpreta como una orden de configuración — sin tocar la UI, desde Telegram, Slack o el chat de escritorio.

### Pipeline de despacho

```
Texto del usuario  ("#audio proveedor OpenAI")
        │
        ▼
┌───────────────────┐
│   CommandParser   │  Tokeniza con respeto de comillas dobles.
│                   │  Soporta legacy #CMD_VALOR (callback Telegram).
└─────────┬─────────┘
          ▼
┌───────────────────┐
│  CommandRegistry  │  Diccionario nombre → handler.
│                   │  Si no existe: Levenshtein ≤ 2 → sugerir alternativas.
└─────────┬─────────┘
          ▼
┌───────────────────┐
│ CommandExecutor   │  Construye CommandContext (args + servicios).
│                   │  Invoca handler.EjecutarAsync(ctx).
└─────────┬─────────┘
          ▼
┌───────────────────┐
│ ResultFormatter   │  Telegram → MarkdownV2  •  Slack → mrkdwn  •  UI → texto
└─────────┬─────────┘
          ▼
   Mensaje final al canal de origen
```

### Catálogo de comandos

#### 🤖 Agente

| Comando | Uso | Descripción |
|---------|-----|-------------|
| `#agente` | `#agente` | Muestra el agente activo y disponibles |
| `#cambiar_agente` | `#cambiar_agente <nombre>` | Cambia el agente activo |
| `#modelo` | `#modelo` | Muestra el modelo activo |
| `#cambiar_modelo` | `#cambiar_modelo <id>` | Cambia el modelo del proveedor actual |
| `#ruta` | `#ruta` | Muestra la ruta de trabajo actual |
| `#cambiar_ruta` | `#cambiar_ruta <path>` | Cambia el directorio de trabajo |

#### ⚙ Configuración

| Comando | Uso | Descripción |
|---------|-----|-------------|
| `#configuraciones` | `#configuraciones` | Resumen de todas las configuraciones activas |
| `#reintentos` | `#reintentos <0..5>` | Número de reintentos del Guardián |
| `#timeout` | `#timeout <10..1800>` | Timeout por petición LLM en segundos |
| `#solochat` | `#solochat on\|off` | Modo conversacional puro (sin pipeline) |
| `#recordar` | `#recordar on\|off` | Activa/desactiva memoria del tema actual |
| `#apis` | `#apis` | Lista credenciales registradas (solo nombres) |
| `#cancelar` | `#cancelar` | Cancela la operación en curso |

#### 📡 Integración

| Comando | Uso | Descripción |
|---------|-----|-------------|
| `#telegram` | `#telegram on\|off` | Activa/desactiva el bot de Telegram |
| `#slack` | `#slack on\|off` | Activa/desactiva el bot de Slack |
| `#audio` | `#audio [sub] [valor]` | Configura TTS — ver subcomandos abajo |

**Subcomandos de `#audio`:**

```
#audio                          → estado actual (proveedor, voz, idioma)
#audio on / #audio off          → toggle de envío de audio
#audio proveedor SystemSpeech|OpenAI
#audio voz <nombre>             → ej. "nova", "Microsoft David"
#audio idioma <bcp-47>          → ej. "es-MX", "en-US"
#audio apikey <key>             → API key del proveedor TTS
#audio activar / #audio desactivar
```

#### 🧩 Habilidades

| Comando | Uso | Descripción |
|---------|-----|-------------|
| `#habilidad` | `#habilidad <clave> on\|off` | Activa/desactiva una habilidad cognitiva |

Habilidades disponibles: `memoria`, `patrones`, `memoria_semantica`. Sin args lista todas con su estado.

#### 📊 Estado / Ayuda

| Comando | Uso | Descripción |
|---------|-----|-------------|
| `#estado` | `#estado` | Snapshot global: agente, modelo, ruta, toggles, habilidades |
| `#ayuda` | `#ayuda [categoría\|comando]` | Lista comandos o detalle de uno específico |

### Sugerencia inteligente de typos

```
Usuario:  #agnte
Bot:      ❓ Comando `agnte` no encontrado.
          ¿Quisiste decir? `agente`, `agentes`
```

### Cobertura de tests

`Tests/ComandosTests/` ejecuta **67 tests** sin dependencia externa (usa `FakeServicios` in-memory):

| Bloque | Tests |
|--------|-------|
| Parser (P1–P5) | 24 |
| Registry (R1–R3) | 11 |
| Executor (E1–E4) | 14 |
| Handlers (H1–H5) | 18 |
| **Total** | **67 / 67 PASS** |

```bash
dotnet run --project Tests/ComandosTests
```

### Cómo extender — un comando = una clase

```csharp
public sealed class MiComando : ICommand
{
    public CommandDescriptor Descriptor { get; } = new()
    {
        Nombre      = "miorden",
        Alias       = new[] { "mo" },
        Descripcion = "Hace algo útil.",
        Uso         = "#miorden <valor>",
        Categoria   = CommandCategoria.Configuracion,
    };

    public Task<CommandResult> EjecutarAsync(CommandContext ctx)
    {
        if (string.IsNullOrEmpty(ctx.Arg0))
            return Task.FromResult(CommandResult.Error("Indica un valor."));
        ctx.Servicios.HacerAlgo(ctx.Arg0);
        return Task.FromResult(CommandResult.Exito($"Hecho: *{ctx.Arg0}*."));
    }
}
```

Registrarlo en `FrmMandos.ConfigurarCommandRouter()`:

```csharp
_cmdRegistry.Registrar(new MiComando());
```

Aparece automáticamente en `#ayuda`, categorizado y con sugerencia de typos.

---

## Sistema de Credenciales

OPENGIOAI gestiona API keys de forma centralizada. Los agentes **nunca tienen acceso directo** a los valores; el sistema inyecta solo los **nombres** disponibles en el contexto.

### Almacenamiento

Las claves se guardan en `ListApis.json` (en la ruta de trabajo):

```json
[
  { "Id": "openai",      "Nombre": "OpenAI",      "ApiKey": "sk-proj-..." },
  { "Id": "openweather", "Nombre": "OpenWeather",  "ApiKey": "abc123..." }
]
```

### Inyección en el contexto del agente

```
================= SISTEMA DE CREDENCIALES =================
RUTA DEL JSON: C:\...\ListApis.json
NOMBRES DISPONIBLES: openai, openweather, telegram, slack, gemini

REGLAS:
* Solo usa una credencial si es estrictamente necesaria para la tarea.
* Para obtener una clave: lee ListApis.json, busca por "Id", extrae "ApiKey".
* Nunca inventes claves. Nunca imprimas los valores en la respuesta.
===========================================================
```

El agente LLM solo ve los nombres. Cuando necesita una clave genera código Python que lee `ListApis.json` en proceso aislado:

```python
import json
with open(r"C:\...\ListApis.json") as f:
    apis = json.load(f)
api_key = next(a["ApiKey"] for a in apis if a["Id"] == "openweather")
```

### .gitignore recomendado

```gitignore
ListApis.json
Configuracion.json
*.env
Memoria/embeddings.jsonl
```

---

## Multi-Proveedor de LLMs

| Proveedor | Modelos ejemplo | Autenticación |
|-----------|----------------|---------------|
| **OpenAI** | GPT-4o, GPT-4 Turbo, GPT-3.5 | API Key (Bearer) |
| **Anthropic Claude** | Claude 3.5 Haiku/Sonnet, Claude Opus 4 | API Key (x-api-key) |
| **Google Gemini** | Gemini 1.5 Flash/Pro, Gemini 2.0 Flash | API Key (querystring) |
| **DeepSeek** | DeepSeek Chat, DeepSeek Reasoner | API Key (Bearer) |
| **Ollama** | Llama, Mistral, Phi, Gemma (local) | Sin clave |
| **Google Vertex AI** | Gemini en Vertex | gcloud ADC (OAuth2) |

### Resiliencia — RetryPolicy

```
Intento 1  →  falla  →  espera 1s ± jitter
Intento 2  →  falla  →  espera 2s ± jitter
Intento 3  →  falla  →  espera 4s ± jitter
Intento 4  →  falla  →  lanza excepción al caller
```

Errores que activan reintento: `429 Rate Limit`, `5xx Server Error`, `HttpRequestException`, `TaskCanceledException` (timeout interno).
Errores que **no** reintentan: `400 Bad Request`, `401 Unauthorized`, `OperationCanceledException` (usuario presionó Stop).

### Streaming en tiempo real

Los providers que soportan SSE (OpenAI, Claude, DeepSeek, Ollama, Gemini) envían tokens vía callback. `BurbujaChat` los renderiza en tiempo real con throttle a ~8 fps para eliminar parpadeos.

### Prompt Caching

`AgentContext` particiona el prompt en:
- **PromptEstable**: prefijo cacheable (PromptMaestro, rutas, dumps estáticos)
- **PromptVariable**: sufijo dinámico (RAG, memoria semántica, instrucción)

Providers que soportan caché (Anthropic) reutilizan el prefijo entre llamadas consecutivas, reduciendo costos hasta un 90 % en el prefijo.

---

## Integración Multi-Canal

### Telegram

- `TelegramListener` con long-polling — sin webhooks, sin servidor expuesto.
- Escucha mensajes, callbacks inline y descargas de archivos.
- Envío de mensajes, botones inline, archivos y mensajes "pensando...".
- Token y ChatId configurados desde `FrmComunicadores`.
- Thread-safe: semáforo asegura que solo una petición al LLM se ejecuta a la vez.

### Slack

- `SlackPollingService` con polling de 50 ms entre requests.
- Filtro de usuarios autorizados por username/ID.
- Envío de mensajes markdown, hilos y reacciones.
- Token de app configurado desde `FrmComunicadores`.

### Síntesis de voz (TTS)

`AudioTTSService` soporta dos proveedores:

| Proveedor | Motor | Configuración |
|-----------|-------|---------------|
| **Windows** | `System.Speech.SpeechSynthesizer` | Voces instaladas en Windows |
| **OpenAI** | API TTS (voces: Alloy, Echo, Fable, Onyx, Nova, Shimmer) | API key en `#audio apikey` |

Toggle desde el chat con `#audio on/off`. Proveedor y voz configurables con `#audio proveedor` y `#audio voz`.

---

## UI — Tema Emerald y Controles Personalizados

### EmeraldTheme

Paleta dinámica con soporte para **modo oscuro** (por defecto) y **modo claro**, intercambiable en tiempo de ejecución sin reiniciar.

| Token | Oscuro | Claro |
|-------|--------|-------|
| `BgDeep` | `#080808` | `#FFFFFF` |
| `BgSurface` | `#1A1A1C` | `#F0F6FF` |
| `BgCard` | `#191919` | `#E8F1FF` |
| `TextPrimary` | `#FFFFFF` | `#002647` |
| `Emerald500` | `#080808` | `#080808` |
| `Emerald400` | `#94E6EC` | `#94E6EC` |

El evento `EmeraldTheme.ThemeChanged` notifica a todos los controles que se suscriben (incluyendo `BurbujaChat`) para actualizar colores sin recrear controles.

### FrmPrincipal — Navegación

- **Sidebar colapsable**: 240 px expandido / 72 px colapsado (solo iconos), con animación smooth.
- **Stack LIFO**: los formularios se apilan — `Esc` cierra el formulario actual y vuelve al anterior.
- **Breadcrumb dinámico**: refleja la ruta de navegación actual.
- **Tray icon**: al cerrar la ventana el proceso continúa en bandeja del sistema ejecutando automatizaciones.
- **Modo segundo plano** toggle con `#solochat` o desde la UI.
- Atajo de teclado: `Ctrl+B` para colapsar/expandir el sidebar.

### Controles personalizados (GDI+, DoubleBuffer)

| Control | Descripción |
|---------|-------------|
| `BurbujaChat` | Burbuja de conversación con streaming token-a-token, gradiente, avatar circular, toggle "Mostrar más/menos", botón copiar, preview de imágenes, syntax highlighting de código, animación "pensando...", zoom global |
| `PanelAgentes` | Panel de estado que muestra las 4 fases ARIA con indicador activo/completado/error en tiempo real |
| `MenuItemBoton` | Botón sidebar con hover animado, indicador izquierdo de selección y modo colapsado |
| `FlowLayoutPanelSuave` | FlowLayoutPanel con scroll suave |
| `NodoVisualControl` | Nodo visual en canvas de automatización (drag & drop, conexiones, parámetros) |
| `CanvasAutomatizacion` | Canvas con zoom, pan, nodos arrastrables y líneas de conexión |
| `DoubleBufferedPanel` | Panel optimizado para eliminar parpadeos en redibujado |

---

## Habilidades Cognitivas

Las **Habilidades** son toggles internos que controlan cómo procesa el agente (a diferencia de los *Skills*, que controlan qué hace). Cada habilidad impacta en tokens, latencia o comportamiento.

| Clave | Nombre | Impacto estimado |
|-------|--------|------------------|
| `memoria` | Memoria del agente | +200–800 tokens por ejecución |
| `patrones` | Detección de patrones | +400–1200 tokens (solo al abrir el módulo) |
| `memoria_semantica` | Memoria semántica (RAG) | **Ahorro neto: −500 a −4000 tokens por instrucción** |

`HabilidadesRegistry` es un singleton con caché en memoria. La consulta `EstaActiva("clave")` es O(1) y no lee disco. **Todas las habilidades nacen desactivadas** — cada funcionalidad que consume tokens extra es una decisión consciente del usuario.

---

## Observabilidad — Traces y Logs

### TraceStorage

Cada ejecución del pipeline genera un `TraceEjecucion` con:
- Nombre de la fase (Analista, Constructor, Guardián, Comunicador)
- Estado (éxito / error)
- Duración en ms
- Salida (stdout resumido)
- Mensajes de error

Los traces se persisten y se visualizan en `FrmTraces` con filtros por fase, estado y fecha.

### Serilog

Logging estructurado con sinks de consola y archivo (rolling diario, retención 7 días):

```
Logs/app-2026-05-02.log
```

Configurado en `Program.cs` con inyección de dependencias (Telegram, Slack, Audio, Broadcast).

---

## Cómo Empezar

### Requisitos

- **.NET 10 Runtime** — [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0)
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

1. Ir a **Proveedores** → Agregar API key de cada proveedor
2. Ir a **Modelos** → Seleccionar el modelo por defecto
3. Ir a **Rutas de trabajo** → Establecer el directorio de trabajo
4. *(Opcional)* Ir a **Habilidades** → activar `memoria` y/o `memoria_semantica`
5. *(Opcional)* Ir a **Embeddings** → configurar proveedor → Re-indexar
6. *(Opcional)* Ir a **Skills** → activar o instalar skills desde el Hub
7. *(Opcional)* Ir a **Comunicadores** → configurar Telegram y/o Slack
8. *(Opcional)* Desde el chat escribir `#ayuda` para ver el catálogo de comandos

### Archivos en tiempo de ejecución

```
[AppDir]/
├── Configuracion.json            ← Configuración raíz (apunta a ruta de trabajo)
├── EmbeddingsConfig.json         ← Proveedor y modelo de embeddings
└── Logs/app-YYYYMMDD.log         ← Serilog rolling

[RutaDeTrabajo]/
├── promtMaestro.md               ← Prompt maestro editable
├── promtAgente.md                ← Prompt de manejo de errores
├── respuesta.txt                 ← Salida del último script
├── ListApis.json                 ← API keys (NO subir a git)
├── ListModelos.json              ← Modelos por proveedor
├── ListAutomatizaciones.json     ← Automatizaciones guardadas
├── ListSkills.json               ← Skills (legacy)
├── ListHabilidades.json          ← Estado de habilidades
├── ListPreciosModelos.json       ← Tarifas USD por modelo
├── ListTelegram.json             ← Config Telegram
├── ListSlack.json                ← Config Slack
├── Skills/
│   └── clima_ciudad.md           ← Definición de skill
├── Memoria/
│   ├── Hechos.md                 ← Verdades sobre el usuario (editable)
│   ├── Episodios.md              ← Timeline append-only
│   ├── embeddings.jsonl          ← VectorStore (Fase C)
│   └── manifest.json            ← Hashes SHA1 por fuente
├── Scripts/                      ← Scripts de automatización
└── Automatizaciones/
    └── {GUID}/
        ├── nodo_01_*.py
        └── messages.json
```

---

## Extensión y Desarrollo

### Agregar un nuevo proveedor LLM

1. Añadir valor al enum `Servicios.cs`
2. Extender `ConstruirRequest()` en `AIModelConector.cs` con endpoint y body
3. Extender `AgregarHeaders()` con la autenticación del provider
4. Extender `ExtraerContenido()` con el path JSON de la respuesta
5. Extender `TokenUsageReader` si el proveedor expone `usage` de forma distinta
6. Añadir tarifas en `PreciosModelos.Defaults()` para que aparezca en el panel 📊

### Crear una Skill personalizada

Crear un archivo `.md` en `Skills/` del directorio de trabajo siguiendo el formato de skill. Al recargar skills, el agente la detecta y la incluye en su manifiesto.

### Agregar un nuevo comando `#cmd`

1. Crear clase que implemente `ICommand` (ver ejemplo en [Sistema de Comandos](#sistema-de-comandos-cmd))
2. Registrar en `FrmMandos.ConfigurarCommandRouter()`
3. El comando aparece automáticamente en `#ayuda` con su categoría y soporte de typos

### Agregar una nueva Habilidad cognitiva

1. Añadir constante `HAB_XXX` en `HabilidadesRegistry.cs`
2. Añadir entrada a `Defaults()` con icono, descripción e impacto estimado
3. Consultar con `HabilidadesRegistry.Instancia.EstaActiva(HAB_XXX)` en el hot-path

### Crear un nuevo preset de PerfilContexto

```csharp
public static PerfilContexto MiPreset => new()
{
    LeerPromptMaestroDeDisco = true,
    IncluirPromptMaestro     = true,
    IncluirMemoria           = true,
};
```

Pasarlo a `AgentContext.BuildAsync(..., perfil: PerfilContexto.MiPreset)`.

---

## Troubleshooting

| Problema | Causa probable | Solución |
|----------|---------------|----------|
| Script Python no ejecuta | Python no está en PATH | Agregar Python al PATH del sistema |
| Error 429 frecuente | Rate limit del proveedor | RetryPolicy lo maneja; considerar modelo más económico |
| Guardián en bucle | Tarea imposible para el modelo | Revisar logs; simplificar instrucción o cambiar modelo |
| Skill no aparece | `activa: false` en el .md | Editar el .md → `activa: true` → recargar skills |
| Credencial no encontrada | Id incorrecto en ListApis.json | El `Id` en JSON debe coincidir exactamente con lo que pide el agente |
| Telegram no responde | Token inválido | Verificar token en FrmComunicadores; comprobar bot en BotFather |
| Burbujas con fondo incorrecto | Tema cambiado tras crear burbuja | `BurbujaChat` se suscribe a `ThemeChanged` — se auto-actualiza |
| 📊 Tokens no muestra nada | Fase no etiquetada | Verificar que la fase llama a `ctx.ComoFase("Nombre")` |
| RAG no devuelve nada | Índice vacío o stale | 🧬 Embeddings → Re-indexar |
| RAG falla silenciosamente | Provider caído | `MemoriaSemantica` hace fallback a dump completo automáticamente |
| Cambié de embedding y falla | Espacio vectorial incompatible | `MemoriaIndexer` detecta el cambio y reconstruye; si no: 🧬 → Limpiar → Re-indexar |
| Costo USD = $0 | Modelo no registrado | Editar `ListPreciosModelos.json` o añadir al `Defaults()` |
| Automatización no dispara | Scheduler no iniciado | Verificar que el modo segundo plano está activo (tray icon visible) |
| Socket exhaustion | HttpClient mal instanciado | Ya resuelto — AIModelConector usa singleton por proveedor |

---

## Dependencias

```xml
<PackageReference Include="Google.Apis.Auth"                         Version="1.68.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.7" />
<PackageReference Include="Newtonsoft.Json"                          Version="13.0.4" />
<PackageReference Include="Serilog"                                  Version="*" />
<PackageReference Include="Serilog.Sinks.Console"                   Version="*" />
<PackageReference Include="Serilog.Sinks.File"                       Version="*" />
<PackageReference Include="SlackAPI"                                 Version="1.1.14" />
<PackageReference Include="System.Management"                        Version="10.0.3" />
<PackageReference Include="System.Speech"                            Version="10.0.0" />
<PackageReference Include="Telegram.Bot"                             Version="22.9.0" />
```

---

## Roadmap

### ✅ Entregado

- [x] Pipeline ARIA de 4 fases (Analista · Constructor · Guardián · Comunicador) con eventos UI reactiva
- [x] Memoria durable en Markdown (`Hechos.md` + `Episodios.md`) + editor en `FrmMemoria`
- [x] **Fase A** — Telemetría de tokens por fase con costo USD en vivo (`FrmConsumoTokens`)
- [x] **Fase B** — Context slicing declarativo por fase (`PerfilContexto`)
- [x] **Fase C** — RAG local con OpenAI / HuggingFace / Vertex AI, indexación incremental y fallback automático
- [x] Prompt caching (`AgentContext` PromptEstable/Variable) compatible con Anthropic
- [x] Multi-proveedor LLM (OpenAI · Claude · Gemini · DeepSeek · Ollama · Vertex AI)
- [x] Skills ejecutables (.md con Python) + SkillHub + creador de skills por IA + parámetros tipados
- [x] Automatizaciones visuales DAG con nodos, validación topológica y programación cron/intervalo/rango
- [x] **Sistema de comandos `#cmd`** (parser + registry + executor + formatter, 19 comandos, typo-suggest, 67/67 tests)
- [x] Integración Telegram (long-polling, thread-safe) + Slack (polling, filtro usuarios)
- [x] Síntesis de voz (Windows System.Speech + OpenAI TTS)
- [x] Traces de ejecución por fase (`TraceStorage` + `FrmTraces`)
- [x] Habilidades cognitivas opt-in con registry persistente
- [x] Detección de patrones recurrentes con propuesta de skills (`FrmPatrones`)
- [x] Modo segundo plano con tray icon + ejecución persistente de automatizaciones
- [x] Tema Emerald dinámico (oscuro/claro) + controles personalizados GDI+ (BurbujaChat, PanelAgentes, sidebar colapsable, breadcrumb)
- [x] Multi-workspace con migración no-destructiva de archivos
- [x] Logging estructurado con Serilog (rolling diario, retención 7 días)
- [x] Aislamiento de proceso Python + credenciales nunca expuestas al agente
- [x] Streaming SSE con cancelación del usuario y throttle a ~8 fps sin parpadeos
- [x] RetryPolicy con backoff exponencial + jitter para todos los providers
- [x] HttpClient singleton por proveedor (sin socket exhaustion)

### 🚧 En progreso / próximas fases

- [ ] RAG para Skills (top-K skills relevantes en lugar del manifiesto completo)
- [ ] Dashboard histórico de tokens con gráficas y comparativas por modelo
- [ ] Cifrado AES-256 para `ListApis.json`
- [ ] Historial persistente de conversaciones con búsqueda semántica
- [ ] Integración con Discord
- [ ] Exportación/importación de automatizaciones en formato portátil
- [ ] Skills Hub con búsqueda y categorías en la UI
- [ ] Fine-tuning de modelos locales (Ollama)

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

Cualquier PR que toque el **pipeline ARIA**, el **sistema de memoria** o los **subsistemas de telemetría/slicing/RAG** debe incluir:
- Actualización del README en la sección correspondiente
- Prueba manual documentada (instrucción de ejemplo + resultado esperado)
- Compilación limpia (`0 errores`)

---

## Disclaimer

OPENGIOAI ejecuta código Python generado dinámicamente por LLMs. El proceso está aislado, pero siempre es recomendable:

- Revisar `script_ia.py` antes de ejecutar en sistemas críticos
- No ejecutar scripts en entornos de producción sin validación previa
- Mantener `ListApis.json` fuera del control de versiones
- Tratar `Memoria/embeddings.jsonl` como datos sensibles — contiene fragmentos literales de tu memoria

---

**Hecho en C# · .NET 10 · Windows Forms · Arquitectura token-aware · RAG semántico · Memoria durable · Tema Emerald**
