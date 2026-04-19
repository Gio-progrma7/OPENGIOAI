// ============================================================
//  MemoriaChunker.cs  — Fase C
//
//  Parte Hechos.md y Episodios.md en "chunks" aptos para embedding.
//
//  ESTRATEGIA POR FUENTE:
//    · Hechos.md      — cada viñeta "- ..." es un chunk natural.
//                       Son frases cortas sobre el usuario (name,
//                       preferencias, etc). Chunk = viñeta completa.
//
//    · Episodios.md   — cada "- YYYY-MM-DD HH:MM — descripción"
//                       es un chunk. Corto, auto-contenido, fácil
//                       de recuperar por similitud.
//
//    · Otros archivos — fallback por caracteres con solapamiento
//                       (sliding window de ChunkSize con ChunkOverlap).
//
//  IDENTIFICADOR:
//    Id = SHA1 corto (10 chars) de "fuente|offset|texto" → estable
//    entre re-indexaciones; si el texto cambia, cambia el Id y el
//    indexer lo reemplaza.
// ============================================================

using OPENGIOAI.Entidades;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace OPENGIOAI.Utilerias
{
    public static class MemoriaChunker
    {
        // Detecta "- texto" o "* texto" (viñetas markdown).
        private static readonly Regex _rxBullet = new(
            @"^\s*(?:[-*])\s+(.+)$", RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>
        /// Divide Hechos.md en chunks (una viñeta = un chunk).
        /// </summary>
        public static List<(string texto, int offset)> ChunkearHechos(string contenido)
        {
            return ExtraerVinetas(contenido);
        }

        /// <summary>
        /// Divide Episodios.md en chunks (una línea = un chunk).
        /// </summary>
        public static List<(string texto, int offset)> ChunkearEpisodios(string contenido)
        {
            // Los episodios suelen ser viñetas también; si no, partimos por líneas.
            var vinetas = ExtraerVinetas(contenido);
            if (vinetas.Count > 0) return vinetas;

            var lineas = new List<(string, int)>();
            int pos = 0;
            foreach (var linea in (contenido ?? "").Split('\n'))
            {
                string t = linea.TrimEnd('\r').Trim();
                if (t.Length >= 8)
                    lineas.Add((t, pos));
                pos += linea.Length + 1;
            }
            return lineas;
        }

        /// <summary>
        /// Fallback para texto libre: sliding window. Usado por cualquier
        /// archivo que no sea viñetas (Arquitectura.md, etc.).
        /// </summary>
        public static List<(string texto, int offset)> ChunkearTextoLibre(
            string contenido, int tamanio, int solapamiento)
        {
            var salida = new List<(string, int)>();
            if (string.IsNullOrWhiteSpace(contenido)) return salida;

            tamanio      = Math.Max(100, tamanio);
            solapamiento = Math.Max(0, Math.Min(solapamiento, tamanio - 50));

            int i = 0;
            while (i < contenido.Length)
            {
                int fin = Math.Min(i + tamanio, contenido.Length);
                string chunk = contenido.Substring(i, fin - i).Trim();
                if (chunk.Length >= 20) salida.Add((chunk, i));
                if (fin >= contenido.Length) break;
                i = fin - solapamiento;
            }
            return salida;
        }

        /// <summary>Calcula un ID estable para un chunk (10 chars hex).</summary>
        public static string ComputarId(string fuente, int offset, string texto)
        {
            string payload = $"{fuente}|{offset}|{texto}";
            using var sha = SHA1.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var sb = new StringBuilder(10);
            for (int i = 0; i < 5; i++) sb.Append(hash[i].ToString("x2"));
            return sb.ToString();
        }

        /// <summary>Hash corto de un archivo completo — para el manifest.</summary>
        public static string HashContenido(string contenido)
        {
            if (string.IsNullOrEmpty(contenido)) return "";
            using var sha = SHA1.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(contenido));
            return Convert.ToHexString(hash);
        }

        // ──────── interno ────────

        private static List<(string texto, int offset)> ExtraerVinetas(string contenido)
        {
            var salida = new List<(string, int)>();
            if (string.IsNullOrWhiteSpace(contenido)) return salida;

            foreach (Match m in _rxBullet.Matches(contenido))
            {
                string t = m.Groups[1].Value.Trim();
                if (t.Length >= 4) salida.Add((t, m.Index));
            }
            return salida;
        }
    }
}
