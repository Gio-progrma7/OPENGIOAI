using OPENGIOAI.Data;
using System.Text;

namespace OPENGIOAI.Tests.CorePipelineTests
{
    internal static class Program
    {
        private static int _pass;
        private static int _fail;

        public static int Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("================================================");
            Console.WriteLine(" CorePipeline — Test Harness");
            Console.WriteLine("================================================\n");

            R1_SalidaPreferida_PrefiereStdoutSobreRespuestaTxt();
            R2_SalidaPreferida_FallbackACodigo();
            R3_SalidaPreferida_RespuestaTxtCuandoStdoutVacio();
            R4_SalidaTecnica_NoEjecutadoDevuelveSoloCodigo();
            R5_SalidaTecnica_EjecutadoConExitoIncluyeCodigoYStdout();
            R6_SalidaTecnica_ConErrorIncluyeMensaje();

            Console.WriteLine("\n================================================");
            Console.WriteLine($"  PASS: {_pass}   FAIL: {_fail}");
            Console.WriteLine("================================================");
            return _fail == 0 ? 0 : 1;
        }

        // ════════════════════════════════════════════════════
        //  ResultadoEjecucionIA — SalidaPreferida
        // ════════════════════════════════════════════════════

        private static void R1_SalidaPreferida_PrefiereStdoutSobreRespuestaTxt()
        {
            var r = new ResultadoEjecucionIA
            {
                CodigoGenerado = "print('hola')",
                Stdout = "salida de stdout",
                RespuestaTxt = "salida de respuesta.txt",
                Ejecutado = true
            };
            Assert(r.SalidaPreferida == "salida de stdout",
                "SalidaPreferida debe priorizar Stdout sobre RespuestaTxt");
        }

        private static void R2_SalidaPreferida_FallbackACodigo()
        {
            var r = new ResultadoEjecucionIA
            {
                CodigoGenerado = "print('fallback')",
                Stdout = "",
                RespuestaTxt = "",
                Ejecutado = false
            };
            Assert(r.SalidaPreferida == "print('fallback')",
                "SalidaPreferida debe caer a CodigoGenerado si Stdout y RespuestaTxt están vacíos");
        }

        private static void R3_SalidaPreferida_RespuestaTxtCuandoStdoutVacio()
        {
            var r = new ResultadoEjecucionIA
            {
                CodigoGenerado = "print('test')",
                Stdout = "",
                RespuestaTxt = "solo respuesta.txt",
                Ejecutado = true
            };
            Assert(r.SalidaPreferida == "solo respuesta.txt",
                "SalidaPreferida debe usar RespuestaTxt si Stdout está vacío");
        }

        // ════════════════════════════════════════════════════
        //  ResultadoEjecucionIA — SalidaTecnica
        // ════════════════════════════════════════════════════

        private static void R4_SalidaTecnica_NoEjecutadoDevuelveSoloCodigo()
        {
            var r = new ResultadoEjecucionIA
            {
                CodigoGenerado = "print('no ejecutado')",
                Stdout = "esto no debería aparecer",
                Ejecutado = false
            };
            Assert(r.SalidaTecnica == "print('no ejecutado')",
                "SalidaTecnica debe devolver solo el código si no se ejecutó");
        }

        private static void R5_SalidaTecnica_EjecutadoConExitoIncluyeCodigoYStdout()
        {
            var r = new ResultadoEjecucionIA
            {
                CodigoGenerado = "print('ok')",
                Stdout = "salida exitosa",
                Stderr = "",
                ExitCode = 0,
                Ejecutado = true
            };
            Assert(r.SalidaTecnica.Contains("print('ok')"),
                "SalidaTecnica debe contener el código generado");
            Assert(r.SalidaTecnica.Contains("salida exitosa"),
                "SalidaTecnica debe contener la salida del script");
            Assert(!r.SalidaTecnica.Contains("Error al ejecutar"),
                "SalidaTecnica no debe contener mensaje de error si ExitCode=0");
        }

        private static void R6_SalidaTecnica_ConErrorIncluyeMensaje()
        {
            var r = new ResultadoEjecucionIA
            {
                CodigoGenerado = "print('fail')",
                Stdout = "",
                Stderr = "ZeroDivisionError",
                ExitCode = 1,
                Ejecutado = true
            };
            Assert(r.SalidaTecnica.Contains("Error al ejecutar el script"),
                "SalidaTecnica debe indicar error si ExitCode != 0");
            Assert(r.SalidaTecnica.Contains("ZeroDivisionError"),
                "SalidaTecnica debe incluir el stderr del script");
        }

        // ════════════════════════════════════════════════════
        //  Helpers
        // ════════════════════════════════════════════════════

        private static void Assert(bool ok, string mensaje)
        {
            if (ok)
            {
                _pass++;
                Console.WriteLine($"    [PASS] {mensaje}");
            }
            else
            {
                _fail++;
                Console.WriteLine($"    [FAIL] {mensaje}");
            }
        }
    }
}
