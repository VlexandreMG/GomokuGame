using GomokuGame.core;

namespace GomokuGame.core.events;

public enum EtatPartieStatus
{
    NotStarted,
    EnCours,
    Finie
}

public sealed class EtatPartie
{
    public EtatPartieStatus Status { get; private set; } = EtatPartieStatus.NotStarted;
    public bool IsInProgress => Status == EtatPartieStatus.EnCours;

    public void StartGame()
    {
        Status = EtatPartieStatus.EnCours;
        TerminalLogger.Action("EtatPartie: status set to EnCours");
    }

    public void EndGame(string reason)
    {
        Status = EtatPartieStatus.Finie;
        TerminalLogger.Action($"EtatPartie: status set to Finie, reason={reason}");
    }
}