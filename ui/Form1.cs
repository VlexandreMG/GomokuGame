// ui/Form1.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GomokuGame.core;
using GomokuGame.core.events;
using GomokuGame.data;
using GomokuGame.model;
using GomokuGame.service;
using GomokuGame.ui.organisms;
using GomokuGame.ui.atoms;

namespace GomokuGame.ui
{
    // Form1 pilote uniquement le flux UI (clics, clavier, affichage).
    // La logique métier reste déléguée au moteur et aux services.
    public partial class Form1 : Form
    {
        // ---------- Composants UI ----------
        private GameBoard _board = null!;
        private Panel _topPanel = null!;
        private Label _turnStatusLabel = null!;
        private Panel _boardHostPanel = null!;
        private Panel _bottomPanel = null!;
        private Button _placePointButton = null!;
        private Button _shootButton = null!;
        private Button _endGameButton = null!;
        private Button _undoButton = null!;

        // ---------- Composants métier ----------
        private GomokuEngine _engine = null!;
        private TurnDetector _turnDetector = null!;
        private EtatPartie _etatPartie = null!;

        // ---------- Etat de tour/interaction ----------
        private int? _pendingBombRowOneBased;
        private bool _isGameInitialized;

        // ---------- Métadonnées de la partie ----------
        private string _player1Name = "Joueur 1";
        private string _player2Name = "Joueur 2";
        private int _gridWidth = 10;
        private int _gridHeight = 10;
        private int _player1Score;
        private int _player2Score;
        private int _currentPartieId;
        private int _turnNumber = 1;

        // ---------- Accès données ----------
        private PartieService _partieService = null!;
        private ActionService _actionService = null!;

        // ---------- Historique local (source de vérité pour rebuild/undo) ----------
        private readonly List<ActionModel> _actionHistory = new List<ActionModel>();

        // ---------- Garde-fous pour éviter double-comptage visuel/score ----------
        private readonly HashSet<string> _awardedLineSignatures = new HashSet<string>();
        private readonly HashSet<string> _displayedLineSignatures = new HashSet<string>();

        public Form1()
        {
            TerminalLogger.Initialize();
            TerminalLogger.Action("Form1 constructor called");
            InitializeComponent();
            InitializeLifecycle();
        }

        private void InitializeLifecycle()
        {
            // Cette méthode orchestre l'initialisation dans un ordre stable et lisible.
            CreateComponents();
            SetupLayout();
            ApplyDefaultStyles();
            SetupEventHandlers();
            Initialize();
        }

        private void CreateComponents()
        {
            // Instanciation des contrôles UI.
            _board = new GameBoard();
            _topPanel = new Panel();
            _turnStatusLabel = new Label();
            _boardHostPanel = new Panel();
            _bottomPanel = new Panel();
            _placePointButton = new Button();
            _shootButton = new Button();
            _endGameButton = new Button();
            _undoButton = new Button();

            _topPanel.Dock = DockStyle.Top;
            _topPanel.Height = 44;
            _topPanel.Padding = new Padding(12, 8, 12, 8);

            _turnStatusLabel.Dock = DockStyle.Fill;
            _turnStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            _turnStatusLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            _turnStatusLabel.Text = "Tour en attente...";
            _topPanel.Controls.Add(_turnStatusLabel);

            _boardHostPanel.Dock = DockStyle.Fill;
            _boardHostPanel.BackColor = Color.White;

            _bottomPanel.Dock = DockStyle.Bottom;
            _bottomPanel.Height = 104;
            _bottomPanel.Padding = new Padding(12, 8, 12, 8);

            TableLayoutPanel bottomLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            bottomLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            bottomLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            FlowLayoutPanel topButtonsRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false,
                Margin = new Padding(0)
            };

            FlowLayoutPanel bottomButtonsRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false,
                Margin = new Padding(0)
            };

            _placePointButton.Width = 120;
            _placePointButton.Height = 30;
            _placePointButton.Text = "Placer point";
            _placePointButton.Enabled = false;
            _placePointButton.Margin = new Padding(0, 0, 8, 0);

            _shootButton.Width = 90;
            _shootButton.Height = 30;
            _shootButton.Text = "Tirer";
            _shootButton.Enabled = false;
            _shootButton.Margin = new Padding(0);

            _endGameButton.Width = 180;
            _endGameButton.Height = 30;
            _endGameButton.Text = "Terminer la partie";
            _endGameButton.Enabled = false;
            _endGameButton.Margin = new Padding(0, 0, 8, 0);

            _undoButton.Width = 140;
            _undoButton.Height = 30;
            _undoButton.Text = "Retour (Ctrl+Z)";
            _undoButton.Enabled = false;
            _undoButton.Margin = new Padding(0);

            topButtonsRow.Controls.Add(_placePointButton);
            topButtonsRow.Controls.Add(_shootButton);
            bottomButtonsRow.Controls.Add(_undoButton);
            bottomButtonsRow.Controls.Add(_endGameButton);

            bottomLayout.Controls.Add(topButtonsRow, 0, 0);
            bottomLayout.Controls.Add(bottomButtonsRow, 0, 1);
            _bottomPanel.Controls.Add(bottomLayout);
            _boardHostPanel.Controls.Add(_board);
            this.Controls.Add(_topPanel);
            this.Controls.Add(_boardHostPanel);
            this.Controls.Add(_bottomPanel);
            TerminalLogger.Action("GameBoard component created and added to form");
        }

        private void SetupLayout()
        {
            this.Size = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void ApplyDefaultStyles()
        {
            this.Text = "Gomoku Game - Dessin Atomique";
        }

        private void SetupEventHandlers()
        {
            // Ici on relie les événements UI aux handlers dédiés.
            _board.MouseClick += Board_MouseClick;
            _placePointButton.Click += PlacePointButton_Click;
            _shootButton.Click += ShootButton_Click;
            _endGameButton.Click += EndGameButton_Click;
            _undoButton.Click += UndoButton_Click;
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
            this.Shown += Form1_Shown;
            this.Resize += Form1_Resize;
        }

        private void PlacePointButton_Click(object? sender, EventArgs e)
        {
            if (!_isGameInitialized || !_etatPartie.IsInProgress)
            {
                return;
            }

            _turnDetector.SetCurrentAction(TurnAction.PlacePoint);
            ApplyActionSelectionUi();
            TerminalLogger.Action($"Action button clicked: {_turnDetector.CurrentPlayer} selected PlacePoint");
        }

        private void ShootButton_Click(object? sender, EventArgs e)
        {
            if (!_isGameInitialized || !_etatPartie.IsInProgress)
            {
                return;
            }

            _turnDetector.SetCurrentAction(TurnAction.LaunchBomb);
            ApplyActionSelectionUi();
            TerminalLogger.Action($"Action button clicked: {_turnDetector.CurrentPlayer} selected LaunchBomb");
        }

        private void Form1_Resize(object? sender, EventArgs e)
        {
            CenterBoardInHost();
        }

        private void Initialize()
        {
            // Initialisation des services data (accès base).
            DatabaseManager databaseManager = new DatabaseManager();
            GenericRepository repository = databaseManager.Repository;
            _partieService = new PartieService(repository);
            _actionService = new ActionService(repository);
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

            StartFromSetup(setup);
        }

        private void Board_MouseClick(object? sender, MouseEventArgs e)
        {
            // Garde-fou: on ignore toute interaction avant le démarrage.
            if (!_isGameInitialized)
            {
                TerminalLogger.Action("Click ignored: game not initialized yet");
                return;
            }

            if (!_etatPartie.IsInProgress)
            {
                TerminalLogger.Action("Click ignored: game is finished");
                return;
            }

            TerminalLogger.Action($"Mouse click received at pixel=({e.X},{e.Y})");

            if (_turnDetector.CurrentAction == TurnAction.LaunchBomb)
            {
                // En mode bombe, un clic sert à choisir la ligne de tir (pas à poser un pion).
                HandleBombRowSelection(e.X, e.Y);
                return;
            }

            // Convertir le clic pixel en coordonnées de matrice
            int x = (int)Math.Round((float)(e.X - _board.BoardMargin) / _board.CellSize);
            int y = (int)Math.Round((float)(e.Y - _board.BoardMargin) / _board.CellSize);
            TerminalLogger.Action($"Translated to grid position=({x},{y})");

            // Vérifier qu'on est bien sur une intersection de la grille
            if (x >= 0 && x < _board.GridColumns && y >= 0 && y < _board.GridRows)
            {
                // La couleur du coup vient du joueur courant.
                Color currentPlayerColor = _turnDetector.CurrentPlayer == _turnDetector.Player1 ? Color.Blue : Color.Red;
                if (!_engine.TryPlaceStone(x, y, currentPlayerColor, out GameStone? placedStone, out IReadOnlyList<WinningLine> newLines) || placedStone is null)
                {
                    TerminalLogger.Action("Move ignored by engine");
                    return;
                }

                var placedPoint = new GamePoint(placedStone.X, placedStone.Y, placedStone.Color);
                _board.PlacedPoints.Add(placedPoint);
                TerminalLogger.Action($"UI point added at ({placedStone.X},{placedStone.Y}) with color={placedStone.Color.Name}");

                SyncWinningLines(newLines);

                // Persistance et historique local du coup (utile pour chargement + undo).
                _actionService.TryRecordPointAction(
                    _currentPartieId,
                    _turnDetector.CurrentPlayer,
                    placedStone.X,
                    placedStone.Y,
                    _turnNumber);

                AddActionToHistory("POINT", _turnDetector.CurrentPlayer, placedStone.X, placedStone.Y, _turnNumber);

                _board.Invalidate(); // Force à redessiner le plateau (OnPaint)
                TerminalLogger.Action("Board invalidated for repaint");
                MoveToNextTurn();
                return;
            }

            TerminalLogger.Action("Click ignored because it is outside grid bounds");
        }

        private void MoveToNextTurn()
        {
            // Changement de tour = reset des états temporaires de bombe.
            _pendingBombRowOneBased = null;
            _board.DisableBombSelection();

            if (!_etatPartie.IsInProgress)
            {
                TerminalLogger.Action("Turn progression blocked: game is finished");
                return;
            }

            _turnDetector.AdvanceTurn();
            _turnNumber++;
            PromptCurrentTurnAction();
        }

        private void HandleBombRowSelection(int pixelX, int pixelY)
        {
            // Le côté de tir dépend du joueur courant.
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
            UpdateTurnStatusText($"Tour de {_turnDetector.CurrentPlayer} - Ligne {selectedRow} choisie, appuie sur Ctrl + pavé numérique (1..9) pour tirer");
        }

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            // Le clavier ne sert que pendant une partie active.
            if (!_isGameInitialized)
            {
                return;
            }

            if (!_etatPartie.IsInProgress)
            {
                TerminalLogger.Action("Keyboard input ignored: game is finished");
                return;
            }

            if (e.Control && e.KeyCode == Keys.Z)
            {
                // Raccourci global de retour arrière.
                e.Handled = true;
                e.SuppressKeyPress = true;
                UndoLastRound();
                return;
            }

            if (_turnDetector.CurrentAction != TurnAction.LaunchBomb || !_pendingBombRowOneBased.HasValue)
            {
                return;
            }

            int? power = TryMapPowerFromKey(e.KeyCode, e.Control);
            if (!power.HasValue)
            {
                return;
            }

            bool fromLeft = _turnDetector.CurrentPlayer == _turnDetector.Player1;
            Color shooterColor = fromLeft ? Color.Blue : Color.Red;
            bool success = _engine.TryLaunchBomb(fromLeft, _pendingBombRowOneBased.Value, power.Value, shooterColor, out Point targetCell, out GameStone? removedStone, out bool hitProtectedWinningPoint, out IReadOnlyList<WinningLine> currentWinningLines);
            TerminalLogger.Action($"Bomb power received from keyboard: {power.Value}");

            if (!success)
            {
                TerminalLogger.Action("Bomb action rejected by engine");
                MoveToNextTurn();
                return;
            }

            if (hitProtectedWinningPoint)
            {
                TerminalLogger.Action($"Bomb had no effect: ({targetCell.X},{targetCell.Y}) is protected by a winning line");
            }
            else if (removedStone is null)
            {
                TerminalLogger.Action($"Bomb had no effect at ({targetCell.X},{targetCell.Y}) (empty cell or own stone)");
            }
            else
            {
                TerminalLogger.Action($"Bomb removed stone at ({removedStone.X},{removedStone.Y})");
            }

            SyncPointsFromEngine();
            SyncWinningLines(currentWinningLines);

            // Même en cas d'échec de bombe, on sauvegarde l'action car elle consomme le tour.
            _actionService.TryRecordBombAction(
                _currentPartieId,
                _turnDetector.CurrentPlayer,
                targetCell.X,
                targetCell.Y,
                _turnNumber);

            AddActionToHistory("BOMBE", _turnDetector.CurrentPlayer, targetCell.X, targetCell.Y, _turnNumber);

            _board.Invalidate();
            TerminalLogger.Action("Board synchronized after bomb action");

            e.Handled = true;
            e.SuppressKeyPress = true;
            MoveToNextTurn();
        }

        private void SyncPointsFromEngine()
        {
            // La vue plateau est toujours synchronisée à partir de l'état moteur.
            _board.PlacedPoints.Clear();
            foreach (GameStone stone in _engine.Stones)
            {
                _board.PlacedPoints.Add(new GamePoint(stone.X, stone.Y, stone.Color));
            }
        }

        private void SyncWinningLines(IReadOnlyList<WinningLine> lines)
        {
            // On garde les lignes déjà dessinées pour éviter les doublons visuels.
            foreach (WinningLine line in lines)
            {
                string signature = BuildLineSignature(line);
                if (_displayedLineSignatures.Add(signature))
                {
                    _board.WinningLines.Add((line.Start, line.End, line.Color));
                    TerminalLogger.Action($"Winning line persisted on board: {signature}");
                }
            }

            UpdateScoresFromLines(lines);
        }

        private void UpdateScoresFromLines(IReadOnlyList<WinningLine> lines)
        {
            // On incrémente le score uniquement lors de la première apparition d'une ligne.
            foreach (WinningLine line in lines)
            {
                string signature = BuildLineSignature(line);
                if (!_awardedLineSignatures.Add(signature))
                {
                    continue;
                }

                if (line.Color == Color.Blue)
                {
                    _player1Score++;
                    TerminalLogger.Action($"Score updated: {_player1Name} +1 (total={_player1Score})");
                }
                else if (line.Color == Color.Red)
                {
                    _player2Score++;
                    TerminalLogger.Action($"Score updated: {_player2Name} +1 (total={_player2Score})");
                }
            }
        }

        private static string BuildLineSignature(WinningLine line)
        {
            Point a = line.Start;
            Point b = line.End;

            bool swap = a.X > b.X || (a.X == b.X && a.Y > b.Y);
            Point first = swap ? b : a;
            Point second = swap ? a : b;

            return $"{line.Color.ToArgb()}|{first.X},{first.Y}|{second.X},{second.Y}";
        }

        private void PromptCurrentTurnAction()
        {
            // Cette méthode centralise l'UX de début de tour.
            if (!_etatPartie.IsInProgress)
            {
                TerminalLogger.Action("Turn prompt skipped: game is finished");
                return;
            }

            ApplyActionSelectionUi();
            TerminalLogger.Action($"Turn UI refreshed for {_turnDetector.CurrentPlayer}");
        }

        private static int? TryMapPowerFromKey(Keys key, bool isCtrlPressed)
        {
            // Le tir canon n'est valide que via Ctrl + pavé numérique 1..9.
            if (!isCtrlPressed)
            {
                return null;
            }

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

        private void EndGameButton_Click(object? sender, EventArgs e)
        {
            if (!_isGameInitialized)
            {
                TerminalLogger.Action("End game ignored: game not initialized");
                return;
            }

            if (!_etatPartie.IsInProgress)
            {
                TerminalLogger.Action("End game ignored: game already finished");
                return;
            }

            _etatPartie.EndGame("User clicked Terminer la partie");
            _pendingBombRowOneBased = null;
            _board.DisableBombSelection();
            _placePointButton.Enabled = false;
            _shootButton.Enabled = false;
            _endGameButton.Enabled = false;
            _undoButton.Enabled = false;

            bool replayRequested = GameResultAlert.ShowResultAndAskReplay(
                this,
                _player1Name,
                _player1Score,
                _player2Name,
                _player2Score);

            TerminalLogger.Action($"Result dialog closed, replayRequested={replayRequested}");

            if (replayRequested)
            {
                if (!GameSetupMenu.TryGetConfiguration(this, out GameSetupResult? setup) || setup is null)
                {
                    TerminalLogger.Action("Replay requested but setup menu was canceled");
                    return;
                }

                StartFromSetup(setup);
            }
        }

        private void StartFromSetup(GameSetupResult setup)
        {
            // Point d'entrée unique: nouvelle partie OU chargement.
            if (setup.IsLoadRequest && setup.PartieIdToLoad.HasValue)
            {
                StartLoadedGame(setup.PartieIdToLoad.Value);
                return;
            }

            StartConfiguredGame(setup.Player1Name, setup.Player2Name, setup.GridWidth, setup.GridHeight);
        }

        private void StartConfiguredGame(string player1Name, string player2Name, int gridWidth, int gridHeight)
        {
            // Nouvelle partie: on crée l'enregistrement puis on reconstruit un état vide.
            ConfigurePartieMetadata(player1Name, player2Name, gridWidth, gridHeight, 0);
            _actionHistory.Clear();
            _currentPartieId = _partieService.TryCreatePartie(player1Name, player2Name, gridWidth, gridHeight);
            RebuildStateFromHistory(false);

            TerminalLogger.Action($"Game setup complete: P1={player1Name} (Blue), P2={player2Name} (Red), grid={gridWidth}x{gridHeight}, partieId={_currentPartieId}");
            PromptCurrentTurnAction();
        }

        private void StartLoadedGame(int partieId)
        {
            // Chargement d'une partie existante en base.
            var partie = _partieService.TryGetPartieById(partieId);
            if (partie is null)
            {
                MessageBox.Show(this, "Impossible de charger cette partie (introuvable).", "Chargement", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ConfigurePartieMetadata(partie.Player1, partie.Player2, partie.GridSize, partie.GridSize, partie.Id);

            var actions = _actionService.TryGetByPartieId(partieId);
            LoadActionHistory(actions);

            RebuildStateFromHistory(false);

            TerminalLogger.Action($"Saved game loaded: partieId={partie.Id}, actions={actions.Count}, nextTurn={_turnNumber}, currentPlayer={_turnDetector.CurrentPlayer}");
            PromptCurrentTurnAction();
        }

        private void UndoButton_Click(object? sender, EventArgs e)
        {
            // Délégation explicite vers la logique de retour.
            UndoLastRound();
        }

        private void UndoLastRound()
        {
            // Retour = annuler les 2 derniers tours (moi + adversaire).
            if (!_isGameInitialized || !_etatPartie.IsInProgress)
            {
                TerminalLogger.Action("Undo ignored: game not ready");
                return;
            }

            if (_actionHistory.Count < 2)
            {
                MessageBox.Show(this, "Il faut au moins 2 tours pour faire un retour.", "Retour", MessageBoxButtons.OK, MessageBoxIcon.Information);
                TerminalLogger.Action("Undo ignored: not enough actions in history");
                return;
            }

            _pendingBombRowOneBased = null;
            _board.DisableBombSelection();

            bool dbUndoOk = _actionService.TryDeleteLastActions(_currentPartieId, 2);
            if (!dbUndoOk)
            {
                TerminalLogger.Action("Undo warning: database history not updated, using local history only");
            }

            _actionHistory.RemoveRange(_actionHistory.Count - 2, 2);
            RebuildStateFromHistory(false);

            TerminalLogger.Action($"Undo applied: 2 actions removed, remaining={_actionHistory.Count}, currentPlayer={_turnDetector.CurrentPlayer}");
            PromptCurrentTurnAction();
        }

        private void AddActionToHistory(string type, string playerName, int x, int y, int tourNumero)
        {
            // L'historique local est notre base pour reconstruire l'état visuel et logique.
            _actionHistory.Add(new ActionModel
            {
                PartieId = _currentPartieId,
                PlayerName = playerName,
                X = x,
                Y = y,
                TourNumero = tourNumero,
                TypeAction = type
            });
        }

        private void RebuildStateFromHistory(bool showActionPrompt)
        {
            // Reconstruction totale déterministe depuis l'historique:
            // 1) reset complet
            // 2) replay ordonné des actions
            // 3) recalcul tour/score/lignes
            _player1Score = 0;
            _player2Score = 0;
            _awardedLineSignatures.Clear();
            _displayedLineSignatures.Clear();

            _board.GridColumns = _gridWidth;
            _board.GridRows = _gridHeight;
            _board.PlacedPoints.Clear();
            _board.WinningLines.Clear();
            _board.DisableBombSelection();

            _engine = new GomokuEngine(_gridWidth, _gridHeight);
            _turnDetector = new TurnDetector(_player1Name, _player2Name);
            _etatPartie = new EtatPartie();
            _etatPartie.StartGame();
            _pendingBombRowOneBased = null;
            _isGameInitialized = true;
            _placePointButton.Enabled = true;
            _shootButton.Enabled = true;
            _endGameButton.Enabled = true;
            _undoButton.Enabled = true;

            foreach (var action in _actionHistory.OrderBy(a => a.TourNumero).ThenBy(a => a.Id))
            {
                // On rejoue exactement chaque action pour retrouver un état cohérent.
                Color actorColor = ResolvePlayerColor(action.PlayerName, action.TourNumero);

                if (string.Equals(action.TypeAction, "POINT", StringComparison.OrdinalIgnoreCase))
                {
                    if (_engine.TryPlaceStone(action.X, action.Y, actorColor, out GameStone? replayPlacedStone, out IReadOnlyList<WinningLine> newLines))
                    {
                        SyncWinningLines(newLines);
                    }
                }
                else if (string.Equals(action.TypeAction, "BOMBE", StringComparison.OrdinalIgnoreCase))
                {
                    if (_engine.TryApplyBombAtTarget(actorColor, new Point(action.X, action.Y), out GameStone? replayRemovedStone, out bool replayHitProtectedPoint, out IReadOnlyList<WinningLine> currentWinningLines))
                    {
                        SyncWinningLines(currentWinningLines);
                    }
                }
            }

            SyncPointsFromEngine();

            _turnNumber = _actionHistory.Count == 0 ? 1 : _actionHistory.Max(a => a.TourNumero) + 1;
            if (_actionHistory.Count > 0)
            {
                string lastPlayer = _actionHistory[^1].PlayerName;
                string expectedCurrent = string.Equals(lastPlayer, _turnDetector.Player1, StringComparison.OrdinalIgnoreCase)
                    ? _turnDetector.Player2
                    : _turnDetector.Player1;

                if (!string.Equals(_turnDetector.CurrentPlayer, expectedCurrent, StringComparison.OrdinalIgnoreCase))
                {
                    _turnDetector.AdvanceTurn();
                }
            }

            _board.Invalidate();
            ApplyAdaptiveWindowLayout();
            if (showActionPrompt)
            {
                PromptCurrentTurnAction();
            }
        }

        private void ApplyAdaptiveWindowLayout()
        {
            Rectangle workingArea = Screen.FromControl(this).WorkingArea;
            Size boardSize = _board.GetRequiredPixelSize();

            const int horizontalClientPadding = 80;
            int desiredClientWidth = boardSize.Width + horizontalClientPadding;
            int nonClientWidth = this.Width - this.ClientSize.Width;
            if (nonClientWidth < 0)
            {
                nonClientWidth = 16;
            }

            int targetWidth = Math.Min(workingArea.Width, desiredClientWidth + nonClientWidth);
            int targetHeight = workingArea.Height;
            int targetX = workingArea.Left + Math.Max(0, (workingArea.Width - targetWidth) / 2);

            this.SetBounds(targetX, workingArea.Top, targetWidth, targetHeight);
            CenterBoardInHost();
        }

        private void CenterBoardInHost()
        {
            if (_boardHostPanel is null || _board is null)
            {
                return;
            }

            Size boardSize = _board.GetRequiredPixelSize();
            _board.Size = boardSize;

            int centeredX = Math.Max(0, (_boardHostPanel.ClientSize.Width - boardSize.Width) / 2);
            int centeredY = Math.Max(0, (_boardHostPanel.ClientSize.Height - boardSize.Height) / 2);
            _board.Location = new Point(centeredX, centeredY);
        }

        private void ConfigurePartieMetadata(string player1Name, string player2Name, int gridWidth, int gridHeight, int partieId)
        {
            // Cette méthode isole les affectations de métadonnées de partie.
            _player1Name = player1Name;
            _player2Name = player2Name;
            _gridWidth = gridWidth;
            _gridHeight = gridHeight;
            _turnNumber = 1;
            _currentPartieId = partieId;
        }

        private void LoadActionHistory(IReadOnlyList<ActionModel> actions)
        {
            // Copie explicite pour éviter les effets de bord sur les objets fournis par le service.
            _actionHistory.Clear();
            _actionHistory.AddRange(actions.Select(a => new ActionModel
            {
                Id = a.Id,
                PartieId = a.PartieId,
                PlayerName = a.PlayerName,
                X = a.X,
                Y = a.Y,
                TourNumero = a.TourNumero,
                TypeAction = a.TypeAction
            }));
        }

        private Color ResolvePlayerColor(string playerName, int turnNumber)
        {
            // Priorité aux noms joueurs réels; sinon fallback basé sur parité du tour.
            if (string.Equals(playerName, _player1Name, StringComparison.OrdinalIgnoreCase))
            {
                return Color.Blue;
            }

            if (string.Equals(playerName, _player2Name, StringComparison.OrdinalIgnoreCase))
            {
                return Color.Red;
            }

            return turnNumber % 2 == 1 ? Color.Blue : Color.Red;
        }

        private void ApplyActionSelectionUi()
        {
            if (!_isGameInitialized || !_etatPartie.IsInProgress)
            {
                return;
            }

            _pendingBombRowOneBased = null;

            bool isBombAction = _turnDetector.CurrentAction == TurnAction.LaunchBomb;
            bool fromLeft = _turnDetector.CurrentPlayer == _turnDetector.Player1;

            if (isBombAction)
            {
                _board.EnableBombSelection(fromLeft);
                UpdateTurnStatusText($"Tour de {_turnDetector.CurrentPlayer} - Action: Tirer (choisis une ligne sur le canon)");
            }
            else
            {
                _board.DisableBombSelection();
                UpdateTurnStatusText($"Tour de {_turnDetector.CurrentPlayer} - Action: Placer point");
            }

            _placePointButton.BackColor = isBombAction ? SystemColors.Control : Color.LightSkyBlue;
            _shootButton.BackColor = isBombAction ? Color.LightSalmon : SystemColors.Control;
        }

        private void UpdateTurnStatusText(string text)
        {
            if (_turnStatusLabel is null)
            {
                return;
            }

            _turnStatusLabel.Text = text;
        }
    }
}