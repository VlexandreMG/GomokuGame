// ui/Form1.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using GomokuGame.ui.organisms;
using GomokuGame.ui.atoms;

namespace GomokuGame.ui
{
    public partial class Form1 : Form
    {
        private GameBoard _board;

        public Form1()
        {
            this.Text = "Gomoku Game - Dessin Atomique";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            _board = new GameBoard();
            _board.MouseClick += Board_MouseClick; // Gérer le clic sur le plateau
            this.Controls.Add(_board);
        }

        private void Board_MouseClick(object? sender, MouseEventArgs e)
        {
            // Convertir le clic pixel en coordonnées de matrice
            int x = (int)Math.Round((float)(e.X - _board.Margin) / _board.CellSize);
            int y = (int)Math.Round((float)(e.Y - _board.Margin) / _board.CellSize);

            // Vérifier qu'on est bien sur une intersection de la grille
            if (x >= 0 && x < _board.GridSize && y >= 0 && y < _board.GridSize)
            {
                // Pour le test : on alterne Bleu et Rouge
                Color pointColor = (_board.PlacedPoints.Count % 2 == 0) ? Color.Blue : Color.Red;
                
                // CRÉER L'ATOME et l'ajouter à l'organisme
                _board.PlacedPoints.Add(new GamePoint(x, y, pointColor));
                _board.Invalidate(); // Force à redessiner le plateau (OnPaint)
            }
        }
    }
}