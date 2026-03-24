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
    private readonly string _connectionString;

    public GenericRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public bool TestConnection(out string? error)
    {
        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
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

    public IReadOnlyList<T> GetAll<T>() where T : new()
    {
        string tableName = GetTableName(typeof(T));
        string sql = $"SELECT * FROM {tableName}";
        return ExecuteQuery<T>(sql);
    }

    public IReadOnlyList<T> FindByColumn<T>(string columnName, object value) where T : new()
    {
        string tableName = GetTableName(typeof(T));
        string sql = $"SELECT * FROM {tableName} WHERE {columnName} = @value";
        return ExecuteQuery<T>(sql, new NpgsqlParameter("@value", value));
    }

    public int Insert<T>(T entity)
    {
        Type type = typeof(T);
        string tableName = GetTableName(type);
        var mappedProperties = GetMappedProperties(type)
            .Where(p => !IsPrimaryKey(p))
            .ToList();

        string columns = string.Join(", ", mappedProperties.Select(GetColumnName));
        string values = string.Join(", ", mappedProperties.Select((_, i) => $"@p{i}"));
        string sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values}) RETURNING id";

        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        using var cmd = new NpgsqlCommand(sql, conn);
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

    public int ExecuteNonQuery(string sql, params NpgsqlParameter[] parameters)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        using var cmd = new NpgsqlCommand(sql, conn);
        if (parameters.Length > 0)
        {
            cmd.Parameters.AddRange(parameters);
        }

        return cmd.ExecuteNonQuery();
    }

    private IReadOnlyList<T> ExecuteQuery<T>(string sql, params NpgsqlParameter[] parameters) where T : new()
    {
        var results = new List<T>();
        Type type = typeof(T);
        var mappedProperties = GetMappedProperties(type);

        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        using var cmd = new NpgsqlCommand(sql, conn);
        if (parameters.Length > 0)
        {
            cmd.Parameters.AddRange(parameters);
        }

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

    private static string GetTableName(Type type)
    {
        return type.GetCustomAttribute<TableAttribute>()?.Name ?? type.Name.ToLowerInvariant();
    }

    private static IEnumerable<PropertyInfo> GetMappedProperties(Type type)
    {
        return type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && !Attribute.IsDefined(p, typeof(IgnoreColumnAttribute)));
    }

    private static string GetColumnName(PropertyInfo property)
    {
        return property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name.ToLowerInvariant();
    }

    private static bool IsPrimaryKey(PropertyInfo property)
    {
        return Attribute.IsDefined(property, typeof(PrimaryKeyAttribute));
    }
}
