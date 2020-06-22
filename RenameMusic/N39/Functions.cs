﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;

namespace RenameMusic.N39
{
    public static class FunctionsN39
    {

        /*
        * Esta funcion se usa para cuando tengamos que renombrar a un archivo
        * Nos aseguramos que tenga solamente los caracteres permitidos por Windows
        */
        public static string NormalizeFileName(string fileName)
        {
            string invalidChars = Regex.Escape(
                 new string(Path.GetInvalidFileNameChars())
            );
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return Regex.Replace(fileName, invalidRegStr, "_");
        }

        public static string RenombrarArchivoCriterioDefault(TagLib.File cancion, string nuevoNombreConRuta)
        {
            if (!string.IsNullOrWhiteSpace(cancion.Tag.JoinedPerformers))
            {
                return nuevoNombreConRuta += NormalizeFileName(cancion.Tag.Title + " - " + cancion.Tag.JoinedPerformers);
            }
            else if (!string.IsNullOrWhiteSpace(cancion.Tag.JoinedAlbumArtists))
            {
                return nuevoNombreConRuta += NormalizeFileName(cancion.Tag.Title + " - " + cancion.Tag.JoinedAlbumArtists);
            }
            else
            {
                return nuevoNombreConRuta += NormalizeFileName(cancion.Tag.Title);
            }
        }

        public static void ShowProblemsList(List<string> problems)
        {
            string msg = string.Join("\n", problems);
            MessageBox.Show(msg, "Problemas detectados :(", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        public static TagLibResultN39 CreateMusicObj(string rutaArchivo, List<string> problems)
        {
            TagLibResultN39 result = new TagLibResultN39();
            try
            {
                // Creo un objeto archivo/cancion y tomo los datos
                result.File = TagLib.File.Create(rutaArchivo);
            }
            catch (TagLib.CorruptFileException)
            {
                result.File = null;
                problems.Add("No se añadirá a la lista el siguiente archivo corrupto o incompatible: " + rutaArchivo);
            }
            return result;
        }
    }
}
