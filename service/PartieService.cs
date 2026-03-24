using GomokuGame.core;
using GomokuGame.data;
using GomokuGame.model;
using System;
using System.Collections.Generic;

namespace GomokuGame.service;

public sealed class PartieService
{
    private readonly GenericRepository _repository;

    public PartieService(GenericRepository repository)
    {
        _repository = repository;
    }

    public int TryCreatePartie(string player1, string player2, int gridSize)
    {
        try
        {
            var partie = new PartieModel
            {
                Player1 = player1,
                Player2 = player2,
                GridSize = gridSize,
                DateCreation = DateTime.UtcNow
            };

            int id = _repository.Insert(partie);
            TerminalLogger.Action($"Partie saved in database with id={id}");
            return id;
        }
        catch (Exception ex)
        {
            TerminalLogger.Action($"Partie save skipped (db unavailable): {ex.Message}");
            return 0;
        }
    }

    public IReadOnlyList<PartieModel> TryGetParties()
    {
        try
        {
            return _repository.GetAll<PartieModel>();
        }
        catch (Exception ex)
        {
            TerminalLogger.Action($"Partie list unavailable: {ex.Message}");
            return Array.Empty<PartieModel>();
        }
    }

    public PartieModel? TryGetPartieById(int id)
    {
        try
        {
            return _repository.FindByColumn<PartieModel>("id", id).FirstOrDefault();
        }
        catch (Exception ex)
        {
            TerminalLogger.Action($"Partie read unavailable: {ex.Message}");
            return null;
        }
    }
}
