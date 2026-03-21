// UI/Organisms/GameBoard.cs
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GomokuGame.ui.atoms; // Pour utiliser l'atome GamePoint

namespace GomokuGame.ui.organisms
{
    public class GameBoard : Panel
    {
        public int GridSize { get; set; } = 15;
        public int CellSize { get; set; } = 40;
        public int Margin { get; set; } = 20;

        // Ta liste d'atomes "GamePoint" déjà posés
        public List<GamePoint> PlacedPoints { get; set; } = new List<GamePoint>();

        public GameBoard()
        {
            this.DoubleBuffered = true; // Pour éviter les clignotements
            this.BackColor = Color.NavajoWhite; // Couleur "plateau de jeu"
            this.Dock = DockStyle.Fill;
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
                    g.DrawLine(pen, Margin, Margin + (i * CellSize), 
                                    Margin + ((GridSize - 1) * CellSize), Margin + (i * CellSize));
                    // Lignes verticales
                    g.DrawLine(pen, Margin + (i * CellSize), Margin, 
                                    Margin + (i * CellSize), Margin + ((GridSize - 1) * CellSize));
                }
            }
        }

        private void DrawAllPoints(Graphics g)
        {
            foreach (var pt in PlacedPoints)
            {
                // Convertir (x, y) de la matrice en pixels
                Point visualLoc = new Point(Margin + (pt.Coordinates.X * CellSize), 
                                            Margin + (pt.Coordinates.Y * CellSize));
                pt.Draw(g, visualLoc); // Appeler le dessin de l'atome
            }
        }
    }
}