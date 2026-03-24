using GomokuGame.core;

namespace GomokuGame.core.events;

public enum TurnAction
{
    // Le joueur pose un point classique sur la grille.
    PlacePoint,
    // Le joueur déclenche un tir canon.
    LaunchBomb
}

public sealed class TurnDetector
{
    // Responsabilité unique: savoir "qui joue" et "quelle action est choisie".
    public string Player1 { get; }
    public string Player2 { get; }
    public TurnAction PlacePointAction => TurnAction.PlacePoint;
    public TurnAction LaunchBombAction => TurnAction.LaunchBomb;

    public string CurrentPlayer { get; private set; }
    public TurnAction CurrentAction { get; private set; }

    /// <summary>
    /// Initialise le détecteur de tour avec les deux joueurs (joueur 1 commence).
    /// </summary>
    public TurnDetector(string player1, string player2)
    {
        // Le joueur 1 commence toujours par convention.
        Player1 = player1;
        Player2 = player2;
        CurrentPlayer = Player1;
        CurrentAction = TurnAction.PlacePoint;
        TerminalLogger.Action($"TurnDetector initialized: current player is {CurrentPlayer}");
    }

    /// <summary>
    /// Enregistre l'action choisie par le joueur courant (point ou bombe).
    /// </summary>
    public void SetCurrentAction(TurnAction action)
    {
        // Le choix d'action est explicitement stocké avant exécution du tour.
        CurrentAction = action;
        TerminalLogger.Action($"{CurrentPlayer} selected action {CurrentAction}");
    }

    /// <summary>
    /// Bascule vers le joueur suivant et remet l'action par défaut à "placer un point".
    /// </summary>
    public void AdvanceTurn()
    {
        // Au changement de tour, on bascule de joueur et on remet l'action par défaut.
        CurrentPlayer = GetOpponent(CurrentPlayer);
        CurrentAction = TurnAction.PlacePoint;
        TerminalLogger.Action($"Turn switched to {CurrentPlayer}");
    }

    /// <summary>
    /// Retourne le joueur opposé au joueur passé en paramètre.
    /// </summary>
    private string GetOpponent(string player)
    {
        return player == Player1 ? Player2 : Player1;
    }
}