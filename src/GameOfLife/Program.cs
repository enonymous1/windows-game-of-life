using System.Windows.Forms;

namespace GameOfLife;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // A backstop, not a substitute for fixing root causes: an
        // unhandled UI-thread exception shows a message instead of
        // killing the process outright.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (sender, args) =>
        {
            _ = sender;
            MessageBox.Show(args.Exception.Message, "Unexpected error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
