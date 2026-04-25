# OPENGIOAI

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![.NET 10](https://img.shields.io/badge/.NET%2010-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Windows Forms](https://img.shields.io/badge/Windows%20Forms-0078D4?style=for-the-badge&logo=windows&logoColor=white)
![Python](https://img.shields.io/badge/Python%203.8+-3776AB?style=for-the-badge&logo=python&logoColor=white)
![RAG](https://img.shields.io/badge/RAG-Ollama%20%7C%20OpenAI-10b981?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-blue)

> **Agente IA de escritorio de clase producción** — arquitectura multi-fase, memoria durable con RAG, telemetría de tokens, context slicing declarativo y ejecución aislada de código Python generado.

**OPENGIOAI** es una plataforma de escritorio desarrollada en C# con .NET 10 que orquesta múltiples agentes de inteligencia artificial, ejecuta scripts Python generados dinámicamente, gestiona un sistema de Skills extensible, permite crear automatizaciones visuales con nodos **y optimiza agresivamente el consumo de tokens** mediante tres pilares: telemetría granular, context slicing por fase y recuperación semántica (RAG) de memoria. Compatible con OpenAI, Google Gemini, Anthropic Claude, DeepSeek, OpenRouter, Ollama y Google Vertex AI.

---

## Tabla de Contenidos

1. [Arquitectura General](#arquitectura-general)
2. [Pipeline ARIA — El Motor de Agentes](#pipeline-aria)
3. [Memoria Durable del Agente](#memoria-durable-del-agente)
4. [Habilidades Cognitivas](#habilidades-cognitivas)
5. [Token-Saving Architecture](#token-saving-architecture)
   - [Fase A — Telemetría de Tokens](#fase-a--telemetría-de-tokens)
   - [Fase B — Context Slicing](#fase-b--context-slicing)
   - [Fase C — RAG Local (Memoria Semántica)](#fase-c--rag-local-memoria-semántica)
6. [Sistema de Skills y Skills Hub](#sistema-de-skills-y-skills-hub)
7. [Automatizaciones con Nodos](#automatizaciones-con-nodos)
8. [Sistema de Comandos `#cmd` — Configuración desde el Chat](#sistema-de-comandos-cmd)
9. [Sistema de Credenciales Seguras](#sistema-de-credenciales-seguras)
10. [Multi-Proveedor de LLMs](#multi-proveedor-de-llms)
11. [Integración Multi-Canal](#integracion-multi-canal)
12. [Cómo Empezar](#como-empezar)
13. [Extensión y Desarrollo](#extension-y-desarrollo)
14. [Troubleshooting](#troubleshooting)

---

## Arquitectura General

OPENGIOAI se organiza en capas bien definidas. El punto de entrada es siempre `AIModelConector.cs`, el orquestador central que coordina todos los agentes, providers y herramientas. Por encima vive el **OrquestadorARIA** (pipeline de 5 fases) y alrededor un stack de subsistemas (Memoria, Habilidades, Embeddings, Telemetría) que colaboran para que cada token enviado al LLM esté justificado.

```
┌─────────────────────────────────────────────────────────────────┐
│                     CAPA DE INTERFAZ                            │
│  FrmMandos (chat)  •  FrmAutomatizaciones  •  FrmPipelineAgente │
│  FrmMemoria  •  FrmHabilidades  •  FrmPatrones                  │
│  FrmEmbeddings  •  FrmConsumoTokens (flotante)                  │
│  Telegram Bot      •  Slack Bot                                 │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│               CAPA DE ORQUESTACIÓN DE AGENTES                   │
│  OrquestadorARIA (pipeline 5 fases + Memorista async)           │
│  PipelineMultiAgente (Planificador → Ejecutor → Verificador)    │
│  AgentePlanificador (ReAct / Chain-of-Thought)                  │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│        SUBSISTEMAS TRANSVERSALES (context & savings)            │
│  AgentContext (inmutable) · PerfilContexto (slicing)            │
│  ConsumoTokensTracker · PreciosModelos · HabilidadesRegistry    │
│  MemoriaManager · MemoriaSemantica · VectorStore · EmbeddingsSvc│
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│               AIModelConector  (hub central)                    │
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
├── Agentes/                    # Motores de orquestación
│   ├── OrquestadorARIA.cs      # Pipeline ARIA de 5 fases
│   ├── AgenteMemorista.cs      # Memorista: escribe memoria al final
│   ├── PipelineMultiAgente.cs  # Pipeline paralelo / verificador
│   ├── AgentePlanificador.cs   # Planificación ReAct/CoT
│   └── PasoDelPlan.cs          # Modelo de paso de plan
│
├── Comandos/                   # Sistema profesional de comandos `#cmd`
│   ├── CommandCategoria.cs     # Enum + extensiones (icono/título)
│   ├── CommandDescriptor.cs    # Metadata declarativa por comando
│   ├── CommandContext.cs       # Args parseados + helpers (Arg0, on/off)
│   ├── CommandResult.cs        # Resultado tipado (Exito/Error/Advertencia/Info)
│   ├── ICommand.cs             # Contrato de un handler
│   ├── CommandParser.cs        # Tokenizer shell-lite con comillas y legacy `#CMD_VALOR`
│   ├── CommandRegistry.cs      # Registrar/Resolver + Levenshtein typo-suggest
│   ├── CommandExecutor.cs      # Despacha: parse → resolve → execute → result
│   ├── ResultFormatter.cs      # Render por canal (Telegram / Slack / UI)
│   ├── IServiciosComandos.cs   # Fachada hacia FrmMandos (UI-thread safe)
│   └── Handlers/               # 19 comandos visibles + 2 legacy ocultos
│       ├── AgenteCommand.cs        # `#agente` → estado + lista
│       ├── CambiarAgenteCommand.cs # `#cambiar_agente <nombre>` (+legacy)
│       ├── ModeloCommand.cs        # `#modelo`
│       ├── CambiarModeloCommand.cs # `#cambiar_modelo <id>` (+legacy)
│       ├── RutaCommand.cs          # `#ruta`
│       ├── CambiarRutaCommand.cs   # `#cambiar_ruta <path>` (+legacy)
│       ├── ConfiguracionesCommand.cs # `#configuraciones`
│       ├── TelegramCommand.cs      # `#telegram on|off` (+legacy ocultos)
│       ├── SlackCommand.cs         # `#slack on|off`
│       ├── RecordarCommand.cs      # `#recordar on|off`
│       ├── SoloChatCommand.cs      # `#solochat on|off`
│       ├── ApisCommand.cs          # `#apis`
│       ├── AyudaCommand.cs         # `#ayuda` con paginación por categoría
│       ├── HabilidadCommand.cs     # `#habilidad <clave> on|off`
│       ├── ReintentosCommand.cs    # `#reintentos <0..5>`
│       ├── TimeoutCommand.cs       # `#timeout <10..1800>`
│       ├── AudioCommand.cs         # `#audio` subcomandos: proveedor/voz/idioma/apikey/activar
│       ├── EstadoCommand.cs        # `#estado` snapshot global
│       └── CancelarCommand.cs      # `#cancelar` (corta operación en curso)
│
├── Data/                       # Núcleo lógico
│   ├── AIModelConector.cs      # Orquestador principal (hub)
│   ├── AgentContext.cs         # Contexto inmutable de ejecución
│   ├── RetryPolicy.cs          # Backoff exponencial con jitter
│   └── ConversationWindow.cs   # Ventana deslizante de historial
│
├── ServiciosAI/                # Servicios transversales de IA
│   ├── AIServicios.cs          # Entrypoints genéricos
│   ├── TokenUsageReader.cs     # Extractor multi-proveedor de usage
│   └── EmbeddingsService.cs    # Embeddings Ollama / OpenAI (Fase C)
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
│   ├── Habilidad.cs            # Capacidad cognitiva (toggle interno)
│   ├── ConsumoTokens.cs        # Registro de consumo por llamada
│   ├── PrecioModelo.cs         # Tarifa USD por 1M tokens
│   ├── PerfilContexto.cs       # Flags de slicing de contexto (Fase B)
│   ├── ProveedorEmbedding.cs   # Enum Ollama | OpenAI
│   ├── EmbeddingConfig.cs      # Config global de embeddings
│   └── ChunkMemoria.cs         # Chunk + resultado + manifest (RAG)
│
├── Vistas/                     # UI Windows Forms
│   ├── FrmPrincipal.cs         # Ventana principal
│   ├── FrmMandos.cs            # Chat con burbujas y display ARIA
│   ├── FrmAutomatizaciones.cs  # Editor visual de nodos
│   ├── FrmApis.cs              # Gestión de API keys
│   ├── FrmModelos.cs           # Selección de modelos
│   ├── FrmPipelineAgente.cs    # Pipeline multi-agente UI
│   ├── FrmMemoria.cs           # Editor de Hechos.md / Episodios.md
│   ├── FrmHabilidades.cs       # Toggles de capacidades cognitivas
│   ├── FrmPatrones.cs          # Detección de patrones → Skills
│   ├── FrmConsumoTokens.cs     # Panel flotante de tokens (Fase A)
│   └── FrmEmbeddings.cs        # Config y operación RAG (Fase C)
│
├── ServiciosTelegram/          # Bot Telegram
├── ServiciosSlack/             # Bot Slack
├── Utilerias/                  # Subsistemas de soporte
│   ├── RutasProyecto.cs        # Rutas canónicas (AppDir / Workspace)
│   ├── MarkdownFileManager.cs  # Lector async de Markdown
│   ├── JsonManager.cs          # Persistencia genérica List<T>
│   ├── HabilidadesRegistry.cs  # Singleton de habilidades
│   ├── PreciosModelos.cs       # Registry de tarifas LLM
│   ├── ConsumoTokensTracker.cs # Telemetría correlacionada (Fase A)
│   ├── MemoriaManager.cs       # Hechos.md / Episodios.md
│   ├── MemoriaChunker.cs       # Chunking + SHA1 IDs estables (Fase C)
│   ├── MemoriaIndexer.cs       # Indexación incremental (Fase C)
│   ├── MemoriaSemantica.cs     # API alto nivel RAG (Fase C)
│   └── VectorStore.cs          # JSONL vector store + cosine (Fase C)
├── Themas/                     # EmeraldTheme, BurbujaChat…
└── Tests/
    ├── ComandosTests/          # 67 tests del módulo Comandos (Parser/Registry/Executor/Handlers)
    │   ├── ComandosTests.csproj
    │   └── Program.cs          # Test harness con FakeServicios in-memory
    └── ConversationWindowTests/  # Tests de la ventana de contexto
```

---

## Pipeline ARIA

ARIA (**A**nalista · Constructor · Guardián · Comunicador · Memor**i**st**a**) es el corazón de OPENGIOAI. Cada instrucción del usuario pasa por cinco fases ejecutadas en secuencia (salvo el Memorista, que corre en background al finalizar), con autocorrección automática integrada y telemetría por fase.

### Diagrama de flujo

```
Instrucción del usuario
        │
        ▼
┌───────────────┐
│   ANALISTA    │  Interpreta la instrucción. Genera un plan amigable
│               │  y lo muestra al usuario en tiempo real.
│  Perfil:      │  [Mínimo] — sin disco, prompt propio
└───────┬───────┘
        │         (en paralelo se pre-build el contexto Completo)
        ▼
┌───────────────┐
│  CONSTRUCTOR  │  Llama a AIModelConector para que el LLM genere
│               │  un script Python. Lo guarda y lo ejecuta.
│  Perfil:      │  [Completo] — incluye skills + memoria (o RAG top-K)
└───────┬───────┘
        │
        ▼
┌───────────────────────┐
│  ANALIZADOR RÁPIDO    │  Lee respuesta.txt. Pregunta al LLM:
│  (0.5 s aprox.)       │  "¿La tarea está completa?"
│  Perfil: [Mínimo]     │
└──────┬───────┬────────┘
       │ SÍ   │ NO
       │       ▼
       │  ┌───────────────┐
       │  │   GUARDIÁN    │  Lee el código generado + la salida
       │  │ (0–3 intentos)│  con errores. Pide al LLM una corrección.
       │  │  Perfil:      │  Re-ejecuta el Constructor con el fix.
       │  │  [Mínimo]     │
       │  └───────┬───────┘
       │          │ éxito / max reintentos
       ▼          ▼
┌───────────────┐
│ COMUNICADOR   │  Convierte el resultado técnico a lenguaje natural.
│               │  Hace streaming token-a-token al usuario.
│  Perfil:      │  [Comunicador] — identidad + memoria, sin skills
└───────┬───────┘
        │
        ▼  (respuesta visible para el usuario)
        │
        ▼  (fire-and-forget, no bloquea)
┌───────────────┐
│   MEMORISTA   │  Lee instrucción + respuesta final + memoria actual.
│  (async)      │  Extrae hechos nuevos y actualiza Hechos.md/Episodios.md.
│  Perfil:      │  [Memorista] — solo identidad, sin credenciales
└───────────────┘
        │
        ▼
  Log completo en ARIALog.md
```

### Fases en detalle

**Analista**
Recibe la instrucción completa. Usa el LLM para generar una explicación en lenguaje natural de lo que se va a hacer. El texto se muestra inmediatamente en la UI mientras el Constructor trabaja en paralelo. Usa `PerfilContexto.Minimo` — no carga skills, ni credenciales, ni memoria (no los necesita).

**Constructor**
Invoca `AIModelConector.EjecutarInstruccionIAAsync()`. El LLM genera el script Python necesario, se guarda como `script_ia.py` en el directorio de trabajo y se ejecuta en un proceso aislado. `stdout` y `stderr` se capturan y se guardan en `respuesta.txt`. Es la fase con contexto más gordo (perfil `Completo`): prompt maestro + skills + credenciales + memoria (completa o RAG según habilidades).

**Guardián (agente de corrección)**
Si el Analizador Rápido detecta que la respuesta tiene errores o está incompleta, el Guardián entra en acción. Lee el script generado y la salida con errores y construye un prompt de corrección contextualizado. Reintenta el ciclo Constructor hasta 3 veces con contexto acumulado. Cada reintento dispara el evento `OnReintentoGuardian` para que la UI lo muestre al usuario. Perfil `Minimo` porque recibe su propio prompt especializado.

**Comunicador**
Toma el resultado final (sea éxito o el mejor intento del Guardián) y lo transforma en una respuesta conversacional, amigable y sin jerga técnica. Hace streaming token-a-token vía callback `OnToken` para que la UI renderice en tiempo real. Perfil `Comunicador`: lleva identidad + memoria (para personalizar) pero nunca expone credenciales ni skills — el streaming arranca más rápido por menos tokens a procesar.

**Memorista (asíncrono)**
Corre en background cuando el usuario ya tiene su respuesta. Su trabajo: leer la conversación completa y decidir qué hechos/episodios merece la pena guardar para el futuro. Actualiza `Hechos.md` y `Episodios.md` en la ruta de trabajo. Si falla, se traga el error silenciosamente — el usuario ni se entera. Perfil `Memorista`: no lee la propia memoria (evita bucles), no lee credenciales (no las necesita), no lee skills.

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

## Memoria Durable del Agente

La memoria de OPENGIOAI vive en **archivos Markdown editables a mano**, asociados al directorio de trabajo activo. Cero BD, cero servicios externos, cero dependencias: la memoria "viaja" con la carpeta del proyecto y se versiona en git si tú lo decides.

### Dos fuentes de memoria

| Archivo | Propósito | Estructura |
|---------|-----------|------------|
| `Memoria/Hechos.md` | Verdades durables sobre el usuario / su entorno. | Lista de bullets — un hecho por línea. |
| `Memoria/Episodios.md` | Timeline append-only de ejecuciones relevantes. | Bloques con timestamp ISO + descripción. |

### Ciclo completo

```
┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐
│  INICIO PIPELINE│      │ FIN DE PIPELINE │      │ LECTURA MANUAL  │
│                 │      │                 │      │                 │
│ AgentContext    │      │  MEMORISTA      │      │  FrmMemoria     │
│ .BuildAsync()   │      │  (async, BG)    │      │  (editor)       │
│                 │      │                 │      │                 │
│ Lee Hechos.md   │      │ Lee convers.    │      │ Edición directa │
│ + Episodios.md  │      │ Extrae hechos   │      │ con autosave    │
│ Formatea como   │      │ Escribe nuevos  │      │ El usuario      │
│ sección del     │      │ bullets/episo   │      │ corrige o       │
│ prompt          │      │ dios al final   │      │ amplia a mano   │
└────────┬────────┘      └────────┬────────┘      └────────┬────────┘
         │                        │                        │
         └───────────┬────────────┴────────────────────────┘
                     ▼
           Memoria/Hechos.md   ← fuente de verdad editable
           Memoria/Episodios.md
```

### Presupuesto de tokens

`MemoriaManager.FormatearParaPromptAsync()` recorta la memoria al presupuesto configurado (por defecto ~800 tokens ≈ 3200 caracteres). Los hechos se conservan enteros; los episodios se truncan desde los más viejos. Con la **Fase C** activa, esta lógica se reemplaza por recuperación semántica top-K (ver [Fase C — RAG Local](#fase-c--rag-local-memoria-semántica)).

### Patrones → Skills (Fase 3)

El módulo `FrmPatrones` analiza `Episodios.md` en busca de tareas recurrentes (≥3 ocurrencias similares) y propone convertirlas en **Skills ejecutables**. El análisis solo se dispara cuando el usuario entra al módulo — nunca en cada pipeline — para que el coste de tokens sea visible y controlado.

---

## Habilidades Cognitivas

Las **Habilidades** son toggles internos que controlan cómo procesa el agente (a diferencia de los *Skills*, que controlan qué hace). Cada habilidad impacta en tokens, latencia o comportamiento — el usuario decide qué activar desde `FrmHabilidades`.

| Clave | Icono | Nombre | Impacto estimado |
|-------|-------|--------|------------------|
| `memoria` | 🧠 | Memoria del agente | +200–800 tokens por ejecución |
| `patrones` | 🔎 | Detección de patrones | +400–1200 tokens por análisis (solo en módulo) |
| `memoria_semantica` | 🧬 | Memoria semántica (RAG) | **Ahorro neto: −500 a −4000 tokens por instrucción** |

### Registry y persistencia

`HabilidadesRegistry` es un singleton con caché en memoria que invalida al guardar. La consulta `EstaActiva("clave")` es O(1) y se llama en hot-path sin leer disco. Los defaults se mezclan con lo que hay en `{AppDir}/ListHabilidades.json`:

```csharp
if (HabilidadesRegistry.Instancia.EstaActiva(HabilidadesRegistry.HAB_MEMORIA))
{
    memoriaFormateada = await MemoriaManager.FormatearParaPromptAsync(rutaArchivo);
}
```

### Opt-in por defecto

**Todas las habilidades nacen desactivadas**. La idea es que cada funcionalidad que consume tokens extra sea una decisión consciente del usuario. Nadie paga por features que no usa.

---

## Token-Saving Architecture

OPENGIOAI ataca el coste del agente por tres frentes complementarios. Las tres fases son **independientes pero componibles** — puedes usar solo telemetría, o solo slicing, o todo junto. Objetivo: que cada token enviado al LLM esté justificado.

```
┌──────────────────────────────────────────────────────────────────┐
│                  PROBLEMA: un agente ingenuo                     │
│  ─ manda prompt maestro enorme en cada fase                      │
│  ─ inyecta memoria completa aunque sea irrelevante               │
│  ─ inyecta TODOS los skills aunque solo use uno                  │
│  ─ el usuario no sabe cuánto gasta ni en qué                     │
└──────────────────────────────────────────────────────────────────┘
                               ▼
┌──────────────────────────────────────────────────────────────────┐
│   FASE A — TELEMETRÍA       │  Ver cuánto gasta por fase y por  │
│   📊 Tokens (flotante)       │  instrucción. Costo USD en vivo.   │
├──────────────────────────────────────────────────────────────────┤
│   FASE B — CONTEXT SLICING  │  Cada fase declara qué secciones  │
│   PerfilContexto             │  necesita. El resto NO se lee NI   │
│                              │  se envía.                         │
├──────────────────────────────────────────────────────────────────┤
│   FASE C — RAG LOCAL        │  Memoria recuperada por similitud │
│   🧬 Embeddings              │  semántica (top-K) en lugar del    │
│                              │  dump completo.                    │
└──────────────────────────────────────────────────────────────────┘
                               ▼
              Ahorro típico combinado: 40–70 % de tokens
              Ahorro adicional en latencia del Comunicador
```

---

### Fase A — Telemetría de Tokens

Panel flotante `📊 Tokens` (always-on-top, arrastrable) que muestra en tiempo real el consumo de **cada llamada** al LLM, con desglose por fase, por instrucción y por proveedor + **costo estimado en USD**.

#### Arquitectura

```
┌──────────────────────────────────────────────────────────────────┐
│               ConsumoTokensTracker  (singleton)                  │
│                                                                  │
│  IniciarEjecucion(instruccion) ─▶ asigna correlation ID          │
│  Registrar(consumo)            ─▶ agrupa por ID y fase           │
│  FinalizarEjecucion(id)        ─▶ emite evento final             │
│                                                                  │
│  Eventos (lock-free hacia UI):                                   │
│    OnEjecucionIniciada / OnConsumoRegistrado                     │
│    OnEjecucionFinalizada                                         │
└───────────────────────┬──────────────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────────────────┐
│                   PreciosModelos                                 │
│                                                                  │
│  Estimar(modelo, tokensIn, tokensOut) → USD                      │
│  Defaults embebidos + override desde ListPreciosModelos.json     │
│  Tarifas de OpenAI · Claude · Gemini · DeepSeek · Embeddings     │
└──────────────────────────────────────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────────────────┐
│                FrmConsumoTokens  (flotante, TopMost)             │
│                                                                  │
│  Pestaña [En vivo]      Ejecución actual, desglose por fase      │
│  Pestaña [Historial]    Últimas 100 ejecuciones con totales      │
│  Footer                 Tokens totales + USD acumulado           │
└──────────────────────────────────────────────────────────────────┘
```

#### Qué se registra

Cada llamada al LLM se etiqueta automáticamente vía `AgentContext.ComoFase(nombre)` antes de enviarse:

```csharp
var ctxAnalista = ctx.ComoFase("Analista")
                     .ConPromptPersonalizado(PromptAnalista);
await AIModelConector.ObtenerRespuestaAsync(..., ctxAnalista);
// ← el tracker ya sabe qué fase generó este consumo
```

Soporta **todos los proveedores**: OpenAI/Claude/Gemini/DeepSeek parsean el campo `usage` del JSON; Ollama parsea los campos `prompt_eval_count`/`eval_count` de la última línea NDJSON del stream.

#### Ejemplo de vista

```
📊 Consumo de Tokens — EN VIVO
┌──────────────────────────────────────────────────────────┐
│ Instrucción: "dame el clima de Madrid y envíamelo a TG"  │
├──────────────────────────────────────────────────────────┤
│ Analista     │  1 204 in  │    318 out  │  $0.0013  ✓    │
│ Constructor  │  4 872 in  │  1 103 out  │  $0.0239  ✓    │
│ Guardián     │      0     │      0     │   –       —    │
│ Comunicador  │  2 108 in  │    412 out  │  $0.0073  ✓    │
│ Memorista    │  1 640 in  │    203 out  │  $0.0057  ⏳   │
├──────────────────────────────────────────────────────────┤
│ TOTAL        │  9 824 in  │  2 036 out  │  $0.0382       │
└──────────────────────────────────────────────────────────┘
```

---

### Fase B — Context Slicing

Cada fase del pipeline declara **qué secciones del prompt necesita** mediante un `PerfilContexto`. Las secciones no declaradas ni se leen del disco ni se envían al LLM.

#### El problema que resuelve

Sin slicing, cada fase recibía el prompt maestro completo + skills + credenciales + memoria + automatizaciones + historial — aunque el Analista solo necesita entender la instrucción y el Guardián solo necesita ver un error. Con 20 skills y memoria mediana, esto puede suponer **5k–10k tokens inútiles por fase**.

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
    public bool IncluirSoloChat           { get; init; }
    public bool IncluirSkills             { get; init; }
    public bool IncluirAutomatizaciones   { get; init; }
    public bool IncluirHistorial          { get; init; }
    public bool IncluirMemoria            { get; init; }
    public bool IncluirPromptsMaestros    { get; init; }
    public bool IncluirUsuario            { get; init; }
}
```

#### Presets disponibles

| Preset | Para quién | Qué incluye |
|--------|-----------|------------|
| `Completo` | Constructor, SoloChat, Agente1 legacy | Todo — prompt maestro + skills + credenciales + memoria + automatizaciones |
| `SoloIdentidad` | ComoInicio | Prompt maestro + usuario |
| `Minimo` | Analista, Guardián, Analizador Rápido | Nada — se inyecta prompt propio vía `ConPromptPersonalizado` |
| `Memorista` | Memorista async | Prompt maestro + ruta + usuario (sin credenciales, sin skills) |
| `Comunicador` | Comunicador streaming | Prompt maestro + memoria + usuario (sin credenciales, sin skills, sin automatizaciones) |

#### Uso

```csharp
// Analista: no necesita NADA del contexto — usa prompt propio
var ctx = await AgentContext.BuildAsync(
    ruta, modelo, apiKey, servicio, soloChat, claves,
    ct, perfil: PerfilContexto.Minimo);

// Constructor: lo quiere todo
var ctx = await AgentContext.BuildAsync(
    ruta, modelo, apiKey, servicio, soloChat, claves,
    ct, perfil: PerfilContexto.Completo,
    instruccionUsuario: instruccion);  // ← activa RAG si hab está ON
```

#### Ahorro medido

- **1 700 – 10 500 tokens por instrucción** (depende del tamaño del prompt maestro y de la memoria).
- **3–4 lecturas de disco evitadas** por ejecución (promt maestro, skills, memoria).
- **Latencia del primer token** del Comunicador reducida ~15-30 % al no procesar secciones irrelevantes.

---

### Fase C — RAG Local (Memoria Semántica)

Cuando la memoria crece (cientos de hechos, timeline de meses de episodios) inyectarla completa en cada instrucción se vuelve carísimo — y el 90 % no es relevante para la instrucción actual. La **Fase C** sustituye el dump completo por **recuperación semántica top-K**: embeber la instrucción del usuario, buscar los chunks más similares de Hechos/Episodios y inyectar solo esos.

#### Arquitectura

```
┌──────────────────────────────────────────────────────────────────┐
│  EmbeddingConfig         {AppDir}/EmbeddingsConfig.json          │
│                                                                  │
│  · Proveedor: Ollama | OpenAI                                    │
│  · Modelo: nomic-embed-text (768d) | text-embedding-3-small (1536d)│
│  · TopK / ChunkSize / ChunkOverlap                               │
└────────────────────────────┬─────────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────────────┐
│                    EmbeddingsService                             │
│                                                                  │
│  EmbedAsync(texto)           → float[]                           │
│  EmbedManyAsync(textos)      → List<float[]>  (batch 128 OpenAI) │
│  ProbarConexionAsync(cfg)    → (ok, mensaje, dim)                │
│                                                                  │
│  HttpClient singletons por proveedor (sin socket exhaustion)     │
│  Las llamadas a OpenAI se registran en ConsumoTokensTracker      │
│  con Fase="Embedding" → aparecen en el panel 📊                  │
└────────────────────────────┬─────────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────────────┐
│   MemoriaChunker               MemoriaIndexer                    │
│                                                                  │
│   ChunkearHechos(contenido)    IndexarAsync(ruta):               │
│   ChunkearEpisodios(contenido)    1. Hash SHA1 por fuente        │
│   ChunkearTextoLibre(...)         2. Skip si sin cambios         │
│   ComputarId(fuente,ofs,txt)      3. Batch embed + Upsert        │
│   → SHA1 10-char hex              4. Update manifest             │
│                                                                  │
│   IDs estables → re-indexación idempotente                       │
└────────────────────────────┬─────────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────────────┐
│                        VectorStore                               │
│                                                                  │
│  Persistencia: JSONL  {ruta}/Memoria/embeddings.jsonl            │
│    · Grep-able, append-friendly, inspeccionable a mano           │
│    · Atomic write via tmp + rename                               │
│  Búsqueda: brute-force cosine normalizado a [0, 1]               │
│    · Típicamente 2–3k chunks por workspace → < 50 ms             │
│  Thread-safe (lock)                                              │
└────────────────────────────┬─────────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────────────┐
│                    MemoriaSemantica                              │
│                                                                  │
│  ObtenerContextoRelevanteAsync(ruta, instruccion):               │
│    1. Gate: HAB_MEMORIA ∧ HAB_MEMORIA_SEMANTICA                  │
│    2. IndexarAsync (incremental, silencioso)                     │
│    3. Embed instrucción + Buscar top-K                           │
│    4. Formatear "================= MEMORIA RELEVANTE (RAG) ==="  │
│    5. Si falla algo → "" → fallback a dump completo              │
│                                                                  │
│  Tolerante a fallos: Ollama apagado / API key mala / disco lleno │
│  NO rompen el pipeline — el agente sigue funcionando.            │
└──────────────────────────────────────────────────────────────────┘
```

#### Proveedores soportados

| Proveedor | Modelo | Dim | Costo | Setup |
|-----------|--------|-----|-------|-------|
| **Ollama** | `nomic-embed-text` | 768 | **$0** (local) | `ollama pull nomic-embed-text` |
| **OpenAI** | `text-embedding-3-small` | 1536 | $0.02 / 1M tokens | API key con permiso de embeddings |
| **OpenAI** | `text-embedding-3-large` | 3072 | $0.13 / 1M tokens | Igual, mayor calidad |

Cambiar de proveedor/modelo **invalida el índice entero** (espacios vectoriales incompatibles). El `MemoriaIndexer` lo detecta por el `ManifestEmbeddings` y hace rebuild automático.

#### Indexación incremental

```
┌──────────────────────────────────────────────────────────────────┐
│   ManifestEmbeddings   {ruta}/Memoria/embeddings.manifest.json   │
│                                                                  │
│   {                                                              │
│     "Proveedor": "Ollama",                                       │
│     "Modelo":    "nomic-embed-text",                             │
│     "Dimension": 768,                                            │
│     "HashPorFuente": {                                           │
│        "Hechos":    "a3f5c9...",                                 │
│        "Episodios": "d7e8b1..."                                  │
│     },                                                           │
│     "UltimaIndexacion": "2026-04-19T12:34:56Z"                   │
│   }                                                              │
└──────────────────────────────────────────────────────────────────┘

   SHA1 del archivo vs SHA1 del manifest
         │
   ┌─────┴─────┐
   │ igual?    │
   └─┬───────┬─┘
  SÍ │       │ NO
     ▼       ▼
   skip    borra chunks de esa fuente
           chunkea + batch embed + upsert
           actualiza manifest
```

#### UI de control — `FrmEmbeddings`

Botón 🧬 en el menú lateral. Cuatro tarjetas:

1. **Proveedor y modelo** — radios Ollama/OpenAI, endpoint, API key (masked).
2. **Parámetros de recuperación** — TopK, ChunkSize, ChunkOverlap.
3. **Acciones** — Probar conexión · Guardar · Re-indexar · Limpiar índice.
4. **Estado del índice** — chunks totales, por fuente, proveedor/modelo/dim, última indexación.

#### Activación paso a paso

```
1. ⚙ Habilidades  → activar 🧠 memoria
                  → activar 🧬 memoria_semantica
2. 🧬 Embeddings  → elegir proveedor → Probar conexión → Guardar
                  → Re-indexar
3. 📊 Tokens      → ejecutar una instrucción
                  → observar ahorro en la fase Constructor
```

Si apagas `memoria_semantica`, el agente vuelve automáticamente al dump completo sin necesidad de reiniciar.

#### Ahorro medido

- Memoria de 50 hechos + 200 episodios: dump ≈ 4 200 tokens → RAG (top-5) ≈ 400 tokens. **Ahorro: ~90 %**.
- Costo de los embeddings con OpenAI: ~$0.00002 por instrucción (despreciable vs. el ahorro del Constructor).
- Con Ollama: **costo cero** y latencia de 50-150 ms adicionales.

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

<a id="sistema-de-comandos-cmd"></a>
## Sistema de Comandos `#cmd` — Configuración desde el Chat

OPENGIOAI incluye un **sistema profesional de comandos en línea** inspirado en Slack, Discord y Claude Code: cualquier mensaje que empiece con `#` se interpreta como una orden de configuración. El usuario puede cambiar el agente activo, alternar canales, ajustar timeouts, gestionar habilidades cognitivas o configurar el TTS **sin tocar la UI** — directamente desde Telegram, Slack o el chat de escritorio.

El módulo está construido con un patrón **Registry + Executor + Formatter** (separación parser → registry → ejecución → formateo por canal) y cuenta con **67 tests automatizados** que cubren parser, registry, executor y handlers.

### Pipeline de despacho

```
Texto del usuario  ("#audio proveedor OpenAI")
        │
        ▼
┌───────────────────┐
│   CommandParser   │  Tokeniza con respeto de comillas dobles.
│                   │  Soporta legacy `#CMD_VALOR` (callback Telegram).
│                   │  Solo lowercase del nombre — args preservan caso.
└─────────┬─────────┘
          ▼
┌───────────────────┐
│  CommandRegistry  │  Diccionario de nombre → handler.
│                   │  Si no existe: Levenshtein ≤ 2 → sugerir alternativas.
│                   │  Resuelve también por alias (`#tts` → audio).
└─────────┬─────────┘
          ▼
┌───────────────────┐
│ CommandExecutor   │  Construye CommandContext (args + servicios).
│                   │  Invoca handler.EjecutarAsync(ctx).
│                   │  Captura excepciones → CommandResult.Error.
└─────────┬─────────┘
          ▼
┌───────────────────┐
│ ResultFormatter   │  Renderiza según el canal:
│                   │   · Telegram → MarkdownV2 con escape
│                   │   · Slack    → mrkdwn
│                   │   · UI       → texto plano + emoji
└─────────┬─────────┘
          ▼
   Mensaje final al canal de origen
```

### Formato de invocación

Tres formas válidas, todas equivalentes según el handler:

```
#audio proveedor OpenAI         ← moderno, tokens separados por espacio
#audio voz "nova premium"       ← comillas dobles preservan espacios
#CAMBIAR_AGENTE_ARIA            ← legacy, soportado por callback_data de Telegram
```

El parser **no** uppercasea los argumentos — esto es crítico para API keys, voces, IDs de modelo y rutas con mayúsculas/minúsculas significativas.

### Catálogo completo de comandos

#### 🤖 Agente

| Comando | Alias | Uso | Descripción |
|---------|-------|-----|-------------|
| `#agente` | `#agentes` | `#agente` | Muestra el agente activo y la lista de agentes disponibles. |
| `#cambiar_agente` | — | `#cambiar_agente <nombre>` | Cambia el agente activo (acepta `#CAMBIAR_AGENTE_NOMBRE` legacy). |
| `#modelo` | `#modelos` | `#modelo` | Muestra el modelo activo y los modelos del proveedor. |
| `#cambiar_modelo` | — | `#cambiar_modelo <id>` | Cambia el modelo del proveedor actual. |
| `#ruta` | `#rutas` | `#ruta` | Muestra la ruta de trabajo actual. |
| `#cambiar_ruta` | — | `#cambiar_ruta <path>` | Cambia el directorio de trabajo (con comillas si tiene espacios). |

#### ⚙ Configuración

| Comando | Alias | Uso | Descripción |
|---------|-------|-----|-------------|
| `#configuraciones` | `#config`, `#ajustes` | `#configuraciones` | Resumen de todas las configuraciones activas. |
| `#reintentos` | `#retry` | `#reintentos <0..5>` | Número de reintentos del Guardián (0 = desactivado). |
| `#timeout` | — | `#timeout <10..1800>` | Timeout por petición LLM en segundos. |
| `#solochat` | `#chat` | `#solochat on\|off` | Activa modo conversacional puro (sin pipeline). |
| `#recordar` | `#memoria` | `#recordar on\|off` | Activa/desactiva memoria del tema actual. |
| `#apis` | — | `#apis` | Lista las credenciales registradas (sólo nombres). |
| `#cancelar` | `#stop`, `#cancel` | `#cancelar` | Cancela la operación en curso. |

#### 📡 Integración

| Comando | Alias | Uso | Descripción |
|---------|-------|-----|-------------|
| `#telegram` | `#tg` | `#telegram on\|off` | Activa/desactiva el bot de Telegram. |
| `#slack` | — | `#slack on\|off` | Activa/desactiva el bot de Slack. |
| `#audio` | `#tts`, `#voz` | `#audio [sub] [valor]` | Configura TTS — ver subcomandos abajo. |

**Subcomandos de `#audio`:**

```
#audio                          → estado actual (proveedor, voz, idioma)
#audio on / #audio off          → toggle de envío de audio
#audio proveedor SystemSpeech|OpenAI|Google
#audio voz <nombre>             → ej. "nova", "es-MX-DaliaNeural"
#audio idioma <bcp-47>          → ej. "es-MX", "en-US"
#audio apikey <key>             → API key del proveedor TTS (no se muestra)
#audio activar / #audio desactivar  → marca TTS como configurado
```

#### 🧩 Habilidades

| Comando | Alias | Uso | Descripción |
|---------|-------|-----|-------------|
| `#habilidad` | `#hab`, `#habilidades` | `#habilidad <clave> on\|off` | Activa/desactiva una habilidad cognitiva. |

Sin args lista todas las habilidades con su estado. Soporta claves: `memoria`, `patrones`, `memoria_semantica`.

#### 📊 Estado

| Comando | Alias | Uso | Descripción |
|---------|-------|-----|-------------|
| `#estado` | `#status`, `#info` | `#estado` | Snapshot global: agente, modelo, ruta, toggles, habilidades. |

#### ❓ Ayuda

| Comando | Alias | Uso | Descripción |
|---------|-------|-----|-------------|
| `#ayuda` | `#help`, `#?` | `#ayuda [categoría\|comando]` | Lista comandos por categoría o detalle de uno específico. |

Ejemplos: `#ayuda` (todas), `#ayuda audio` (detalle de un comando), `#ayuda integracion` (filtro por categoría).

### Sugerencia inteligente de typos

Si el usuario escribe un comando inexistente, el registry calcula la **distancia de Levenshtein** contra todos los nombres y alias registrados, devolviendo hasta 3 candidatos con distancia ≤ 2:

```
Usuario:  #agnte
Bot:      ❓ Comando `agnte` no encontrado.
          ¿Quisiste decir? `agente`, `agentes`
```

### Resultado tipado por handler

Cada handler devuelve un `CommandResult` con tipo (`Exito`, `Error`, `Advertencia`, `Info`), título, mensaje y opcional lista de detalles. El `ResultFormatter` adapta el render según el canal de origen — el mismo handler funciona idéntico en Telegram, Slack o UI.

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
        Ejemplos    = new[] { "#miorden 42" },
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

Y registrarlo en `FrmMandos.ConfigurarCommandRouter()`:

```csharp
_cmdRegistry.Registrar(new MiComando());
```

Listo — aparece automáticamente en `#ayuda`, en la categoría correspondiente y con sugerencia de typos.

### UI-thread safety

Los comandos pueden disparar desde un mensaje de Telegram, Slack o la UI. La fachada `IServiciosComandos` (implementada como `partial class` de `FrmMandos` en `Vistas/FrmMandos.Comandos.cs`) marshallea automáticamente al hilo UI mediante `EnUIThread()` antes de tocar controles WinForms.

### Cobertura de tests

`Tests/ComandosTests/Program.cs` ejecuta **67 tests** sin dependencia externa (usa `FakeServicios` in-memory):

| Bloque | Tests | Cobertura |
|--------|-------|-----------|
| Parser  (P1–P5) | 24 | Tokenización, comillas, legacy `#CMD_VALOR`, mayúsculas, vacíos |
| Registry (R1–R3) | 11 | Registro, alias, sugerencias Levenshtein |
| Executor (E1–E4) | 14 | Despacho OK, no encontrado, sugerencias, captura de excepciones |
| Handlers (H1–H5) | 18 | Casos clave de los 19 comandos visibles |
| **Total** | **67** | **67/67 PASS** |

Ejecutar:

```bash
dotnet run --project Tests/ComandosTests
```

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
# Embeddings (puede contener datos sensibles de la memoria)
Memoria/embeddings.jsonl
```

---

## Multi-Proveedor de LLMs

| Proveedor | Modelos | Autenticación | Embeddings |
|-----------|---------|---------------|------------|
| **OpenAI** | GPT-3.5 Turbo, GPT-4, GPT-4 Turbo, GPT-4o / 4o-mini | API Key (Bearer) | text-embedding-3-small/large |
| **Anthropic Claude** | Claude 3 Haiku / Sonnet / Opus, Claude Sonnet/Opus 4.5 | API Key (x-api-key) | — |
| **Google Gemini** | Gemini 1.5 Flash/Pro, Gemini 2.0 Flash | API Key (querystring) | — |
| **DeepSeek** | DeepSeek Chat, DeepSeek Reasoner | API Key (Bearer) | — |
| **OpenRouter** | Todos los modelos disponibles | API Key (Bearer) | — |
| **Ollama** | Llama, Mistral, Phi, Gemma… | Sin clave (local) | nomic-embed-text, mxbai-embed-large |
| **Google Vertex AI** | Gemini en Vertex | gcloud ADC (`Antigravity`) | — |

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

### HttpClient singletons

Para evitar socket exhaustion en cargas altas (típico de polling de Telegram + UI + automatizaciones concurrentes), cada proveedor tiene su `HttpClient` singleton reutilizado. Lo mismo aplica al `EmbeddingsService`.

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
- `FrmMemoria` — editor directo de `Hechos.md` / `Episodios.md`
- `FrmHabilidades` — toggles de capacidades cognitivas
- `FrmPatrones` — detección de patrones recurrentes → skills propuestas
- `FrmConsumoTokens` — panel flotante de telemetría (siempre visible sobre otros forms)
- `FrmEmbeddings` — configuración y operación del RAG local

---

## Como Empezar

### Requisitos

- **.NET 10 Runtime** — [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Windows 10+**
- **Python 3.8+** en el PATH del sistema
- **API Key** de al menos un proveedor LLM
- (Opcional) **Ollama** local para embeddings gratis: `ollama pull nomic-embed-text`

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
6. (Opcional) Abrir **📊 Tokens** → panel flotante que muestra consumo en vivo
7. (Opcional) Ir a **⚙ Habilidades** → activar `memoria` y/o `memoria_semantica`
8. (Opcional) Ir a **🧬 Embeddings** → configurar proveedor (Ollama/OpenAI) → Re-indexar
9. (Opcional) Desde el chat — Telegram, Slack o UI — escribir `#ayuda` para ver el catálogo de **comandos `#cmd`** y configurar el agente sin abrir formularios. Ejemplos rápidos: `#estado`, `#cambiar_agente ARIA`, `#timeout 60`, `#audio proveedor OpenAI`.

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
├── Memoria/                  ← Fase 1 — memoria durable
│   ├── Hechos.md             ← Verdades sobre el usuario (editable)
│   ├── Episodios.md          ← Timeline append-only
│   ├── embeddings.jsonl      ← VectorStore (Fase C — autogenerado)
│   └── embeddings.manifest.json  ← Hashes SHA1 por fuente
└── Automatizaciones/
    └── {GUID}/
        ├── nodo_01_*.py
        └── messages.json

[DirectorioBinario]/
├── ListApis.json             ← API keys (NO subir a git)
├── Configuracion.json        ← Configuración general
├── ListModelos.json          ← Modelos por proveedor
├── ListAutomatizaciones.json ← Automatizaciones guardadas
├── ListHabilidades.json      ← Estado de habilidades (Fase B)
├── ListPreciosModelos.json   ← Tarifas USD por modelo (Fase A)
└── EmbeddingsConfig.json     ← Config RAG (Fase C)
```

---

## Extension y Desarrollo

### Agregar un nuevo proveedor LLM

1. Añadir valor al enum `Servicios.cs`
2. Extender `ConstruirRequest()` en `AIModelConector.cs` con endpoint y body
3. Extender `AgregarHeaders()` con la autenticación del provider
4. Extender `ExtraerContenido()` con el path JSON de la respuesta
5. Extender `TokenUsageReader` si el proveedor expone `usage` de forma distinta
6. (Opcional) Extender `ObtenerRespuestaStreamingAsync()` si soporta SSE
7. Añadir tarifas en `PreciosModelos.Defaults()` para que aparezca en 📊 Tokens

### Crear una Skill personalizada

Crear un archivo `.md` en el directorio `Skills/` del directorio de trabajo siguiendo el formato de skill (ver [Sistema de Skills](#sistema-de-skills-y-skills-hub)). Al reiniciar o recargar skills, el agente la detecta automáticamente y la incluye en su manifiesto.

### Agregar un nuevo tipo de nodo de automatización

1. Añadir valor al enum `TipoNodo` en `NodoAutomatizacion.cs`
2. Implementar la lógica de evaluación en `FrmAutomatizaciones.cs`
3. Agregar el ícono y propiedades en el panel de configuración del nodo

### Agregar una nueva Habilidad cognitiva

1. Añadir constante `HAB_XXX` en `HabilidadesRegistry.cs`
2. Añadir entrada a `Defaults()` con icono + descripción + impacto estimado
3. Consultar con `HabilidadesRegistry.Instancia.EstaActiva(HAB_XXX)` en el hot-path

### Crear un nuevo preset de PerfilContexto

En `Entidades/PerfilContexto.cs` añadir una propiedad estática:

```csharp
public static PerfilContexto MiPreset => new()
{
    LeerPromptMaestroDeDisco = true,
    IncluirPromptMaestro     = true,
    IncluirMemoria           = true,
    // resto en false por defecto
};
```

Luego pasarlo a `AgentContext.BuildAsync(..., perfil: PerfilContexto.MiPreset)`.

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
| 📊 Tokens no muestra nada | Tracker no correlacionó | Verificar que la fase llama a `ctx.ComoFase("Nombre")` antes de enviar al LLM |
| RAG no devuelve nada relevante | Índice vacío o stale | Abrir 🧬 Embeddings → Re-indexar; verificar que Hechos/Episodios tienen contenido |
| RAG rompe sin Ollama | Habilidad ON pero servicio caído | MemoriaSemantica hace fallback a dump completo automáticamente — revisar panel `📊` para confirmar |
| Cambié de embedding y todo falla | Espacio vectorial incompatible | El MemoriaIndexer detecta el cambio y rebuildea automáticamente; si no, 🧬 → Limpiar índice → Re-indexar |
| Costo USD reportado = $0 | Modelo no registrado en PreciosModelos | Editar `ListPreciosModelos.json` o añadir al `Defaults()` del código |

---

## Dependencias

```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
<PackageReference Include="SlackAPI" Version="1.1.14" />
<PackageReference Include="System.Management" Version="10.0.3" />
<PackageReference Include="Telegram.Bot" Version="22.9.0" />
```

---

## Roadmap — lo entregado y lo que viene

### ✅ Entregado

- [x] Pipeline ARIA de 5 fases con Memorista asíncrono
- [x] Sistema de memoria durable en Markdown (`Hechos.md` + `Episodios.md`)
- [x] Detección de patrones recurrentes con propuesta de skills
- [x] Habilidades cognitivas opt-in con registry persistente
- [x] **Fase A — Telemetría de tokens** con desglose por fase y costo USD
- [x] **Fase B — Context slicing** declarativo por fase (PerfilContexto)
- [x] **Fase C — RAG local** con Ollama / OpenAI, indexación incremental y fallback
- [x] Multi-proveedor LLM (OpenAI · Claude · Gemini · DeepSeek · OpenRouter · Ollama · Vertex)
- [x] Aislamiento de proceso Python + credenciales nunca expuestas al agente
- [x] Streaming SSE con cancelación del usuario
- [x] Automatizaciones visuales con nodos conectables
- [x] Telegram + Slack multi-canal
- [x] **Sistema de comandos `#cmd`** profesional (parser + registry + executor + formatter, 19 comandos visibles, typo-suggest Levenshtein, 67/67 tests PASS)

### 🚧 En progreso / próximas fases

- [ ] **Fase D.1** — RAG para Skills (top-K skills relevantes en vez del manifiesto completo)
- [ ] **Fase D.2** — Provider prompt caching (OpenAI automático + Anthropic `cache_control`)
- [ ] Dashboard histórico de tokens con gráficas y comparativas por modelo
- [ ] Cifrado AES-256 para `ListApis.json`
- [ ] Historial persistente de conversaciones con búsqueda semántica (reutiliza VectorStore)
- [ ] Integración con Discord
- [ ] Fine-tuning de modelos locales (Ollama)
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

Cualquier PR que toque el **pipeline ARIA**, el **sistema de memoria** o los **subsistemas de telemetría/slicing/RAG** debe incluir:

- Actualización del README en la sección correspondiente
- Prueba manual documentada (instrucción de ejemplo + resultado esperado)
- Compilación limpia (`0 Errores` — las 170+ advertencias heredadas están bajo tracking)

---

## Disclaimer

OPENGIOAI ejecuta código Python generado dinámicamente por LLMs. El proceso de ejecución está aislado, pero siempre es recomendable:

- Revisar `script_ia.py` antes de ejecutar en sistemas críticos
- No ejecutar scripts en entornos de producción sin validación previa
- Mantener `ListApis.json` fuera del control de versiones
- Considerar que `Memoria/embeddings.jsonl` contiene fragmentos literales de tu memoria — trátalo como datos sensibles

---

**Hecho en C# y .NET 10 · Arquitectura token-aware · RAG local · Memoria durable**
