// ============================================================
//  ComandosTests — suite mecánica para el módulo Comandos/
//
//  Verifica el contrato externo (parser, registry, executor) sin
//  tocar UI ni servicios reales. Usa un FakeServiciosComandos para
//  que los handlers se ejecuten en un entorno determinista.
//
//  CUBRE:
//    P1.  CommandParser: forma moderna `#cmd a b`
//    P2.  CommandParser: forma legacy `#CMD_VALOR`
//    P3.  CommandParser: comillas `"frase con espacios"`
//    P4.  CommandParser: case preservado en args, lower en nombre
//    P5.  CommandParser: texto sin `#` → null
//    R1.  Registry: lookup por canónico y alias
//    R2.  Registry: sugerencias por typo (Levenshtein ≤ 2)
//    R3.  Registry: TodosVisibles excluye ocultos
//    E1.  Executor: comando válido devuelve resultado
//    E2.  Executor: comando desconocido sugiere alternativa
//    E3.  Executor: texto sin `#` → EsComando=false
//    E4.  Executor: handler que lanza → CommandResult.Error
//    H1.  AyudaCommand: lista comandos visibles agrupados
//    H2.  TimeoutCommand: clamp y rechazo fuera de rango
//    H3.  ReintentosCommand: parse y rango 0–5
//    H4.  RecordarCommand: toggle on/off
//    H5.  AudioCommand: subcomandos de configuración persisten cfg
// ============================================================

using OPENGIOAI.Comandos;
using OPENGIOAI.Comandos.Handlers;
using OPENGIOAI.Entidades;
using OPENGIOAI.ServiciosTTS;
using OPENGIOAI.Utilerias;

namespace OPENGIOAI.Tests.ComandosTests
{
    internal static class Program
    {
        private static int _pass;
        private static int _fail;

        public static async Task<int> Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("================================================");
            Console.WriteLine(" Comandos — test harness");
            Console.WriteLine("================================================\n");

            P1_Parser_FormaModerna();
            P2_Parser_Legacy();
            P3_Parser_Comillas();
            P4_Parser_CasePreservado();
            P5_Parser_TextoSinHash();

            R1_Registry_LookupCanonicoYAlias();
            R2_Registry_Sugerencias();
            R3_Registry_TodosVisibles();

            await E1_Executor_ComandoValido();
            await E2_Executor_Desconocido_Sugiere();
            await E3_Executor_NoEsComando();
            await E4_Executor_HandlerLanza_DevuelveError();

            await H1_Ayuda_ListaVisibles();
            await H2_Timeout_RangoYClamp();
            await H3_Reintentos_Rango();
            await H4_Recordar_Toggle();
            await H5_Audio_PersisteConfig();

            Console.WriteLine("\n================================================");
            Console.WriteLine($"  PASS: {_pass}   FAIL: {_fail}");
            Console.WriteLine("================================================\n");

            return _fail == 0 ? 0 : 1;
        }

        // ── Helpers de aserción ─────────────────────────────────────────────

        private static void Asegurar(string nombre, bool ok, string? detalle = null)
        {
            if (ok)
            {
                Console.WriteLine($"  [PASS] {nombre}");
                _pass++;
            }
            else
            {
                Console.WriteLine($"  [FAIL] {nombre}{(detalle != null ? "  →  " + detalle : "")}");
                _fail++;
            }
        }

        private static void AsegurarIgual<T>(string nombre, T esperado, T actual)
            => Asegurar(nombre, Equals(esperado, actual),
                $"esperado=`{esperado}`, actual=`{actual}`");

        // ═══════════════════════════════════════════════════════════════════
        //  PARSER
        // ═══════════════════════════════════════════════════════════════════

        private static void P1_Parser_FormaModerna()
        {
            Console.WriteLine("[P1] Parser — forma moderna #cmd a b");
            var p = CommandParser.Parsear("#agente Claude 3.5");
            Asegurar("P1.1 parse no nulo", p != null);
            AsegurarIgual("P1.2 nombre", "agente", p!.Nombre);
            AsegurarIgual("P1.3 nombreOriginal", "agente", p.NombreOriginal);
            AsegurarIgual("P1.4 args.count", 2, p.Args.Count);
            AsegurarIgual("P1.5 args[0]", "Claude", p.Args[0]);
            AsegurarIgual("P1.6 args[1]", "3.5", p.Args[1]);
            AsegurarIgual("P1.7 argsRaw", "Claude 3.5", p.ArgsRaw);
            Console.WriteLine();
        }

        private static void P2_Parser_Legacy()
        {
            Console.WriteLine("[P2] Parser — legacy #CMD_VALOR");
            var p = CommandParser.Parsear("#AGENTE_ChatGpt");
            Asegurar("P2.1 parse no nulo", p != null);
            AsegurarIgual("P2.2 nombre lowercase", "agente", p!.Nombre);
            AsegurarIgual("P2.3 args.count", 1, p.Args.Count);
            AsegurarIgual("P2.4 args[0] case preservado", "ChatGpt", p.Args[0]);
            Console.WriteLine();
        }

        private static void P3_Parser_Comillas()
        {
            Console.WriteLine("[P3] Parser — comillas para frase");
            var p = CommandParser.Parsear("#ruta \"Mi Carpeta De Pruebas\" extra");
            Asegurar("P3.1 parse no nulo", p != null);
            AsegurarIgual("P3.2 args.count", 2, p!.Args.Count);
            AsegurarIgual("P3.3 frase preserva espacios", "Mi Carpeta De Pruebas", p.Args[0]);
            AsegurarIgual("P3.4 segundo arg", "extra", p.Args[1]);
            Console.WriteLine();
        }

        private static void P4_Parser_CasePreservado()
        {
            Console.WriteLine("[P4] Parser — case del nombre vs args");
            var p = CommandParser.Parsear("#AYUDA Audio");
            AsegurarIgual("P4.1 nombre canónico lowercase", "ayuda", p!.Nombre);
            AsegurarIgual("P4.2 nombreOriginal preserva case", "AYUDA", p.NombreOriginal);
            AsegurarIgual("P4.3 arg case preservado", "Audio", p.Args[0]);
            Console.WriteLine();
        }

        private static void P5_Parser_TextoSinHash()
        {
            Console.WriteLine("[P5] Parser — texto que no es comando → null");
            Asegurar("P5.1 vacío", CommandParser.Parsear("") == null);
            Asegurar("P5.2 sólo whitespace", CommandParser.Parsear("   \t  ") == null);
            Asegurar("P5.3 sin #", CommandParser.Parsear("hola mundo") == null);
            Asegurar("P5.4 sólo #", CommandParser.Parsear("#") == null);
            Console.WriteLine();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  REGISTRY
        // ═══════════════════════════════════════════════════════════════════

        private static CommandRegistry RegistryConHandlers()
        {
            var r = new CommandRegistry();
            r.Registrar(new AgenteCommand());                // canónico "agente", alias "ag"
            r.Registrar(new TelegramCommand());              // "telegram", alias "tg"
            r.Registrar(new ActivaTelegramCommand());        // OCULTO
            r.Registrar(new HabilidadCommand());             // "habilidad", alias "habilidades", "skills"
            r.Registrar(new TimeoutCommand());
            r.Registrar(new ReintentosCommand());
            return r;
        }

        private static void R1_Registry_LookupCanonicoYAlias()
        {
            Console.WriteLine("[R1] Registry — lookup canónico y alias");
            var r = RegistryConHandlers();
            Asegurar("R1.1 canónico",      r.Resolver("agente") is AgenteCommand);
            Asegurar("R1.2 case-insens",   r.Resolver("AGENTE") is AgenteCommand);
            Asegurar("R1.3 alias",         r.Resolver("ag") is AgenteCommand);
            Asegurar("R1.4 alias 2",       r.Resolver("tg") is TelegramCommand);
            Asegurar("R1.5 inexistente",   r.Resolver("xxxxxx") == null);
            Console.WriteLine();
        }

        private static void R2_Registry_Sugerencias()
        {
            Console.WriteLine("[R2] Registry — sugerencias por typo");
            var r = RegistryConHandlers();
            var sugs = r.Sugerencias("agnte");   // distancia 1 a "agente"
            Asegurar("R2.1 hay al menos una sugerencia", sugs.Count >= 1);
            Asegurar("R2.2 primera sugerencia es 'agente'",
                sugs[0].Descriptor.Nombre == "agente");

            var sugs2 = r.Sugerencias("habilidde");
            Asegurar("R2.3 sugiere habilidad",
                sugs2.Any(c => c.Descriptor.Nombre == "habilidad"));

            var lejano = r.Sugerencias("zzzzzzz");
            Asegurar("R2.4 nada para input muy lejano", lejano.Count == 0);
            Console.WriteLine();
        }

        private static void R3_Registry_TodosVisibles()
        {
            Console.WriteLine("[R3] Registry — TodosVisibles excluye ocultos");
            var r = RegistryConHandlers();
            var visibles = r.TodosVisibles();
            Asegurar("R3.1 ActivaTelegram NO está visible",
                !visibles.Any(c => c.Descriptor.Nombre == "activatelegram"));
            Asegurar("R3.2 Telegram SÍ está visible",
                visibles.Any(c => c.Descriptor.Nombre == "telegram"));
            Console.WriteLine();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  EXECUTOR
        // ═══════════════════════════════════════════════════════════════════

        private static (CommandExecutor exe, FakeServicios fake) NuevoExecutor()
        {
            var reg = new CommandRegistry();
            reg.Registrar(new AgenteCommand());
            reg.Registrar(new RecordarCommand());
            reg.Registrar(new ReintentosCommand());
            reg.Registrar(new TimeoutCommand());
            reg.Registrar(new EstadoCommand());
            reg.Registrar(new HabilidadCommand());
            reg.Registrar(new AyudaCommand(reg));
            reg.Registrar(new LanzaExcepcionCommand()); // para E4
            var fake = new FakeServicios();
            return (new CommandExecutor(reg, fake), fake);
        }

        private static async Task E1_Executor_ComandoValido()
        {
            Console.WriteLine("[E1] Executor — comando válido devuelve resultado");
            var (exe, fake) = NuevoExecutor();
            var r = await exe.DespacharAsync("#recordar on", chatIdTelegram: 0,
                usarTelegram: false, usarSlack: false);
            Asegurar("E1.1 esComando", r.EsComando);
            Asegurar("E1.2 reconocido", r.ComandoReconocido);
            Asegurar("E1.3 resultado no nulo", r.Resultado != null);
            Asegurar("E1.4 ok", r.Resultado!.Ok);
            Asegurar("E1.5 estado RecordarTema=true", fake.RecordarTema);
            Console.WriteLine();
        }

        private static async Task E2_Executor_Desconocido_Sugiere()
        {
            Console.WriteLine("[E2] Executor — desconocido sugiere alternativa");
            var (exe, _) = NuevoExecutor();
            var r = await exe.DespacharAsync("#recodar", chatIdTelegram: 0,
                usarTelegram: false, usarSlack: false);
            Asegurar("E2.1 esComando", r.EsComando);
            Asegurar("E2.2 NO reconocido", !r.ComandoReconocido);
            Asegurar("E2.3 mensaje sugiere recordar",
                (r.Resultado?.Mensaje ?? "").Contains("recordar"));
            Console.WriteLine();
        }

        private static async Task E3_Executor_NoEsComando()
        {
            Console.WriteLine("[E3] Executor — texto sin # → no es comando");
            var (exe, _) = NuevoExecutor();
            var r = await exe.DespacharAsync("hola, ¿qué puedes hacer?",
                chatIdTelegram: 0, usarTelegram: false, usarSlack: false);
            Asegurar("E3.1 NO es comando", !r.EsComando);
            Asegurar("E3.2 resultado nulo", r.Resultado == null);
            Console.WriteLine();
        }

        private static async Task E4_Executor_HandlerLanza_DevuelveError()
        {
            Console.WriteLine("[E4] Executor — handler que lanza es capturado");
            var (exe, _) = NuevoExecutor();
            var r = await exe.DespacharAsync("#explotar", chatIdTelegram: 0,
                usarTelegram: false, usarSlack: false);
            Asegurar("E4.1 esComando", r.EsComando);
            Asegurar("E4.2 reconocido", r.ComandoReconocido);
            Asegurar("E4.3 resultado tipo Error",
                r.Resultado?.Tipo == ResultTipo.Error);
            Asegurar("E4.4 contiene texto del error",
                (r.Resultado?.Mensaje ?? "").Contains("kaboom"));
            Console.WriteLine();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  HANDLERS
        // ═══════════════════════════════════════════════════════════════════

        private static async Task H1_Ayuda_ListaVisibles()
        {
            Console.WriteLine("[H1] AyudaCommand — lista visibles");
            var (exe, _) = NuevoExecutor();
            var r = await exe.DespacharAsync("#ayuda", chatIdTelegram: 0,
                usarTelegram: false, usarSlack: false);
            string msg = r.Resultado?.Mensaje ?? "";
            Asegurar("H1.1 contiene #recordar", msg.Contains("#recordar"));
            Asegurar("H1.2 contiene #estado",   msg.Contains("#estado"));
            Asegurar("H1.3 NO contiene comandos ocultos",
                !msg.Contains("explotar"));
            Console.WriteLine();
        }

        private static async Task H2_Timeout_RangoYClamp()
        {
            Console.WriteLine("[H2] TimeoutCommand — rango 10..1800");
            var (exe, fake) = NuevoExecutor();

            var ok = await exe.DespacharAsync("#timeout 180", 0, false, false);
            Asegurar("H2.1 valor válido aceptado", ok.Resultado!.Ok);
            AsegurarIgual("H2.2 estado actualizado", 180, fake.TimeoutSegundos);

            var mal = await exe.DespacharAsync("#timeout 5", 0, false, false);
            Asegurar("H2.3 fuera de rango → Error",
                mal.Resultado!.Tipo == ResultTipo.Error);
            AsegurarIgual("H2.4 estado no cambió", 180, fake.TimeoutSegundos);

            var noNum = await exe.DespacharAsync("#timeout abc", 0, false, false);
            Asegurar("H2.5 no-número → Error",
                noNum.Resultado!.Tipo == ResultTipo.Error);
            Console.WriteLine();
        }

        private static async Task H3_Reintentos_Rango()
        {
            Console.WriteLine("[H3] ReintentosCommand — rango 0..5");
            var (exe, fake) = NuevoExecutor();

            var ok = await exe.DespacharAsync("#reintentos 4", 0, false, false);
            Asegurar("H3.1 valor válido aceptado", ok.Resultado!.Ok);
            AsegurarIgual("H3.2 estado actualizado", 4, fake.Reintentos);

            var mal = await exe.DespacharAsync("#reintentos 9", 0, false, false);
            Asegurar("H3.3 >5 → Error",
                mal.Resultado!.Tipo == ResultTipo.Error);
            AsegurarIgual("H3.4 estado no cambió", 4, fake.Reintentos);
            Console.WriteLine();
        }

        private static async Task H4_Recordar_Toggle()
        {
            Console.WriteLine("[H4] RecordarCommand — toggle on/off");
            var (exe, fake) = NuevoExecutor();
            Asegurar("H4.1 inicio false", !fake.RecordarTema);
            await exe.DespacharAsync("#recordar", 0, false, false);
            Asegurar("H4.2 toggle → true", fake.RecordarTema);
            await exe.DespacharAsync("#recordar off", 0, false, false);
            Asegurar("H4.3 explícito off → false", !fake.RecordarTema);
            await exe.DespacharAsync("#recordar on", 0, false, false);
            Asegurar("H4.4 explícito on → true", fake.RecordarTema);
            Console.WriteLine();
        }

        private static async Task H5_Audio_PersisteConfig()
        {
            Console.WriteLine("[H5] AudioCommand — config persiste vía AudioConfigurar");
            var reg = new CommandRegistry();
            reg.Registrar(new AudioCommand());
            var fake = new FakeServicios();
            var exe = new CommandExecutor(reg, fake);

            var r = await exe.DespacharAsync("#audio proveedor OpenAI",
                0, false, false);
            Asegurar("H5.1 ok", r.Resultado!.Ok);
            AsegurarIgual("H5.2 proveedor persistido",
                ProveedorTTS.OpenAI, fake.UltimaConfigGuardada!.Proveedor);

            var r2 = await exe.DespacharAsync("#audio voz nova", 0, false, false);
            Asegurar("H5.3 ok voz", r2.Resultado!.Ok);
            AsegurarIgual("H5.4 voz persistida",
                "nova", fake.UltimaConfigGuardada!.Voz);
            Console.WriteLine();
        }
    }

    // ───────────────────────────────────────────────────────────────────
    //  Comando "kaboom" — sólo para el test E4
    // ───────────────────────────────────────────────────────────────────
    internal sealed class LanzaExcepcionCommand : ICommand
    {
        public CommandDescriptor Descriptor { get; } = new()
        {
            Nombre      = "explotar",
            Descripcion = "(test) lanza excepción",
            Categoria   = CommandCategoria.Estado,
            Oculto      = true,
        };

        public Task<CommandResult> EjecutarAsync(CommandContext ctx) =>
            throw new InvalidOperationException("kaboom");
    }

    // ───────────────────────────────────────────────────────────────────
    //  FakeServicios — implementación in-memory de IServiciosComandos
    // ───────────────────────────────────────────────────────────────────
    internal sealed class FakeServicios : IServiciosComandos
    {
        public List<Modelo>       Agentes = new();
        public List<ModeloAgente> Modelos = new();
        public List<Archivo>      Rutas   = new();
        public List<Api>          Apis    = new();

        public IReadOnlyList<Modelo>       ListaAgentes() => Agentes;
        public IReadOnlyList<ModeloAgente> ListaModelos() => Modelos;
        public IReadOnlyList<Archivo>      ListaRutas()   => Rutas;
        public IReadOnlyList<Api>          ListaApis()    => Apis;

        public string AgenteActual() => "FakeAgente";
        public string ModeloActual() => "fake-model";
        public string RutaActual()   => "/tmp/fake";

        public bool SeleccionarAgente(string nombre) { AgenteSel = nombre; return true; }
        public bool SeleccionarModelo(string nombre) { ModeloSel = nombre; return true; }
        public bool SeleccionarRuta(string nombre)   { RutaSel   = nombre; return true; }
        public string AgenteSel = "", ModeloSel = "", RutaSel = "";
        public void RecargarApis() { ApisRecargadas++; }
        public int ApisRecargadas;

        public bool RecordarTema           { get; set; }
        public bool SoloChat               { get; set; }
        public bool TelegramActivo         { get; set; }
        public bool SlackActivo            { get; set; }
        public bool AudioActivo            { get; set; }
        public bool EnviarConstructorTg    { get; set; }
        public bool EnviarConstructorSlack { get; set; }
        public bool EnviarArchivosTg       { get; set; }
        public bool EnviarArchivosSlack    { get; set; }

        public int Reintentos      { get; set; } = 3;
        public int TimeoutSegundos { get; set; } = 120;

        public ConfiguracionTTS? UltimaConfigGuardada;
        public ConfiguracionTTS AudioConfigActual() => UltimaConfigGuardada ?? new ConfiguracionTTS();
        public bool AudioConfigurar(ConfiguracionTTS cfg) { UltimaConfigGuardada = cfg; return true; }
        public AudioTTSService AudioService =>
            throw new NotSupportedException("Test fake — no usar AudioService directo.");

        public Task CancelarInstruccionAsync() { Cancelaciones++; return Task.CompletedTask; }
        public int Cancelaciones;

        public BroadcastService Broadcast =>
            throw new NotSupportedException("Test fake — handlers no deberían broadcastear.");

        public List<string> MensajesUI = new();
        public void MostrarEnUI(string mensaje) => MensajesUI.Add(mensaje);

        public object KeyboardCambiarAgente() => new object();
        public object KeyboardCambiarModelo() => new object();
        public object KeyboardCambiarRuta()   => new object();
        public object KeyboardConfiguraciones() => new object();
    }
}
