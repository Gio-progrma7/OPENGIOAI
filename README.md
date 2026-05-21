# 🌟 OPENGIOAI

<div align="center">
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/.NET%2010-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 10" />
  <img src="https://img.shields.io/badge/Windows%20Forms-0078D4?style=for-the-badge&logo=windows&logoColor=white" alt="Windows Forms" />
  <img src="https://img.shields.io/badge/Python%203.8+-3776AB?style=for-the-badge&logo=python&logoColor=white" alt="Python" />
  <img src="https://img.shields.io/badge/RAG-Semántico-10b981?style=for-the-badge" alt="RAG" />
  <img src="https://img.shields.io/badge/License-MIT-blue?style=for-the-badge" alt="License" />
</div>

<br/>

> **Tu asistente de Inteligencia Artificial personal, local y de clase mundial.**  
> Diseñado para recordar quién eres, ahorrarte dinero en cada instrucción, automatizar tus tareas aburridas y conectarse con cualquier aplicación externa a través de su servidor embebido.

---

## 🚀 ¿Qué es OPENGIOAI?

Imagina tener a un empleado incansable viviendo en tu computadora. **OPENGIOAI** no es un simple chat. Es una **plataforma completa de Inteligencia Artificial** para tu escritorio que puede:

- **Escribir y ejecutar código real** para resolver tus problemas.
- **Aprender de ti**: Tiene una memoria duradera. Si le dices cómo te gusta que haga las cosas, no tendrás que repetírselo mañana.
- **Automatizar tu trabajo**: Conecta cajas y flechas visualmente para crear rutinas que se ejecuten solas todos los días.
- **Hablar contigo donde estés**: Conéctalo a **Telegram** o **Slack**, o haz que te hable con voz mediante **TTS (Text-to-Speech)**.
- **Conectarse con tus otros programas**: Gracias a su servidor local **ARNES**, otras aplicaciones (como Visual Studio Code, tus propios scripts, etc.) pueden usar la inteligencia de OPENGIOAI de forma silenciosa.

---

## ✨ Características Estrella y Módulos Principales

### 🧠 Memoria Real (RAG Local) y Agente Memorista
OPENGIOAI tiene dos archivos mágicos: **Hechos** (lo que sabe de ti) y **Episodios** (lo que ha hecho antes). Cuando le preguntas algo, no lee todo el archivo (lo cual sería muy caro), sino que usa **Inteligencia Artificial semántica (RAG)** para recuperar exactamente el recuerdo que necesita en milisegundos. Además, cuenta con un **Agente Memorista** que corre en segundo plano (*fire-and-forget*), deduplicando y guardando nueva información sin añadir latencia a tus respuestas.

### 🗂️ Soporte Multi-Workspaces (Aislamiento de Contexto)
La arquitectura central utiliza **AgentContext**, un objeto inmutable que elimina las colisiones de estado global. Esto te permite tener múltiples espacios de trabajo (*Workspaces*), cada uno con su propio historial, credenciales y configuración. Puedes tener un agente trabajando en un proyecto de programación y otro atendiendo Telegram al mismo tiempo, sin que mezclen su memoria.

### 💰 Ahorro Inteligente de Tokens y Prompt Caching
¿Cansado de facturas altas de API? Nuestro motor corta dinámicamente el contexto separándolo en **Prompt Estable** (reglas cacheadas) y **Prompt Variable** (contexto dinámico). Si le preguntas el clima, no envía tu memoria completa ni tus habilidades de programación al modelo. **Ahorrarás entre un 40% y un 70%** en tu consumo (hasta 90% con caché en Anthropic), visible gracias a nuestra **Telemetría en Vivo**.

### 🤖 Multi-Proveedores de IA
Tú eliges el "cerebro". Desde la configuración puedes intercambiar libremente entre los mejores modelos del mercado:
- **OpenAI** (GPT-4o, etc.)
- **Anthropic** (Claude 3.5 Sonnet, etc.)
- **Google** (Gemini)
- **DeepSeek**
- **Ollama**: ¡Para ejecutar modelos gratuitos 100% locales en tu propia PC sin pagar API!

### 📝 Sistema de Prompts Personalizables
El comportamiento del agente es completamente moldeable. OPENGIOAI utiliza un sistema de **Prompts Maestros** y **Prompts de Error**. Desde la interfaz gráfica, puedes editar y heredar estos prompts para cambiar la personalidad, las reglas de formateo o la manera en que el Guardián autocorrige los fallos.

### 🛠️ Sistema de Skills (Habilidades)
OPENGIOAI puede aprender nuevos trucos instalando **Skills**. Estas habilidades son archivos `.md` que contienen código Python embebido. El sistema escanea tu *workspace*, descubre estas Skills y las convierte automáticamente en funciones nativas (**Tool Calling**) que el agente puede invocar cuando las necesite para resolver tareas específicas.

### 🧩 Flujos Visuales (Nodos y Automatizaciones)
No necesitas saber programar para crear verdaderos asistentes autónomos. OPENGIOAI incluye un **Editor de Automatizaciones (DAG)** con un lienzo interactivo donde puedes conectar nodos arrastrando y soltando:
- **🟢 Disparador**: ¿Cuándo inicia? (Manual, diario, cada 30 min, fecha única).
- **🟡 Condición**: Lógica condicional impulsada por IA (IF/ELSE).
- **🔵 Acción**: Ejecuta una instrucción, envía notificaciones, etc.
- **🔴 Fin**: Termina el flujo.

Puedes programar automatizaciones para que corran silenciosamente en segundo plano, ¡incluso si la ventana de la app está cerrada!

### 🔌 Módulo ARNES (Tu API Local) y Seguridad
OPENGIOAI actúa como un servidor invisible en tu computadora (puerto 5050). Otras aplicaciones pueden pedirle favores a tu IA de forma *headless* (sin interfaz).
Todo esto está protegido por el **ArnesSecurityManager**, que se encarga de:
- Generar **Llaves de Seguridad** locales (`sk-arnes-...`).
- Autenticar y auditar qué aplicación externa se conecta.
- Permitir la revocación instantánea de tokens desde el panel de control.

### 📡 Canales de Comunicación y Audio
Tu asistente no está atrapado en la computadora. Puedes interactuar con él mediante:
- **Telegram y Slack**: Responde a tus mensajes desde tu celular.
- **Voz (TTS)**: Respuestas leídas en voz alta de manera natural mediante integración de *Text-To-Speech*.

---

## 💻 Bajo el Capó (Para Programadores)

Si eres desarrollador, te enamorarás de la arquitectura interna. 
OPENGIOAI está construido de manera robusta usando **C# Windows Forms** (.NET 10). Cuenta con **doble buffer** para eliminar cualquier parpadeo de la interfaz, un sistema de temas dinámicos (`EmeraldTheme`) para modo claro/oscuro, y una arquitectura altamente desacoplada basada en inyección de dependencias y aislamiento de contexto.

### El Motor ARIA (Arquitectura Multi-Fase)
Cada instrucción del usuario no va directamente al LLM y ya. Pasa por un pipeline llamado **ARIA**, diseñado para ser autónomo y tolerante a fallos:

```text
     [Instrucción del Usuario]
                 │
                 ▼
          ┌─────────────┐
          │ 🕵️ Analista │ (Plan rápido en streaming)
          └──────┬──────┘
                 │
                 ▼
        ┌─────────────────┐
   ┌───►│ 🏗️ Constructor │ (Genera script Python + Contexto)
   │    └────────┬────────┘
   │             │
   │             ▼
   │    [Ejecución Local]
   │             │
   │             ▼
   │       {🛡️ Guardián} ────(Si falla)────┐
   │             │                         │
   └─────────────┘                         │
            (Si hay Éxito)                 │
                 │                         │
                 ▼                         │
        ┌─────────────────┐                │
        │ 🗣️ Comunicador │ ◄──────────────┘
        └────────┬────────┘
                 │
                 ▼
         [Respuesta Final]
```

1. 🕵️ **Analista**: Entiende lo que pides y te dice qué va a hacer.
2. 🏗️ **Constructor**: Usa el contexto (Prompt + RAG + Skills) para generar un script de Python real. El script se ejecuta en un proceso aislado en tu máquina local.
3. 🛡️ **Guardián**: Analiza la salida de consola de Python. ¿Hubo un error de sintaxis? ¿Faltó instalar una librería? El Guardián inyecta el error de vuelta al LLM y lo auto-corrige hasta 3 veces sin que tú intervengas.
4. 🗣️ **Comunicador**: Lee el resultado exitoso y te lo explica de forma humana, usando streaming en tiempo real.
5. 🧠 **Memorista**: (Background) Extrae y consolida recuerdos de la conversación.

### Comandos `#cmd`
Controla toda la app desde la caja de chat o desde Telegram. Escribe `#ayuda` para ver la magia. ¡Incluye corrección de errores tipográficos (distancia de Levenshtein) si escribes mal un comando!

---

## 🛠️ ¿Cómo Empezar? (Para Usuarios)

### Requisitos Mínimos
1. Windows 10 o superior.
2. Instalar [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0).
3. Instalar [Python 3.8+](https://www.python.org/downloads/) (marca la casilla "Add to PATH" al instalar).

### Instalación Rápida
1. Descarga el proyecto y descomprímelo.
2. Abre la consola en esa carpeta y ejecuta:
   ```bash
   dotnet run --project OPENGIOAI
   ```
3. ¡Listo! En la interfaz, ve a **Configuración > Proveedores**, pon tu API Key de tu proveedor favorito (ej. OpenAI, Gemini) o elige Ollama, y empieza a chatear.

---

## 🤝 Roadmap y Contribución

OPENGIOAI es un proyecto vivo y ambicioso. 

✅ **Entregado**: 
- Pipeline ARIA autocorrector.
- Memoria RAG Semántica y Multi-Workspaces.
- Flujos Visuales DAG y Servidor ARNES.
- Integración nativa Telegram/Slack y TTS.
- Soporte para múltiples proveedores (OpenAI, Anthropic, Gemini, DeepSeek, Ollama).

🚧 **Próximos pasos**:
- Búsqueda semántica dentro del historial de conversaciones.
- Integración nativa con Discord.
- Mercado de Skills (Skills Hub Visual).

**¡Tu ayuda es bienvenida!** Si quieres contribuir, haz un Fork, crea tu rama `feature/mi-idea`, asegúrate de que compile (`dotnet build`) y abre un Pull Request. 

---

<div align="center">
  <b>Hecho con pasión en C# · Arquitectura Token-Aware · RAG Semántico · Tema Emerald</b><br>
  <i>"No es magia, es código bien pensado."</i><br><br>
  <b>Autor: Giovanni Sanchez</b> — <a href="https://github.com/Gio-progrma7">GitHub @Gio-progrma7</a>
</div>
