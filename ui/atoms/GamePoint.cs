using System.Drawing;
using System.Windows.Forms;
using GomokuGame.ui;

namespace GomokuGame.ui.atoms;

public class GamePoint : BaseComponent {

    public Point Coordinates {get;set;}
    public Color PointColor {get;set;}
    private int _visualSize = 30;
    
    public GamePoint(int x, int y, Color color) {

        this.Coordinates= new Point(x,y);
        this.PointColor = color;
    }

    protected override void CreateComponents()
    {
    }

    protected override void SetupLayout()
    {
        this.Size = new Size(_visualSize, _visualSize);
    }

    protected override void ApplyDefaultStyles()
    {
        this.BackColor = Color.Transparent;
    }

    protected override void SetupEventHandlers()
    {
    }

    protected override void Initialize()
    {
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