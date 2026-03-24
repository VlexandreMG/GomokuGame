using System.Collections.Generic;
using System.Drawing;

namespace GomokuGame.core;

public sealed class GomokuEngine
{
    // Etat interne du moteur: positions occupées, historique des pierres, protection des lignes.
    private readonly Dictionary<Point, GameStone> _stonesByPosition = new Dictionary<Point, GameStone>();
    private readonly List<GameStone> _stones = new List<GameStone>();
    private readonly HashSet<Point> _protectedWinningPoints = new HashSet<Point>();
    private readonly (int Dx, int Dy)[] _scanDirections =
    {
        (1, 0),
        (0, 1),
        (1, 1),
        (1, -1)
    };

    public int GridWidth { get; }
    public int GridHeight { get; }
    public IReadOnlyList<GameStone> Stones => _stones;

    /// <summary>
    /// Initialise le moteur avec une taille de grille fixe.
    /// </summary>
    public GomokuEngine(int gridWidth, int gridHeight)
    {
        GridWidth = gridWidth;
        GridHeight = gridHeight;
        TerminalLogger.Action($"Engine created with grid size {GridWidth}x{GridHeight}");
    }

    /// <summary>
    /// Tente de poser une pierre, puis détecte et retourne les nouvelles lignes gagnantes.
    /// </summary>
    public bool TryPlaceStone(int x, int y, Color stoneColor, out GameStone? placedStone, out IReadOnlyList<WinningLine> newWinningLines)
    {
        // Place une pierre si la case est valide, puis détecte les nouvelles lignes de 5.
        TerminalLogger.Action($"TryPlaceStone called at ({x},{y}) with color={stoneColor.Name}");
        placedStone = null;
        newWinningLines = new List<WinningLine>();

        if (!IsInsideBoard(x, y))
        {
            TerminalLogger.Action($"Rejected move ({x},{y}) because it is outside board bounds");
            return false;
        }

        var position = new Point(x, y);
        if (_stonesByPosition.ContainsKey(position))
        {
            TerminalLogger.Action($"Rejected move ({x},{y}) because position is already occupied");
            return false;
        }

        var stone = new GameStone(x, y, stoneColor);
        _stones.Add(stone);
        _stonesByPosition[position] = stone;
        TerminalLogger.Action($"Stone placed: color={stoneColor.Name}, position=({x},{y}), moveIndex={_stones.Count}");

        placedStone = stone;
        newWinningLines = FindNewWinningLines(stone);
        ProtectPointsFromNewWinningLines(newWinningLines);
        TerminalLogger.Action($"Scan finished for ({x},{y}), new winning lines={newWinningLines.Count}");
        return true;
    }

    /// <summary>
    /// Calcule la case ciblée par une bombe (ligne + puissance), puis applique l'impact.
    /// </summary>
    public bool TryLaunchBomb(bool fromLeft, int lineOneBased, int power, Color shooterColor, out Point targetCell, out GameStone? removedStone, out bool hitProtectedWinningPoint, out IReadOnlyList<WinningLine> currentWinningLines)
    {
        targetCell = Point.Empty;
        removedStone = null;
        hitProtectedWinningPoint = false;
        currentWinningLines = new List<WinningLine>();

        TerminalLogger.Action($"TryLaunchBomb called: fromLeft={fromLeft}, line={lineOneBased}, power={power}, shooterColor={shooterColor.Name}");

        if (lineOneBased < 1 || lineOneBased > GridHeight)
        {
            TerminalLogger.Action($"Bomb rejected: line {lineOneBased} is out of range 1..{GridHeight}");
            return false;
        }

        if (power < 1 || power > 9)
        {
            TerminalLogger.Action("Bomb rejected: power is out of range 1..9");
            return false;
        }

        // Mapping puissance (1..9) -> colonne cible via règle de trois.
        double mappedExact = (power * (double)GridWidth) / 9d;
        int mappedOneBased = (int)Math.Floor(mappedExact);
        if (mappedOneBased < 1)
        {
            mappedOneBased = 1;
        }
        TerminalLogger.Action($"Bomb power mapping: exact={mappedExact:F2}, floored={mappedOneBased}");

        targetCell = ResolveBombTarget(fromLeft, lineOneBased, mappedOneBased);
        TerminalLogger.Action($"Bomb target resolved to ({targetCell.X},{targetCell.Y})");

        return TryApplyBombAtTarget(shooterColor, targetCell, out removedStone, out hitProtectedWinningPoint, out currentWinningLines);
    }

    /// <summary>
    /// Applique directement une bombe sur une case donnée (utile pour replay/chargement).
    /// </summary>
    public bool TryApplyBombAtTarget(Color shooterColor, Point targetCell, out GameStone? removedStone, out bool hitProtectedWinningPoint, out IReadOnlyList<WinningLine> currentWinningLines)
    {
        // API de replay: applique un tir sur une case déjà calculée.
        removedStone = null;
        hitProtectedWinningPoint = false;
        currentWinningLines = new List<WinningLine>();

        if (!IsInsideBoard(targetCell.X, targetCell.Y))
        {
            TerminalLogger.Action($"Bomb rejected: target ({targetCell.X},{targetCell.Y}) outside board");
            return false;
        }

        if (_protectedWinningPoints.Contains(targetCell))
        {
            hitProtectedWinningPoint = true;
            TerminalLogger.Action($"Bomb blocked: ({targetCell.X},{targetCell.Y}) is a protected winning-line point");
            currentWinningLines = GetWinningLinesExactFive();
            return true;
        }

        if (_stonesByPosition.TryGetValue(targetCell, out GameStone? hitStone))
        {
            if (hitStone.Color == shooterColor)
            {
                TerminalLogger.Action($"Bomb ignored at ({targetCell.X},{targetCell.Y}): target is shooter's own stone ({hitStone.Color.Name})");
                currentWinningLines = GetWinningLinesExactFive();
                return true;
            }

            _stonesByPosition.Remove(targetCell);
            _stones.Remove(hitStone);
            removedStone = hitStone;
            TerminalLogger.Action($"Bomb hit: stone removed at ({targetCell.X},{targetCell.Y}) color={hitStone.Color.Name}");
        }
        else
        {
            TerminalLogger.Action("Bomb miss: no stone at target cell");
        }

        currentWinningLines = GetWinningLinesExactFive();
        TerminalLogger.Action($"Winning lines recomputed after bomb: count={currentWinningLines.Count}");
        return true;
    }

    /// <summary>
    /// Marque comme protégés les points des nouvelles lignes de 5 validées.
    /// </summary>
    private void ProtectPointsFromNewWinningLines(IReadOnlyList<WinningLine> newWinningLines)
    {
        // Une fois une ligne validée, ses 5 points deviennent invulnérables aux bombes.
        foreach (WinningLine line in newWinningLines)
        {
            foreach (Point p in EnumerateLinePoints(line))
            {
                if (_protectedWinningPoints.Add(p))
                {
                    TerminalLogger.Action($"Protected point registered from winning line: ({p.X},{p.Y})");
                }
            }
        }
    }

    /// <summary>
    /// Enumère les 5 points exacts d'une ligne gagnante (du start vers end).
    /// </summary>
    private static IEnumerable<Point> EnumerateLinePoints(WinningLine line)
    {
        int dx = Math.Sign(line.End.X - line.Start.X);
        int dy = Math.Sign(line.End.Y - line.Start.Y);

        int x = line.Start.X;
        int y = line.Start.Y;

        for (int i = 0; i < 5; i++)
        {
            yield return new Point(x, y);
            x += dx;
            y += dy;
        }
    }

    /// <summary>
    /// Recalcule toutes les lignes gagnantes de longueur exactement 5.
    /// </summary>
    public IReadOnlyList<WinningLine> GetWinningLinesExactFive()
    {
        // Recalcul complet de toutes les lignes visibles de longueur exactement 5.
        var lines = new List<WinningLine>();

        foreach (GameStone stone in _stones)
        {
            foreach (var (dx, dy) in _scanDirections)
            {
                int prevX = stone.X - dx;
                int prevY = stone.Y - dy;
                if (HasSameColorStoneAt(prevX, prevY, stone.Color))
                {
                    continue;
                }

                int x = stone.X;
                int y = stone.Y;
                var runPoints = new List<Point>
                {
                    new Point(stone.X, stone.Y)
                };

                while (true)
                {
                    x += dx;
                    y += dy;
                    if (!HasSameColorStoneAt(x, y, stone.Color))
                    {
                        break;
                    }

                    runPoints.Add(new Point(x, y));
                }

                if (runPoints.Count < 5)
                {
                    continue;
                }

                for (int startIndex = 0; startIndex + 4 < runPoints.Count; startIndex += 5)
                {
                    lines.Add(new WinningLine(runPoints[startIndex], runPoints[startIndex + 4], stone.Color));
                }
            }
        }

        return lines;
    }

    /// <summary>
    /// Recherche les nouvelles lignes créées par le dernier coup posé.
    /// </summary>
    private List<WinningLine> FindNewWinningLines(GameStone originStone)
    {
        // Scan orienté autour du dernier coup pour trouver uniquement les nouvelles lignes.
        var lines = new List<WinningLine>();

        foreach (var (dx, dy) in _scanDirections)
        {
            List<Point> alignedRun = CollectAlignedRunPoints(originStone, dx, dy);
            TerminalLogger.Action($"Direction ({dx},{dy}) -> alignedRun={alignedRun.Count}");

            if (alignedRun.Count < 5)
            {
                continue;
            }

            for (int startIndex = 0; startIndex + 4 < alignedRun.Count; startIndex += 5)
            {
                Point start = alignedRun[startIndex];
                Point end = alignedRun[startIndex + 4];
                lines.Add(new WinningLine(start, end, originStone.Color));
                TerminalLogger.Action($"5-block line found: start=({start.X},{start.Y}), end=({end.X},{end.Y}), color={originStone.Color.Name}");
            }
        }

        return lines;
    }

    /// <summary>
    /// Récupère le segment continu aligné contenant la pierre d'origine.
    /// </summary>
    private List<Point> CollectAlignedRunPoints(GameStone originStone, int dx, int dy)
    {
        // Collecte un segment continu de même couleur dans une direction donnée.
        var runPoints = new List<Point>();
        int startX = originStone.X;
        int startY = originStone.Y;

        while (HasSameColorStoneAt(startX - dx, startY - dy, originStone.Color))
        {
            startX -= dx;
            startY -= dy;
        }

        int x = startX;
        int y = startY;

        while (true)
        {
            if (!HasSameColorStoneAt(x, y, originStone.Color))
            {
                break;
            }

            runPoints.Add(new Point(x, y));
            x += dx;
            y += dy;
        }

        return runPoints;
    }

    /// <summary>
    /// Indique si les coordonnées sont incluses dans les bornes de la grille.
    /// </summary>
    private bool IsInsideBoard(int x, int y)
    {
        return x >= 0 && x < GridWidth && y >= 0 && y < GridHeight;
    }

    /// <summary>
    /// Vérifie si une pierre de la couleur demandée existe sur la case indiquée.
    /// </summary>
    private bool HasSameColorStoneAt(int x, int y, Color color)
    {
        if (!IsInsideBoard(x, y))
        {
            return false;
        }

        var p = new Point(x, y);
        return _stonesByPosition.TryGetValue(p, out GameStone? stone) && stone.Color == color;
    }

    /// <summary>
    /// Transforme une ligne + puissance mappée en coordonnées exactes de cible.
    /// </summary>
    private Point ResolveBombTarget(bool fromLeft, int lineOneBased, int mappedOneBased)
    {
        int targetX = fromLeft ? mappedOneBased - 1 : GridWidth - mappedOneBased;
        int targetY = lineOneBased - 1;
        return new Point(targetX, targetY);
    }
}