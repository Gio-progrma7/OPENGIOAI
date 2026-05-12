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
- **Hablar contigo donde estés**: Conéctalo a Telegram o Slack, o haz que te hable con voz (TTS).
- **Conectarse con tus otros programas**: Gracias a su servidor local **ARNES**, otras aplicaciones (como Visual Studio Code, tus propios scripts, etc.) pueden usar la inteligencia de OPENGIOAI de forma silenciosa.

¿Lo mejor? Tú eliges el "cerebro". Puedes usar **OpenAI (ChatGPT), Anthropic (Claude), Google (Gemini), DeepSeek**, o incluso modelos gratuitos y locales instalados en tu PC usando **Ollama**.

---

## ✨ Características Estrella

### 🧠 Memoria Real (RAG Local)
OPENGIOAI tiene dos archivos mágicos: **Hechos** (lo que sabe de ti) y **Episodios** (lo que ha hecho antes). Cuando le preguntas algo, no lee todo el archivo (lo cual sería muy caro), sino que usa **Inteligencia Artificial semántica** para recuperar exactamente el recuerdo que necesita en milisegundos.

### 💰 Ahorro Inteligente de Tokens
¿Cansado de facturas altas de API? Nuestro motor corta dinámicamente el contexto. Si le preguntas el clima, no envía tu memoria completa ni tus habilidades de programación al modelo. **Ahorrarás entre un 40% y un 70%** en tu consumo, y podrás ver exactamente cuánto gastaste en centavos de dólar gracias a nuestra **Telemetría en Vivo**.

### 🧩 Flujos Visuales (Nodos)
No necesitas saber programar para crear verdaderos asistentes autónomos. OPENGIOAI incluye un **Editor de Automatizaciones (DAG)** con un lienzo interactivo donde puedes conectar nodos arrastrando y soltando:

- **🟢 Disparador**: ¿Cuándo inicia? (Manual, diario, cada 30 min, fecha única).
- **🟡 Condición**: Si ocurre algo, toma el camino A o el camino B.
- **🔵 Acción**: Ejecuta una instrucción usando IA.
- **🔴 Fin**: Termina el flujo.

Puedes programar automatizaciones para que corran silenciosamente en segundo plano, ¡incluso si la ventana principal está cerrada!

```text
       [🟢 Disparador: 08:00 AM]
                  │
                  ▼
   [🔵 Acción: Leer bandeja de correo]
                  │
                  ▼
     {🟡 Condición: ¿Hay urgentes?}
            │               │
        SÍ  │               │ NO
            ▼               ▼
  [🔵 Enviar Telegram]   [🔴 Fin]
            │               │
            └──────►◄───────┘
```

### 🔌 Módulo ARNES (Tu API Local)
OPENGIOAI actúa como un servidor invisible en tu computadora (puerto 5050). Puedes generar **Llaves de Seguridad (API Keys)** locales y permitir que otras aplicaciones le pidan favores a tu Inteligencia Artificial, todo auditado desde un panel de control profesional.

---

## 🛠️ ¿Cómo Empezar? (Para Usuarios)

### Requisitos Mínimos
1. Windows 10 o superior.
2. Instalar [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0).
3. Instalar [Python 3.8+](https://www.python.org/downloads/) (marca la casilla "Add to PATH" al instalar).
4. Una API Key (clave secreta) de tu proveedor favorito (ej. OpenAI, Gemini) o usar Ollama si no quieres pagar nada.

### Instalación Rápida
1. Descarga el proyecto y descomprímelo.
2. Abre la consola en esa carpeta y ejecuta:
   ```bash
   dotnet run --project OPENGIOAI
   ```
3. ¡Listo! En la interfaz, ve a **Configuración > Proveedores**, pon tu API Key, elige un modelo y empieza a chatear.

---

## 💻 Bajo el Capó (Para Programadores)

Si eres desarrollador, te enamorarás de la arquitectura interna. OPENGIOAI está construido con C# Windows Forms, doble buffer para eliminar parpadeos, un tema oscuro/claro dinámico (`EmeraldTheme`) y una arquitectura altamente desacoplada.

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

1. 🕵️ **Analista**: Entiende lo que pides y te dice qué va a hacer (streaming rápido sin cargar memoria pesada).
2. 🏗️ **Constructor**: Usa el contexto máximo (Prompt + RAG + Skills) para generar un script de Python real. El script se ejecuta en un proceso aislado en tu máquina local.
3. 🛡️ **Guardián**: Analiza la salida de consola de Python. ¿Hubo un error de sintaxis? ¿Faltó instalar una librería? El Guardián inyecta el error de vuelta al LLM y lo auto-corrige hasta 3 veces sin que tú intervengas.
4. 🗣️ **Comunicador**: Lee el resultado final exitoso y te lo explica de forma humana, usando streaming de tokens (SSE) en tiempo real.

### El Servidor ARNES (Headless)
El proyecto incluye un servidor HTTP embebido (`ArnesServer`).  
Puedes consumir ARIA de forma programática. Genera una API Key desde el **Hub ARNES** en la interfaz y haz peticiones así:

```bash
curl -X POST http://localhost:5050/arnes/ \
  -H "Authorization: Bearer sk-arnes-tu_llave" \
  -H "Content-Type: application/json" \
  -d '{
    "source_app": "mi_script",
    "action": "execute_aria",
    "parameters": {
      "instruction": "Escribe un script de python que ordene mis descargas"
    }
  }'
```
El motor ARIA arrancará de forma *headless*, ejecutará las fases, usará Python localmente y te devolverá el resultado limpio.

### Sistema de Skills y Comandos (#cmd)
- **Skills**: Instala plugins en formato `.md` que contienen Python embebido. El agente las descubre y las convierte en `Tool Calling` para usarlas cuando las necesite.
- **Comandos `#cmd`**: Controla toda la app desde la caja de chat (o desde Telegram). Escribe `#ayuda` para ver la magia. ¡Incluso tiene corrección de errores (distancia de Levenshtein) si escribes mal un comando!

---

## 🤝 Roadmap y Contribución

OPENGIOAI es un proyecto vivo y ambicioso. 

✅ **Entregado**: 
- Pipeline ARIA autocorrector.
- Memoria RAG (Embeddings).
- Flujos Visuales DAG.
- Servidor ARNES e integraciones Headless.
- Integración nativa Telegram/Slack.

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
