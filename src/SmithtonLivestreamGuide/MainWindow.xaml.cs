using System;
using System.Diagnostics;
using System.Windows;
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

    private void AlwaysOnTopCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        Topmost = AlwaysOnTopCheckBox.IsChecked == true;
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
