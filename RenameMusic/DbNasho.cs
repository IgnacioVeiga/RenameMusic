using RenameMusic;
using RenameMusic.DTOs;
using System;
using System.Data.SQLite;
using System.IO;
using System.Windows;

namespace SQLiteDemo
{
    public class DbNasho
    {
        private readonly string dbFilename = @"‪D:\database.db";

        public SQLiteConnection CreateConnection()
        {
            SQLiteConnection sqlite_conn;
            // Create a new database connection:
            sqlite_conn = new SQLiteConnection(@"Data Source = " + dbFilename + "; Version = 3; New = True; Compress = True; ");
            // Open the connection:
            try
            {
                sqlite_conn.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            return sqlite_conn;
        }

        public void CreateTable(SQLiteConnection connection)
        {
            try
            {
                if (!File.Exists(dbFilename))
                {
                    SQLiteCommand sqlite_cmd;
                    string CreateSQLCarpetas = "CREATE TABLE Carpetas (CancionesId TEXT NOT NULL, RutaCompleta TEXT NOT NULL)";
                    string CreateSQLCanciones = "CREATE TABLE Canciones (CarpetaId TEXT NOT NULL, Titulo TEXT, Album TEXT, AlbumArtista TEXT, Artista TEXT, NombreArchivo TEXT NOT NULL, Formato TEXT NOT NULL)";
                    sqlite_cmd = connection.CreateCommand();
                    sqlite_cmd.CommandText = CreateSQLCarpetas;
                    sqlite_cmd.ExecuteNonQuery();
                    sqlite_cmd.CommandText = CreateSQLCanciones;
                    sqlite_cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void InsertData(SQLiteConnection connection, string tabla, object datos)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(tabla))
                {
                    SQLiteCommand sqlite_cmd;
                    sqlite_cmd = connection.CreateCommand();
                    switch (tabla)
                    {
                        case "Carpetas":
                            CarpetaDTO carpeta = datos as CarpetaDTO;
                            sqlite_cmd.CommandText = "INSERT INTO Carpetas (CancionesId, RutaCompleta) VALUES ('" + carpeta.CancionesId +"','"+ carpeta.Ruta + "');";
                            sqlite_cmd.ExecuteNonQuery();
                            break;
                        case "Canciones":
                            CancionDTO cancion = datos as CancionDTO;
                            sqlite_cmd.CommandText = "INSERT INTO Canciones (CarpetaId, Titulo, Album, AlbumArtista, Artista, NombreArchivo, Formato) VALUES ('" + cancion.CarpetaId + "', '" + cancion.Titulo + "', '" + cancion.Album + "', '" + cancion.AlbumArtista + "', '" + cancion.Artista + "', '" + cancion.NombreActual + "', '" + cancion.Formato + "');";
                            sqlite_cmd.ExecuteNonQuery();
                            break;
                        default:
                            MessageBox.Show("EL nombre de la tabla es incorrecto");
                            break;
                    }
                }
                else
                {
                    MessageBox.Show("EL nombre de la tabla está vacio o incorrecto");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void ReadData(SQLiteConnection connection, string tabla)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(tabla))
                {
                    SQLiteDataReader sqlite_datareader;
                    SQLiteCommand sqlite_cmd;
                    sqlite_cmd = connection.CreateCommand();
                    sqlite_cmd.CommandText = "SELECT * FROM " + tabla;
                    sqlite_datareader = sqlite_cmd.ExecuteReader();

                    // TODO: Revisar
                    //while (sqlite_datareader.Read())
                    //{
                    //    string myreader = sqlite_datareader.GetString(0);
                    //    MessageBox.Show(myreader);
                    //}
                    connection.Close();
                }
                else
                {
                    MessageBox.Show("EL nombre de la tabla está vacio o incorrecto");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}