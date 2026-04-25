// ============================================================
//  ConversationWindowTests — Fase 2
//
//  Suite de pruebas mecánicas para ConversationWindow.AgregarAsync
//  y el flujo de resumen acumulado. No llama a ningún LLM real —
//  usa un resumidor fake que devuelve strings deterministas.
//
//  CUBRE:
//    T1. Sin habilidad activa: expulsión sin resumen; fake NO se llama.
//    T2. Con habilidad activa: expulsión dispara resumen; acumula.
//    T3. Resumen incremental: el fake recibe el resumen previo.
//    T4. Fail-open: resumidor que lanza no rompe el flujo.
//    T5. Limpiar(): resetea resumen y ventana.
//    T6. ConstruirBloque(): antepone [RESUMEN PREVIO] cuando hay resumen.
//    T7. Sin expulsión (≤ MaxTurnos): resumidor NO se llama.
//
//  ESTADO GLOBAL:
//    Toggle HabilidadesRegistry.HAB_HISTORIAL_COMPRIMIDO (persiste a
//    disco). Se lee el valor original al inicio y se restaura al final.
// ============================================================

using OPENGIOAI.Data;
using OPENGIOAI.Utilerias;

namespace OPENGIOAI.Tests.ConversationWindowTests
{
    internal static class Program
    {
        private static int _pass;
        private static int _fail;

        [STAThread]
        public static async Task<int> Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("================================================");
            Console.WriteLine(" ConversationWindow — Fase 2 test harness");
            Console.WriteLine("================================================\n");

            // Guardamos el estado original de la habilidad para restaurarlo al final.
            bool estadoOriginal = HabilidadesRegistry.Instancia.EstaActiva(
                HabilidadesRegistry.HAB_HISTORIAL_COMPRIMIDO);

            try
            {
                await T1_SinHabilidad_NoLlamaResumidor();
                await T2_ConHabilidad_AcumulaResumen();
                await T3_Incremental_RecibePrevio();
                await T4_FailOpen_ResumidorLanza();
                await T5_Limpiar_ReseteaResumen();
                await T6_ConstruirBloque_AnteponeResumen();
                await T7_SinExpulsion_NoLlamaResumidor();
            }
            finally
            {
                HabilidadesRegistry.Instancia.Establecer(
                    HabilidadesRegistry.HAB_HISTORIAL_COMPRIMIDO, estadoOriginal);
            }

            Console.WriteLine("\n================================================");
            Console.WriteLine($" RESULTADO: {_pass} PASS · {_fail} FAIL");
            Console.WriteLine("================================================");
            return _fail == 0 ? 0 : 1;
        }

        // ═════════════════════════ Tests ═════════════════════════

        private static async Task T1_SinHabilidad_NoLlamaResumidor()
        {
            Iniciar("T1  Sin habilidad → NO llama resumidor");
            HabilidadesRegistry.Instancia.Establecer(
                HabilidadesRegistry.HAB_HISTORIAL_COMPRIMIDO, false);

            var win = new ConversationWindow(maxTurnos: 3, maxTokens: 10000);
            int llamadas = 0;

            ResumirHistorialDelegate resumidor =
                (prev, exp, ct) => { llamadas++; return Task.FromResult("NO-DEBERIA-EJECUTAR"); };

            for (int i = 0; i < 6; i++)
                await win.AgregarAsync($"instr #{i}", $"resp #{i}", resumidor);

            Assert(win.Count == 3, $"Ventana debe tener 3 turnos, tiene {win.Count}");
            Assert(llamadas == 0, $"Resumidor NO debe invocarse, se invocó {llamadas} veces");
            Assert(string.IsNullOrEmpty(win.ResumenAcumulado),
                $"ResumenAcumulado debe estar vacío, tiene: '{win.ResumenAcumulado}'");
        }

        private static async Task T2_ConHabilidad_AcumulaResumen()
        {
            Iniciar("T2  Con habilidad → expulsión genera resumen");
            HabilidadesRegistry.Instancia.Establecer(
                HabilidadesRegistry.HAB_HISTORIAL_COMPRIMIDO, true);

            var win = new ConversationWindow(maxTurnos: 3, maxTokens: 10000);
            int llamadas = 0;
            int totalExpulsados = 0;

            ResumirHistorialDelegate resumidor = (prev, exp, ct) =>
            {
                llamadas++;
                totalExpulsados += exp.Count;
                return Task.FromResult($"RESUMEN v{llamadas} ({exp.Count} turnos)");
            };

            // 3 turnos: no hay expulsión (llenamos la ventana justo).
            await win.AgregarAsync("i1", "r1", resumidor);
            await win.AgregarAsync("i2", "r2", resumidor);
            await win.AgregarAsync("i3", "r3", resumidor);
            Assert(llamadas == 0, "Aún no debe llamar al resumidor (ventana no desbordada)");

            // 4º turno: expulsa 1.
            await win.AgregarAsync("i4", "r4", resumidor);
            Assert(llamadas == 1, $"Debe haber 1 llamada, hay {llamadas}");
            Assert(totalExpulsados == 1, $"Debe haber 1 turno expulsado, hay {totalExpulsados}");
            Assert(win.ResumenAcumulado.Contains("RESUMEN v1"),
                $"Resumen debe contener 'RESUMEN v1', es: '{win.ResumenAcumulado}'");
            Assert(win.Count == 3, $"Ventana debe mantener 3 turnos, tiene {win.Count}");
        }

        private static async Task T3_Incremental_RecibePrevio()
        {
            Iniciar("T3  Resumen incremental → fake recibe el previo");
            HabilidadesRegistry.Instancia.Establecer(
                HabilidadesRegistry.HAB_HISTORIAL_COMPRIMIDO, true);

            var win = new ConversationWindow(maxTurnos: 2, maxTokens: 10000);
            var previosVistos = new List<string>();

            ResumirHistorialDelegate resumidor = (prev, exp, ct) =>
            {
                previosVistos.Add(prev);
                return Task.FromResult($"RESUMEN#{previosVistos.Count}");
            };

            await win.AgregarAsync("i1", "r1", resumidor);
            await win.AgregarAsync("i2", "r2", resumidor);
            // 3º turno expulsa el primero — primera llamada, previo = ""
            await win.AgregarAsync("i3", "r3", resumidor);
            // 4º turno expulsa otro — segunda llamada, previo = "RESUMEN#1"
            await win.AgregarAsync("i4", "r4", resumidor);

            Assert(previosVistos.Count == 2, $"Debe haber 2 invocaciones, hay {previosVistos.Count}");
            Assert(previosVistos[0] == "",
                $"Primera invocación debe recibir previo vacío, recibió: '{previosVistos[0]}'");
            Assert(previosVistos[1] == "RESUMEN#1",
                $"Segunda invocación debe recibir 'RESUMEN#1', recibió: '{previosVistos[1]}'");
            Assert(win.ResumenAcumulado == "RESUMEN#2",
                $"ResumenAcumulado final debe ser 'RESUMEN#2', es: '{win.ResumenAcumulado}'");
        }

        private static async Task T4_FailOpen_ResumidorLanza()
        {
            Iniciar("T4  Fail-open → resumidor que lanza no rompe el flujo");
            HabilidadesRegistry.Instancia.Establecer(
                HabilidadesRegistry.HAB_HISTORIAL_COMPRIMIDO, true);

            var win = new ConversationWindow(maxTurnos: 2, maxTokens: 10000);

            // Primera llamada: devuelve un resumen "bueno".
            // Segunda: lanza. ResumenAcumulado debe quedarse como el primero.
            int call = 0;
            ResumirHistorialDelegate resumidor = (prev, exp, ct) =>
            {
                call++;
                if (call == 1) return Task.FromResult("RESUMEN-BUENO");
                throw new InvalidOperationException("red caída simulada");
            };

            await win.AgregarAsync("i1", "r1", resumidor);
            await win.AgregarAsync("i2", "r2", resumidor);
            await win.AgregarAsync("i3", "r3", resumidor); // llamada 1 → OK
            Assert(win.ResumenAcumulado == "RESUMEN-BUENO",
                $"Tras 1ª llamada el resumen debe ser 'RESUMEN-BUENO', es: '{win.ResumenAcumulado}'");

            bool excepcionBurbujeo = false;
            try
            {
                await win.AgregarAsync("i4", "r4", resumidor); // llamada 2 → lanza
            }
            catch
            {
                excepcionBurbujeo = true;
            }
            Assert(!excepcionBurbujeo, "AgregarAsync NO debe propagar excepciones del resumidor");
            Assert(win.ResumenAcumulado == "RESUMEN-BUENO",
                $"Tras fallo el resumen debe preservar el previo, es: '{win.ResumenAcumulado}'");
            Assert(win.Count == 2, $"Ventana debe seguir en 2 turnos, tiene {win.Count}");
        }

        private static async Task T5_Limpiar_ReseteaResumen()
        {
            Iniciar("T5  Limpiar() → resetea resumen y ventana");
            HabilidadesRegistry.Instancia.Establecer(
                HabilidadesRegistry.HAB_HISTORIAL_COMPRIMIDO, true);

            var win = new ConversationWindow(maxTurnos: 2, maxTokens: 10000);
            ResumirHistorialDelegate resumidor =
                (prev, exp, ct) => Task.FromResult("R");

            await win.AgregarAsync("i1", "r1", resumidor);
            await win.AgregarAsync("i2", "r2", resumidor);
            await win.AgregarAsync("i3", "r3", resumidor);

            Assert(win.ResumenAcumulado == "R", "Pre-condición: debe haber resumen antes de Limpiar");
            win.Limpiar();
            Assert(win.Count == 0, $"Ventana debe quedar vacía, tiene {win.Count}");
            Assert(string.IsNullOrEmpty(win.ResumenAcumulado),
                $"ResumenAcumulado debe quedar vacío, tiene: '{win.ResumenAcumulado}'");
        }

        private static async Task T6_ConstruirBloque_AnteponeResumen()
        {
            Iniciar("T6  ConstruirBloque() → antepone [RESUMEN PREVIO]");
            HabilidadesRegistry.Instancia.Establecer(
                HabilidadesRegistry.HAB_HISTORIAL_COMPRIMIDO, true);

            var win = new ConversationWindow(maxTurnos: 2, maxTokens: 10000);
            ResumirHistorialDelegate resumidor =
                (prev, exp, ct) => Task.FromResult("• decisión X\n• archivo Y.cs");

            await win.AgregarAsync("i1", "r1", resumidor);
            await win.AgregarAsync("i2", "r2", resumidor);
            await win.AgregarAsync("quiero cambiar Z", "cambiado", resumidor);

            string bloque = win.ConstruirBloque();

            Assert(bloque.Contains("[RESUMEN PREVIO"),
                "Bloque debe contener header '[RESUMEN PREVIO'");
            Assert(bloque.Contains("decisión X"),
                "Bloque debe contener el texto del resumen");
            Assert(bloque.Contains("[FIN RESUMEN]"),
                "Bloque debe contener cierre '[FIN RESUMEN]'");
            Assert(bloque.Contains("[CONTEXTO PREVIO"),
                "Bloque debe contener también los turnos vigentes");
            Assert(bloque.IndexOf("[RESUMEN PREVIO") < bloque.IndexOf("[CONTEXTO PREVIO"),
                "El resumen debe ir ANTES del contexto verbatim");
        }

        private static async Task T7_SinExpulsion_NoLlamaResumidor()
        {
            Iniciar("T7  Sin expulsión (≤ MaxTurnos) → resumidor NO se llama");
            HabilidadesRegistry.Instancia.Establecer(
                HabilidadesRegistry.HAB_HISTORIAL_COMPRIMIDO, true);

            var win = new ConversationWindow(maxTurnos: 5, maxTokens: 10000);
            int llamadas = 0;
            ResumirHistorialDelegate resumidor =
                (prev, exp, ct) => { llamadas++; return Task.FromResult("x"); };

            for (int i = 0; i < 5; i++)
                await win.AgregarAsync($"i{i}", $"r{i}", resumidor);

            Assert(llamadas == 0,
                $"Resumidor NO debe invocarse con la ventana llena sin desbordar, se invocó {llamadas}");
            Assert(win.Count == 5, $"Ventana debe tener 5 turnos, tiene {win.Count}");
            Assert(string.IsNullOrEmpty(win.ResumenAcumulado),
                "Sin expulsión no debe haber resumen acumulado");
        }

        // ═════════════════════════ Helpers ═════════════════════════

        private static void Iniciar(string nombre)
        {
            Console.WriteLine($"\n▸ {nombre}");
        }

        private static void Assert(bool condicion, string mensaje)
        {
            if (condicion)
            {
                _pass++;
                Console.WriteLine($"    ✓ {mensaje}");
            }
            else
            {
                _fail++;
                Console.WriteLine($"    ✗ FAIL: {mensaje}");
            }
        }
    }
}
