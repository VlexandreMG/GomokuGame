using System.Windows.Forms;

namespace GomokuGame.ui.atoms;

public static class GameResultAlert
{
    /// <summary>
    /// Affiche le résultat final (scores + vainqueur) et demande si l'utilisateur veut rejouer.
    /// </summary>
    public static bool ShowResultAndAskReplay(
        IWin32Window? owner,
        string player1Name,
        int player1Score,
        string player2Name,
        int player2Score)
    {
        string winnerText;
        if (player1Score > player2Score)
        {
            winnerText = $"Vainqueur: {player1Name}";
        }
        else if (player2Score > player1Score)
        {
            winnerText = $"Vainqueur: {player2Name}";
        }
        else
        {
            winnerText = "Egalite";
        }

        using Form dialog = new Form();
        dialog.Text = "Resultat de la partie";
        dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
        dialog.StartPosition = FormStartPosition.CenterParent;
        dialog.MinimizeBox = false;
        dialog.MaximizeBox = false;
        dialog.ClientSize = new System.Drawing.Size(440, 220);

        Label title = new Label
        {
            Left = 16,
            Top = 14,
            Width = 408,
            Height = 28,
            Text = "Resultat de la partie",
            Font = new System.Drawing.Font("Segoe UI", 11, System.Drawing.FontStyle.Bold)
        };

        Label score1 = new Label
        {
            Left = 16,
            Top = 56,
            Width = 408,
            Height = 26,
            Text = $"{player1Name}: {player1Score} point(s)"
        };

        Label score2 = new Label
        {
            Left = 16,
            Top = 84,
            Width = 408,
            Height = 26,
            Text = $"{player2Name}: {player2Score} point(s)"
        };

        Label winner = new Label
        {
            Left = 16,
            Top = 120,
            Width = 408,
            Height = 30,
            Text = winnerText,
            Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)
        };

        Button replay = new Button
        {
            Left = 196,
            Top = 170,
            Width = 140,
            Text = "Refaire une partie",
            DialogResult = DialogResult.Retry
        };

        Button close = new Button
        {
            Left = 344,
            Top = 170,
            Width = 80,
            Text = "Fermer",
            DialogResult = DialogResult.Cancel
        };

        dialog.Controls.Add(title);
        dialog.Controls.Add(score1);
        dialog.Controls.Add(score2);
        dialog.Controls.Add(winner);
        dialog.Controls.Add(replay);
        dialog.Controls.Add(close);

        dialog.AcceptButton = replay;
        dialog.CancelButton = close;

        return dialog.ShowDialog(owner) == DialogResult.Retry;
    }
}