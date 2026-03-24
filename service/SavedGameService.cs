using GomokuGame.data;
using System;
using System.Collections.Generic;

namespace GomokuGame.service;

public sealed class SavedGameService
{
    private readonly DatabaseManager _databaseManager;

    public SavedGameService(DatabaseManager databaseManager)
    {
        _databaseManager = databaseManager;
    }

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
