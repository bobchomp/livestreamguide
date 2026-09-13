using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace SmithtonLivestreamGuide;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        VersionTextBlock.Text = $"Version {UpdateChecker.CurrentVersion ?? "development build"}";
        LastUpdatedTextBlock.Text = $"Last updated: {GetBuildTimestampDisplay()}";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // BuildTimestamp is stamped in by the release workflow (see the csproj) as a UTC
    // "yyyy-MM-ddTHH:mm:ssZ" string; shown here converted to the viewer's local time.
    private static string GetBuildTimestampDisplay()
    {
        var raw = Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "BuildTimestamp")?.Value;

        if (string.IsNullOrEmpty(raw) ||
            !DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var utc))
        {
            return "unknown (development build)";
        }

        return utc.ToLocalTime().ToString("d MMMM yyyy, h:mm tt", CultureInfo.InvariantCulture);
    }
}
