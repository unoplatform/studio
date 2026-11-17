using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Uno.Lint.Cli;
using Xunit;
using static Uno.Lint.Tests.TestHost;

namespace Uno.Lint.Tests;

/// <summary>The command line in memory, as Program.Main runs it: exit codes, the JSON contract, the text report and help.</summary>
public class CliTests : IDisposable
{
    private readonly TempProject _project = new();

    public CliTests()
    {
        _project.Write("App.csproj", "<Project Sdk=\"Uno.Sdk\"><PropertyGroup><UnoFeatures>Material;Toolkit;Mvux</UnoFeatures></PropertyGroup></Project>");
    }

    public void Dispose() => _project.Dispose();

    private sealed record Run(int ExitCode, string Output, string Error);

    /// <summary>Runs one command line, split like a shell: double quotes group, and are dropped.</summary>
    private static async Task<Run> RunAsync(string commandLine, string stdin = "")
    {
        var args = Regex.Matches(commandLine, "\"([^\"]*)\"|(\\S+)").Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value).ToArray();
        var output = new StringWriter();
        var error = new StringWriter();
        var exitCode = await UnoLintCli.RunAsync(args, () => new StringReader(stdin), output, error, Engine);
        return new Run(exitCode, output.ToString(), error.ToString());
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
    [InlineData("lint does-not-exist", "Path not found: does-not-exist")]
    [InlineData("lint --bogus", "unknown option --bogus")]
    [InlineData("lint --top 0", "--top must be a whole number of at least 1")]
    [InlineData("lint --top many", "--top must be a whole number of at least 1")]
    [InlineData("lint --top", "--top needs a value")]
    [InlineData("lint --profile strictest", "--profile must be recommended or strict, not 'strictest'")]
    [InlineData("lint --profile", "--profile needs a value")]
    [InlineData("lint --include-dev=true", "--include-dev takes no value")]
    [InlineData("lint one two", "lint takes one path, not 2")]
    [InlineData("rules extra", "unknown argument extra")]
    [InlineData("frobnicate", "unknown command frobnicate")]
    public async Task Exit_code_is_2_with_one_line_on_stderr_when_the_run_cannot_complete(string commandLine, string message)
    {
        var run = await RunAsync(commandLine);

        Assert.Equal(2, run.ExitCode);
        Assert.Empty(run.Output);
        var line = Assert.Single(run.Error.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        Assert.StartsWith("uno-lint: ", line);
        Assert.Contains(message, line);
    }

    [Fact]
    public async Task A_flag_before_the_path_is_not_read_as_its_value()
    {
        _project.Write("Hex.xaml", $"<Page {Ns}><Border Background=\"#FF0000\"/></Page>");

        foreach (var flag in new[] { "--no-cache", "--include-dev", "--toolkit", "--no-toolkit" })
        {
            var run = await RunAsync($"lint {flag} {Quoted("Hex.xaml")}");
            Assert.Equal(string.Empty, run.Error);
            Assert.Equal(1, run.ExitCode);
        }
    }

    [Fact]
    public async Task Profile_and_toolkit_options_bind_and_the_last_toolkit_flag_wins()
    {
        _project.Write("Card.xaml", $"<Page {Ns}><Border CornerRadius=\"8\" Background=\"{{ThemeResource SurfaceBrush}}\"><Grid/></Border></Page>");

        Assert.Equal(0, (await RunAsync($"lint {Quoted()} --no-cache")).ExitCode);
        Assert.Equal(1, (await RunAsync($"lint {Quoted()} --profile strict --no-cache")).ExitCode);
        Assert.Equal(1, (await RunAsync($"lint {Quoted()} --profile=STRICT --no-cache")).ExitCode);
        Assert.Equal(0, (await RunAsync($"lint {Quoted()} --profile strict --no-toolkit --no-cache")).ExitCode);
        Assert.Equal(1, (await RunAsync($"lint {Quoted()} --profile strict --no-toolkit --toolkit --no-cache")).ExitCode);
    }

    // ------------------------------------------------------------ JSON contract

    [Fact]
    public async Task Json_report_has_a_stable_shape()
    {
        _project.Write("Views/MainPage.xaml", $"<Page {Ns}>\n<!-- uno-lint: allow hex - chart -->\n<Border Background=\"#FF0000\"/>\n<TextBox Text=\"{{Binding Name.Value}}\"/>\n</Page>");

        var execution = await RunAsync($"lint {Quoted()} --profile strict --no-cache --json");
        var report = JsonNode.Parse(execution.Output)!.AsObject();

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
        Assert.Contains("UNOL201  BINDVALUE    correctness  recommended  mvux", text.Output);
        Assert.Contains("UNOL101  CARD         style        strict       toolkit", text.Output);

        var json = JsonNode.Parse((await RunAsync("rules --json")).Output)!["rules"]!.AsArray();
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

        var output = (await RunAsync($"lint {Quoted()} --no-cache")).Output;

        Assert.Contains("HEX: 4 hit(s) in 1 file(s)", output);
        Assert.Contains("  fix: Use a theme role brush", output);
        Assert.Contains("(+1 more)", output);
    }

    [Theory]
    [InlineData("rules", "Id       Rule")]
    [InlineData("rules --json", "{")]
    [InlineData("rules --markdown", "```text")]
    [InlineData("rules --json --markdown", "```text")]
    [InlineData("rules --markdown --json", "{")]
    public async Task The_last_format_flag_decides_the_output(string commandLine, string start)
    {
        Assert.StartsWith(start, (await RunAsync(commandLine)).Output.TrimStart());
    }

    [Fact]
    public async Task Markdown_output_is_the_whole_text_report()
    {
        _project.Write("Hex.xaml", $"<Page {Ns}><Border Background=\"#FF0000\"/></Page>");

        var output = (await RunAsync($"lint {Quoted()} --no-cache --markdown")).Output;

        Assert.StartsWith("```text", output.TrimStart());
        Assert.Contains("HEX: 1 hit(s) in 1 file(s)", output);
        Assert.EndsWith("```", output.TrimEnd());
    }

    // ------------------------------------------------------------ help

    [Theory]
    [InlineData("")]
    [InlineData("--help")]
    [InlineData("-h")]
    public async Task Help_lists_the_commands(string commandLine)
    {
        var run = await RunAsync(commandLine);

        Assert.Equal(0, run.ExitCode);
        Assert.Empty(run.Error);
        foreach (var command in UnoLintCli.Commands)
        {
            Assert.Contains($"  {command.Name} ", run.Output);
        }
    }

    [Fact]
    public async Task Each_commands_help_lists_its_options()
    {
        foreach (var command in UnoLintCli.Commands)
        {
            var run = await RunAsync($"{command.Name} --help");

            Assert.Equal(0, run.ExitCode);
            Assert.StartsWith($"Usage: uno-lint {command.Name}", run.Output);
            Assert.All(command.Options, option => Assert.Contains(option.Name, run.Output));
        }
    }

    [Fact]
    public void The_readme_documents_every_lint_option()
    {
        var readme = File.ReadAllText(Path.Combine(UnoLintRoot(), "README.md")).ReplaceLineEndings("\n");
        var table = readme[readme.IndexOf("| Option of `lint` |", StringComparison.Ordinal)..];
        table = table[..table.IndexOf("\n\n", StringComparison.Ordinal)];

        Assert.All(
            UnoLintCli.Commands.Single(c => c.Name == "lint").Options.Where(o => o.Name != "--help"),
            option => Assert.Contains($"`{option.Name}", table));
    }

    private static string UnoLintRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(directory.FullName, "Uno.Lint.slnx")))
        {
            directory = directory.Parent ?? throw new InvalidOperationException("Uno.Lint.slnx not found above the test output.");
        }

        return directory.FullName;
    }
}
