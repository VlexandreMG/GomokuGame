namespace _;

using GomokuGame.ui;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    /// <summary>
    /// Point d'entrée WinForms: initialise la configuration globale puis ouvre la fenêtre principale.
    /// </summary>
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }    
}