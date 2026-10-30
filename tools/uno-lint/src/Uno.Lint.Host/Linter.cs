using Uno.Lint.Xaml;

namespace Uno.Lint;

/// <summary>How a lint run is configured. Immutable; a setting given here wins over the project's .editorconfig.</summary>
public sealed record LintOptions
{
    /// <summary>The rule set, when given explicitly (--profile). Otherwise uno_lint.profile from .editorconfig, else <see cref="DefaultProfile"/>.</summary>
    public Profile? Profile { get; init; }

    /// <summary>The profile when neither an explicit one nor .editorconfig sets it: Recommended for the CLI, Strict for the hook.</summary>
    public Profile DefaultProfile { get; init; } = Lint.Profile.Recommended;

    /// <summary>Also lint Dev/Harness/Probe/Tests paths.</summary>
    public bool IncludeDev { get; init; }

    /// <summary>Force the Toolkit gate open or closed instead of detecting it from the project.</summary>
    public bool? AssumeToolkit { get; init; }

    /// <summary>Cache the detected project context between single-file runs (see <see cref="ContextCache"/>).</summary>
    public bool UseContextCache { get; init; } = true;

    /// <summary>The context cache folder; null for <see cref="ContextCache.DefaultDirectory"/>.</summary>
    public string? ContextCacheDirectory { get; init; }
}

/// <summary>Non-gating counts reported alongside findings.</summary>
public sealed record InfoStats(int TokenReferences, int TokenKeys, int AdaptiveTriggers, int ResponsiveExtensions, int ControlTemplates, int WorkaroundMarkers);

/// <summary>The outcome of one lint run. Immutable.</summary>
public sealed class LintResult
{
    internal LintResult(
        string root,
        Profile profile,
        ProjectContext context,
        int fileCount,
        int skippedDevFiles,
        int unparsedXamlFiles,
        int repairedXamlFiles,
        int partialXamlFiles,
        IReadOnlyList<string> lineShiftedXamlFiles,
        IReadOnlyList<Finding> findings,
        IReadOnlyList<Finding> suppressed,
        InfoStats info,
        IReadOnlyList<string> ignoredPreambleKeys)
    {
        Root = root;
        Profile = profile;
        Context = context;
        FileCount = fileCount;
        SkippedDevFiles = skippedDevFiles;
        UnparsedXamlFiles = unparsedXamlFiles;
        RepairedXamlFiles = repairedXamlFiles;
        PartialXamlFiles = partialXamlFiles;
        LineShiftedXamlFiles = lineShiftedXamlFiles;
        Findings = findings;
        Suppressed = suppressed;
        Info = info;
        IgnoredPreambleKeys = ignoredPreambleKeys;
    }

    public string Root { get; }

    /// <summary>The profile the run used, after --profile, .editorconfig and the host's default.</summary>
    public Profile Profile { get; }

    public ProjectContext Context { get; }

    public int FileCount { get; }

    public int SkippedDevFiles { get; }

    public int UnparsedXamlFiles { get; }

    /// <summary>XAML files that were malformed and that the parser repaired before the rules ran.</summary>
    public int RepairedXamlFiles { get; }

    /// <summary>Repaired files the parser could only read up to a point; rules ran on that prefix.</summary>
    public int PartialXamlFiles { get; }

    /// <summary>Repaired files (relative paths) whose repair changed the line count, so their line numbers may be off.</summary>
    public IReadOnlyList<string> LineShiftedXamlFiles { get; }

    /// <summary>Active findings in file and line order, after profile, .editorconfig and project gating; never includes suppressed hits.</summary>
    public IReadOnlyList<Finding> Findings { get; }

    /// <summary>
    /// Hits an "allow" comment covers, for enabled rules. Reported as a count per rule so suppression is a metric
    /// rather than an invisible escape hatch: an agent that answers every hit with an allow comment shows up here.
    /// </summary>
    public IReadOnlyList<Finding> Suppressed { get; }

    public InfoStats Info { get; }

    /// <summary>Severity keys found above any [section] of an .editorconfig, which no analyzer honors.</summary>
    public IReadOnlyList<string> IgnoredPreambleKeys { get; }

    public bool HasGatingFindings => Findings.Any(f => f.Severity >= Severity.Warning);

    /// <summary>A rule's severity for the project as a whole: profile and project gates, before per-file .editorconfig sections.</summary>
    public Severity ProjectSeverity(RuleDescriptor rule) => Rules.Resolve(rule, Profile, Context);
}

/// <summary>
/// The file-system host for the rules: walks a folder (or takes one file), detects the project context, applies
/// .editorconfig per file and collects findings. A Roslyn analyzer host would replace this class, not the rule code
/// it calls.
/// </summary>
public sealed class Linter
{
    private readonly LintOptions _options;
    private readonly XamlLintEngine _xaml;

    /// <param name="xaml">Shared across runs: building the parser pipelines costs more than linting a file.</param>
    public Linter(LintOptions options, XamlLintEngine xaml)
    {
        _options = options;
        _xaml = xaml;
    }

    /// <summary>
    /// Lint every .xaml and .cs file under <paramref name="path"/>, or the single file it names. Every file is linted
    /// with its own project (the nearest folder at or above it holding a .csproj or solution), dev-path skip and
    /// .editorconfig chain, whether the run starts at the file or at a folder above it, so the hook and the CLI agree
    /// on every file. A file outside any project is linted with an empty project context and only the .editorconfig
    /// files above it, without walking its folder; in a folder run, such files share the folder's context.
    /// </summary>
    /// <exception cref="ArgumentException">A file that is not .xaml or .cs.</exception>
    public async Task<LintResult> RunAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullRoot = Path.GetFullPath(path);
        var isDirectory = Directory.Exists(fullRoot);
        if (!isDirectory && !FileKinds.IsSourceFile(fullRoot))
        {
            throw new ArgumentException($"Not a XAML or C# file: {fullRoot}", nameof(path));
        }

        var fileDirectory = Path.GetDirectoryName(fullRoot) ?? fullRoot;
        var startDirectory = isDirectory ? fullRoot : fileDirectory;
        var config = EditorConfigSet.Load(fullRoot);
        var runProfile = _options.Profile ?? config.Profile ?? _options.DefaultProfile;

        // Findings are shown relative to the folder linted, or to a single file's project.
        var baseDir = isDirectory ? fullRoot : ProjectLocator.FindProjectRoot(fileDirectory) ?? fileDirectory;

        // Project roots per folder and dev boundaries per project: a folder run asks once per folder, not per file.
        var projectRoots = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var devBoundaries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? ProjectRootOf(string file)
        {
            var directory = Path.GetDirectoryName(file) ?? file;
            if (!projectRoots.TryGetValue(directory, out var root))
            {
                root = ProjectLocator.FindProjectRoot(directory);
                projectRoots[directory] = root;
            }

            return root;
        }

        bool IsDev(string file)
        {
            var root = ProjectRootOf(file);
            string boundary;
            if (root == null)
            {
                boundary = startDirectory;
            }
            else if (!devBoundaries.TryGetValue(root, out boundary!))
            {
                boundary = ProjectLocator.DevBoundary(root);
                devBoundaries[root] = boundary;
            }

            return ProjectLocator.IsDevPath(Relative(boundary, file));
        }

        var candidates = isDirectory
            ? FileWalker.Enumerate(fullRoot, FileKinds.IsSourceFile).ToList()
            : [fullRoot];

        var files = new List<string>();
        var skippedDev = 0;
        foreach (var file in candidates.Where(f => !ProjectLocator.IsGeneratedCode(f)))
        {
            if (!_options.IncludeDev && IsDev(file))
            {
                skippedDev++;
            }
            else
            {
                files.Add(file);
            }
        }

        // In directory mode every source file is read once and reused for context detection.
        var texts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string ReadCached(string file)
        {
            if (!texts.TryGetValue(file, out var text))
            {
                text = ReadText(file);
                texts[file] = text;
            }

            return text;
        }

        // One context per project. Files outside any project share the folder's context in a folder run; a lone file
        // gets the empty context. The on-disk cache serves single-file runs, which the hook makes once per edit.
        var cacheDirectory = !isDirectory && _options.UseContextCache && !ContextCache.IsDisabledByEnvironment ? _options.ContextCacheDirectory ?? ContextCache.DefaultDirectory : null;
        var contexts = new Dictionary<string, ProjectContext>(StringComparer.OrdinalIgnoreCase);
        ProjectContext? runContext = null;
        ProjectContext ContextFor(string? contextRoot)
        {
            var key = contextRoot ?? string.Empty;
            if (!contexts.TryGetValue(key, out var context))
            {
                context = DetectContext(contextRoot, cacheDirectory, ReadCached);
                if (_options.AssumeToolkit is bool assumeToolkit)
                {
                    context = context.WithToolkit(assumeToolkit);
                }

                contexts[key] = context;
                runContext = runContext?.Union(context) ?? context;
            }

            return context;
        }

        ProjectContext ContextOf(string file) => ContextFor(ProjectRootOf(file) ?? (isDirectory ? fullRoot : null));

        Profile ProfileOf(string file) => _options.Profile ?? config.ProfileFor(file) ?? _options.DefaultProfile;

        Severity SeverityFor(RuleDescriptor rule, string file) =>
            Rules.Resolve(rule, ProfileOf(file), ContextOf(file), config.TryGetSeverity(rule.Id, file, out var configured) ? configured : null);

        var definedBrushKeys = new HashSet<string>(StringComparer.Ordinal);
        var referencedBrushKeys = new HashSet<string>(StringComparer.Ordinal);
        var findings = new List<Finding>();
        var suppressed = new List<Finding>();
        var lineShifted = new List<string>();
        int unparsed = 0, repaired = 0, partial = 0, tokenReferences = 0, adaptiveTriggers = 0, responsive = 0, templates = 0, workarounds = 0;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = ReadCached(file);
            if (text.Length == 0)
            {
                continue;
            }

            var relative = Relative(baseDir, file);
            bool IsEnabled(RuleDescriptor rule) => SeverityFor(rule, file) != Severity.None;
            IEnumerable<Finding> raw;

            if (FileKinds.IsXaml(file))
            {
                // XAML rules run inside Uno.XamlParser, which repairs the broken markup agents write instead of
                // rejecting the file.
                var xaml = await _xaml.AnalyzeAsync(relative, text, IsEnabled, cancellationToken).ConfigureAwait(false);
                if (!xaml.Parsed)
                {
                    unparsed++;
                    continue;
                }

                if (xaml.Repaired)
                {
                    repaired++;
                    partial += xaml.Partial ? 1 : 0;
                    if (xaml.LinesShifted)
                    {
                        lineShifted.Add(relative);
                    }
                }

                raw = xaml.Findings;
                adaptiveTriggers += xaml.Info.AdaptiveTriggers;
                templates += xaml.Info.ControlTemplates;
                responsive += xaml.Info.ResponsiveExtensions;
                tokenReferences += xaml.Info.TokenReferences;
                definedBrushKeys.UnionWith(xaml.Info.DefinedBrushKeys);
                referencedBrushKeys.UnionWith(xaml.Info.ReferencedBrushKeys);
            }
            else
            {
                var companionPath = file.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase) ? file[..^3] : null;
                var companion = companionPath != null && File.Exists(companionPath) ? ReadCached(companionPath) : null;
                raw = CSharpAnalyzer.Analyze(new CSharpFile(relative, text, companion), ContextOf(file), IsEnabled);
            }

            foreach (var finding in raw)
            {
                var severity = SeverityFor(finding.Rule, file);
                if (severity == Severity.None)
                {
                    continue;
                }

                var resolved = finding.WithResolution(severity, Rules.FixFor(finding.Rule, ContextOf(file)));
                (resolved.Suppressed ? suppressed : findings).Add(resolved);
            }

            workarounds += CountOccurrences(text, "WORKAROUND(");
        }

        // TOKEN info only counts references to brushes the app itself defines.
        referencedBrushKeys.IntersectWith(definedBrushKeys);
        var info = new InfoStats(tokenReferences, referencedBrushKeys.Count, adaptiveTriggers, responsive, templates, workarounds);

        return new LintResult(
            fullRoot, runProfile, runContext ?? ContextFor(ProjectLocator.FindProjectRoot(startDirectory) ?? (isDirectory ? fullRoot : null)), files.Count, skippedDev, unparsed, repaired, partial, lineShifted,
            InFileOrder(findings), InFileOrder(suppressed), info, config.IgnoredPreambleKeys.Distinct().ToList());
    }

    /// <summary>
    /// Project context from the project files first (the csproj files below the root plus every
    /// Directory.Build.props / Directory.Packages.props above it, where the Uno template puts UnoFeatures), then from
    /// XAML namespaces only for flags the project files left unset.
    /// </summary>
    private static ProjectContext DetectContext(string? projectRoot, string? cacheDirectory, Func<string, string> read)
    {
        if (projectRoot == null || !Directory.Exists(projectRoot))
        {
            return ProjectContext.Empty;
        }

        var projectFiles = ProjectLocator.FindProjectFiles(projectRoot);
        var key = cacheDirectory != null ? ContextCache.Key(projectRoot, projectFiles) : null;
        if (cacheDirectory != null && key != null && ContextCache.TryRead(cacheDirectory, key, out var cached))
        {
            return cached;
        }

        var context = ProjectContext.FromProjectFiles(projectFiles.Select(read));
        if (!context.IsComplete)
        {
            context = context.AddXaml(FileWalker.Enumerate(projectRoot, FileKinds.IsXaml).Select(read));
        }

        if (cacheDirectory != null && key != null)
        {
            ContextCache.Write(cacheDirectory, key, context);
        }

        return context;
    }

    private static List<Finding> InFileOrder(List<Finding> findings) =>
        findings.OrderBy(f => f.File, StringComparer.OrdinalIgnoreCase).ThenBy(f => f.Line).ThenBy(f => f.Rule.Id, StringComparer.Ordinal).ToList();

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        for (var at = text.IndexOf(value, StringComparison.Ordinal); at >= 0; at = text.IndexOf(value, at + value.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    private static string Relative(string baseDir, string path) =>
        path.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase) ? path[baseDir.Length..].TrimStart('\\', '/') : Path.GetFileName(path);

    private static string ReadText(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return string.Empty;
        }
    }
}
