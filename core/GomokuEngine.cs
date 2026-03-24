using System.Collections.Generic;
using System.Drawing;

namespace GomokuGame.core;

public sealed class GomokuEngine
{
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

    public int GridSize { get; }
    public IReadOnlyList<GameStone> Stones => _stones;

    public GomokuEngine(int gridSize)
    {
        GridSize = gridSize;
        TerminalLogger.Action($"Engine created with grid size {GridSize}");
    }

    public bool TryPlaceStone(int x, int y, out GameStone? placedStone, out IReadOnlyList<WinningLine> newWinningLines)
    {
        TerminalLogger.Action($"TryPlaceStone called at ({x},{y})");
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

        Color stoneColor = (_stones.Count % 2 == 0) ? Color.Blue : Color.Red;
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

    public bool TryLaunchBomb(bool fromLeft, int lineOneBased, int power, out Point targetCell, out GameStone? removedStone, out bool hitProtectedWinningPoint, out IReadOnlyList<WinningLine> currentWinningLines)
    {
        targetCell = Point.Empty;
        removedStone = null;
        hitProtectedWinningPoint = false;
        currentWinningLines = new List<WinningLine>();

        TerminalLogger.Action($"TryLaunchBomb called: fromLeft={fromLeft}, line={lineOneBased}, power={power}");

        if (lineOneBased < 1 || lineOneBased > GridSize)
        {
            TerminalLogger.Action($"Bomb rejected: line {lineOneBased} is out of range 1..{GridSize}");
            return false;
        }

        if (power < 1 || power > 9)
        {
            TerminalLogger.Action("Bomb rejected: power is out of range 1..9");
            return false;
        }

        int mappedOneBased = (power * GridSize) / 9;
        if (mappedOneBased < 1)
        {
            mappedOneBased = 1;
        }

        int targetX = fromLeft ? mappedOneBased - 1 : GridSize - mappedOneBased;
        int targetY = lineOneBased - 1;
        targetCell = new Point(targetX, targetY);
        TerminalLogger.Action($"Bomb target resolved to ({targetX},{targetY})");

        if (_protectedWinningPoints.Contains(targetCell))
        {
            hitProtectedWinningPoint = true;
            TerminalLogger.Action($"Bomb blocked: ({targetX},{targetY}) is a protected winning-line point");
            currentWinningLines = GetWinningLinesExactFive();
            return true;
        }

        if (_stonesByPosition.TryGetValue(targetCell, out GameStone? hitStone))
        {
            _stonesByPosition.Remove(targetCell);
            _stones.Remove(hitStone);
            removedStone = hitStone;
            TerminalLogger.Action($"Bomb hit: stone removed at ({targetX},{targetY}) color={hitStone.Color.Name}");
        }
        else
        {
            TerminalLogger.Action("Bomb miss: no stone at target cell");
        }

        currentWinningLines = GetWinningLinesExactFive();
        TerminalLogger.Action($"Winning lines recomputed after bomb: count={currentWinningLines.Count}");
        return true;
    }

    private void ProtectPointsFromNewWinningLines(IReadOnlyList<WinningLine> newWinningLines)
    {
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

    public IReadOnlyList<WinningLine> GetWinningLinesExactFive()
    {
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

    private List<WinningLine> FindNewWinningLines(GameStone originStone)
    {
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

    private List<Point> CollectAlignedRunPoints(GameStone originStone, int dx, int dy)
    {
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

    private bool IsInsideBoard(int x, int y)
    {
        return x >= 0 && x < GridSize && y >= 0 && y < GridSize;
    }

    private bool HasSameColorStoneAt(int x, int y, Color color)
    {
        if (!IsInsideBoard(x, y))
        {
            return false;
        }

        var p = new Point(x, y);
        return _stonesByPosition.TryGetValue(p, out GameStone? stone) && stone.Color == color;
    }
}