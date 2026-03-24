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

    /// <summary>
    /// Passe l'état de la partie à "en cours" après la configuration initiale.
    /// </summary>
    public void StartGame()
    {
        Status = EtatPartieStatus.EnCours;
        TerminalLogger.Action("EtatPartie: status set to EnCours");
    }

    /// <summary>
    /// Termine explicitement la partie et journalise la raison d'arrêt.
    /// </summary>
    public void EndGame(string reason)
    {
        Status = EtatPartieStatus.Finie;
        TerminalLogger.Action($"EtatPartie: status set to Finie, reason={reason}");
    }
}