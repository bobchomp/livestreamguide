using System;
using System.ComponentModel;
using System.Windows;

namespace SmithtonLivestreamGuide;

public partial class UpdateWindow : Window
{
    private readonly UpdateInfo _updateInfo;
    private bool _allowClose;

    public UpdateWindow(UpdateInfo updateInfo)
    {
        InitializeComponent();
        _updateInfo = updateInfo;
    }

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateButton.IsEnabled = false;
        RetryButton.IsEnabled = false;

        ErrorPanel.Visibility = Visibility.Collapsed;
        PromptPanel.Visibility = Visibility.Collapsed;
        ProgressPanel.Visibility = Visibility.Visible;
        UpdateButton.Visibility = Visibility.Collapsed;
        RetryButton.Visibility = Visibility.Collapsed;

        StatusTextBlock.Text = "Downloading update...";
        ProgressBarControl.Value = 0;

        var progress = new Progress<double>(percent => ProgressBarControl.Value = percent);

        try
        {
            await UpdateInstaller.DownloadAndLaunchInstallerAsync(_updateInfo.DownloadUrl, progress);

            StatusTextBlock.Text = "Installing update...";
            _allowClose = true;
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            ProgressPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Visible;
            ErrorTextBlock.Text = $"Couldn't download or start the update. Check the internet connection and try again.\n\n{ex.Message}";

            RetryButton.Visibility = Visibility.Visible;
            RetryButton.IsEnabled = true;
        }
    }

    private void UpdateWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
        }
    }
}
