using GomokuGame.core;
using GomokuGame.data;
using GomokuGame.model;
using Npgsql;
using System.Drawing;
using System.Text.Json;

namespace GomokuGame.service;

public sealed class SuggestionService
{
    public const string SuggestionOneMove = "ONE_MOVE";
    public const string SuggestionTwoMoves = "TWO_MOVES";

    private readonly GenericRepository _repository;

    private sealed class SuggestionPointDto
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public SuggestionService(GenericRepository repository)
    {
        _repository = repository;
    }

    public bool TrySaveSnapshot(int partieId, string playerName, int tourNumero, string suggestionType, IReadOnlyList<Point> points)
    {
        if (partieId <= 0 || string.IsNullOrWhiteSpace(playerName) || tourNumero <= 0)
        {
            return false;
        }

        try
        {
            const string deleteSql = @"
DELETE FROM suggestions
WHERE partie_id = @partieId
  AND player_name = @playerName
  AND tour_numero = @tourNumero
  AND suggestion_type = @suggestionType;";

            _repository.ExecuteNonQuery(
                deleteSql,
                new NpgsqlParameter("@partieId", partieId),
                new NpgsqlParameter("@playerName", playerName),
                new NpgsqlParameter("@tourNumero", tourNumero),
                new NpgsqlParameter("@suggestionType", suggestionType));

            SuggestionModel model = new SuggestionModel
            {
                PartieId = partieId,
                PlayerName = playerName,
                TourNumero = tourNumero,
                SuggestionType = suggestionType,
                SuggestionCount = points.Count,
                PointsJson = SerializePoints(points)
            };

            int id = _repository.Insert(model);
            TerminalLogger.Action($"Suggestions saved in database: id={id}, type={suggestionType}, count={points.Count}, partie={partieId}, tour={tourNumero}, player={playerName}");
            return id > 0;
        }
        catch (Exception ex)
        {
            TerminalLogger.Action($"Suggestion save skipped (db unavailable): {ex.Message}");
            return false;
        }
    }

    public bool TryGetSnapshot(int partieId, string playerName, int tourNumero, string suggestionType, out IReadOnlyList<Point> points)
    {
        try
        {
            SuggestionModel? model = _repository
                .FindByColumn<SuggestionModel>("partie_id", partieId)
                .Where(s => string.Equals(s.PlayerName, playerName, StringComparison.OrdinalIgnoreCase))
                .Where(s => s.TourNumero == tourNumero)
                .Where(s => string.Equals(s.SuggestionType, suggestionType, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(s => s.Id)
                .FirstOrDefault();

            if (model is null)
            {
                points = Array.Empty<Point>();
                return false;
            }

            points = DeserializePoints(model.PointsJson);
            TerminalLogger.Action($"Suggestions loaded from database: type={suggestionType}, count={points.Count}, partie={partieId}, tour={tourNumero}, player={playerName}");
            return true;
        }
        catch (Exception ex)
        {
            TerminalLogger.Action($"Suggestion read unavailable: {ex.Message}");
            points = Array.Empty<Point>();
            return false;
        }
    }

    private static string SerializePoints(IReadOnlyList<Point> points)
    {
        List<SuggestionPointDto> payload = points
            .Select(p => new SuggestionPointDto { X = p.X, Y = p.Y })
            .ToList();

        return JsonSerializer.Serialize(payload);
    }

    private static IReadOnlyList<Point> DeserializePoints(string pointsJson)
    {
        if (string.IsNullOrWhiteSpace(pointsJson))
        {
            return Array.Empty<Point>();
        }

        List<SuggestionPointDto>? payload = JsonSerializer.Deserialize<List<SuggestionPointDto>>(pointsJson);
        if (payload is null)
        {
            return Array.Empty<Point>();
        }

        return payload.Select(p => new Point(p.X, p.Y)).ToList();
    }
}
