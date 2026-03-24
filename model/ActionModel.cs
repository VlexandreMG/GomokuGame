namespace GomokuGame.model;

[Table("actions")]
public sealed class ActionModel
{
    [PrimaryKey]
    [Column("id")]
    public int Id { get; set; }

    [Column("partie_id")]
    public int PartieId { get; set; }

    [Column("player_name")]
    public string PlayerName { get; set; } = string.Empty;

    [Column("x")]
    public int X { get; set; }

    [Column("y")]
    public int Y { get; set; }

    [Column("tour_numero")]
    public int TourNumero { get; set; }

    [Column("type_action")]
    public string TypeAction { get; set; } = string.Empty;
}
