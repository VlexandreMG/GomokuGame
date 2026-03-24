using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GomokuGame.data
{
    public class DatabaseManager
    {
        // Remplace par tes vrais identifiants Docker
        private string _connectionString = 
            "Host=127.0.0.1;" +
            "Port=5432;" +
            "Username=postgres;" +
            "Password=postgres;" +
            "Database=gomoku_db";

        public void TestConnection()
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    Console.WriteLine("Connexion à Postgres réussie !");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur de connexion : {ex.Message}");
            }
        }

        public IReadOnlyList<string> GetSavedGames()
        {
            try
            {
                string savesDirectory = Path.Combine(AppContext.BaseDirectory, "saves");
                if (!Directory.Exists(savesDirectory))
                {
                    return Array.Empty<string>();
                }

                return Directory
                    .GetFiles(savesDirectory, "*.json", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileNameWithoutExtension)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lecture sauvegardes : {ex.Message}");
                return Array.Empty<string>();
            }
        }
    }
}