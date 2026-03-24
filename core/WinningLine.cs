using System.Drawing;

namespace GomokuGame.core;

public sealed class WinningLine
{
    public Point Start { get; }
    public Point End { get; }
    public Color Color { get; }

    /// <summary>
    /// Représente une ligne gagnante: point de départ, point de fin et couleur du joueur.
    /// </summary>
    public WinningLine(Point start, Point end, Color color)
    {
        Start = start;
        End = end;
        Color = color;
    }
}