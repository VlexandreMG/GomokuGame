using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GomokuGame.ui.atoms;

/// <summary>
/// Atome représentant une suggestion de coup gagnant.
/// Affiche une croix clignotante avec animation.
/// </summary>
public class SuggestionMarker : BaseComponent
{
    public Point Coordinates { get; set; }
    public Color MarkerColor { get; set; } = Color.Yellow;
    
    private int _visualSize = 28;
    private float _opacity = 1.0f;
    private const float BLINK_SPEED = 0.08f;
    private bool _blinkingIn = false;

    public SuggestionMarker(int x, int y, Color color)
    {
        Coordinates = new Point(x, y);
        MarkerColor = color;
    }

    protected override void CreateComponents()
    {
    }

    protected override void SetupLayout()
    {
        this.Size = new Size(_visualSize, _visualSize);
    }

    protected override void ApplyDefaultStyles()
    {
        this.BackColor = Color.Transparent;
    }

    protected override void SetupEventHandlers()
    {
    }

    protected override void Initialize()
    {
    }

    /// <summary>
    /// Dessine une croix clignotante à la position visuelle spécifiée.
    /// </summary>
    public void Draw(Graphics g, Point visualLocation)
    {
        // Animation de clignotement
        UpdateBlinkAnimation();

        // Créer une couleur avec transparence basée sur l'animation
        Color blinkColor = Color.FromArgb((int)(_opacity * 255), MarkerColor);

        using (Pen pen = new Pen(blinkColor, 3))
        {
            pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

            int halfSize = _visualSize / 2;
            int x = visualLocation.X;
            int y = visualLocation.Y;

            // Dessiner une croix (X)
            g.DrawLine(pen, 
                x - halfSize, y - halfSize, 
                x + halfSize, y + halfSize);
            
            g.DrawLine(pen, 
                x + halfSize, y - halfSize, 
                x - halfSize, y + halfSize);
        }
    }

    /// <summary>
    /// Gère l'animation de clignotement.
    /// </summary>
    private void UpdateBlinkAnimation()
    {
        if (_blinkingIn)
        {
            _opacity += BLINK_SPEED;
            if (_opacity >= 1.0f)
            {
                _opacity = 1.0f;
                _blinkingIn = false;
            }
        }
        else
        {
            _opacity -= BLINK_SPEED;
            if (_opacity <= 0.3f)
            {
                _opacity = 0.3f;
                _blinkingIn = true;
            }
        }
    }
}
