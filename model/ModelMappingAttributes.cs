using System;

namespace GomokuGame.model;

[AttributeUsage(AttributeTargets.Class)]
public sealed class TableAttribute : Attribute
{
    public string Name { get; }

    public TableAttribute(string name)
    {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class ColumnAttribute : Attribute
{
    public string Name { get; }

    public ColumnAttribute(string name)
    {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class PrimaryKeyAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class IgnoreColumnAttribute : Attribute
{
}
