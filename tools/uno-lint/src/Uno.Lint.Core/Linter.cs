using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Uno.Lint;

/// <summary>
/// The file-system host for the rules: walks a folder, detects project context, applies .editorconfig per file and
/// collects findings. A Roslyn analyzer host would replace this class, not the rule code it calls.
/// </summary>
public sealed class Linter
{
    // A folder named Dev, Harness, Probe(s) or Test(s), or a project folder ending in one (MyApp.Tests). Whole segments
    // only: TestApp/, ProbeResultsPage.xaml and Content/TestPages/ are app code.
    private static readonly Regex DevSegment = new Regex(@"(?i)^(?:.+\.)?(?:Dev|Harness|Probes?|Tests?)$", RegexOptions.Compiled);
    private static readonly Regex GeneratedCs = new Regex(@"(?i)\.g(\.i)?\.cs$|\.designer\.cs$|GlobalUsings\.g\.cs$", RegexOptions.Compiled);

    private readonly LintOptions _options;

    public Linter(LintOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Lint every .xaml and .cs file under <paramref name="root"/>, or the single file it names. A single file is
    /// linted with the same project root, dev-path skip and configuration a directory run would give it, so the hook
    /// and the CLI agree on every file.
    /// </summary>
    public LintResult Run(string root)
    {
        var fullRoot = Path.GetFullPath(root);
        var isDirectory = Directory.Exists(fullRoot);
        var result = new LintResult { Root = fullRoot };

        if (!isDirectory && !FileKinds.IsSourceFile(fullRoot))
        {
            throw new ArgumentException($"Not a XAML or C# file: {fullRoot}", nameof(root));
        }

        var projectRoot = isDirectory ? fullRoot : FindProjectRoot(Path.GetDirectoryName(fullRoot) ?? fullRoot);
        var baseDir = projectRoot;

        var candidates = isDirectory
            ? FileWalker.Enumerate(fullRoot, FileKinds.IsSourceFile).Where(f => !GeneratedCs.IsMatch(f)).ToList()
            : new List<string> { fullRoot };

        var files = new List<string>();
        foreach (var f in candidates)
        {
            var relative = Relative(baseDir, f);
            if (!_options.IncludeDev && IsDevPath(relative))
            {
                result.SkippedDevFiles++;
                continue;
            }

            files.Add(f);
        }

        result.FileCount = files.Count;

        // In directory mode every source file is read once and reused for context detection.
        var texts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string ReadCached(string path)
        {
            if (!texts.TryGetValue(path, out var t))
            {
                t = ReadText(path);
                texts[path] = t;
            }

            return t;
        }

        result.Context = DetectContext(projectRoot, useCache: !isDirectory, ReadCached);

        var definedBrushKeys = new HashSet<string>(StringComparer.Ordinal);
        var referencedBrushKeys = new HashSet<string>(StringComparer.Ordinal);
        var raw = new List<Finding>();

        foreach (var path in files)
        {
            var text = ReadCached(path);
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            var relative = Relative(baseDir, path);
            var enabled = BuildEnabledPredicate(result.Context, path);

            if (path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                if (!XamlFile.TryParse(relative, text, out var xaml))
                {
                    result.UnparsedXamlFiles++;
                    continue;
                }

                raw.AddRange(XamlAnalyzer.Analyze(xaml!, result.Context, enabled).Select(f => WithSeverity(f, path, result.Context)));
                XamlAnalyzer.CollectInfo(xaml!, result.Info, definedBrushKeys, referencedBrushKeys);
            }
            else
            {
                var companionPath = path.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase)
                    ? path.Substring(0, path.Length - 3)
                    : null;
                var companion = companionPath != null && File.Exists(companionPath) ? ReadCached(companionPath) : null;
                var cs = new CSharpFile(relative, text, companion);
                raw.AddRange(CSharpAnalyzer.Analyze(cs, result.Context, enabled).Select(f => WithSeverity(f, path, result.Context)));
            }

            result.Info.WorkaroundMarkers += Regex.Matches(text, @"WORKAROUND\(").Count;
        }

        // TOKEN info only counts references to brushes the app itself defines.
        referencedBrushKeys.IntersectWith(definedBrushKeys);
        result.Info.TokenKeys = referencedBrushKeys.Count;

        foreach (var finding in raw)
        {
            if (finding.Severity == Severity.None)
            {
                continue;
            }

            if (finding.Suppressed)
            {
                result.Suppressed.Add(finding);
            }
            else
            {
                result.Findings.Add(finding);
            }
        }

        return result;
    }

    /// <summary>Lint one file with project context detected from the nearest .csproj; used by the agent hook.</summary>
    public LintResult RunSingleFile(string filePath) => Run(filePath);

    // ---------------------------------------------------------------- helpers

    private Finding WithSeverity(Finding finding, string absolutePath, ProjectContext context)
    {
        finding.Severity = Rules.Resolve(finding.Rule, _options, context, absolutePath);
        finding.Fix = Rules.FixFor(finding.Rule, context);
        return finding;
    }

    private Func<RuleDescriptor, bool> BuildEnabledPredicate(ProjectContext context, string absolutePath) =>
        rule => Rules.Resolve(rule, _options, context, absolutePath) != Severity.None;

    /// <summary>
    /// Project context from the project files first (the csproj files below the root plus every
    /// Directory.Build.props / Directory.Packages.props above it, where the Uno template puts UnoFeatures), then from
    /// XAML namespaces only for flags the project files left unset. In single-file (hook) mode the result is cached
    /// on disk keyed by the project files' timestamps, so a hook call on a large solution does not re-read every XAML file.
    /// </summary>
    private ProjectContext DetectContext(string projectRoot, bool useCache, Func<string, string> read)
    {
        ProjectContext ctx;
        if (!Directory.Exists(projectRoot))
        {
            ctx = new ProjectContext();
        }
        else
        {
            var projectFiles = FindProjectFiles(projectRoot);
            var cacheKey = useCache ? CacheKey(projectRoot, projectFiles) : null;
            if (cacheKey != null && TryReadCache(cacheKey, out var cached))
            {
                ctx = cached;
            }
            else
            {
                ctx = new ProjectContext();
                ctx.AddProjectFiles(projectFiles.Select(read));
                if (!ctx.IsComplete)
                {
                    ctx.AddXaml(FileWalker.Enumerate(projectRoot, f => f.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)).Select(read));
                }

                if (cacheKey != null)
                {
                    WriteCache(cacheKey, ctx);
                }
            }
        }

        if (_options.AssumeToolkit.HasValue)
        {
            ctx.HasToolkit = _options.AssumeToolkit.Value;
        }

        return ctx;
    }

    /// <summary>Directory.Build.props and Directory.Packages.props from the drive root down to the project root, then the project files below it.</summary>
    public static List<string> FindProjectFiles(string projectRoot)
    {
        var above = new List<string>();
        for (var dir = Path.GetDirectoryName(projectRoot); !string.IsNullOrEmpty(dir); dir = Path.GetDirectoryName(dir))
        {
            foreach (var name in new[] { "Directory.Build.props", "Directory.Packages.props" })
            {
                var candidate = Path.Combine(dir!, name);
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

    private static string? CacheKey(string projectRoot, List<string> projectFiles)
    {
        try
        {
            var sb = new StringBuilder(projectRoot.ToLowerInvariant());
            foreach (var f in projectFiles.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                sb.Append('|').Append(f.ToLowerInvariant()).Append('@').Append(File.GetLastWriteTimeUtc(f).Ticks);
            }

            // XAML namespaces also feed detection, but hashing every XAML timestamp would cost what the cache saves.
            // Adding a reference changes a project file, which is in the key; a reference first used only in XAML
            // shows up after the next project-file change or a cache clear. Toolkit/MVUX adoption is coarse enough for that.
            sb.Append('|').Append(Directory.EnumerateFileSystemEntries(projectRoot).Count());

            using (var sha = SHA1.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string CacheDir => Path.Combine(Path.GetTempPath(), "uno-lint", "context");

    private static bool TryReadCache(string key, out ProjectContext context)
    {
        context = new ProjectContext();
        try
        {
            var path = Path.Combine(CacheDir, key + ".txt");
            return File.Exists(path) && ProjectContext.TryDeserialize(File.ReadAllText(path).Trim(), out context);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void WriteCache(string key, ProjectContext context)
    {
        try
        {
            Directory.CreateDirectory(CacheDir);
            File.WriteAllText(Path.Combine(CacheDir, key + ".txt"), context.Serialize());
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    /// <summary>Walk up from a file's directory to the nearest folder that holds a .csproj (or .sln), else the directory itself.</summary>
    public static string FindProjectRoot(string directory)
    {
        var dir = directory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (Directory.EnumerateFiles(dir, "*.csproj").Any() || Directory.EnumerateFiles(dir, "*.sln").Any() || Directory.EnumerateFiles(dir, "*.slnx").Any())
            {
                return dir;
            }

            dir = Path.GetDirectoryName(dir);
        }

        return directory;
    }

    /// <summary>True when a directory segment of <paramref name="relativePath"/> (not the file name) marks dev or test code.</summary>
    public static bool IsDevPath(string relativePath)
    {
        var segments = relativePath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
        return segments.Take(segments.Length - 1).Any(DevSegment.IsMatch);
    }

    private static bool IsProjectFile(string path)
    {
        var name = Path.GetFileName(path);
        return path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Directory.Packages.props", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Directory.Build.props", StringComparison.OrdinalIgnoreCase);
    }

    private static string Relative(string baseDir, string path)
    {
        if (path.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
        {
            return path.Substring(baseDir.Length).TrimStart('\\', '/');
        }

        return Path.GetFileName(path);
    }

    private static string ReadText(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (IOException)
        {
            return string.Empty;
        }
        catch (UnauthorizedAccessException)
        {
            return string.Empty;
        }
    }
}
