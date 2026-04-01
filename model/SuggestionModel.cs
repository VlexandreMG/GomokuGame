using System;

namespace GomokuGame.model;

[Table("suggestions")]
public sealed class SuggestionModel
{
    [PrimaryKey]
    [Column("id")]
    public int Id { get; set; }

    [Column("partie_id")]
    public int PartieId { get; set; }

    [Column("player_name")]
    public string PlayerName { get; set; } = string.Empty;

    [Column("tour_numero")]
    public int TourNumero { get; set; }

    [Column("suggestion_type")]
    public string SuggestionType { get; set; } = string.Empty;

    [Column("suggestion_count")]
    public int SuggestionCount { get; set; }

    [Column("points_json")]
    public string PointsJson { get; set; } = "[]";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
