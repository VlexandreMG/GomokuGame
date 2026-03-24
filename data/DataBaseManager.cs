using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GomokuGame.data
{
    public class DatabaseManager
    {
        // Point d'entrée data: connexion + fabrique de repository.
        // Remplace par tes vrais identifiants Docker.
        private readonly string _connectionString = 
            "Host=127.0.0.1;" +
            "Port=5432;" +
            "Username=postgres;" +
            "Password=postgres;" +
            "Database=gomoku_db";

        /// <summary>
        /// Expose la chaîne de connexion active (utile pour les services/repositories).
        /// </summary>
        public string ConnectionString => _connectionString;
        /// <summary>
        /// Fabrique un repository générique branché sur la connexion courante.
        /// </summary>
        public GenericRepository Repository => new GenericRepository(_connectionString);

        /// <summary>
        /// Vérifie rapidement la disponibilité de PostgreSQL et écrit le résultat en console.
        /// </summary>
        public void TestConnection()
        {
            // Test simple utilisé pendant le développement.
            if (Repository.TestConnection(out string? error))
            {
                Console.WriteLine("Connexion à Postgres réussie !");
                return;
            }

            Console.WriteLine($"Erreur de connexion : {error}");
        }

        /// <summary>
        /// Lit les fichiers de sauvegarde JSON locaux pour compatibilité du flux existant.
        /// </summary>
        public IReadOnlyList<string> GetSavedGames()
        {
            // Cette méthode est conservée pour la compatibilité du flux existant
            // basé sur des sauvegardes JSON locales.
            try
            {
                string savesDirectory = GetSavesDirectory();
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

        /// <summary>
        /// Retourne le chemin absolu du dossier local contenant les sauvegardes JSON.
        /// </summary>
        private static string GetSavesDirectory()
        {
            return Path.Combine(AppContext.BaseDirectory, "saves");
        }
    }
}