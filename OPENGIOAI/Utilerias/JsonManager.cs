using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OPENGIOAI.Utilerias
{
    public static class JsonManager
    {
        private static JsonSerializerOptions opciones = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        public static List<T> Leer<T>(string ruta)
        {
            if (!File.Exists(ruta))
                return new List<T>();

            string json = File.ReadAllText(ruta);

            if (string.IsNullOrWhiteSpace(json))
                return new List<T>();

            return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        }

        /// <summary>
        /// Lee la lista desde el archivo JSON.
        /// Si el archivo no existe (o la carpeta tampoco), lo crea con un array
        /// vacío <c>[]</c> para que la estructura quede lista para futuras escrituras.
        /// </summary>
        public static List<T> LeerOCrear<T>(string ruta)
        {
            // Crear directorio si no existe
            string? carpeta = Path.GetDirectoryName(ruta);
            if (!string.IsNullOrEmpty(carpeta) && !Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            // Crear archivo vacío si no existe
            if (!File.Exists(ruta))
            {
                File.WriteAllText(ruta, "[]");
                return new List<T>();
            }

            string json = File.ReadAllText(ruta);

            // Si el archivo estaba vacío, dejarlo como [] y devolver lista vacía
            if (string.IsNullOrWhiteSpace(json))
            {
                File.WriteAllText(ruta, "[]");
                return new List<T>();
            }

            return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        }

        public static void Guardar<T>(string ruta, List<T> datos)
        {
            string json = JsonSerializer.Serialize(datos, opciones);
            File.WriteAllText(ruta, json);
        }

        public static void Agregar<T>(string ruta, T nuevoElemento)
        {
            var lista = Leer<T>(ruta);
            lista.Add(nuevoElemento);
            Guardar(ruta, lista);
        }

        public static void Modificar<T>(string ruta, Func<T, bool> condicion, Action<T> modificarAccion)
        {
            var lista = Leer<T>(ruta);

            foreach (var item in lista)
            {
                if (condicion(item))
                {
                    modificarAccion(item); // Aquí puedes cambiar TODO lo que quieras
                }
            }

            Guardar(ruta, lista);
        }

        public static void Eliminar<T>(string ruta, Func<T, bool> condicion)
        {
            var lista = Leer<T>(ruta);

            lista.RemoveAll(item => condicion(item));

            Guardar(ruta, lista);
        }


        

    }
}
