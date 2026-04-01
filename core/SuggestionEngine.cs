using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GomokuGame.core;

/// <summary>
/// Moteur d'analyse des suggestions de coups gagnants.
/// Détecte les patterns 3-points et 4-points pour proposer des mouvements stratégiques.
/// </summary>
public sealed class SuggestionEngine
{
    private readonly GomokuEngine _engine;
    private readonly (int Dx, int Dy)[] _scanDirections =
    {
        (1, 0),   // Horizontal
        (0, 1),   // Vertical
        (1, 1),   // Diagonal \
        (1, -1)   // Diagonal /
    };

    public SuggestionEngine(GomokuEngine engine)
    {
        _engine = engine;
    }

    /// <summary>
    /// Analyse toutes les suggestions pour un joueur (couleur donnée).
    /// Retourne les positions suggérées avec leur type (3-point ou 4-point).
    /// </summary>
    public List<(Point Position, SuggestionType Type)> AnalyzeSuggestions(Color playerColor)
    {
        var bestByPosition = new Dictionary<Point, SuggestionType>();
        HashSet<(Point Start, int Dx, int Dy)> handledRunStarts = new HashSet<(Point Start, int Dx, int Dy)>();

        foreach (GameStone stone in _engine.Stones.Where(s => s.Color == playerColor))
        {
            Point stonePoint = new Point(stone.X, stone.Y);

            foreach (var (dx, dy) in _scanDirections)
            {
                Point previous = new Point(stonePoint.X - dx, stonePoint.Y - dy);
                if (HasStoneOfColor(previous, playerColor))
                {
                    // Pas le début du segment maximal.
                    continue;
                }

                if (!handledRunStarts.Add((stonePoint, dx, dy)))
                {
                    continue;
                }

                List<Point> run = CollectRun(stonePoint, dx, dy, playerColor);
                if (run.Count < 3)
                {
                    continue;
                }

                if (run.Count == 4)
                {
                    // Cas 4 points: il faut au moins 1 extrémité libre (_XXXX_ ou XXXX_ ou _XXXX).
                    Point left = new Point(run[0].X - dx, run[0].Y - dy);
                    Point right = new Point(run[run.Count - 1].X + dx, run[run.Count - 1].Y + dy);

                    bool leftEmpty = IsPositionEmpty(left);
                    bool rightEmpty = IsPositionEmpty(right);

                    // Cas _XXXX_ : 2 côtés libres
                    if (leftEmpty && rightEmpty)
                    {
                        TryAddSuggestion(bestByPosition, left, SuggestionType.FourPoints);
                        TryAddSuggestion(bestByPosition, right, SuggestionType.FourPoints);
                    }
                    // Cas XXXX_ : côté droit libre (gauche bloqué)
                    else if (!leftEmpty && rightEmpty)
                    {
                        TryAddSuggestion(bestByPosition, right, SuggestionType.FourPoints);
                    }
                    // Cas _XXXX : côté gauche libre (droite bloquée)
                    else if (leftEmpty && !rightEmpty)
                    {
                        TryAddSuggestion(bestByPosition, left, SuggestionType.FourPoints);
                    }
                    continue;
                }

                if (run.Count == 3)
                {
                    // Cas 3 points strict: 2 cases libres de chaque côté (_ _ XXX _ _).
                    Point left1 = new Point(run[0].X - dx, run[0].Y - dy);
                    Point left2 = new Point(run[0].X - (2 * dx), run[0].Y - (2 * dy));
                    Point right1 = new Point(run[run.Count - 1].X + dx, run[run.Count - 1].Y + dy);
                    Point right2 = new Point(run[run.Count - 1].X + (2 * dx), run[run.Count - 1].Y + (2 * dy));

                    if (IsPositionEmpty(left1) && IsPositionEmpty(left2) && IsPositionEmpty(right1) && IsPositionEmpty(right2))
                    {
                        TryAddSuggestion(bestByPosition, left1, SuggestionType.ThreePoints);
                        TryAddSuggestion(bestByPosition, left2, SuggestionType.ThreePoints);
                        TryAddSuggestion(bestByPosition, right1, SuggestionType.ThreePoints);
                        TryAddSuggestion(bestByPosition, right2, SuggestionType.ThreePoints);
                    }
                }
            }
        }

        // Scanner supplémentaire: trous au milieu de 4 pierces (XX_XX)
        for (int x = 0; x < _engine.GridWidth; x++)
        {
            for (int y = 0; y < _engine.GridHeight; y++)
            {
                var position = new Point(x, y);
                if (!IsPositionEmpty(position))
                {
                    continue;
                }

                foreach (var (dx, dy) in _scanDirections)
                {
                    int forward = CountConsecutiveStones(position, playerColor, dx, dy);
                    int backward = CountConsecutiveStones(position, playerColor, -dx, -dy);

                    // Pattern: XX_XX (2 pierres d'un côté, 2 de l'autre avec trou au milieu)
                    if (forward == 2 && backward == 2)
                    {
                        TryAddSuggestion(bestByPosition, position, SuggestionType.FourPoints);
                    }
                }
            }
        }

        return bestByPosition
            .Select(kvp => (kvp.Key, kvp.Value))
            .ToList();
    }

    private void TryAddSuggestion(Dictionary<Point, SuggestionType> bestByPosition, Point position, SuggestionType type)
    {
        if (!IsPositionEmpty(position))
        {
            return;
        }

        // Conserver le type le plus fort si la même case provient de plusieurs segments.
        if (!bestByPosition.TryGetValue(position, out SuggestionType current) || type > current)
        {
            bestByPosition[position] = type;
        }
    }

    private List<Point> CollectRun(Point start, int dx, int dy, Color playerColor)
    {
        List<Point> run = new List<Point>();
        Point current = start;

        while (HasStoneOfColor(current, playerColor))
        {
            run.Add(current);
            current = new Point(current.X + dx, current.Y + dy);
        }

        return run;
    }

    private bool HasStoneOfColor(Point position, Color playerColor)
    {
        if (!IsInsideBoard(position))
        {
            return false;
        }

        GameStone? stone = GetStoneAt(position);
        return stone != null && stone.Color == playerColor;
    }

    /// <summary>
    /// Compte les pierres du joueur consécutives à partir d'une position dans une direction.
    /// </summary>
    private int CountConsecutiveStones(Point startPosition, Color playerColor, int dx, int dy)
    {
        int count = 0;
        int x = startPosition.X + dx;
        int y = startPosition.Y + dy;

        while (IsInsideBoard(new Point(x, y)))
        {
            var stone = GetStoneAt(new Point(x, y));
            if (stone != null && stone.Color == playerColor)
            {
                count++;
                x += dx;
                y += dy;
            }
            else
            {
                break;
            }
        }

        return count;
    }

    /// <summary>
    /// Retourne la pierre à une position donnée, ou null si vide.
    /// </summary>
    private GameStone? GetStoneAt(Point position)
    {
        return _engine.Stones.FirstOrDefault(s => s.X == position.X && s.Y == position.Y);
    }

    /// <summary>
    /// Vérifie si une position est vide.
    /// </summary>
    private bool IsPositionEmpty(Point position)
    {
        return IsInsideBoard(position) && GetStoneAt(position) == null;
    }

    /// <summary>
    /// Vérifie si une position est à l'intérieur du plateau.
    /// </summary>
    private bool IsInsideBoard(Point position)
    {
        return position.X >= 0 && position.X < _engine.GridWidth &&
               position.Y >= 0 && position.Y < _engine.GridHeight;
    }
}

public enum SuggestionType
{
    None = 0,
    ThreePoints = 1,
    FourPoints = 2
}
