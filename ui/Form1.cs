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
        private int? _pendingBombRowOneBased;
        private bool _isGameInitialized;

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
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
            this.Shown += Form1_Shown;
        }

        private void Initialize()
        {
            TerminalLogger.Action("Form initialized, waiting for startup menu");
        }

        private void Form1_Shown(object? sender, EventArgs e)
        {
            if (_isGameInitialized)
            {
                return;
            }

            if (!GameSetupMenu.TryGetConfiguration(this, out GameSetupResult? setup) || setup is null)
            {
                TerminalLogger.Action("Startup menu canceled, closing application");
                this.Close();
                return;
            }

            _board.GridSize = setup.GridSize;
            _board.PlacedPoints.Clear();
            _board.WinningLines.Clear();
            _board.DisableBombSelection();

            _engine = new GomokuEngine(setup.GridSize);
            _turnDetector = new TurnDetector(setup.Player1Name, setup.Player2Name);
            _isGameInitialized = true;

            TerminalLogger.Action($"Game setup complete: P1={setup.Player1Name} (Blue), P2={setup.Player2Name} (Red), grid={setup.GridSize}");
            _board.Invalidate();
            PromptCurrentTurnAction();
        }

        private void Board_MouseClick(object? sender, MouseEventArgs e)
        {
            if (!_isGameInitialized)
            {
                TerminalLogger.Action("Click ignored: game not initialized yet");
                return;
            }

            TerminalLogger.Action($"Mouse click received at pixel=({e.X},{e.Y})");

            if (_turnDetector.CurrentAction == TurnAction.LaunchBomb)
            {
                HandleBombRowSelection(e.X, e.Y);
                return;
            }

            // Convertir le clic pixel en coordonnées de matrice
            int x = (int)Math.Round((float)(e.X - _board.BoardMargin) / _board.CellSize);
            int y = (int)Math.Round((float)(e.Y - _board.BoardMargin) / _board.CellSize);
            TerminalLogger.Action($"Translated to grid position=({x},{y})");

            // Vérifier qu'on est bien sur une intersection de la grille
            if (x >= 0 && x < _board.GridSize && y >= 0 && y < _board.GridSize)
            {
                if (!_engine.TryPlaceStone(x, y, out GameStone? placedStone, out IReadOnlyList<WinningLine> _) || placedStone is null)
                {
                    TerminalLogger.Action("Move ignored by engine");
                    return;
                }

                var placedPoint = new GamePoint(placedStone.X, placedStone.Y, placedStone.Color);
                _board.PlacedPoints.Add(placedPoint);
                TerminalLogger.Action($"UI point added at ({placedStone.X},{placedStone.Y}) with color={placedStone.Color.Name}");

                IReadOnlyList<WinningLine> lines = _engine.GetWinningLinesExactFive();
                SyncWinningLines(lines);

                _board.Invalidate(); // Force à redessiner le plateau (OnPaint)
                TerminalLogger.Action("Board invalidated for repaint");
                MoveToNextTurn();
                return;
            }

            TerminalLogger.Action("Click ignored because it is outside grid bounds");
        }

        private void MoveToNextTurn()
        {
            _pendingBombRowOneBased = null;
            _board.DisableBombSelection();
            _turnDetector.AdvanceTurn();
            PromptCurrentTurnAction();
        }

        private void HandleBombRowSelection(int pixelX, int pixelY)
        {
            bool fromLeft = _turnDetector.CurrentPlayer == _turnDetector.Player1;
            if (_pendingBombRowOneBased.HasValue)
            {
                TerminalLogger.Action($"Bomb row already selected: {_pendingBombRowOneBased.Value}, waiting for numpad power");
                return;
            }

            if (!_board.TryGetBombRowFromClick(pixelX, pixelY, fromLeft, out int selectedRow))
            {
                TerminalLogger.Action("Bomb row selection ignored: click was not on cannon");
                return;
            }

            _pendingBombRowOneBased = selectedRow;
            _board.SetSelectedBombRow(selectedRow);
            TerminalLogger.Action($"Bomb row selected: row={selectedRow}");
            TurnActionAlert.ShowBombPowerInputHint(this, _turnDetector.CurrentPlayer, selectedRow);
        }

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (!_isGameInitialized)
            {
                return;
            }

            if (_turnDetector.CurrentAction != TurnAction.LaunchBomb || !_pendingBombRowOneBased.HasValue)
            {
                return;
            }

            int? power = TryMapPowerFromKey(e.KeyCode);
            if (!power.HasValue)
            {
                return;
            }

            bool fromLeft = _turnDetector.CurrentPlayer == _turnDetector.Player1;
            bool success = _engine.TryLaunchBomb(fromLeft, _pendingBombRowOneBased.Value, power.Value, out Point targetCell, out GameStone? removedStone, out IReadOnlyList<WinningLine> currentWinningLines);
            TerminalLogger.Action($"Bomb power received from keyboard: {power.Value}");

            if (!success)
            {
                TerminalLogger.Action("Bomb action rejected by engine");
                return;
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

            e.Handled = true;
            e.SuppressKeyPress = true;
            MoveToNextTurn();
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

            if (selectedAction == TurnAction.LaunchBomb)
            {
                bool fromLeft = _turnDetector.CurrentPlayer == _turnDetector.Player1;
                _pendingBombRowOneBased = null;
                _board.EnableBombSelection(fromLeft);
                TurnActionAlert.ShowBombRowSelectionHint(this, _turnDetector.CurrentPlayer, fromLeft);
                TerminalLogger.Action($"Bomb selection enabled (fromLeft={fromLeft})");
            }
            else
            {
                _board.DisableBombSelection();
            }
        }

        private static int? TryMapPowerFromKey(Keys key)
        {
            return key switch
            {
                Keys.NumPad1 => 1,
                Keys.NumPad2 => 2,
                Keys.NumPad3 => 3,
                Keys.NumPad4 => 4,
                Keys.NumPad5 => 5,
                Keys.NumPad6 => 6,
                Keys.NumPad7 => 7,
                Keys.NumPad8 => 8,
                Keys.NumPad9 => 9,
                _ => null
            };
        }
    }
}