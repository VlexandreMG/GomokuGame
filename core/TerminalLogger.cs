using System;
using System.Runtime.InteropServices;

namespace GomokuGame.core;

public static class TerminalLogger
{
    private static bool _isReady;

    /// <summary>
    /// Prépare une console de sortie pour le debug (terminal parent ou nouvelle console).
    /// </summary>
    public static void Initialize()
    {
        if (_isReady)
        {
            return;
        }

        // Attach to the parent terminal when launched from one, otherwise allocate a new console.
        if (!AttachConsole(AttachParentProcess))
        {
            AllocConsole();
        }

        _isReady = true;
        Action("Logger initialised");
    }

    /// <summary>
    /// Ecrit une action horodatée dans la console pour faciliter le suivi du flux.
    /// </summary>
    public static void Action(string message)
    {
        if (!_isReady)
        {
            Initialize();
        }

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
    }

    private const int AttachParentProcess = -1;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);
}