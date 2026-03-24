using System.Windows.Forms;

namespace GomokuGame.ui
{
    // Classe de base UI: impose un cycle de vie cohérent à chaque composant visuel.
    public abstract class BaseComponent : UserControl
    {
        /// <summary>
        /// Constructeur commun: exécute immédiatement le cycle de vie du composant.
        /// </summary>
        protected BaseComponent()
        {
            InitializeLifecycle();
        }

        /// <summary>
        /// Orchestre les étapes d'initialisation dans un ordre unique et prévisible.
        /// </summary>
        private void InitializeLifecycle()
        {
            CreateComponents();
            SetupLayout();
            ApplyDefaultStyles();
            SetupEventHandlers();
            Initialize();
        }

        // Le contrat à remplir pour chaque UI
        /// <summary>
        /// Crée les contrôles enfants nécessaires au composant.
        /// </summary>
        protected abstract void CreateComponents();
        /// <summary>
        /// Définit la disposition (taille, docking, positions).
        /// </summary>
        protected abstract void SetupLayout();
        /// <summary>
        /// Applique les styles visuels par défaut.
        /// </summary>
        protected abstract void ApplyDefaultStyles();
        /// <summary>
        /// Branche les événements (clic, clavier, etc.).
        /// </summary>
        protected abstract void SetupEventHandlers();
        /// <summary>
        /// Exécute la logique finale d'initialisation métier/état.
        /// </summary>
        protected abstract void Initialize();
    }
}