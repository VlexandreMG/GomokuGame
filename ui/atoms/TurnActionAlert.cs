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

    public static bool TryGetBombParameters(IWin32Window? owner, string playerName, int gridSize, out int selectedLine, out int selectedPower)
    {
        selectedLine = 1;
        selectedPower = 9;

        using Form dialog = new Form();
        dialog.Text = $"Bombe - {playerName}";
        dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
        dialog.StartPosition = FormStartPosition.CenterParent;
        dialog.MinimizeBox = false;
        dialog.MaximizeBox = false;
        dialog.ClientSize = new System.Drawing.Size(360, 200);

        Label titleLabel = new Label
        {
            AutoSize = false,
            Left = 16,
            Top = 14,
            Width = 328,
            Height = 44,
            Text = $"{playerName}, choisis la ligne et la puissance (1..9) de la bombe.",
        };

        Label lineLabel = new Label
        {
            AutoSize = true,
            Left = 16,
            Top = 70,
            Text = $"Ligne (1..{gridSize})"
        };

        NumericUpDown lineInput = new NumericUpDown
        {
            Left = 160,
            Top = 66,
            Width = 184,
            Minimum = 1,
            Maximum = gridSize,
            Value = 1
        };

        Label powerLabel = new Label
        {
            AutoSize = true,
            Left = 16,
            Top = 108,
            Text = "Puissance (1..9)"
        };

        NumericUpDown powerInput = new NumericUpDown
        {
            Left = 160,
            Top = 104,
            Width = 184,
            Minimum = 1,
            Maximum = 9,
            Value = 9
        };

        Button confirmButton = new Button
        {
            Left = 188,
            Top = 148,
            Width = 75,
            Text = "Valider",
            DialogResult = DialogResult.OK
        };

        Button cancelButton = new Button
        {
            Left = 269,
            Top = 148,
            Width = 75,
            Text = "Annuler",
            DialogResult = DialogResult.Cancel
        };

        dialog.Controls.Add(titleLabel);
        dialog.Controls.Add(lineLabel);
        dialog.Controls.Add(lineInput);
        dialog.Controls.Add(powerLabel);
        dialog.Controls.Add(powerInput);
        dialog.Controls.Add(confirmButton);
        dialog.Controls.Add(cancelButton);

        dialog.AcceptButton = confirmButton;
        dialog.CancelButton = cancelButton;

        DialogResult result = dialog.ShowDialog(owner);
        if (result != DialogResult.OK)
        {
            return false;
        }

        selectedLine = (int)lineInput.Value;
        selectedPower = (int)powerInput.Value;
        return true;
    }
}