using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Uno.Lint;

/// <summary>Where a file's project is, which files describe it, and which paths are dev or test code.</summary>
public static partial class ProjectLocator
{
    /// <summary>The nearest folder at or above <paramref name="directory"/> that holds a .csproj, .sln or .slnx; null when there is none.</summary>
    public static string? FindProjectRoot(string directory)
    {
        for (var dir = directory; dir is { Length: > 0 }; dir = Path.GetDirectoryName(dir))
        {
            try
            {
                if (Directory.EnumerateFiles(dir, "*.csproj").Any() || Directory.EnumerateFiles(dir, "*.sln").Any() || Directory.EnumerateFiles(dir, "*.slnx").Any())
                {
                    return dir;
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // An unreadable folder is not a project root; keep walking up.
            }
        }

        return null;
    }

    /// <summary>Directory.Build.props and Directory.Packages.props from the drive root down to the project root, then the project files below it.</summary>
    public static List<string> FindProjectFiles(string projectRoot)
    {
        var above = new List<string>();
        for (var dir = Path.GetDirectoryName(projectRoot); dir is { Length: > 0 }; dir = Path.GetDirectoryName(dir))
        {
            foreach (var name in new[] { "Directory.Build.props", "Directory.Packages.props" })
            {
                var candidate = Path.Combine(dir, name);
                if (File.Exists(candidate))
                {
                    above.Add(candidate);
                }
            }
        }

        above.Reverse();
        above.AddRange(FileWalker.Enumerate(projectRoot, IsProjectFile));
        return above;
    }

    /// <summary>True when a directory segment of <paramref name="relativePath"/> (not the file name) marks dev or test code.</summary>
    public static bool IsDevPath(string relativePath)
    {
        var segments = relativePath.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
        return segments.Take(segments.Length - 1).Any(DevSegment().IsMatch);
    }

    public static bool IsGeneratedCode(string path) => GeneratedCs().IsMatch(path);

    private static bool IsProjectFile(string path)
    {
        var name = Path.GetFileName(path);
        return path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Directory.Packages.props", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Directory.Build.props", StringComparison.OrdinalIgnoreCase);
    }

    // A folder named Dev, Harness, Probe(s) or Test(s), or a project folder ending in one (MyApp.Tests). Whole segments
    // only: TestApp/, ProbeResultsPage.xaml and Content/TestPages/ are app code.
    [GeneratedRegex(@"(?i)^(?:.+\.)?(?:Dev|Harness|Probes?|Tests?)$")]
    private static partial Regex DevSegment();

    [GeneratedRegex(@"(?i)\.g(\.i)?\.cs$|\.designer\.cs$|GlobalUsings\.g\.cs$")]
    private static partial Regex GeneratedCs();
}

/// <summary>
/// Hook calls lint one file at a time; re-reading every project file and XAML file of a large solution on each call
/// would cost more than the lint. The detected <see cref="ProjectContext"/> is cached per project, keyed by the project
/// files' paths and timestamps. The cache is a folder of 4-byte files under the user's local application data
/// (<c>uno-lint/context</c>), or under <c>UNO_LINT_CACHE_DIR</c>; <c>UNO_LINT_NO_CACHE=1</c> or <c>--no-cache</c>
/// bypasses it. A reference first used only in XAML shows up after the next project-file change.
/// </summary>
public static class ContextCache
{
    public static bool IsDisabledByEnvironment => Environment.GetEnvironmentVariable("UNO_LINT_NO_CACHE") is "1" or "true";

    /// <summary>Where entries go when the options name no folder: UNO_LINT_CACHE_DIR, else the user's local application data.</summary>
    public static string DefaultDirectory
    {
        get
        {
            if (Environment.GetEnvironmentVariable("UNO_LINT_CACHE_DIR") is { Length: > 0 } configured)
            {
                return configured;
            }

            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);
            return Path.Combine(local is { Length: > 0 } ? local : Path.GetTempPath(), "uno-lint", "context");
        }
    }

    public static string? Key(string projectRoot, IEnumerable<string> projectFiles)
    {
        try
        {
            var sb = new StringBuilder(projectRoot.ToLowerInvariant());
            foreach (var file in projectFiles.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                sb.Append('|').Append(file.ToLowerInvariant()).Append('@').Append(File.GetLastWriteTimeUtc(file).Ticks);
            }

            // XAML namespaces also feed detection, but hashing every XAML timestamp would cost what the cache saves.
            sb.Append('|').Append(System.IO.Directory.EnumerateFileSystemEntries(projectRoot).Count());
            return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string PathFor(string directory, string key) => Path.Combine(directory, key + ".txt");

    public static bool TryRead(string directory, string key, out ProjectContext context)
    {
        context = ProjectContext.Empty;
        try
        {
            var path = PathFor(directory, key);
            return File.Exists(path) && ProjectContext.TryDeserialize(File.ReadAllText(path).Trim(), out context);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static void Write(string directory, string key, ProjectContext context)
    {
        try
        {
            System.IO.Directory.CreateDirectory(directory);
            File.WriteAllText(PathFor(directory, key), context.Serialize());
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // The cache is an optimization: a read-only or full disk means the next call detects the context again.
        }
    }
}
