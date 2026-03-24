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
        // Paramètres visuels du plateau (taille grille, marges, canons).
        public int GridColumns { get; set; } = 10;
        public int GridRows { get; set; } = 10;
        public int CellSize { get; set; } = 40;
        public int BoardMargin { get; set; } = 50;
        public int CannonOffset { get; set; } = 26;
        public int CannonWidth { get; set; } = 20;
        public int CannonHeight { get; set; } = 18;

        // Données affichées par l'organisme.
        public List<GamePoint> PlacedPoints { get; set; } = new List<GamePoint>();
        public List<(Point Start, Point End, Color Color)> WinningLines { get; } = new List<(Point Start, Point End, Color Color)>();
        public bool IsBombSelectionActive { get; private set; }
        public bool BombFromLeft { get; private set; }
        public int? SelectedBombRowOneBased { get; private set; }

        /// <summary>
        /// Aucun contrôle enfant à créer: tout est dessiné dans OnPaint.
        /// </summary>
        protected override void CreateComponents()
        {
        }

        /// <summary>
        /// Fait occuper toute la zone disponible au plateau de jeu.
        /// </summary>
        protected override void SetupLayout()
        {
            this.Dock = DockStyle.None;
        }

        /// <summary>
        /// Active le double buffering et applique la couleur de fond du plateau.
        /// </summary>
        protected override void ApplyDefaultStyles()
        {
            this.DoubleBuffered = true; // Pour éviter les clignotements
            this.BackColor = Color.White; // Plateau blanc
        }

        /// <summary>
        /// Le board n'attache pas d'événement interne; les événements sont gérés par Form1.
        /// </summary>
        protected override void SetupEventHandlers()
        {
        }

        /// <summary>
        /// Aucune initialisation supplémentaire nécessaire pour cet organisme.
        /// </summary>
        protected override void Initialize()
        {
        }

        // LA méthode qui dessine tout l'organisme
        /// <summary>
        /// Dessine l'ensemble du plateau: grille, repères, canons, points et lignes gagnantes.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Ordre de rendu: plateau -> repères -> canons -> points -> lignes gagnantes.
            DrawGridLines(g);
            DrawGridReferences(g);
            DrawBombCannons(g);
            DrawAllPoints(g);
            DrawWinningLine(g);
        }

        /// <summary>
        /// Trace les lignes horizontales et verticales de la grille.
        /// </summary>
        private void DrawGridLines(Graphics g)
        {
            using (Pen pen = new Pen(Color.Black, 1))
            {
                for (int row = 0; row < GridRows; row++)
                {
                    // Lignes horizontales
                    g.DrawLine(pen, BoardMargin, BoardMargin + (row * CellSize),
                                    BoardMargin + ((GridColumns - 1) * CellSize), BoardMargin + (row * CellSize));
                }

                for (int column = 0; column < GridColumns; column++)
                {
                    // Lignes verticales
                    g.DrawLine(pen, BoardMargin + (column * CellSize), BoardMargin,
                                    BoardMargin + (column * CellSize), BoardMargin + ((GridRows - 1) * CellSize));
                }
            }
        }

        /// <summary>
        /// Dessine les numéros de référence des lignes/colonnes autour de la grille.
        /// </summary>
        private void DrawGridReferences(Graphics g)
        {
            using Font labelFont = new Font("Segoe UI", 9, FontStyle.Bold);
            using Brush labelBrush = new SolidBrush(Color.Black);

            for (int column = 0; column < GridColumns; column++)
            {
                string label = (column + 1).ToString();

                SizeF colTextSize = g.MeasureString(label, labelFont);
                float colX = BoardMargin + (column * CellSize) - (colTextSize.Width / 2f);
                g.DrawString(label, labelFont, labelBrush, colX, BoardMargin - 32);

                string reverseLabel = (GridColumns - column).ToString();
                SizeF bottomTextSize = g.MeasureString(reverseLabel, labelFont);
                float bottomX = BoardMargin + (column * CellSize) - (bottomTextSize.Width / 2f);
                float bottomY = BoardMargin + ((GridRows - 1) * CellSize) + 12;
                g.DrawString(reverseLabel, labelFont, labelBrush, bottomX, bottomY);
            }
        }

        /// <summary>
        /// Dessine tous les pions posés en convertissant leurs coordonnées logiques en pixels.
        /// </summary>
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

        /// <summary>
        /// Ajoute une ligne gagnante à l'affichage puis force un redraw.
        /// </summary>
        public void AddWinningLine(Point start, Point end, Color color)
        {
            WinningLines.Add((start, end, color));
            Invalidate();
        }

        /// <summary>
        /// Active le mode sélection de ligne canon (avec le côté de tir).
        /// </summary>
        public void EnableBombSelection(bool fromLeft)
        {
            IsBombSelectionActive = true;
            BombFromLeft = fromLeft;
            SelectedBombRowOneBased = null;
            Invalidate();
        }

        /// <summary>
        /// Désactive le mode sélection de bombe et efface la ligne choisie.
        /// </summary>
        public void DisableBombSelection()
        {
            IsBombSelectionActive = false;
            SelectedBombRowOneBased = null;
            Invalidate();
        }

        /// <summary>
        /// Met à jour la ligne actuellement sélectionnée pour le tir canon.
        /// </summary>
        public void SetSelectedBombRow(int rowOneBased)
        {
            SelectedBombRowOneBased = rowOneBased;
            Invalidate();
        }

        /// <summary>
        /// Détermine si un clic correspond à un canon et renvoie la ligne ciblée.
        /// </summary>
        public bool TryGetBombRowFromClick(int pixelX, int pixelY, bool fromLeft, out int rowOneBased)
        {
            // Traduit un clic pixel en numéro de ligne ciblée par canon.
            rowOneBased = -1;

            if (!IsBombSelectionActive)
            {
                return false;
            }

            for (int rowIndex = 0; rowIndex < GridRows; rowIndex++)
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

        /// <summary>
        /// Dessine les canons interactifs pour chaque ligne pendant le mode bombe.
        /// </summary>
        private void DrawBombCannons(Graphics g)
        {
            if (!IsBombSelectionActive)
            {
                return;
            }

            using Pen cannonPen = new Pen(Color.FromArgb(70, 70, 70), 2);
            using Brush activeBrush = new SolidBrush(Color.OrangeRed);
            using Brush normalBrush = new SolidBrush(Color.Gray);

            for (int rowIndex = 0; rowIndex < GridRows; rowIndex++)
            {
                int rowOneBased = rowIndex + 1;
                bool isSelected = SelectedBombRowOneBased.HasValue && SelectedBombRowOneBased.Value == rowOneBased;
                Brush cannonBrush = isSelected ? activeBrush : normalBrush;

                Point[] cannon = BuildCannonTriangle(rowIndex, BombFromLeft);
                g.FillPolygon(cannonBrush, cannon);
                g.DrawPolygon(cannonPen, cannon);
            }
        }

        /// <summary>
        /// Construit la zone de clic d'un canon pour une ligne donnée.
        /// </summary>
        private Rectangle BuildCannonHitBox(int rowIndex, bool fromLeft)
        {
            int centerY = BoardMargin + (rowIndex * CellSize);
            int centerX = fromLeft
                ? BoardMargin - CannonOffset
                : BoardMargin + ((GridColumns - 1) * CellSize) + CannonOffset;

            return new Rectangle(
                centerX - (CannonWidth / 2),
                centerY - (CannonHeight / 2),
                CannonWidth,
                CannonHeight);
        }

        /// <summary>
        /// Construit les 3 points du triangle visuel représentant un canon.
        /// </summary>
        private Point[] BuildCannonTriangle(int rowIndex, bool fromLeft)
        {
            // Le triangle pointe vers la grille: droite si canon à gauche, inversement.
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

        /// <summary>
        /// Dessine toutes les lignes gagnantes persistées sur le plateau.
        /// </summary>
        private void DrawWinningLine(Graphics g)
        {
            if (WinningLines.Count == 0)
            {
                return;
            }

            foreach (var line in WinningLines)
            {
                Point startPixel = ToPixel(line.Start);
                Point endPixel = ToPixel(line.End);

                using (Pen winPen = new Pen(line.Color, 6))
                {
                    g.DrawLine(winPen, startPixel, endPixel);
                }
            }
        }

        /// <summary>
        /// Convertit une position logique de la grille en position pixel à l'écran.
        /// </summary>
        private Point ToPixel(Point boardPoint)
        {
            // Conversion centralisée matrice -> pixels.
            return new Point(
                BoardMargin + (boardPoint.X * CellSize),
                BoardMargin + (boardPoint.Y * CellSize));
        }

        /// <summary>
        /// Renvoie la taille minimale en pixels nécessaire pour afficher le plateau complet.
        /// </summary>
        public Size GetRequiredPixelSize()
        {
            int gridPixelWidth = (GridColumns - 1) * CellSize;
            int gridPixelHeight = (GridRows - 1) * CellSize;

            int rightVisualExtra = Math.Max(36, CannonOffset + CannonWidth);
            int bottomVisualExtra = Math.Max(24, CannonHeight + 8);

            int width = BoardMargin + gridPixelWidth + rightVisualExtra + 6;
            int height = BoardMargin + gridPixelHeight + bottomVisualExtra + 6;

            return new Size(width, height);
        }
    }
}