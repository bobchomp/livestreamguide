using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
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

    private async void CheckForUpdatesMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var result = await UpdateChecker.CheckForUpdateInteractiveAsync();

        if (!result.Succeeded)
        {
            MessageBox.Show(
                this,
                "Couldn't check for updates. Check the internet connection and try again.",
                "Smithton Livestream Guide",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (result.UpdateInfo is null)
        {
            MessageBox.Show(
                this,
                "You're already on the latest version.",
                "Smithton Livestream Guide",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var updateWindow = new UpdateWindow(result.UpdateInfo) { Owner = this };
        updateWindow.ShowDialog();
    }

    private void PrintButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var document = BuildPrintDocument(dialog.PrintableAreaWidth, dialog.PrintableAreaHeight);
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Smithton Livestream Guide");
    }

    // Printing the on-screen visual would only capture one page, so the guide's TextBlocks are
    // copied into a paginated FlowDocument instead, skipping the Open/Copy buttons.
    private FlowDocument BuildPrintDocument(double pageWidth, double pageHeight)
    {
        var document = new FlowDocument
        {
            PageWidth = pageWidth,
            PageHeight = pageHeight,
            PagePadding = new Thickness(56),
            ColumnWidth = double.PositiveInfinity,
            FontFamily = FontFamily,
            Background = Brushes.White,
        };

        document.Blocks.Add(new Paragraph(new Run("Smithton Livestream Guide"))
        {
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x16, 0x32, 0x4F)),
            Margin = new Thickness(0),
        });

        foreach (var child in GuideContentPanel.Children)
        {
            switch (child)
            {
                case TextBlock textBlock:
                    document.Blocks.Add(ToParagraph(textBlock, new Thickness(0)));
                    break;

                case StackPanel row when row.Children.OfType<TextBlock>().FirstOrDefault() is TextBlock rowText:
                    document.Blocks.Add(ToParagraph(rowText, row.Margin));
                    break;

                case Border box when box.Child is Grid grid && grid.Children.OfType<TextBlock>().FirstOrDefault() is TextBlock boxText:
                    var note = ToParagraph(boxText, box.Margin);
                    note.Background = box.Background;
                    note.BorderBrush = box.BorderBrush;
                    note.BorderThickness = box.BorderThickness;
                    note.Padding = box.Padding;
                    document.Blocks.Add(note);
                    break;
            }
        }

        return document;
    }

    private static Paragraph ToParagraph(TextBlock source, Thickness outerMargin)
    {
        var paragraph = new Paragraph
        {
            FontFamily = source.FontFamily,
            FontSize = source.FontSize,
            FontWeight = source.FontWeight,
            Foreground = source.Foreground,
            Margin = new Thickness(
                source.Margin.Left + outerMargin.Left,
                source.Margin.Top + outerMargin.Top,
                0,
                source.Margin.Bottom + outerMargin.Bottom),
        };

        foreach (var inline in source.Inlines)
        {
            if (inline is Run run)
            {
                paragraph.Inlines.Add(new Run(run.Text) { FontWeight = run.FontWeight });
            }
        }

        return paragraph;
    }

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow { Owner = this };
        aboutWindow.ShowDialog();
    }
}
