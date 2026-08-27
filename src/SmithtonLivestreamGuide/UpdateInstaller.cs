using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SmithtonLivestreamGuide;

public static class UpdateInstaller
{
    private const string UserAgent = "SmithtonLivestreamGuide-Updater";

    /// <summary>
    /// Downloads the installer to a temp file (reporting 0-100 progress), then launches it in
    /// fully silent mode. The installer force-closes this app to release the file lock and,
    /// once installed, relaunches it automatically (see the Inno Setup script's [Run] section).
    /// </summary>
    public static async Task DownloadAndLaunchInstallerAsync(
        string downloadUrl,
        IProgress<double> progress,
        CancellationToken cancellationToken = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"SmithtonLivestreamGuide-Update-{Guid.NewGuid():N}.exe");

        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

        using (var response = await http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                   .ConfigureAwait(false))
        {
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);

            var buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                totalRead += bytesRead;

                if (totalBytes > 0)
                {
                    progress.Report(Math.Min(100.0, totalRead * 100.0 / totalBytes));
                }
            }
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = tempPath,
            Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART",
            UseShellExecute = true,
        });
    }
}
