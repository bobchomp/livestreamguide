using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SmithtonLivestreamGuide;

public sealed record UpdateInfo(string Version, string DownloadUrl);

public static class UpdateChecker
{
    private const string ReleasesApiUrl = "https://api.github.com/repos/bobchomp/livestreamguide/releases/latest";
    private const string UserAgent = "SmithtonLivestreamGuide-UpdateChecker";

    public static string? CurrentVersion =>
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

    public static async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        var currentVersion = CurrentVersion;
        if (currentVersion is null || !TryParseVersion(currentVersion, out var currentParsed))
        {
            // Not a release build (e.g. a local dev build) - nothing to compare against.
            return null;
        }

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        string json;
        try
        {
            json = await http.GetStringAsync(ReleasesApiUrl, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Offline, GitHub unreachable, rate-limited, etc. Fail open - don't block the guide.
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.GetProperty("tag_name").GetString();
            if (string.IsNullOrEmpty(tagName))
            {
                return null;
            }

            var latestVersionText = tagName.StartsWith('v') ? tagName[1..] : tagName;
            if (!TryParseVersion(latestVersionText, out var latestParsed) || latestParsed.CompareTo(currentParsed) <= 0)
            {
                return null;
            }

            foreach (var asset in root.GetProperty("assets").EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString();
                if (name != null && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    var downloadUrl = asset.GetProperty("browser_download_url").GetString();
                    if (!string.IsNullOrEmpty(downloadUrl))
                    {
                        return new UpdateInfo(latestVersionText, downloadUrl);
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    // Versions are produced by the release workflow as "yyyy.MM.dd-N" (e.g. "2026.08.27-42").
    // Parsed rather than string-compared so build numbers of differing digit-width still sort correctly.
    private static bool TryParseVersion(string version, out ParsedVersion parsed)
    {
        parsed = default;

        var dashIndex = version.IndexOf('-');
        if (dashIndex < 0)
        {
            return false;
        }

        var datePart = version[..dashIndex];
        var buildPart = version[(dashIndex + 1)..];

        if (!DateOnly.TryParseExact(datePart, "yyyy.MM.dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            || !int.TryParse(buildPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var build))
        {
            return false;
        }

        parsed = new ParsedVersion(date, build);
        return true;
    }

    private readonly record struct ParsedVersion(DateOnly Date, int Build) : IComparable<ParsedVersion>
    {
        public int CompareTo(ParsedVersion other)
        {
            var dateComparison = Date.CompareTo(other.Date);
            return dateComparison != 0 ? dateComparison : Build.CompareTo(other.Build);
        }
    }
}
