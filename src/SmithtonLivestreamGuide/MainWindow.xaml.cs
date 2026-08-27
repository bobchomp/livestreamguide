using System.Windows;

namespace SmithtonLivestreamGuide;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void AlwaysOnTopCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        Topmost = AlwaysOnTopCheckBox.IsChecked == true;
    }
}
