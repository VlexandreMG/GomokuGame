using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GomokuGame.core;

public sealed class GomokuEngine
{
    // Etat interne du moteur: positions occupées, historique des pierres, protection des lignes.
    private readonly Dictionary<Point, GameStone> _stonesByPosition = new Dictionary<Point, GameStone>();
    private readonly List<GameStone> _stones = new List<GameStone>();
    private readonly HashSet<Point> _protectedWinningPoints = new HashSet<Point>();
    private readonly List<WinningLine> _registeredWinningLines = new List<WinningLine>();
    private readonly Dictionary<Point, Dictionary<Color, int>> _revivableCreditsByPosition = new Dictionary<Point, Dictionary<Color, int>>();
    private readonly Dictionary<(int Dx, int Dy), HashSet<Point>> _linePointsByDirection = new Dictionary<(int Dx, int Dy), HashSet<Point>>();
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
        foreach (var direction in _scanDirections)
        {
            _linePointsByDirection[direction] = new HashSet<Point>();
        }
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
        List<WinningLine> detectedLines = FindNewWinningLines(stone);
        List<WinningLine> acceptedLines = FilterOutCrossingRegisteredLines(detectedLines);

        placedStone = stone;
        newWinningLines = acceptedLines;
        ProtectPointsFromNewWinningLines(newWinningLines);
        TerminalLogger.Action($"Scan finished for ({x},{y}), new winning lines={newWinningLines.Count}");
        return true;
    }

    private List<WinningLine> FilterOutCrossingRegisteredLines(IReadOnlyList<WinningLine> candidateLines)
    {
        if (candidateLines.Count == 0 || _registeredWinningLines.Count == 0)
        {
            return candidateLines.ToList();
        }

        List<WinningLine> accepted = new List<WinningLine>(candidateLines.Count);

        foreach (WinningLine candidate in candidateLines)
        {
            bool crossesExistingLine = _registeredWinningLines.Any(existing =>
                existing.Color != candidate.Color
                && SegmentsIntersect(candidate.Start, candidate.End, existing.Start, existing.End));

            if (crossesExistingLine)
            {
                TerminalLogger.Action($"Winning line ignored due to crossing: start=({candidate.Start.X},{candidate.Start.Y}), end=({candidate.End.X},{candidate.End.Y})");
                continue;
            }

            accepted.Add(candidate);
        }

        return accepted;
    }

    private static bool SegmentsIntersect(Point a1, Point a2, Point b1, Point b2)
    {
        int o1 = Orientation(a1, a2, b1);
        int o2 = Orientation(a1, a2, b2);
        int o3 = Orientation(b1, b2, a1);
        int o4 = Orientation(b1, b2, a2);

        if (o1 != o2 && o3 != o4)
        {
            return true;
        }

        if (o1 == 0 && IsPointOnSegment(a1, b1, a2))
        {
            return true;
        }

        if (o2 == 0 && IsPointOnSegment(a1, b2, a2))
        {
            return true;
        }

        if (o3 == 0 && IsPointOnSegment(b1, a1, b2))
        {
            return true;
        }

        if (o4 == 0 && IsPointOnSegment(b1, a2, b2))
        {
            return true;
        }

        return false;
    }

    private static int Orientation(Point p, Point q, Point r)
    {
        long cross = ((long)q.Y - p.Y) * ((long)r.X - q.X) - ((long)q.X - p.X) * ((long)r.Y - q.Y);
        if (cross == 0)
        {
            return 0;
        }

        return cross > 0 ? 1 : 2;
    }

    private static bool IsPointOnSegment(Point p, Point q, Point r)
    {
        return q.X <= Math.Max(p.X, r.X)
            && q.X >= Math.Min(p.X, r.X)
            && q.Y <= Math.Max(p.Y, r.Y)
            && q.Y >= Math.Min(p.Y, r.Y);
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
        int mappedOneBased = MapPowerToColumnOneBased(power);
        double mappedExact = (power * (double)GridWidth) / 9d;
        TerminalLogger.Action($"Bomb power mapping: exact={mappedExact:F2}, mapped={mappedOneBased}");

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

        if (_stonesByPosition.TryGetValue(targetCell, out GameStone? ownStoneAtTarget) && ownStoneAtTarget.Color == shooterColor)
        {
            TerminalLogger.Action($"Bomb ignored at ({targetCell.X},{targetCell.Y}): target is shooter's own stone ({ownStoneAtTarget.Color.Name})");
            currentWinningLines = GetWinningLinesExactFive();
            return true;
        }

        if (TryConsumeRevivableCredit(targetCell, shooterColor))
        {
            if (_stonesByPosition.TryGetValue(targetCell, out GameStone? currentStone))
            {
                if (currentStone.Color != shooterColor)
                {
                    _stonesByPosition.Remove(targetCell);
                    _stones.Remove(currentStone);
                    removedStone = currentStone;
                    AddRevivableCredit(targetCell, currentStone.Color);
                    TerminalLogger.Action($"Bomb recall: removed occupying enemy stone at ({targetCell.X},{targetCell.Y}) before restoring owner stone");
                }
            }

            if (!_stonesByPosition.ContainsKey(targetCell))
            {
                GameStone restoredStone = new GameStone(targetCell.X, targetCell.Y, shooterColor);
                _stonesByPosition[targetCell] = restoredStone;
                _stones.Add(restoredStone);
                TerminalLogger.Action($"Bomb recall: restored owner stone at ({targetCell.X},{targetCell.Y}) color={shooterColor.Name}");
            }

            currentWinningLines = GetWinningLinesExactFive();
            return true;
        }

        if (_stonesByPosition.TryGetValue(targetCell, out GameStone? hitStone))
        {
            _stonesByPosition.Remove(targetCell);
            _stones.Remove(hitStone);
            removedStone = hitStone;
            AddRevivableCredit(targetCell, hitStone.Color);
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

    private void AddRevivableCredit(Point position, Color ownerColor)
    {
        if (!_revivableCreditsByPosition.TryGetValue(position, out Dictionary<Color, int>? creditsByColor))
        {
            creditsByColor = new Dictionary<Color, int>();
            _revivableCreditsByPosition[position] = creditsByColor;
        }

        if (!creditsByColor.TryGetValue(ownerColor, out int existingCredits))
        {
            existingCredits = 0;
        }

        creditsByColor[ownerColor] = existingCredits + 1;
    }

    private bool TryConsumeRevivableCredit(Point position, Color ownerColor)
    {
        if (!_revivableCreditsByPosition.TryGetValue(position, out Dictionary<Color, int>? creditsByColor))
        {
            return false;
        }

        if (!creditsByColor.TryGetValue(ownerColor, out int availableCredits) || availableCredits <= 0)
        {
            return false;
        }

        if (availableCredits == 1)
        {
            creditsByColor.Remove(ownerColor);
            if (creditsByColor.Count == 0)
            {
                _revivableCreditsByPosition.Remove(position);
            }
        }
        else
        {
            creditsByColor[ownerColor] = availableCredits - 1;
        }

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
            _registeredWinningLines.Add(line);

            int directionDx = Math.Sign(line.End.X - line.Start.X);
            int directionDy = Math.Sign(line.End.Y - line.Start.Y);
            (int Dx, int Dy) directionKey = NormalizeDirection(directionDx, directionDy);

            foreach (Point p in EnumerateLinePoints(line))
            {
                if (_protectedWinningPoints.Add(p))
                {
                    TerminalLogger.Action($"Protected point registered from winning line: ({p.X},{p.Y})");
                }

                _linePointsByDirection[directionKey].Add(p);
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
        // Retourne uniquement les lignes effectivement validées au fil des coups.
        return _registeredWinningLines.ToList();
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
            int originIndex = alignedRun.FindIndex(p => p.X == originStone.X && p.Y == originStone.Y);
            TerminalLogger.Action($"Direction ({dx},{dy}) -> alignedRun={alignedRun.Count}, originIndex={originIndex}");

            if (alignedRun.Count < 5 || originIndex < 0)
            {
                continue;
            }

            for (int startIndex = 0; startIndex + 4 < alignedRun.Count; startIndex += 5)
            {
                int endIndex = startIndex + 4;
                bool includesOrigin = originIndex >= startIndex && originIndex <= endIndex;
                if (!includesOrigin)
                {
                    continue;
                }

                if (OverlapsExistingLineInSameDirection(alignedRun, startIndex, endIndex, dx, dy))
                {
                    TerminalLogger.Action($"5-block rejected: overlaps existing line in same direction (startIndex={startIndex}, endIndex={endIndex})");
                    continue;
                }

                Point start = alignedRun[startIndex];
                Point end = alignedRun[endIndex];
                lines.Add(new WinningLine(start, end, originStone.Color));
                TerminalLogger.Action($"Exact 5-line found: start=({start.X},{start.Y}), end=({end.X},{end.Y}), color={originStone.Color.Name}");
            }
        }

        return lines;
    }

    private bool OverlapsExistingLineInSameDirection(List<Point> points, int startIndex, int endIndex, int dx, int dy)
    {
        // Interdit de réutiliser des points d'une ligne déjà validée dans la même direction.
        (int Dx, int Dy) key = NormalizeDirection(dx, dy);
        HashSet<Point> existingPoints = _linePointsByDirection[key];

        for (int i = startIndex; i <= endIndex; i++)
        {
            Point p = points[i];
            if (existingPoints.Contains(p))
            {
                return true;
            }
        }

        return false;
    }

    private (int Dx, int Dy) NormalizeDirection(int dx, int dy)
    {
        // Canonicalisation: (1,0), (0,1), (1,1), (1,-1)
        if (dx < 0)
        {
            dx = -dx;
            dy = -dy;
        }
        else if (dx == 0 && dy < 0)
        {
            dy = -dy;
        }

        return (dx, dy);
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

    private int MapPowerToColumnOneBased(int power)
    {
        int mappedOneBased = (int)Math.Floor((power * (double)GridWidth) / 9d);
        if (mappedOneBased < 1)
        {
            return 1;
        }

        if (mappedOneBased > GridWidth)
        {
            return GridWidth;
        }

        return mappedOneBased;
    }
}