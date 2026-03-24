using GomokuGame.core;
using GomokuGame.data;
using GomokuGame.model;
using System;
using System.Collections.Generic;

namespace GomokuGame.service;

public sealed class ActionService
{
    private readonly GenericRepository _repository;

    public ActionService(GenericRepository repository)
    {
        _repository = repository;
    }

    public bool TryRecordPointAction(int partieId, string playerName, int x, int y, int tourNumero)
    {
        return TryRecordAction(partieId, playerName, x, y, tourNumero, "POINT");
    }

    public bool TryRecordBombAction(int partieId, string playerName, int x, int y, int tourNumero)
    {
        return TryRecordAction(partieId, playerName, x, y, tourNumero, "BOMBE");
    }

    public IReadOnlyList<ActionModel> TryGetByPartieId(int partieId)
    {
        try
        {
            return _repository.FindByColumn<ActionModel>("partie_id", partieId);
        }
        catch (Exception ex)
        {
            TerminalLogger.Action($"Action list unavailable: {ex.Message}");
            return Array.Empty<ActionModel>();
        }
    }

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
