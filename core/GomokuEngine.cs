using System.Collections.Generic;
using System.Drawing;

namespace GomokuGame.core;

public sealed class GomokuEngine
{
    private readonly Dictionary<Point, GameStone> _stonesByPosition = new Dictionary<Point, GameStone>();
    private readonly List<GameStone> _stones = new List<GameStone>();
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
        TerminalLogger.Action($"Scan finished for ({x},{y}), new winning lines={newWinningLines.Count}");
        return true;
    }

    public bool TryLaunchBomb(bool fromLeft, int lineOneBased, int power, out Point targetCell, out GameStone? removedStone, out IReadOnlyList<WinningLine> currentWinningLines)
    {
        targetCell = Point.Empty;
        removedStone = null;
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

                int runLength = 1;
                int x = stone.X;
                int y = stone.Y;
                Point end = new Point(stone.X, stone.Y);

                while (true)
                {
                    x += dx;
                    y += dy;
                    if (!HasSameColorStoneAt(x, y, stone.Color))
                    {
                        break;
                    }

                    runLength++;
                    end = new Point(x, y);
                }

                if (runLength == 5)
                {
                    lines.Add(new WinningLine(new Point(stone.X, stone.Y), end, stone.Color));
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
            int forwardCount = CountAlignedStones(originStone, dx, dy, out Point forwardEnd);
            int backwardCount = CountAlignedStones(originStone, -dx, -dy, out Point backwardEnd);
            int totalAligned = 1 + forwardCount + backwardCount;
            TerminalLogger.Action($"Direction ({dx},{dy}) -> backward={backwardCount}, forward={forwardCount}, total={totalAligned}");

            // Une ligne est valide uniquement si elle contient exactement 5 points alignes.
            if (totalAligned == 5)
            {
                lines.Add(new WinningLine(backwardEnd, forwardEnd, originStone.Color));
                TerminalLogger.Action($"Exact line found: start=({backwardEnd.X},{backwardEnd.Y}), end=({forwardEnd.X},{forwardEnd.Y}), color={originStone.Color.Name}");
            }
        }

        return lines;
    }

    private int CountAlignedStones(GameStone originStone, int dx, int dy, out Point furthestPoint)
    {
        furthestPoint = new Point(originStone.X, originStone.Y);
        int count = 0;
        int x = originStone.X;
        int y = originStone.Y;

        while (true)
        {
            x += dx;
            y += dy;

            if (!IsInsideBoard(x, y))
            {
                break;
            }

            var candidatePosition = new Point(x, y);
            if (!_stonesByPosition.TryGetValue(candidatePosition, out GameStone? candidateStone))
            {
                break;
            }

            if (candidateStone.Color != originStone.Color)
            {
                break;
            }

            count++;
            furthestPoint = candidatePosition;
        }

        return count;
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