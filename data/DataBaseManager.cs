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
        private readonly string _connectionString = 
            "Host=127.0.0.1;" +
            "Port=5432;" +
            "Username=postgres;" +
            "Password=postgres;" +
            "Database=gomoku_db";

        public string ConnectionString => _connectionString;
        public GenericRepository Repository => new GenericRepository(_connectionString);

        public void TestConnection()
        {
            if (Repository.TestConnection(out string? error))
            {
                Console.WriteLine("Connexion à Postgres réussie !");
                return;
            }

            Console.WriteLine($"Erreur de connexion : {error}");
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
                    .Select(file => Path.GetFileNameWithoutExtension(file))
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name!)
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