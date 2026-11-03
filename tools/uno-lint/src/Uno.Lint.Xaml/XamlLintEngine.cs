using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Uno.XamlParser;
using Uno.XamlParser.Configuration;
using Uno.XamlParser.Stage0;
using Uno.XamlParser.Stage1;

namespace Uno.Lint.Xaml;

/// <summary>
/// Runs the uno-lint XAML rules on Uno.XamlParser: the three steps of <see cref="UnoLintXaml"/> in one call.
/// Stage-0 repairs are on for malformed text, so broken markup still yields a model; Stage-1 edits are off, so values
/// are read as written. The parser's own fixers are not registered, and Stage 2/3 need a type resolver, which a CLI or
/// hook does not have. Each pipeline processes one document at a time, so calls are serialized; they can be cancelled
/// while they wait.
/// </summary>
public sealed class XamlLintEngine : IDisposable
{
    // The repaired text is only reachable through a non-public property of the string source; it tells whether the
    // parser's line numbers still point into the saved file. Looked up once; a test pins that it exists.
    internal static readonly PropertyInfo? RepairedTextProperty =
        XamlStreamSources.FromString(string.Empty, null).GetType().GetProperty("Text", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Lazy<Host> _strict = new(() => new Host(useSanitizers: false));
    private readonly Lazy<Host> _repairing = new(() => new Host(useSanitizers: true));

    /// <param name="relativePath">Path used in findings and for file-name roles (palette, icons).</param>
    /// <param name="text">The file as saved; findings carry line numbers into this text.</param>
    /// <param name="isEnabled">Whether a rule is enabled for this file after profile, configuration and project gates.</param>
    public async ValueTask<XamlFileFindings> AnalyzeAsync(string relativePath, string text, Func<RuleDescriptor, bool> isEnabled, CancellationToken cancellationToken = default)
    {
        var wellFormed = UnoLintXaml.IsWellFormed(text);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var host = wellFormed ? _strict.Value : _repairing.Value;
            var source = XamlStreamSources.FromString(text, relativePath);
            var result = await host.Pipeline.ProcessAsync(source, policyOverride: null, cancellationToken).ConfigureAwait(false);
            var repairedText = wellFormed ? null : RepairedTextProperty?.GetValue(source) as string;
            return UnoLintXaml.Collect(result, relativePath, text, isEnabled, repairedText);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();

    private sealed class Host
    {
        public Host(bool useSanitizers)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IXamlProcessingPolicy>(RepairMarkupOnly.Instance);
            services.AddXamlStreamPipeline(options =>
            {
                options.UseDefaultSanitizers = useSanitizers;
                options.UseDefaultSemanticFixers = false;
                UnoLintXaml.AddTo(options);
            }).AddXamlProcessingPipeline();
            Pipeline = services.BuildServiceProvider().GetRequiredService<XamlProcessingPipeline>();
        }

        public XamlProcessingPipeline Pipeline { get; }
    }

    /// <summary>Repair markup so it parses (Stage 0); never rewrite values (Stage 1-3). Diagnostics are collected either way.</summary>
    private sealed class RepairMarkupOnly : IXamlProcessingPolicy
    {
        public static readonly RepairMarkupOnly Instance = new();

        public bool ShouldApplyStage0Fix(XamlDiagnostic diagnostic) => true;

        public bool ShouldApplyStage1Fix(XamlDiagnostic diagnostic) => false;

        public bool ShouldApplyStage2Edit(XamlPropertyEdit edit) => false;

        public bool ShouldApplyStage3Edit(XamlPropertyEdit edit) => false;
    }
}
