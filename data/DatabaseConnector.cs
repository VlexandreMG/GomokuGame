using System;
using System.Data.Common;
using GomokuGame.core;

namespace GomokuGame.data;

public sealed class DatabaseConnector
{
    public string ProviderInvariantName { get; }
    public string ConnectionString { get; }

    public DatabaseConnector(string providerInvariantName, string connectionString)
    {
        ProviderInvariantName = providerInvariantName;
        ConnectionString = connectionString;
        TerminalLogger.Action($"DatabaseConnector created for provider '{ProviderInvariantName}'");
    }

    public DbConnection OpenConnection()
    {
        TerminalLogger.Action($"Opening database connection with provider '{ProviderInvariantName}'");

        DbProviderFactory factory = DbProviderFactories.GetFactory(ProviderInvariantName);
        DbConnection? connection = factory.CreateConnection();
        if (connection is null)
        {
            throw new InvalidOperationException($"Unable to create a DB connection for provider '{ProviderInvariantName}'.");
        }

        connection.ConnectionString = ConnectionString;
        connection.Open();
        TerminalLogger.Action("Database connection opened successfully");
        return connection;
    }

    public bool TryTestConnection(out string message)
    {
        try
        {
            using DbConnection connection = OpenConnection();
            message = "Connexion BDD reussie.";
            TerminalLogger.Action(message);
            return true;
        }
        catch (Exception ex)
        {
            message = $"Connexion BDD echouee: {ex.Message}";
            TerminalLogger.Action(message);
            return false;
        }
    }
}
