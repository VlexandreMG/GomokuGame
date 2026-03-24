using System;

namespace GomokuGame.model;

[Table("partie")]
public sealed class PartieModel
{
    [PrimaryKey]
    [Column("id")]
    public int Id { get; set; }

    [Column("player1")]
    public string Player1 { get; set; } = string.Empty;

    [Column("player2")]
    public string Player2 { get; set; } = string.Empty;

    [Column("grid_size")]
    public int GridSize { get; set; } = 15;

    [Column("date_creation")]
    public DateTime DateCreation { get; set; }
}
