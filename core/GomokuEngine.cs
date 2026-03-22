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
}