using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SmithtonLivestreamGuide;

public partial class MainWindow : Window
{
    private bool _updateCheckStarted;

    public MainWindow()
    {
        InitializeComponent();
        FitToScreen();
    }

    // On a small/low-resolution laptop screen, the XAML-defined default size can be taller than
    // the visible work area, pushing the title bar (and its close button) off-screen. Shrink to
    // fit the actual work area (screen minus taskbar) and center within it instead.
    private void FitToScreen()
    {
        var workArea = SystemParameters.WorkArea;
        const double margin = 40;

        Width = Math.Min(Width, Math.Max(MinWidth, workArea.Width - margin));
        Height = Math.Min(Height, Math.Max(MinHeight, workArea.Height - margin));

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = workArea.Left + (workArea.Width - Width) / 2;
        Top = workArea.Top + (workArea.Height - Height) / 2;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_updateCheckStarted)
        {
            return;
        }
        _updateCheckStarted = true;

        var updateInfo = await UpdateChecker.CheckForUpdateAsync();
        if (updateInfo is null)
        {
            return;
        }

        var updateWindow = new UpdateWindow(updateInfo) { Owner = this };
        updateWindow.ShowDialog();
    }

    // WPF's default mouse-wheel scroll distance feels too fast for this much text; scale it down.
    private const double ScrollSpeedFactor = 0.25;

    private void GuideScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;
        GuideScrollViewer.ScrollToVerticalOffset(GuideScrollViewer.VerticalOffset - (e.Delta * ScrollSpeedFactor));
    }

    private static readonly SolidColorBrush CopiedFlashBrush = new(Color.FromRgb(0xD1, 0xF2, 0xDD));

    private async void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string value)
        {
            return;
        }

        Clipboard.SetText(value);

        if (button.Template.FindName("CopyButtonBackground", button) is Border background)
        {
            background.Background = CopiedFlashBrush;
            await Task.Delay(600);
            background.Background = Brushes.Transparent;
        }
    }

    private void OpenLinkButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string url)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Couldn't open the link:\n{ex.Message}",
                "Smithton Livestream Guide",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow { Owner = this };
        settingsWindow.ShowDialog();
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
