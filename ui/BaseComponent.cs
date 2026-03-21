using System.Windows.Forms;

namespace GomokuGame.ui
{
    public abstract class BaseComponent : UserControl
    {
        protected BaseComponent()
        {
            InitializeLifecycle();
        }

        private void InitializeLifecycle()
        {
            CreateComponents();
            SetupLayout();
            ApplyDefaultStyles();
            SetupEventHandlers();
            Initialize();
        }

        // Le contrat à remplir pour chaque UI
        protected abstract void CreateComponents();
        protected abstract void SetupLayout();
        protected abstract void ApplyDefaultStyles();
        protected abstract void SetupEventHandlers();
        protected abstract void Initialize();
    }
}