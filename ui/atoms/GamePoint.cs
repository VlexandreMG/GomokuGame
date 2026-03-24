using System.Drawing;
using System.Windows.Forms;
using GomokuGame.ui;

namespace GomokuGame.ui.atoms;

public class GamePoint : BaseComponent {

    public Point Coordinates {get;set;}
    public Color PointColor {get;set;}
    private int _visualSize = 30;
    
    /// <summary>
    /// Crée un point de jeu avec des coordonnées logiques (grille) et une couleur.
    /// </summary>
    public GamePoint(int x, int y, Color color) {

        this.Coordinates= new Point(x,y);
        this.PointColor = color;
    }

    /// <summary>
    /// Aucun sous-composant à créer: l'atome se dessine lui-même.
    /// </summary>
    protected override void CreateComponents()
    {
    }

    /// <summary>
    /// Définit la taille visuelle du point à l'écran.
    /// </summary>
    protected override void SetupLayout()
    {
        this.Size = new Size(_visualSize, _visualSize);
    }

    /// <summary>
    /// Rend le fond transparent pour ne pas masquer la grille.
    /// </summary>
    protected override void ApplyDefaultStyles()
    {
        this.BackColor = Color.Transparent;
    }

    /// <summary>
    /// Aucun événement local requis pour cet atome.
    /// </summary>
    protected override void SetupEventHandlers()
    {
    }

    /// <summary>
    /// Initialisation spécifique inutile ici (méthode obligatoire du cycle de vie).
    /// </summary>
    protected override void Initialize()
    {
    }

    // La méthode "atomique" : dessiner CE point précis
        /// <summary>
        /// Dessine le point sur les coordonnées pixel calculées par l'organisme parent.
        /// </summary>
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