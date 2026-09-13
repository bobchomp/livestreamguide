using System.Windows;

namespace SmithtonLivestreamGuide;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        VersionTextBlock.Text = $"Version {UpdateChecker.CurrentVersion ?? "development build"}";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
