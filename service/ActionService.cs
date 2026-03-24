using GomokuGame.core;
using GomokuGame.data;
using GomokuGame.model;
using Npgsql;
using System;
using System.Collections.Generic;

namespace GomokuGame.service;

public sealed class ActionService
{
    // Constantes métier pour éviter les chaînes magiques dispersées.
    private const string ActionPoint = "POINT";
    private const string ActionBombe = "BOMBE";

    private readonly GenericRepository _repository;

    /// <summary>
    /// Reçoit le repository générique pour effectuer les opérations SQL sur la table actions.
    /// </summary>
    public ActionService(GenericRepository repository)
    {
        _repository = repository;
    }

    public bool TryRecordPointAction(int partieId, string playerName, int x, int y, int tourNumero)
    {
        // Enregistre un placement de point en base.
        return TryRecordAction(partieId, playerName, x, y, tourNumero, ActionPoint);
    }

    public bool TryRecordBombAction(int partieId, string playerName, int x, int y, int tourNumero)
    {
        // Enregistre un tir canon (même si effet nul) car il consomme le tour.
        return TryRecordAction(partieId, playerName, x, y, tourNumero, ActionBombe);
    }

    public IReadOnlyList<ActionModel> TryGetByPartieId(int partieId)
    {
        try
        {
            // Tri déterministe pour rejouer une partie sans ambiguïté.
            return _repository
                .FindByColumn<ActionModel>("partie_id", partieId)
                .OrderBy(a => a.TourNumero)
                .ThenBy(a => a.Id)
                .ToList();
        }
        catch (Exception ex)
        {
            TerminalLogger.Action($"Action list unavailable: {ex.Message}");
            return Array.Empty<ActionModel>();
        }
    }

    public bool TryDeleteLastActions(int partieId, int count)
    {
        // Suppression des derniers coups: utilisée par le bouton "Retour" (undo).
        if (partieId <= 0 || count <= 0)
        {
            return true;
        }

        try
        {
            const string sql = @"
DELETE FROM actions
WHERE ctid IN (
    SELECT ctid
    FROM actions
    WHERE partie_id = @partieId
    ORDER BY tour_numero DESC, id DESC
    LIMIT @count
);";

            int deletedRows = _repository.ExecuteNonQuery(
                sql,
                new NpgsqlParameter("@partieId", partieId),
                new NpgsqlParameter("@count", count));

            TerminalLogger.Action($"Undo persisted in database: deletedActions={deletedRows}, partie={partieId}");
            return true;
        }
        catch (Exception ex)
        {
            TerminalLogger.Action($"Undo persistence skipped (db unavailable): {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Méthode commune d'enregistrement d'action (point/bombe) dans l'historique de partie.
    /// </summary>
    private bool TryRecordAction(int partieId, string playerName, int x, int y, int tourNumero, string type)
    {
        if (partieId <= 0)
        {
            return false;
        }

        try
        {
            var action = new ActionModel
            {
                PartieId = partieId,
                PlayerName = playerName,
                X = x,
                Y = y,
                TourNumero = tourNumero,
                TypeAction = type
            };

            int actionId = _repository.Insert(action);
            TerminalLogger.Action($"Action saved in database: id={actionId}, type={type}, partie={partieId}");
            return actionId > 0;
        }
        catch (Exception ex)
        {
            TerminalLogger.Action($"Action save skipped (db unavailable): {ex.Message}");
            return false;
        }
    }
}
