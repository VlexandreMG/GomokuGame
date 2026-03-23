using System.Windows.Forms;
using GomokuGame.core.events;

namespace GomokuGame.ui.atoms;

public static class TurnActionAlert
{
    public static TurnAction ShowTurnChoice(IWin32Window? owner, string playerName)
    {
        DialogResult result = MessageBox.Show(
            owner,
            $"C'est le tour de {playerName}.\nChoisis une action:\nOui = Placer un point\nNon = Lancer une bombe",
            "Choix d'action",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);

        return result == DialogResult.No ? TurnAction.LaunchBomb : TurnAction.PlacePoint;
    }

    public static void ShowBombNotImplemented(IWin32Window? owner)
    {
        MessageBox.Show(
            owner,
            "L'action 'Lancer une bombe' sera implementee ensuite.",
            "Action non disponible",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }
}