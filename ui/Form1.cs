// ui/Form1.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GomokuGame.core;
using GomokuGame.core.events;
using GomokuGame.ui.organisms;
using GomokuGame.ui.atoms;

namespace GomokuGame.ui
{
    public partial class Form1 : Form
    {
        private GameBoard _board = null!;
        private GomokuEngine _engine = null!;
        private TurnDetector _turnDetector = null!;

        public Form1()
        {
            TerminalLogger.Initialize();
            TerminalLogger.Action("Form1 constructor called");
            InitializeComponent();
            InitializeLifecycle();
        }

        private void InitializeLifecycle()
        {
            CreateComponents();
            SetupLayout();
            ApplyDefaultStyles();
            SetupEventHandlers();
            Initialize();
        }

        private void CreateComponents()
        {
            _board = new GameBoard();
            this.Controls.Add(_board);
            TerminalLogger.Action("GameBoard component created and added to form");
        }

        private void SetupLayout()
        {
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void ApplyDefaultStyles()
        {
            this.Text = "Gomoku Game - Dessin Atomique";
        }

        private void SetupEventHandlers()
        {
            _board.MouseClick += Board_MouseClick; // Gérer le clic sur le plateau
        }

        private void Initialize()
        {
            _engine = new GomokuEngine(_board.GridSize);
            _turnDetector = new TurnDetector("Joueur 1", "Joueur 2");
            TerminalLogger.Action("Form initialization complete");
            PromptCurrentTurnAction();
        }

        private void Board_MouseClick(object? sender, MouseEventArgs e)
        {
            TerminalLogger.Action($"Mouse click received at pixel=({e.X},{e.Y})");

            if (_turnDetector.CurrentAction == TurnAction.LaunchBomb)
            {
                bool bombCompleted = HandleBombAction();
                if (bombCompleted)
                {
                    MoveToNextTurn();
                }
                return;
            }

            // Convertir le clic pixel en coordonnées de matrice
            int x = (int)Math.Round((float)(e.X - _board.BoardMargin) / _board.CellSize);
            int y = (int)Math.Round((float)(e.Y - _board.BoardMargin) / _board.CellSize);
            TerminalLogger.Action($"Translated to grid position=({x},{y})");

            // Vérifier qu'on est bien sur une intersection de la grille
            if (x >= 0 && x < _board.GridSize && y >= 0 && y < _board.GridSize)
            {
                if (!_engine.TryPlaceStone(x, y, out GameStone? placedStone, out IReadOnlyList<WinningLine> newWinningLines) || placedStone is null)
                {
                    TerminalLogger.Action("Move ignored by engine");
                    return;
                }

                var placedPoint = new GamePoint(placedStone.X, placedStone.Y, placedStone.Color);
                _board.PlacedPoints.Add(placedPoint);
                TerminalLogger.Action($"UI point added at ({placedStone.X},{placedStone.Y}) with color={placedStone.Color.Name}");

                foreach (WinningLine line in newWinningLines)
                {
                    _board.AddWinningLine(line.Start, line.End, line.Color);
                    TerminalLogger.Action($"Winning line rendered from ({line.Start.X},{line.Start.Y}) to ({line.End.X},{line.End.Y})");
                }

                _board.Invalidate(); // Force à redessiner le plateau (OnPaint)
                TerminalLogger.Action("Board invalidated for repaint");
                MoveToNextTurn();
                return;
            }

            TerminalLogger.Action("Click ignored because it is outside grid bounds");
        }

        private void MoveToNextTurn()
        {
            _turnDetector.AdvanceTurn();
            PromptCurrentTurnAction();
        }

        private bool HandleBombAction()
        {
            TerminalLogger.Action($"{_turnDetector.CurrentPlayer} initiated bomb action");

            if (!TurnActionAlert.TryGetBombParameters(this, _turnDetector.CurrentPlayer, _board.GridSize, out int selectedLine, out int selectedPower))
            {
                TerminalLogger.Action("Bomb action canceled by user");
                return false;
            }

            bool fromLeft = _turnDetector.CurrentPlayer == _turnDetector.Player1;
            bool success = _engine.TryLaunchBomb(fromLeft, selectedLine, selectedPower, out Point targetCell, out GameStone? removedStone, out IReadOnlyList<WinningLine> currentWinningLines);

            if (!success)
            {
                TerminalLogger.Action("Bomb action rejected by engine");
                return false;
            }

            if (removedStone is null)
            {
                TerminalLogger.Action($"Bomb had no effect at ({targetCell.X},{targetCell.Y})");
            }
            else
            {
                TerminalLogger.Action($"Bomb removed stone at ({removedStone.X},{removedStone.Y})");
            }

            SyncPointsFromEngine();
            SyncWinningLines(currentWinningLines);
            _board.Invalidate();
            TerminalLogger.Action("Board synchronized after bomb action");
            return true;
        }

        private void SyncPointsFromEngine()
        {
            _board.PlacedPoints.Clear();
            foreach (GameStone stone in _engine.Stones)
            {
                _board.PlacedPoints.Add(new GamePoint(stone.X, stone.Y, stone.Color));
            }
        }

        private void SyncWinningLines(IReadOnlyList<WinningLine> lines)
        {
            _board.WinningLines.Clear();
            foreach (WinningLine line in lines)
            {
                _board.WinningLines.Add((line.Start, line.End, line.Color));
            }
        }

        private void PromptCurrentTurnAction()
        {
            TurnAction selectedAction = TurnActionAlert.ShowTurnChoice(this, _turnDetector.CurrentPlayer);
            _turnDetector.SetCurrentAction(selectedAction);
            TerminalLogger.Action($"Action prompt displayed for {_turnDetector.CurrentPlayer}");
        }
    }
}