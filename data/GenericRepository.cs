using GomokuGame.model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace GomokuGame.data;

public sealed class GenericRepository
{
    // Unique responsabilité: exécuter des opérations SQL génériques
    // à partir des modèles annotés (Table/Column/PrimaryKey).
    private readonly string _connectionString;

    /// <summary>
    /// Construit le repository avec la connexion SQL à utiliser.
    /// </summary>
    public GenericRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Teste l'ouverture de la connexion SQL et renvoie le message d'erreur éventuel.
    /// </summary>
    public bool TestConnection(out string? error)
    {
        try
        {
            using var conn = OpenConnection();
            conn.Open();
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Exécute un SELECT global sur la table du modèle T.
    /// </summary>
    public IReadOnlyList<T> GetAll<T>() where T : new()
    {
        // Lit toute la table associée au modèle T.
        string tableName = GetTableName(typeof(T));
        string sql = $"SELECT * FROM {tableName}";
        return ExecuteQuery<T>(sql);
    }

    /// <summary>
    /// Exécute un SELECT filtré par une colonne et une valeur.
    /// </summary>
    public IReadOnlyList<T> FindByColumn<T>(string columnName, object value) where T : new()
    {
        // Requête générique filtrée par une colonne.
        string tableName = GetTableName(typeof(T));
        string sql = $"SELECT * FROM {tableName} WHERE {columnName} = @value";
        return ExecuteQuery<T>(sql, new NpgsqlParameter("@value", value));
    }

    /// <summary>
    /// Insère un enregistrement du modèle T et retourne l'identifiant généré.
    /// </summary>
    public int Insert<T>(T entity)
    {
        // Insertion dynamique d'un modèle en ignorant la clé primaire auto-générée.
        Type type = typeof(T);
        string tableName = GetTableName(type);
        var mappedProperties = GetMappedProperties(type)
            .Where(p => !IsPrimaryKey(p))
            .ToList();

        string columns = string.Join(", ", mappedProperties.Select(GetColumnName));
        string values = string.Join(", ", mappedProperties.Select((_, i) => $"@p{i}"));
        string sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values}) RETURNING id";

        using var conn = OpenConnection();
        conn.Open();

        using var cmd = CreateCommand(conn, sql, Array.Empty<NpgsqlParameter>());
        for (int i = 0; i < mappedProperties.Count; i++)
        {
            object? value = mappedProperties[i].GetValue(entity) ?? DBNull.Value;
            cmd.Parameters.AddWithValue($"@p{i}", value);
        }

        object? scalar = cmd.ExecuteScalar();
        if (scalar is null || scalar == DBNull.Value)
        {
            return 0;
        }

        return Convert.ToInt32(scalar, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Exécute une requête SQL sans résultat tabulaire (UPDATE/DELETE/INSERT).
    /// </summary>
    public int ExecuteNonQuery(string sql, params NpgsqlParameter[] parameters)
    {
        // Exécution utilitaire (DELETE/UPDATE/INSERT sans RETURNING exploité ici).
        using var conn = OpenConnection();
        conn.Open();

        using var cmd = CreateCommand(conn, sql, parameters);

        return cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Exécute une requête SQL et mappe chaque ligne vers le modèle T.
    /// </summary>
    private IReadOnlyList<T> ExecuteQuery<T>(string sql, params NpgsqlParameter[] parameters) where T : new()
    {
        var results = new List<T>();
        Type type = typeof(T);
        var mappedProperties = GetMappedProperties(type);

        using var conn = OpenConnection();
        conn.Open();

        using var cmd = CreateCommand(conn, sql, parameters);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var instance = new T();

            foreach (PropertyInfo property in mappedProperties)
            {
                string columnName = GetColumnName(property);
                int ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal))
                {
                    continue;
                }

                object rawValue = reader.GetValue(ordinal);
                Type targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                object converted = Convert.ChangeType(rawValue, targetType, CultureInfo.InvariantCulture);
                property.SetValue(instance, converted);
            }

            results.Add(instance);
        }

        return results;
    }

    /// <summary>
    /// Détermine le nom SQL de table pour un type donné.
    /// </summary>
    private static string GetTableName(Type type)
    {
        // Convention: attribut [Table] prioritaire, sinon nom de type en minuscules.
        return type.GetCustomAttribute<TableAttribute>()?.Name ?? type.Name.ToLowerInvariant();
    }

    /// <summary>
    /// Liste les propriétés mappées vers la base (hors IgnoreColumn).
    /// </summary>
    private static IEnumerable<PropertyInfo> GetMappedProperties(Type type)
    {
        // Ne garde que les propriétés sérialisables vers SQL.
        return type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && !Attribute.IsDefined(p, typeof(IgnoreColumnAttribute)));
    }

    /// <summary>
    /// Retourne le nom SQL de colonne pour une propriété.
    /// </summary>
    private static string GetColumnName(PropertyInfo property)
    {
        return property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name.ToLowerInvariant();
    }

    /// <summary>
    /// Indique si une propriété correspond à une clé primaire.
    /// </summary>
    private static bool IsPrimaryKey(PropertyInfo property)
    {
        return Attribute.IsDefined(property, typeof(PrimaryKeyAttribute));
    }

    /// <summary>
    /// Crée une nouvelle connexion Npgsql sans l'ouvrir.
    /// </summary>
    private NpgsqlConnection OpenConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    /// <summary>
    /// Construit une commande SQL et injecte ses paramètres si présents.
    /// </summary>
    private static NpgsqlCommand CreateCommand(NpgsqlConnection conn, string sql, NpgsqlParameter[] parameters)
    {
        var cmd = new NpgsqlCommand(sql, conn);
        if (parameters.Length > 0)
        {
            cmd.Parameters.AddRange(parameters);
        }

        return cmd;
    }
}
