using System.Windows;
using System.Windows.Threading;

namespace SmithtonLivestreamGuide;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    // Show what actually went wrong instead of letting the whole guide silently disappear
    // mid-service over what might be a minor, recoverable UI bug.
    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"{e.Exception.GetType().Name}: {e.Exception.Message}\n\n{e.Exception.StackTrace}",
            "Smithton Livestream Guide - Unexpected Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }
}
