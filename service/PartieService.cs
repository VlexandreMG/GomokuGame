using GomokuGame.core;
using GomokuGame.data;
using GomokuGame.model;
using System;
using System.Collections.Generic;

namespace GomokuGame.service;

public sealed class PartieService
{
    // Responsabilité: opérations métier liées aux parties (création/lecture).
    private readonly GenericRepository _repository;

    /// <summary>
    /// Reçoit le repository générique qui porte les accès à la table partie.
    /// </summary>
    public PartieService(GenericRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Crée une nouvelle partie en base avec les paramètres de la configuration initiale.
    /// </summary>
    public int TryCreatePartie(string player1, string player2, int gridWidth, int gridHeight)
    {
        try
        {
            PartieModel partie = BuildPartieModel(player1, player2, gridWidth, gridHeight);

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

    /// <summary>
    /// Retourne l'ensemble des parties enregistrées.
    /// </summary>
    public IReadOnlyList<PartieModel> TryGetParties()
    {
        try
        {
            // Renvoie la liste brute; le tri de présentation est fait côté UI.
            return _repository.GetAll<PartieModel>();
        }
        catch (Exception ex)
        {
            TerminalLogger.Action($"Partie list unavailable: {ex.Message}");
            return Array.Empty<PartieModel>();
        }
    }

    /// <summary>
    /// Retourne une partie par identifiant (ou null si indisponible).
    /// </summary>
    public PartieModel? TryGetPartieById(int id)
    {
        try
        {
            // On prend le premier résultat pour rester tolérant
            // même si la requête renvoie une liste.
            return _repository.FindByColumn<PartieModel>("id", id).FirstOrDefault();
        }
        catch (Exception ex)
        {
            TerminalLogger.Action($"Partie read unavailable: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Construit le modèle de persistance de partie à insérer en base.
    /// </summary>
    private static PartieModel BuildPartieModel(string player1, string player2, int gridWidth, int gridHeight)
    {
        int persistedGridSize = Math.Max(gridWidth, gridHeight);

        return new PartieModel
        {
            Player1 = player1,
            Player2 = player2,
            GridSize = persistedGridSize,
            DateCreation = DateTime.UtcNow
        };
    }
}
