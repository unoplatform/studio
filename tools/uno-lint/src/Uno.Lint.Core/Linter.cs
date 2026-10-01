using System.Text.RegularExpressions;

namespace Uno.Lint;

public sealed class Linter
{
    // A folder named Dev, Harness, Probe(s) or Test(s), or a project folder ending in one (MyApp.Tests). Whole segments
    // only: TestApp/, ProbeResultsPage.xaml and Content/TestPages/ are app code.
    private static readonly Regex DevSegment = new Regex(@"(?i)^(?:.+\.)?(?:Dev|Harness|Probes?|Tests?)$", RegexOptions.Compiled);
    private static readonly Regex ExcludedDir = new Regex(@"[\\/](bin|obj|node_modules|\.git|\.vs)[\\/]", RegexOptions.Compiled);
    private static readonly Regex GeneratedCs = new Regex(@"(?i)\.g(\.i)?\.cs$|\.designer\.cs$|GlobalUsings\.g\.cs$", RegexOptions.Compiled);

    private readonly LintOptions _options;

    public Linter(LintOptions options)
    {
        _options = options;
    }

    /// <summary>Lint every .xaml and .cs file under <paramref name="root"/> (or the single file it names).</summary>
    public LintResult Run(string root)
    {
        var fullRoot = Path.GetFullPath(root);
        var isDirectory = Directory.Exists(fullRoot);
        var baseDir = isDirectory ? fullRoot : (Path.GetDirectoryName(fullRoot) ?? fullRoot);

        var result = new LintResult { Root = fullRoot };

        var candidates = isDirectory
            ? Directory.EnumerateFiles(fullRoot, "*.*", SearchOption.AllDirectories)
                .Where(f => IsSourceFile(f) && !ExcludedDir.IsMatch(f) && !GeneratedCs.IsMatch(f))
                .ToList()
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

        var projectRoot = isDirectory ? fullRoot : FindProjectRoot(baseDir);
        result.Context = DetectContext(projectRoot, files);

        var enabled = BuildEnabledPredicate(result.Context);
        var definedBrushKeys = new HashSet<string>(StringComparer.Ordinal);
        var referencedBrushKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var path in files)
        {
            var text = ReadText(path);
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            var relative = Relative(baseDir, path);
            if (path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                if (!XamlFile.TryParse(relative, text, out var xaml))
                {
                    result.UnparsedXamlFiles++;
                    continue;
                }

                result.Findings.AddRange(XamlAnalyzer.Analyze(xaml!, result.Context, enabled));
                XamlAnalyzer.CollectInfo(xaml!, result.Info, definedBrushKeys, referencedBrushKeys);
            }
            else
            {
                var companionPath = path.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase)
                    ? path.Substring(0, path.Length - 3)
                    : null;
                var companion = companionPath != null && File.Exists(companionPath) ? ReadText(companionPath) : null;
                var cs = new CSharpFile(relative, text, companion);
                result.Findings.AddRange(CSharpAnalyzer.Analyze(cs, result.Context, enabled));
            }

            result.Info.WorkaroundMarkers += Regex.Matches(text, @"WORKAROUND\(").Count;
        }

        // TOKEN info only counts references to brushes the app itself defines.
        referencedBrushKeys.IntersectWith(definedBrushKeys);
        result.Info.TokenKeys = referencedBrushKeys.Count;

        foreach (var finding in result.Findings)
        {
            finding.Severity = Rules.Resolve(finding.Rule, _options, result.Context);
        }

        result.Findings.RemoveAll(f => f.Severity == Severity.None);
        return result;
    }

    /// <summary>Lint one file with project context detected from the nearest .csproj; used by the agent hook.</summary>
    public LintResult RunSingleFile(string filePath) => Run(filePath);

    // ---------------------------------------------------------------- helpers

    private Func<RuleDescriptor, bool> BuildEnabledPredicate(ProjectContext context) =>
        rule => Rules.Resolve(rule, _options, context) != Severity.None;

    private ProjectContext DetectContext(string projectRoot, List<string> files)
    {
        IEnumerable<string> projectTexts = Directory.Exists(projectRoot)
            ? Directory.EnumerateFiles(projectRoot, "*.csproj", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(projectRoot, "Directory.Packages.props", SearchOption.AllDirectories))
                .Concat(Directory.EnumerateFiles(projectRoot, "Directory.Build.props", SearchOption.AllDirectories))
                .Where(f => !ExcludedDir.IsMatch(f))
                .Select(ReadText)
            : Enumerable.Empty<string>();

        IEnumerable<string> xamlTexts = Directory.Exists(projectRoot)
            ? Directory.EnumerateFiles(projectRoot, "*.xaml", SearchOption.AllDirectories).Where(f => !ExcludedDir.IsMatch(f)).Select(ReadText)
            : files.Where(f => f.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)).Select(ReadText);

        var ctx = ProjectContext.Detect(projectTexts, xamlTexts);
        if (_options.AssumeToolkit.HasValue)
        {
            ctx.HasToolkit = _options.AssumeToolkit.Value;
        }

        return ctx;
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

    private static bool IsSourceFile(string path) =>
        path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);

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
    }
}
