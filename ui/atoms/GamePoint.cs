using System.Drawing;

namespace GomokuGame.ui.atoms;

public class GamePoint {

    public Point Coordinates {get;set;}
    public Color PointColor {get;set;}
    private int _visualSize = 30;
    
    public GamePoint(int x, int y, Color color) {

        this.Coordinates= new Point(x,y);
        this.PointColor = new Color();
    }

    // La méthode "atomique" : dessiner CE point précis
        public void Draw(Graphics g, Point visualLocation)
        {
            using (Brush brush = new SolidBrush(this.PointColor))
            {
                // On centre le cercle sur l'emplacement visuel
                g.FillEllipse(brush, visualLocation.X - (_visualSize / 2), 
                                     visualLocation.Y - (_visualSize / 2), 
                                     _visualSize, _visualSize);
            }
        }
}