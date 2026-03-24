using System.Windows.Forms;
using GomokuGame.data;
using GomokuGame.service;

namespace GomokuGame.ui.atoms;

public sealed class GameSetupResult
{
    public string Player1Name { get; }
    public string Player2Name { get; }
    public int GridSize { get; }

    public GameSetupResult(string player1Name, string player2Name, int gridSize)
    {
        Player1Name = player1Name;
        Player2Name = player2Name;
        GridSize = gridSize;
    }
}

public static class GameSetupMenu
{
    public static bool TryGetConfiguration(IWin32Window? owner, out GameSetupResult? result)
    {
        result = null;

        using Form dialog = new Form();
        dialog.Text = "Configuration de la partie";
        dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
        dialog.StartPosition = FormStartPosition.CenterParent;
        dialog.MinimizeBox = false;
        dialog.MaximizeBox = false;
        dialog.ClientSize = new System.Drawing.Size(420, 260);

        Label introLabel = new Label
        {
            Left = 16,
            Top = 12,
            Width = 388,
            Height = 34,
            Text = "Avant de lancer la partie, configure les joueurs et la taille de la grille."
        };

        Label colorLabel = new Label
        {
            Left = 16,
            Top = 46,
            Width = 388,
            Height = 34,
            Text = "Couleurs: Joueur 1 = Bleu, Joueur 2 = Rouge"
        };

        Label p1Label = new Label
        {
            Left = 16,
            Top = 92,
            Width = 130,
            Height = 24,
            Text = "Nom Joueur 1"
        };

        TextBox p1Input = new TextBox
        {
            Left = 150,
            Top = 90,
            Width = 254,
            Text = "Joueur 1"
        };

        Label p2Label = new Label
        {
            Left = 16,
            Top = 126,
            Width = 130,
            Height = 24,
            Text = "Nom Joueur 2"
        };

        TextBox p2Input = new TextBox
        {
            Left = 150,
            Top = 124,
            Width = 254,
            Text = "Joueur 2"
        };

        Label gridLabel = new Label
        {
            Left = 16,
            Top = 160,
            Width = 130,
            Height = 24,
            Text = "Taille grille"
        };

        NumericUpDown gridInput = new NumericUpDown
        {
            Left = 150,
            Top = 158,
            Width = 254,
            Minimum = 10,
            Maximum = 25,
            Value = 15
        };

        Button okButton = new Button
        {
            Left = 248,
            Top = 214,
            Width = 75,
            Text = "Lancer",
            DialogResult = DialogResult.OK
        };

        Button cancelButton = new Button
        {
            Left = 329,
            Top = 214,
            Width = 75,
            Text = "Annuler",
            DialogResult = DialogResult.Cancel
        };

        Button loadButton = new Button
        {
            Left = 16,
            Top = 214,
            Width = 120,
            Text = "Charger partie"
        };

        loadButton.Click += (_, _) => ShowSavedGamesList(owner);

        dialog.Controls.Add(introLabel);
        dialog.Controls.Add(colorLabel);
        dialog.Controls.Add(p1Label);
        dialog.Controls.Add(p1Input);
        dialog.Controls.Add(p2Label);
        dialog.Controls.Add(p2Input);
        dialog.Controls.Add(gridLabel);
        dialog.Controls.Add(gridInput);
        dialog.Controls.Add(okButton);
        dialog.Controls.Add(cancelButton);
        dialog.Controls.Add(loadButton);

        dialog.AcceptButton = okButton;
        dialog.CancelButton = cancelButton;

        if (dialog.ShowDialog(owner) != DialogResult.OK)
        {
            return false;
        }

        string p1 = string.IsNullOrWhiteSpace(p1Input.Text) ? "Joueur 1" : p1Input.Text.Trim();
        string p2 = string.IsNullOrWhiteSpace(p2Input.Text) ? "Joueur 2" : p2Input.Text.Trim();
        int gridSize = (int)gridInput.Value;

        result = new GameSetupResult(p1, p2, gridSize);
        return true;
    }

    private static void ShowSavedGamesList(IWin32Window? owner)
    {
        SavedGameService savedGameService = new SavedGameService(new DatabaseManager());
        var savedGames = savedGameService.GetSavedGames();

        using Form listDialog = new Form();
        listDialog.Text = "Parties sauvegardees";
        listDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
        listDialog.StartPosition = FormStartPosition.CenterParent;
        listDialog.MinimizeBox = false;
        listDialog.MaximizeBox = false;
        listDialog.ClientSize = new System.Drawing.Size(380, 300);

        Label infoLabel = new Label
        {
            Left = 12,
            Top = 10,
            Width = 356,
            Height = 24,
            Text = "Liste des parties disponibles"
        };

        ListBox gamesList = new ListBox
        {
            Left = 12,
            Top = 36,
            Width = 356,
            Height = 220
        };

        if (savedGames.Count == 0)
        {
            gamesList.Items.Add("Aucune partie sauvegardee");
            gamesList.Enabled = false;
        }
        else
        {
            foreach (string game in savedGames)
            {
                gamesList.Items.Add(game);
            }
        }

        Button closeButton = new Button
        {
            Left = 293,
            Top = 264,
            Width = 75,
            Text = "Fermer",
            DialogResult = DialogResult.OK
        };

        listDialog.Controls.Add(infoLabel);
        listDialog.Controls.Add(gamesList);
        listDialog.Controls.Add(closeButton);
        listDialog.AcceptButton = closeButton;
        listDialog.CancelButton = closeButton;

        listDialog.ShowDialog(owner);
    }
}