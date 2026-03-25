# OPENGIOAI 🤖

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![.NET 9](https://img.shields.io/badge/.NET%209-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Windows Forms](https://img.shields.io/badge/Windows%20Forms-0078D4?style=for-the-badge&logo=windows&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-blue)

**OPENGIOAI** es una aplicación de escritorio moderna desarrollada en C# con .NET 9 que proporciona una interfaz unificada para interactuar con múltiples modelos de inteligencia artificial a través de diferentes proveedores. Diseñada para desarrolladores y profesionales que necesitan automatizar tareas con IA, ejecutar scripts dinámicos y gestionar agentes inteligentes.

---

## 🎯 Características Principales

### 🧠 Multi-Proveedor de LLMs
Integración transparente con los principales proveedores de inteligencia artificial:
- **OpenAI** — GPT-3.5 Turbo, GPT-4, GPT-4 Turbo
- **Google Gemini** — Gemini 1.5 Flash, Gemini Pro
- **Anthropic Claude** — Claude 3 Haiku, Claude 3 Sonnet, Claude 3 Opus
- **DeepSeek** — DeepSeek Chat, DeepSeek Code
- **OpenRouter** — Acceso a múltiples modelos a través de un solo endpoint
- **Ollama** — Modelos locales (sin límites de API)

### 🤖 Sistema de Agentes Inteligentes
- **Agente 1**: Generador de código Python — Transforma instrucciones en scripts ejecutables
- **Agente 2**: Validador y procesador de respuestas — Verifica, valida y mejora los resultados
- **Sistema modular**: Extensible para agregar nuevos agentes personalizados

### ⚙️ Ejecución de Scripts Dinámicos
- Generación automática de scripts Python basados en instrucciones en lenguaje natural
- Ejecución segura y monitorizada de procesos Python
- Captura de salida estándar y errores
- Gestión inteligente de procesos activos

### 📊 Integración Multi-Canal
- **Telegram** — Bot de Telegram integrado para acceso móvil
- **Slack** — Integración nativa con espacios de trabajo Slack
- **UI Desktop** — Interfaz gráfica moderna y responsiva

### 🔄 Streaming en Tiempo Real
- Respuestas de streaming para modelos compatibles
- Renderización token-a-token para experiencia de usuario mejorada
- Soporte para OpenAI, Claude, DeepSeek y Ollama

### 🛡️ Resiliencia y Confiabilidad
- **Retry Policy avanzado** — Reintentos automáticos con backoff exponencial y jitter
- Manejo inteligente de errores 429 (rate limiting) y 5xx (server errors)
- Recuperación automática de fallos transitorios de red
- Timeout configurable por proveedor

### 🎨 Interfaz de Usuario Premium
- Tema moderno y personalizable (Emerald Theme)
- Componentes visuales redondeados y pulidos
- Disposición fluida y responsiva
- Chat bubbles con soporte para emojis

### 🔐 Gestión de Configuración
- API keys seguros y configurables por proveedor
- Almacenamiento persistente de configuraciones
- Rutas de proyecto dinámicas
- Modelos seleccionables por servicio

---

## 🏗️ Arquitectura del Proyecto

### Estructura de Capas

```
OPENGIOAI/
├── Vistas/                 # Windows Forms (UI)
│   ├── FrmPrincipal.cs        # Ventana principal
│   ├── FrmMandos.cs           # Gestión de comandos
│   ├── FrmApis.cs             # Configuración de APIs
│   ├── FrmRutas.cs            # Gestión de rutas
│   ├── FrmModelos.cs          # Selección de modelos
│   ├── FrmAutomatizaciones.cs # Automatizaciones
│   ├── FrmMultipleAgent.cs    # Ejecución en paralelo
│   └── Skills.cs              # Gestión de skills
│
├── Data/                   # Capa de Datos y Lógica
│   ├── AIModelConector.cs      # ⭐ Orquestador principal de LLMs
│   ├── AgentContext.cs         # Contexto inmutable de ejecución
│   ├── RetryPolicy.cs          # Política de reintentos
│   └── ConversationWindow.cs   # Gestión de ventanas de conversación
│
├── ServiciosAI/            # Servicios de Inteligencia Artificial
│   ├── AIServicios.cs         # Operaciones comunes de IA
│   └── TokenUsageReader.cs    # Lectura de consumo de tokens
│
├── ServiciosTelegram/      # Integración Telegram
│   ├── TelegramListener.cs     # Listener de mensajes
│   ├── TelegramSender.cs       # Envío de mensajes
│   └── TelegramPollingService.cs
│
├── ServiciosSlack/         # Integración Slack
│   ├── SlackListener.cs        # Listener de eventos
│   ├── SlackService.cs         # Operaciones Slack
│   ├── SlackPollingService.cs  # Polling de mensajes
│   └── SlackChat.cs            # Modelos de chat Slack
│
├── Entidades/              # Modelos de Datos
│   ├── Servicios.cs            # Enum de proveedores
│   ├── Modelo.cs               # Definición de modelos
│   ├── Api.cs                  # Configuración de APIs
│   ├── ConfiguracionClient.cs  # Configuración general
│   ├── ConfiguracionIA.cs      # Configuración de IA
│   ├── ChatMessage.cs          # Mensajes de chat
│   ├── Skill.cs                # Definición de skills
│   ├── ConsumoTokens.cs        # Estadísticas de tokens
│   └── [Otros modelos...]
│
├── Promts/                 # Gestión de Prompts
│   └── PromtsBase.cs          # Base de prompts maestros
│
├── Themas/                 # Temas y Estilos UI
│   ├── EmeraldTheme.cs        # Tema principal
│   ├── ButtonRounder.cs       # Botones redondeados
│   ├── PanelRounder.cs        # Paneles redondeados
│   ├── BurbujaChat.cs         # Chat bubbles
│   └── [Otros componentes visuales...]
│
├── Utilerias/              # Funciones Auxiliares
│   ├── MarkdownFileManager.cs  # Gestión de archivos Markdown
│   ├── JsonManager.cs          # Manejo de JSON
│   ├── RutasProyecto.cs        # Gestión de rutas
│   └── Utils.cs                # Utilidades generales
│
└── Properties/             # Recursos y configuración
    └── Resources.resx         # Recursos de la aplicación
```

### Flujo de Ejecución Principal

```
Usuario Input (UI/Telegram/Slack)
        ↓
EjecutarInstruccionIAAsync() [AIModelConector]
        ↓
AgentContext.BuildAsync() → Construcción de contexto inmutable
        ↓
ObtenerRespuestaLLMAsync() → Enrutamiento al proveedor
        ↓
RetryPolicy.EjecutarAsync() → Reintentos automáticos
        ↓
[ObtenerRespuestaAPIAsync | ObtenerRespuestaOllamaAsync] → HTTP/API
        ↓
ExtraerContenido() → Parseo de respuesta
        ↓
GenerarScriptIA() → Ejecución Python
        ↓
Respuesta al Usuario
```

---

## 🚀 Cómo Empezar

### Requisitos Previos

- **.NET 9 Runtime** — [Descargar](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Windows 7+** con .NET Framework o Windows 10+
- **Python 3.8+** — Para ejecución de scripts dinámicos
- **API Keys** — De al menos un proveedor de LLM

### Instalación

1. **Clonar el repositorio**
   ```bash
   git clone https://github.com/Gio-progrma7/OPENGIOAI.git
   cd OPENGIOAI
   ```

2. **Restaurar dependencias**
   ```bash
   dotnet restore
   ```

3. **Compilar el proyecto**
   ```bash
   dotnet build
   ```

4. **Ejecutar la aplicación**
   ```bash
   dotnet run
   ```

### Configuración Inicial

1. **Abrir la aplicación** → Se abre `FrmPrincipal`
2. **Ir a "APIs"** → Tab de configuración de proveedores
3. **Agregar API Keys** — Para cada proveedor que desees usar:
   - OpenAI: [https://platform.openai.com/api-keys](https://platform.openai.com/api-keys)
   - Gemini: [https://makersuite.google.com/app/apikey](https://makersuite.google.com/app/apikey)
   - Claude: [https://console.anthropic.com/](https://console.anthropic.com/)
   - DeepSeek: [https://platform.deepseek.com/](https://platform.deepseek.com/)
   - OpenRouter: [https://openrouter.ai/](https://openrouter.ai/)
   - Ollama: [https://ollama.ai/](https://ollama.ai/) (local, sin key)

4. **Seleccionar modelo** → En "Modelos", elige tu proveedor y modelo predeterminado
5. **Configurar rutas** — Establece la ruta de trabajo para almacenar scripts

---

## 💡 Casos de Uso

### 1. Análisis de Datos Automatizado
```
Instrucción: "Analiza este archivo CSV y dame un resumen estadístico con visualizaciones"
↓
Agente 1 genera script Python con pandas + matplotlib
↓
Script ejecuta y devuelve gráficos
```

### 2. Automatización de Tareas Repetitivas
```
Instrucción: "Convierte todos los archivos PNG en la carpeta a JPG"
↓
Agente 1 genera script con Pillow
↓
Script procesa masivamente
```

### 3. Chat Inteligente Multicanal
```
Usuario en Telegram → Mensaje → Hook de TelegramListener
↓
AIModelConector → Streaming de respuesta
↓
Usuario recibe respuesta token-a-token
```

### 4. Procesamiento Paralelo
```
5 instrucciones simultáneamente
↓
EjecutarIAEnParaleloAsync()
↓
Task.WhenAll() coordina ejecuciones
↓
5 resultados sin bloqueo
```

### 5. Validación Inteligente
```
Script generado → Agente 2 (validador)
↓
Verifica errores, valida sintaxis
↓
Devuelve respuesta mejorada o correcciones
```

---

## 🔑 Componentes Clave

### AIModelConector.cs
**Orquestador central** de todas las operaciones de IA.

**Métodos principales:**
- `EjecutarInstruccionIAAsync()` — Punto de entrada Agente 1
- `EjecutarInstruccionIAAsyncRespuesta()` — Punto de entrada Agente 2
- `EjecutarIAEnParaleloAsync()` — Ejecución en paralelo
- `ObtenerRespuestaStreamingAsync()` — Streaming en tiempo real

**Características:**
- HttpClient singleton por proveedor (previene socket exhaustion)
- Retry Policy integrado con backoff exponencial
- Soporte para 6 proveedores de LLM
- Manejo de streaming por proveedor

### AgentContext.cs
**Contexto inmutable** que encapsula toda la información de una ejecución.

**Propiedades:**
- `PromptMaestro` — Prompt base del agente
- `PromptEfectivo` — Prompt calculado incluyendo clavesDisponibles
- `Modelo` — Modelo seleccionado
- `ApiKey` — API key del proveedor
- `Servicio` — Proveedor (enum)
- `SoloChat` — Modo chat puro sin ejecución

**Métodos:**
- `BuildAsync()` — Construcción asíncrona desde archivos .md
- `ComoAgente2()` — Variante para Agente 2
- `ComoInicio()` — Contexto para saludo inicial

### RetryPolicy.cs
**Política de reintentos** automática con algoritmo sofisticado.

**Características:**
- Backoff exponencial: 1s → 2s → 4s
- Jitter aleatorio (±20%) para evitar thundering herd
- Detección de errores reintentables (429, 5xx, timeouts)
- Máximo 4 intentos (3 reintentos)
- Integración con CancellationToken del usuario

**Errores reintentables:**
- `HttpRequestException` (red caída, DNS, timeout)
- Respuestas 429 (rate limit)
- Respuestas 5xx (server errors)
- TaskCanceledException (timeout interno, NO cancelación de usuario)

---

## 🔌 Uso Programático

### Ejemplo 1: Ejecución Básica

```csharp
var resultado = await AIModelConector.EjecutarInstruccionIAAsync(
    instruccion: "Dame 5 ideas para un proyecto en C#",
    modelo: "gpt-4",
    rutaArchivo: "C:\\Scripts",
    apiKey: "sk-...",
    servicio: Servicios.ChatGpt,
    soloChat: true 
);
```

### Ejemplo 2: Con Streaming

```csharp
var sb = new StringBuilder();

await AIModelConector.ObtenerRespuestaStreamingAsync(
    instruccion: "Explícame qué es la programación asíncrona",
    ctx: miContexto,
    onToken: token => 
    {
        sb.Append(token);
        Console.Write(token);  // Mostrar en tiempo real
    }
);

string respuestaCompleta = sb.ToString();
```

### Ejemplo 3: Ejecución Paralela

```csharp
var configuraciones = new List<ConfiguracionIA>
{
    new() { 
        Instruccion = "Dame 5 películas de ciencia ficción",
        Modelo = "gpt-4",
        Servicio = Servicios.ChatGpt,
        Chat = true
    },
    new() { 
        Instruccion = "Explícame la relatividad",
        Modelo = "claude-3-sonnet",
        Servicio = Servicios.Claude,
        Chat = true
    }
};

var resultados = await AIModelConector.EjecutarIAEnParaleloAsync(configuraciones);
// resultados[0] → "Películas de ciencia ficción..."
// resultados[1] → "La relatividad es..."
```

### Ejemplo 4: Generación de Script Ejecutable

```csharp
var codigo = await AIModelConector.EjecutarInstruccionIAAsync(
    instruccion: "Crea un script que procese 100 imágenes y las convierta a escala de grises",
    modelo: "gpt-4",
    rutaArchivo: "C:\\Proyecto\\Scripts",
    apiKey: "sk-...",
    servicio: Servicios.ChatGpt,
    soloChat: false  // Generar Y ejecutar script
);
// Script automáticamente guardado en C:\Proyecto\Scripts\script_ia.py
// Y ejecutado directamente
```

---

## 🔐 Gestión de Seguridad

### API Keys
- Almacenadas en `ConfiguracionClient` (considerar encriptación para producción)
- Se pasan por parámetro, nunca como estado global
- Por defecto, no se loguean

### Ejecución de Scripts Python
- Ejecutados en proceso separado (`Process.Start`)
- I/O redirigido hacia captura de salida
- Validación básica de contenido antes de ejecutar
- Se puede terminar el proceso si está en ejecución

### Recomendaciones Adicionales
- 🔒 Encriptar API keys en almacenamiento persistente
- 🔐 Usar variables de entorno para datos sensibles en CI/CD
- 🛡️ Validar/sanitizar prompts para evitar injection
- 📝 Loguear accesos y operaciones para auditoría

---

## 📊 Estadísticas y Monitoreo

### Consumo de Tokens
- Captura automática de uso de tokens por proveedor
- Disponible en `ConsumoTokens` entity
- Rastreo en tiempo real mediante `AIServicios.MostrarConsumoTokens()`

### Debugging
- Logs de retry en `Debug.WriteLine()` con tiempo de espera
- Información de intentos y excepciones
- Compatible con Visual Studio Output window

---

## 🛠️ Desarrollo y Extensión

### Agregar un Nuevo Proveedor de LLM

1. **Actualizar enum `Servicios.cs`**
   ```csharp
   public enum Servicios
   {
       // ... existentes ...
       MiProveedor = 7
   }
   ```

2. **Extender `ConstruirRequest()` en `AIModelConector.cs`**
   ```csharp
   Servicios.MiProveedor => (
       "https://api.miproveedor.com/v1/generate",
       (object)new { /* structure */ }
   )
   ```

3. **Extender `AgregarHeaders()`**
   ```csharp
   case Servicios.MiProveedor:
       req.Headers.Authorization = 
           new AuthenticationHeaderValue("Bearer", ctx.ApiKey);
       break;
   ```

4. **Extender `ExtraerContenido()`**
   ```csharp
   Servicios.MiProveedor =>
       root?["result"]?["text"]?.ToString() ?? ""
   ```

### Agregar un Nuevo Agente

1. Crear clase que implemente lógica de agente
2. Exponer método en `AIModelConector`
3. Integrar con UI en `FrmPrincipal`

### Crear Custom Skills

En `FrmSkills.cs`:
```csharp
var miSkill = new Skill
{
    Nombre = "Análisis de Sentimientos",
    Descripcion = "Analiza el sentimiento de un texto",
    Prompt = "Eres un experto en análisis de sentimientos...",
    // Guardar y reutilizar
};
```

---

## 🐛 Troubleshooting

| Problema | Solución |
|----------|----------|
| **"Socket exhaustion"** | Ya resuelto en PASO 2 con HttpClient singleton |
| **"Error 429 (rate limit)"** | RetryPolicy maneja automáticamente con backoff |
| **"Python script no ejecuta"** | Verificar que Python esté en PATH; usar ruta absoluta |
| **"Contexto corrupto"** | AgentContext es inmutable desde PASO 1 |
| **"API Key inválida"** | Verificar key en FrmApis → Configuración |
| **"Streaming muy lento"** | Reducir tamaño de tokens; considerar modelo más rápido |
| **Telegram/Slack desconectado | Verificar token de integración; logs en Debug output |

---

## 📦 Dependencias

```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
<PackageReference Include="SlackAPI" Version="1.1.14" />
<PackageReference Include="System.Management" Version="10.0.3" />
<PackageReference Include="Telegram.Bot" Version="22.9.0" />
```

- **Newtonsoft.Json** — Serialización/deserialización JSON
- **SlackAPI** — Integración con Slack
- **System.Management** — Gestión de procesos Windows
- **Telegram.Bot** — Bot SDK de Telegram

---

## 🔄 Ciclo de Vida de una Ejecución

```
1. CONSTRUCCIÓN DE CONTEXTO (PASO 1)
   ├─ Leer archivos .md base
   ├─ Construir PromptMaestro
   ├─ Computar PromptEfectivo
   └─ Crear AgentContext inmutable

2. ENRUTAMIENTO (capa LLM)
   └─ Determinar proveedor → Ollama vs Externo

3. RETRY POLICY (PASO 4)
   ├─ Intento 1: Llamada HTTP/API
   ├─ Si falla y es reintentable → Esperar backoff
   ├─ Intento 2, 3, 4...
   └─ Si todos fallan → Excepción

4. CONSTRUCCIÓN DE REQUEST
   ├─ Endpoint según proveedor
   ├─ Body según formato del proveedor
   └─ Headers específicos por proveedor

5. ENVÍO HTTP (PASO 2)
   ├─ HttpClient singleton reutilizado
   └─ Sin creación de nuevos clientes

6. PARSEO DE RESPUESTA
   ├─ Extraer contenido según proveedor
   ├─ Limpiar formatting (backticks, etc.)
   └─ Mostrar consumo de tokens

7. EJECUCIÓN DE SCRIPT (si aplica)
   ├─ Guardar script_ia.py
   ├─ Ejecutar en Process separado
   ├─ Capturar stdout/stderr
   └─ Devolver resultado

8. RESPUESTA AL USUARIO
   └─ A través de UI/Telegram/Slack
```

---

## 📈 Mejoras Futuras

- [ ] Almacenamiento persistente de conversaciones
- [ ] Fine-tuning de modelos locales
- [ ] Caché inteligente de respuestas frecuentes
- [ ] Dashboard de analytics
- [ ] Integración con Discord
- [ ] Encriptación de API keys
- [ ] Multi-idioma UI
- [ ] Historial de execuciones con rollback
- [ ] Debugging visual de prompts

---

## 👨‍💻 Autor

**Gio** — [GitHub](https://github.com/Gio-progrma7)

---

## 📄 Licencia

Este proyecto está bajo licencia **MIT**. Ver archivo `LICENSE` para detalles.

---

## 🤝 Contribuciones

Las contribuciones son bienvenidas! Por favor:

1. Fork el repositorio
2. Crea una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

---

## ⚠️ Disclaimer

OPENGIOAI ejecuta código Python generado dinámicamente por LLMs. Aunque se incluyen validaciones básicas:
- 🔍 Revisa siempre los scripts antes de ejecutarlos en producción
- ⚠️ No ejecutes scripts en sistemas críticos sin validación
- 🔐 Las API keys son sensibles — mantenlas seguras

---

## 📞 Soporte

Para problemas, sugerencias o preguntas:
- 📍 [Issues GitHub](https://github.com/Gio-progrma7/OPENGIOAI/issues)
- 💬 [Discussions GitHub](https://github.com/Gio-progrma7/OPENGIOAI/discussions)

---

## 🌟 Reconocimientos

Agradecimiento especial a:
- OpenAI, Google, Anthropic, DeepSeek y OpenRouter por sus APIs
- Comunidad de .NET por herramientas excelentes
- Contributores que han ayudado a mejorar el proyecto

---

**Hecho con ❤️ en C# y .NET 9**