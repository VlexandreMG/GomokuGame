using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GomokuGame.ui.atoms;

/// <summary>
/// Indicateur affichant le nombre de suggestions gagnantes disponibles.
/// Permet de cliquer pour afficher/masquer les croix de suggestion.
/// </summary>
public class SuggestionIndicator : BaseComponent
{
    public int SuggestionCount { get; set; } = 0;
    public bool SuggestionsVisible { get; set; } = false;
    
    private Color? _playerColor = null;
    public event EventHandler? OnToggleSuggestions;

    protected override void CreateComponents()
    {
    }

    protected override void SetupLayout()
    {
        this.Width = 150;
        this.Height = 35;
    }

    protected override void ApplyDefaultStyles()
    {
        this.BackColor = Color.LightBlue;
        this.ForeColor = Color.White;
        this.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        this.BorderStyle = BorderStyle.FixedSingle;
    }

    protected override void SetupEventHandlers()
    {
        this.Click += SuggestionIndicator_Click;
        this.MouseEnter += SuggestionIndicator_MouseEnter;
        this.MouseLeave += SuggestionIndicator_MouseLeave;
    }

    protected override void Initialize()
    {
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;

        if (SuggestionCount == 0)
        {
            e.Graphics.DrawString("Aucune suggestion", this.Font, new SolidBrush(Color.Gray), 5, 8);
            return;
        }

        string text = $"💡 {SuggestionCount} suggestion{(SuggestionCount > 1 ? "s" : "")}";
        
        // Fond coloré si des suggestions sont visibles
        if (SuggestionsVisible && _playerColor.HasValue)
        {
            using (Brush brush = new SolidBrush(Color.FromArgb(100, _playerColor.Value)))
            {
                g.FillRectangle(brush, this.ClientRectangle);
            }
        }

        e.Graphics.DrawString(text, this.Font, new SolidBrush(this.ForeColor), 5, 8);
    }

    private void SuggestionIndicator_Click(object? sender, EventArgs e)
    {
        SuggestionsVisible = !SuggestionsVisible;
        OnToggleSuggestions?.Invoke(this, EventArgs.Empty);
        this.Invalidate();
    }

    private void SuggestionIndicator_MouseEnter(object? sender, EventArgs e)
    {
        this.BackColor = Color.SkyBlue;
    }

    private void SuggestionIndicator_MouseLeave(object? sender, EventArgs e)
    {
        this.BackColor = Color.LightBlue;
    }

    public void SetPlayerColor(Color color)
    {
        _playerColor = color;
    }
}
