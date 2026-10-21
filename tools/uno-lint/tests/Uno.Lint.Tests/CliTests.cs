using System.Text.Json.Nodes;
using Repl;
using Repl.Testing;
using Uno.Lint.Cli;
using Xunit;
using static Uno.Lint.Tests.TestHost;

namespace Uno.Lint.Tests;

/// <summary>The command surface through Repl's in-memory harness: exit codes, the JSON contract and the text report.</summary>
public class CliTests : IDisposable
{
    private readonly TempProject _project = new();

    public CliTests()
    {
        _project.Write("App.csproj", "<Project Sdk=\"Uno.Sdk\"><PropertyGroup><UnoFeatures>Material;Toolkit;Mvux</UnoFeatures></PropertyGroup></Project>");
    }

    public void Dispose() => _project.Dispose();

    /// <summary>Runs one command line the way Program.Main wires the app.</summary>
    private static async Task<CommandExecution> RunAsync(string commandLine)
    {
        var args = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        await using var host = ReplTestHost.Create(() =>
        {
            var app = ReplApp.Create();
            app.Options(options => UnoLintCommands.Configure(options, hookMode: false));
            app.MapModule(new UnoLintCommands(OutputFormats.Of(args), Engine));
            return app;
        });
        await using var session = await host.OpenSessionAsync();
        return await session.RunCommandAsync(commandLine);
    }

    private string Quoted(string relative = "") => $"\"{_project.Path(relative).TrimEnd('\\', '/')}\"";

    // ------------------------------------------------------------ exit codes

    [Fact]
    public async Task Exit_code_is_0_without_gating_findings_and_1_with_them()
    {
        _project.Write("Clean.xaml", $"<Page {Ns}><Border Background=\"{{ThemeResource SurfaceBrush}}\"/></Page>");
        Assert.Equal(0, (await RunAsync($"lint {Quoted("Clean.xaml")} --no-cache")).ExitCode);

        _project.Write("Hex.xaml", $"<Page {Ns}><Border Background=\"#FF0000\"/></Page>");
        Assert.Equal(1, (await RunAsync($"lint {Quoted()} --no-cache")).ExitCode);
    }

    [Theory]
    [InlineData("lint does-not-exist")]
    [InlineData("lint --bogus")]
    [InlineData("lint --top 0")]
    [InlineData("lint --top many")]
    [InlineData("lint --profile strictest")]
    [InlineData("frobnicate")]
    public async Task Exit_code_is_2_when_the_run_cannot_complete(string commandLine)
    {
        Assert.Equal(2, (await RunAsync(commandLine)).ExitCode);
    }

    [Fact]
    public async Task Profile_and_toolkit_options_bind()
    {
        _project.Write("Card.xaml", $"<Page {Ns}><Border CornerRadius=\"8\" Background=\"{{ThemeResource SurfaceBrush}}\"><Grid/></Border></Page>");

        Assert.Equal(0, (await RunAsync($"lint {Quoted()} --no-cache")).ExitCode);
        Assert.Equal(1, (await RunAsync($"lint {Quoted()} --profile strict --no-cache")).ExitCode);
        Assert.Equal(0, (await RunAsync($"lint {Quoted()} --profile strict --no-toolkit --no-cache")).ExitCode);
    }

    // ------------------------------------------------------------ JSON contract

    [Fact]
    public async Task Json_report_has_a_stable_shape()
    {
        _project.Write("Views/MainPage.xaml", $"<Page {Ns}>\n<!-- uno-lint: allow hex - chart -->\n<Border Background=\"#FF0000\"/>\n<TextBox Text=\"{{Binding Name.Value}}\"/>\n</Page>");

        var execution = await RunAsync($"lint {Quoted()} --profile strict --no-cache --json");
        var report = JsonNode.Parse(execution.OutputText)!.AsObject();

        Assert.Equal(1, execution.ExitCode);
        Assert.Equal(["root", "profile", "project", "files", "gating", "rules", "info", "findings", "suppressedFindings", "warnings"], report.Select(p => p.Key));
        Assert.Equal(["toolkit", "extensionsNavigation", "mvux", "unoThemes"], report["project"]!.AsObject().Select(p => p.Key));
        Assert.Equal(["linted", "skippedDev", "unparsedXaml", "repairedXaml", "partialXaml", "lineShiftedXaml"], report["files"]!.AsObject().Select(p => p.Key));
        Assert.Equal(["tokenRefs", "tokenKeys", "adaptiveTriggers", "utuResponsive", "controlTemplates", "workarounds"], report["info"]!.AsObject().Select(p => p.Key));
        Assert.Equal("strict", report["profile"]!.GetValue<string>());
        Assert.True(report["gating"]!.GetValue<bool>());

        var rules = report["rules"]!.AsArray();
        Assert.Equal(Rules.All.Select(r => r.Id), rules.Select(r => r!["id"]!.GetValue<string>()));
        Assert.Equal(["id", "name", "severity", "count", "suppressed", "title", "fix"], rules[0]!.AsObject().Select(p => p.Key));

        var finding = Assert.Single(report["findings"]!.AsArray())!.AsObject();
        Assert.Equal(["id", "rule", "category", "severity", "file", "line", "message", "fix"], finding.Select(p => p.Key));
        Assert.Equal("UNOL201", finding["id"]!.GetValue<string>());
        Assert.Equal("correctness", finding["category"]!.GetValue<string>());
        Assert.Equal("warning", finding["severity"]!.GetValue<string>());
        Assert.Equal(4, finding["line"]!.GetValue<int>());

        var suppressed = Assert.Single(report["suppressedFindings"]!.AsArray())!;
        Assert.Equal("HEX", suppressed["rule"]!.GetValue<string>());
    }

    [Fact]
    public async Task Rules_lists_the_catalog_as_text_and_json()
    {
        var text = await RunAsync("rules");
        Assert.Equal(0, text.ExitCode);
        Assert.Contains("UNOL201  BINDVALUE    correctness  recommended  mvux", text.OutputText);
        Assert.Contains("UNOL101  CARD         style        strict       toolkit", text.OutputText);

        var json = JsonNode.Parse((await RunAsync("rules --json")).OutputText)!["rules"]!.AsArray();
        Assert.Equal(Rules.All.Count, json.Count);
        var bindValue = json.Single(r => r!["id"]!.GetValue<string>() == "UNOL201")!;
        Assert.Equal(["id", "name", "category", "profile", "requires", "defaultSeverity", "title"], bindValue.AsObject().Select(p => p.Key));
        Assert.Equal("mvux", bindValue["requires"]!.GetValue<string>());
        Assert.Equal("recommended", bindValue["profile"]!.GetValue<string>());
    }

    // ------------------------------------------------------------ text report

    [Fact]
    public async Task Text_report_prints_each_rules_fix_and_counts_hidden_hits()
    {
        _project.Write("Views/Swatches.xaml", $"<Page {Ns}>\n<Border Background=\"Red\"/>\n<Border Background=\"Blue\"/>\n<Border Background=\"Green\"/>\n<Border Background=\"Gray\"/>\n</Page>");

        var output = (await RunAsync($"lint {Quoted()} --no-cache")).OutputText;

        Assert.Contains("HEX: 4 hit(s) in 1 file(s)", output);
        Assert.Contains("  fix: Use a theme role brush", output);
        Assert.Contains("(+1 more)", output);
    }

    [Theory]
    [InlineData("lint --json", OutputFormat.Structured)]
    [InlineData("lint --output:yaml", OutputFormat.Structured)]
    [InlineData("lint --output:human", OutputFormat.Text)]
    [InlineData("lint --human", OutputFormat.Text)]
    [InlineData("lint --json --human", OutputFormat.Text)]
    [InlineData("lint --human --json", OutputFormat.Structured)]
    [InlineData("lint --markdown", OutputFormat.Markdown)]
    [InlineData("lint --output:markdown", OutputFormat.Markdown)]
    [InlineData("lint", OutputFormat.Text)]
    public void The_last_format_flag_decides_the_output(string commandLine, OutputFormat format)
    {
        Assert.Equal(format, OutputFormats.Of(commandLine.Split(' ')));
    }

    [Fact]
    public async Task Markdown_output_is_the_whole_text_report()
    {
        _project.Write("Hex.xaml", $"<Page {Ns}><Border Background=\"#FF0000\"/></Page>");

        var output = (await RunAsync($"lint {Quoted()} --no-cache --markdown")).OutputText;

        Assert.StartsWith("```text", output.TrimStart());
        Assert.Contains("HEX: 1 hit(s) in 1 file(s)", output);
    }
}
