using GomokuGame.core;
using GomokuGame.data;
using GomokuGame.model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GomokuGame.service;

/// <summary>
/// Service de gestion des suggestions de coups gagnants.
/// Responsable de l'analyse, du stockage et de la récupération des suggestions.
/// </summary>
public sealed class SuggestionService
{
    private readonly GenericRepository _repository;
    private readonly SuggestionEngine _suggestionEngine;

    public SuggestionService(GenericRepository repository, GomokuEngine engine)
    {
        _repository = repository;
        _suggestionEngine = new SuggestionEngine(engine);
    }

    /// <summary>
    /// Analyse les suggestions pour le joueur courant et les persistantes en base.
    /// </summary>
    public List<(Point Position, SuggestionType Type)> AnalyzeAndSaveSuggestions(
        int partieId, 
        string playerName, 
        int tourNumero, 
        Color playerColor)
    {
        var suggestions = _suggestionEngine.AnalyzeSuggestions(playerColor);

        // Persister chaque suggestion
        foreach (var (position, suggestionType) in suggestions)
        {
            try
            {
                var model = new SuggestionModel
                {
                    PartieId = partieId,
                    PlayerName = playerName,
                    TourNumero = tourNumero,
                    PositionX = position.X,
                    PositionY = position.Y,
                    SuggestionType = (int)suggestionType,
                    DateCreation = DateTime.UtcNow
                };

                _repository.Insert(model);
                TerminalLogger.Action($"Suggestion saved: partie={partieId}, player={playerName}, type={suggestionType}, pos=({position.X},{position.Y})");
            }
            catch (Exception ex)
            {
                TerminalLogger.Action($"Suggestion save failed: {ex.Message}");
            }
        }

        return suggestions;
    }

    /// <summary>
    /// Récupère toutes les suggestions d'une partie.
    /// </summary>
    public IReadOnlyList<SuggestionModel> TryGetSuggestionsByPartieId(int partieId)
    {
        try
        {
            return _repository
                .FindByColumn<SuggestionModel>("partie_id", partieId)
                .OrderBy(s => s.TourNumero)
                .ThenBy(s => s.Id)
                .ToList();
        }
        catch (Exception ex)
        {
            TerminalLogger.Action($"Suggestion list unavailable: {ex.Message}");
            return Array.Empty<SuggestionModel>();
        }
    }

    /// <summary>
    /// Récupère les suggestions pour un tour spécifique et un joueur.
    /// </summary>
    public IReadOnlyList<SuggestionModel> TryGetSuggestionsForTurn(int partieId, int tourNumero, string playerName)
    {
        try
        {
            return _repository
                .FindByColumn<SuggestionModel>("partie_id", partieId)
                .Where(s => s.TourNumero == tourNumero && s.PlayerName == playerName)
                .ToList();
        }
        catch (Exception ex)
        {
            TerminalLogger.Action($"Suggestion for turn unavailable: {ex.Message}");
            return Array.Empty<SuggestionModel>();
        }
    }

    /// <summary>
    /// Supprime les suggestions d'une partie (lors de undo).
    /// </summary>
    public bool TryDeleteSuggestionsForTurn(int partieId, int tourNumero)
    {
        try
        {
            const string sql = @"
DELETE FROM suggestions
WHERE partie_id = @partieId AND tour_numero >= @tourNumero";

            _repository.ExecuteNonQuery(
                sql,
                new NpgsqlParameter("@partieId", partieId),
                new NpgsqlParameter("@tourNumero", tourNumero));

            return true;
        }
        catch (Exception ex)
        {
            TerminalLogger.Action($"Suggestion deletion failed: {ex.Message}");
            return false;
        }
    }
}
