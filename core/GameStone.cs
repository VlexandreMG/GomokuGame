using System.Drawing;

namespace GomokuGame.core;

public sealed class GameStone
{
    public int X { get; }
    public int Y { get; }
    public Color Color { get; }

    /// <summary>
    /// Crée une pierre immuable avec sa position logique et sa couleur.
    /// </summary>
    public GameStone(int x, int y, Color color)
    {
        X = x;
        Y = y;
        Color = color;
    }
}