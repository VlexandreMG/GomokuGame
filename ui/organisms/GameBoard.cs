// UI/Organisms/GameBoard.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GomokuGame.ui;
using GomokuGame.ui.atoms; // Pour utiliser l'atome GamePoint

namespace GomokuGame.ui.organisms
{
    public class GameBoard : BaseComponent
    {
        public int GridSize { get; set; } = 15;
        public int CellSize { get; set; } = 40;
        public int BoardMargin { get; set; } = 50;
        public int CannonOffset { get; set; } = 26;
        public int CannonWidth { get; set; } = 20;
        public int CannonHeight { get; set; } = 18;

        // Ta liste d'atomes "GamePoint" déjà posés
        public List<GamePoint> PlacedPoints { get; set; } = new List<GamePoint>();
        public List<(Point Start, Point End, Color Color)> WinningLines { get; } = new List<(Point Start, Point End, Color Color)>();
        public bool IsBombSelectionActive { get; private set; }
        public bool BombFromLeft { get; private set; }
        public int? SelectedBombRowOneBased { get; private set; }

        protected override void CreateComponents()
        {
        }

        protected override void SetupLayout()
        {
            this.Dock = DockStyle.Fill;
        }

        protected override void ApplyDefaultStyles()
        {
            this.DoubleBuffered = true; // Pour éviter les clignotements
            this.BackColor = Color.NavajoWhite; // Couleur "plateau de jeu"
        }

        protected override void SetupEventHandlers()
        {
        }

        protected override void Initialize()
        {
        }

        // LA méthode qui dessine tout l'organisme
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            DrawGridLines(g);
            DrawGridReferences(g);
            DrawBombCannons(g);
            DrawAllPoints(g);
            DrawWinningLine(g);
        }

        private void DrawGridLines(Graphics g)
        {
            using (Pen pen = new Pen(Color.Black, 1))
            {
                for (int i = 0; i < GridSize; i++)
                {
                    // Lignes horizontales
                    g.DrawLine(pen, BoardMargin, BoardMargin + (i * CellSize), 
                                    BoardMargin + ((GridSize - 1) * CellSize), BoardMargin + (i * CellSize));
                    // Lignes verticales
                    g.DrawLine(pen, BoardMargin + (i * CellSize), BoardMargin, 
                                    BoardMargin + (i * CellSize), BoardMargin + ((GridSize - 1) * CellSize));
                }
            }
        }

        private void DrawGridReferences(Graphics g)
        {
            using Font labelFont = new Font("Segoe UI", 9, FontStyle.Bold);
            using Brush labelBrush = new SolidBrush(Color.Black);

            for (int i = 0; i < GridSize; i++)
            {
                string label = (i + 1).ToString();
                SizeF rowTextSize = g.MeasureString(label, labelFont);
                float rowY = BoardMargin + (i * CellSize) - (rowTextSize.Height / 2f);

                g.DrawString(label, labelFont, labelBrush, BoardMargin - 36, rowY);
                g.DrawString(label, labelFont, labelBrush, BoardMargin + ((GridSize - 1) * CellSize) + 20, rowY);

                SizeF colTextSize = g.MeasureString(label, labelFont);
                float colX = BoardMargin + (i * CellSize) - (colTextSize.Width / 2f);
                g.DrawString(label, labelFont, labelBrush, colX, BoardMargin - 32);
            }
        }

        private void DrawAllPoints(Graphics g)
        {
            foreach (var pt in PlacedPoints)
            {
                // Convertir (x, y) de la matrice en pixels
                Point visualLoc = new Point(BoardMargin + (pt.Coordinates.X * CellSize), 
                                            BoardMargin + (pt.Coordinates.Y * CellSize));
                pt.Draw(g, visualLoc); // Appeler le dessin de l'atome
            }
        }

        public void AddWinningLine(Point start, Point end, Color color)
        {
            WinningLines.Add((start, end, color));
            Invalidate();
        }

        public void EnableBombSelection(bool fromLeft)
        {
            IsBombSelectionActive = true;
            BombFromLeft = fromLeft;
            SelectedBombRowOneBased = null;
            Invalidate();
        }

        public void DisableBombSelection()
        {
            IsBombSelectionActive = false;
            SelectedBombRowOneBased = null;
            Invalidate();
        }

        public void SetSelectedBombRow(int rowOneBased)
        {
            SelectedBombRowOneBased = rowOneBased;
            Invalidate();
        }

        public bool TryGetBombRowFromClick(int pixelX, int pixelY, bool fromLeft, out int rowOneBased)
        {
            rowOneBased = -1;

            if (!IsBombSelectionActive)
            {
                return false;
            }

            for (int rowIndex = 0; rowIndex < GridSize; rowIndex++)
            {
                Rectangle hitBox = BuildCannonHitBox(rowIndex, fromLeft);
                if (hitBox.Contains(pixelX, pixelY))
                {
                    rowOneBased = rowIndex + 1;
                    return true;
                }
            }

            return false;
        }

        private void DrawBombCannons(Graphics g)
        {
            if (!IsBombSelectionActive)
            {
                return;
            }

            using Pen cannonPen = new Pen(Color.FromArgb(70, 70, 70), 2);
            using Brush activeBrush = new SolidBrush(Color.OrangeRed);
            using Brush normalBrush = new SolidBrush(Color.Gray);

            for (int rowIndex = 0; rowIndex < GridSize; rowIndex++)
            {
                int rowOneBased = rowIndex + 1;
                bool isSelected = SelectedBombRowOneBased.HasValue && SelectedBombRowOneBased.Value == rowOneBased;
                Brush cannonBrush = isSelected ? activeBrush : normalBrush;

                Point[] cannon = BuildCannonTriangle(rowIndex, BombFromLeft);
                g.FillPolygon(cannonBrush, cannon);
                g.DrawPolygon(cannonPen, cannon);
            }
        }

        private Rectangle BuildCannonHitBox(int rowIndex, bool fromLeft)
        {
            int centerY = BoardMargin + (rowIndex * CellSize);
            int centerX = fromLeft
                ? BoardMargin - CannonOffset
                : BoardMargin + ((GridSize - 1) * CellSize) + CannonOffset;

            return new Rectangle(
                centerX - (CannonWidth / 2),
                centerY - (CannonHeight / 2),
                CannonWidth,
                CannonHeight);
        }

        private Point[] BuildCannonTriangle(int rowIndex, bool fromLeft)
        {
            Rectangle box = BuildCannonHitBox(rowIndex, fromLeft);
            int left = box.Left;
            int right = box.Right;
            int top = box.Top;
            int bottom = box.Bottom;
            int midY = box.Top + (box.Height / 2);

            if (fromLeft)
            {
                return new[]
                {
                    new Point(left, top),
                    new Point(left, bottom),
                    new Point(right, midY)
                };
            }

            return new[]
            {
                new Point(right, top),
                new Point(right, bottom),
                new Point(left, midY)
            };
        }

        private void DrawWinningLine(Graphics g)
        {
            if (WinningLines.Count == 0)
            {
                return;
            }

            foreach (var line in WinningLines)
            {
                Point startPixel = new Point(BoardMargin + (line.Start.X * CellSize), BoardMargin + (line.Start.Y * CellSize));
                Point endPixel = new Point(BoardMargin + (line.End.X * CellSize), BoardMargin + (line.End.Y * CellSize));

                using (Pen winPen = new Pen(line.Color, 6))
                {
                    g.DrawLine(winPen, startPixel, endPixel);
                }
            }
        }
    }
}