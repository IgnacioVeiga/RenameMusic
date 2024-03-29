﻿using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace RenameMusic.Util
{
    internal static class Multimedia
    {
        internal static BitmapImage GetBitmapImage(byte[] buffer)
        {
            MemoryStream ms = new(buffer);
            ms.Seek(0, SeekOrigin.Begin);

            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.StreamSource = ms;
            bitmap.EndInit();

            return bitmap;
        }

        internal static BitmapImage GetBitmapImage(string filepath)
        {
            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filepath);
            bitmap.EndInit();

            return bitmap;
        }
    }
}
