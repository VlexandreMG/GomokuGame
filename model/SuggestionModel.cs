using System;

namespace GomokuGame.model;

[Table("suggestions")]
public class SuggestionModel
{
    [PrimaryKey]
    [Column("id")]
    public int Id { get; set; }

    [Column("partie_id")]
    public int PartieId { get; set; }

    [Column("player_name")]
    public string PlayerName { get; set; } = "";

    [Column("tour_numero")]
    public int TourNumero { get; set; }

    [Column("position_x")]
    public int PositionX { get; set; }

    [Column("position_y")]
    public int PositionY { get; set; }

    [Column("suggestion_type")]
    public int SuggestionType { get; set; } // 1=ThreePoints, 2=FourPoints

    [Column("date_creation")]
    public DateTime DateCreation { get; set; }
}
