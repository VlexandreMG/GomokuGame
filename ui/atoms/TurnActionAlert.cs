using System.Windows.Forms;
using GomokuGame.core.events;

namespace GomokuGame.ui.atoms;

public static class TurnActionAlert
{
    /// <summary>
    /// Affiche la boîte de choix d'action du tour et retourne l'action sélectionnée.
    /// </summary>
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

    /// <summary>
    /// Informe le joueur de la sélection de ligne canon à effectuer sur le plateau.
    /// </summary>
    public static void ShowBombRowSelectionHint(IWin32Window? owner, string playerName, bool fromLeft)
    {
        string sideText = fromLeft ? "gauche" : "droite";
        MessageBox.Show(
            owner,
            $"{playerName}, choisis une ligne en cliquant sur un canon a l'extremite {sideText} du plateau.",
            "Selection de ligne",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

            /// <summary>
            /// Rappelle la combinaison clavier attendue pour envoyer le tir canon.
            /// </summary>
    public static void ShowBombPowerInputHint(IWin32Window? owner, string playerName, int row)
    {
        MessageBox.Show(
            owner,
            $"{playerName}, ligne {row} selectionnee.\nAppuie maintenant sur Ctrl + une touche du pave numerique (1 a 9) pour tirer.",
            "Puissance de la bombe",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}