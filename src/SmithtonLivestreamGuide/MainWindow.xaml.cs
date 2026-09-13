using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace SmithtonLivestreamGuide;

public partial class MainWindow : Window
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "SmithtonLivestreamGuide";

    private bool _isLoaded;
    private bool _updateCheckStarted;

    public MainWindow()
    {
        InitializeComponent();
        StartWithWindowsCheckBox.IsChecked = IsStartWithWindowsEnabled();
        _isLoaded = true;
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

    private void StartWithWindowsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded)
        {
            return;
        }

        try
        {
            if (StartWithWindowsCheckBox.IsChecked == true)
            {
                using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
                key.SetValue(RunValueName, $"\"{ExecutablePath}\"");
            }
            else
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
                key?.DeleteValue(RunValueName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Couldn't update the Windows startup setting:\n{ex.Message}",
                "Smithton Livestream Guide",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            StartWithWindowsCheckBox.IsChecked = IsStartWithWindowsEnabled();
        }
    }

    private static bool IsStartWithWindowsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        var value = key?.GetValue(RunValueName) as string;
        return value != null && string.Equals(value.Trim('"'), ExecutablePath, StringComparison.OrdinalIgnoreCase);
    }

    private static string ExecutablePath => Process.GetCurrentProcess().MainModule!.FileName!;
}
