using GomokuGame.core;

namespace GomokuGame.core.events;

public enum TurnAction
{
    PlacePoint,
    LaunchBomb
}

public sealed class TurnDetector
{
    public string Player1 { get; }
    public string Player2 { get; }
    public TurnAction PlacePointAction => TurnAction.PlacePoint;
    public TurnAction LaunchBombAction => TurnAction.LaunchBomb;

    public string CurrentPlayer { get; private set; }
    public TurnAction CurrentAction { get; private set; }

    public TurnDetector(string player1, string player2)
    {
        Player1 = player1;
        Player2 = player2;
        CurrentPlayer = Player1;
        CurrentAction = TurnAction.PlacePoint;
        TerminalLogger.Action($"TurnDetector initialized: current player is {CurrentPlayer}");
    }

    public void SetCurrentAction(TurnAction action)
    {
        CurrentAction = action;
        TerminalLogger.Action($"{CurrentPlayer} selected action {CurrentAction}");
    }

    public void AdvanceTurn()
    {
        CurrentPlayer = (CurrentPlayer == Player1) ? Player2 : Player1;
        CurrentAction = TurnAction.PlacePoint;
        TerminalLogger.Action($"Turn switched to {CurrentPlayer}");
    }
}