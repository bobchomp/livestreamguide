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

/// <summary>Result of an interactive (user-initiated) update check.</summary>
public sealed record UpdateCheckResult(bool Succeeded, UpdateInfo? UpdateInfo);

public static class UpdateChecker
{
    private const string ReleasesApiUrl = "https://api.github.com/repos/bobchomp/livestreamguide/releases/latest";
    private const string UserAgent = "SmithtonLivestreamGuide-UpdateChecker";

    public static string? CurrentVersion =>
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

    /// <summary>Silent check used on startup. Returns null on "up to date" and on any failure
    /// alike (offline, GitHub down, etc.) - it never needs to tell those apart, since either
    /// way nothing should interrupt the guide.</summary>
    public static async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        var result = await CheckCoreAsync(cancellationToken).ConfigureAwait(false);
        return result.Succeeded ? result.UpdateInfo : null;
    }

    /// <summary>Check used from Help > Check for Updates, where the user is waiting on a
    /// result and "couldn't check" needs to read differently from "you're up to date".</summary>
    public static Task<UpdateCheckResult> CheckForUpdateInteractiveAsync(CancellationToken cancellationToken = default) =>
        CheckCoreAsync(cancellationToken);

    private static async Task<UpdateCheckResult> CheckCoreAsync(CancellationToken cancellationToken)
    {
        var currentVersion = CurrentVersion;
        if (currentVersion is null || !TryParseVersion(currentVersion, out var currentParsed))
        {
            // Not a release build (e.g. a local dev build) - nothing to compare against.
            return new UpdateCheckResult(Succeeded: false, UpdateInfo: null);
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
            // Offline, GitHub unreachable, rate-limited, etc.
            return new UpdateCheckResult(Succeeded: false, UpdateInfo: null);
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.GetProperty("tag_name").GetString();
            if (string.IsNullOrEmpty(tagName))
            {
                return new UpdateCheckResult(Succeeded: false, UpdateInfo: null);
            }

            var latestVersionText = tagName.StartsWith('v') ? tagName[1..] : tagName;
            if (!TryParseVersion(latestVersionText, out var latestParsed) || latestParsed.CompareTo(currentParsed) <= 0)
            {
                return new UpdateCheckResult(Succeeded: true, UpdateInfo: null);
            }

            foreach (var asset in root.GetProperty("assets").EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString();
                if (name != null && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    var downloadUrl = asset.GetProperty("browser_download_url").GetString();
                    if (!string.IsNullOrEmpty(downloadUrl))
                    {
                        return new UpdateCheckResult(Succeeded: true, UpdateInfo: new UpdateInfo(latestVersionText, downloadUrl));
                    }
                }
            }

            // Newer tag exists but no installer asset was found on it - nothing installable yet.
            return new UpdateCheckResult(Succeeded: true, UpdateInfo: null);
        }
        catch
        {
            return new UpdateCheckResult(Succeeded: false, UpdateInfo: null);
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
