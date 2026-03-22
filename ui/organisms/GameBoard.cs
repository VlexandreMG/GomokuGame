// UI/Organisms/GameBoard.cs
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
        public int BoardMargin { get; set; } = 20;

        // Ta liste d'atomes "GamePoint" déjà posés
        public List<GamePoint> PlacedPoints { get; set; } = new List<GamePoint>();

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
            DrawAllPoints(g);
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
    }
}