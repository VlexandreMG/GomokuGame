using GomokuGame.data;
using System;
using System.Collections.Generic;

namespace GomokuGame.service;

public sealed class SavedGameService
{
    private readonly DatabaseManager _databaseManager;

    /// <summary>
    /// Reçoit le gestionnaire de données utilisé pour récupérer les sauvegardes.
    /// </summary>
    public SavedGameService(DatabaseManager databaseManager)
    {
        _databaseManager = databaseManager;
    }

    /// <summary>
    /// Retourne les noms de sauvegarde disponibles (ou une liste vide en cas d'erreur).
    /// </summary>
    public IReadOnlyList<string> GetSavedGames()
    {
        try
        {
            return _databaseManager.GetSavedGames();
        }
        catch (Exception)
        {
            return Array.Empty<string>();
        }
    }
}
