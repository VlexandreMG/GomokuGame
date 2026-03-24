using Npgsql;
using System;

namespace GomokuGame.data
{
    public class DatabaseManager
    {
        // Remplace par tes vrais identifiants Docker
        private string _connectionString = 
            "Host=127.0.0.1;" +
            "Port=5432;" +
            "Username=postgres;" +
            "Password=ton_mot_de_passe;" +
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
    }
}