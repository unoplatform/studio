using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using Xunit;
using static Uno.Lint.Tests.TestHost;

namespace Uno.Lint.Tests;

/// <summary>
/// The built tool as a harness runs it: a child process fed raw UTF-8 bytes on stdin. This is what the in-memory tests
/// cannot show: the console code page, the exit code and stray output on stdout.
/// </summary>
public class HookProcessTests : IDisposable
{
    private readonly TempProject _project = new();

    public void Dispose() => _project.Dispose();

    private static (int ExitCode, string Output, string Error) RunTool(string arguments, byte[] stdin)
    {
        var tool = Path.Combine(AppContext.BaseDirectory, "uno-lint.dll");
        var start = new ProcessStartInfo("dotnet", $"\"{tool}\" {arguments}")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
        };
        start.Environment["UNO_LINT_NO_CACHE"] = "1";

        using var process = Process.Start(start)!;
        process.StandardInput.BaseStream.Write(stdin);
        process.StandardInput.Close();
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        Assert.True(process.WaitForExit(60_000), "the hook did not finish");
        return (process.ExitCode, output.Result, error.Result);
    }

    private byte[] WritePayload(string relative)
    {
        var file = _project.Write(relative, $"<Page {Ns}>\n  <Border Background=\"#FF0000\"/>\n</Page>");
        var payload = new JsonObject
        {
            ["tool_name"] = "Write",
            ["tool_input"] = new JsonObject { ["file_path"] = file, ["content"] = "x" },
            ["tool_response"] = new JsonObject { ["type"] = "create", ["structuredPatch"] = new JsonArray() },
        };

        // Raw UTF-8, with non-ASCII characters left unescaped, as Claude Code and Codex send them.
        return Encoding.UTF8.GetBytes(payload.ToJsonString(new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
    }

    [Fact]
    public void A_non_ascii_path_in_a_raw_utf8_payload_is_linted()
    {
        _project.Write("App.csproj", "<Project/>");
        var (exitCode, output, error) = RunTool("hook", WritePayload("Écrans/Pagé.xaml"));

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, error);
        var reply = JsonNode.Parse(output)!;
        Assert.Equal("uno-lint: 1 hit(s) in Pagé.xaml (HEX)", reply["systemMessage"]!.GetValue<string>());
    }

    [Fact]
    public void Unknown_hook_options_are_noted_on_stderr_and_the_hook_still_runs()
    {
        _project.Write("App.csproj", "<Project/>");
        var (exitCode, output, error) = RunTool("hook --from-a-newer-snippet --profile strict", WritePayload("MainPage.xaml"));

        Assert.Equal(0, exitCode);
        Assert.Contains("ignoring unknown hook option(s) --from-a-newer-snippet", error);
        Assert.Contains("HEX", output);
    }

    [Fact]
    public void Inline_option_values_and_repl_options_are_not_called_unknown()
    {
        _project.Write("App.csproj", "<Project/>");
        foreach (var options in new[] { "--profile=strict", "--profile:strict", "--json" })
        {
            var (exitCode, output, error) = RunTool($"hook {options}", WritePayload("MainPage.xaml"));

            Assert.Equal(0, exitCode);
            Assert.DoesNotContain("unknown", error);
            Assert.Contains("HEX", output);
        }
    }

    [Theory]
    [InlineData("--profile strictest", "strictest")]
    [InlineData("--profile=strictest", "strictest")]
    [InlineData("--profile", "")]
    public void A_bad_profile_value_is_noted_on_stderr_and_the_hook_still_runs(string options, string value)
    {
        _project.Write("App.csproj", "<Project/>");
        var (exitCode, output, error) = RunTool($"hook {options}", WritePayload("MainPage.xaml"));

        Assert.Equal(0, exitCode);
        Assert.Contains("HEX", output);
        var line = Assert.Single(error.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        Assert.StartsWith($"uno-lint: ignoring --profile '{value}'", line);
    }

    [Theory]
    [InlineData("--no-cache")]
    [InlineData("--include-dev")]
    [InlineData("--toolkit")]
    [InlineData("--profile strict --no-cache")]
    public void A_flag_before_the_path_does_not_take_the_path_as_its_value(string options)
    {
        _project.Write("App.csproj", "<Project/>");
        var file = _project.Write("MainPage.xaml", $"<Page {Ns}>\n  <Border Background=\"#FF0000\"/>\n</Page>");

        var (exitCode, output, error) = RunTool($"lint {options} \"{file}\"", []);

        Assert.Equal(string.Empty, error);
        Assert.Equal(1, exitCode);
        Assert.Contains("HEX", output);
    }

    [Fact]
    public void A_flag_still_takes_an_explicit_boolean()
    {
        _project.Write("App.csproj", "<Project/>");
        var file = _project.Write("Tests/MainPage.xaml", $"<Page {Ns}>\n  <Border Background=\"#FF0000\"/>\n</Page>");

        Assert.Equal(0, RunTool($"lint --include-dev false \"{file}\"", []).ExitCode);
        Assert.Equal(1, RunTool($"lint --include-dev true \"{file}\"", []).ExitCode);
    }

    [Fact]
    public void Garbage_on_stdin_exits_0_with_nothing_on_stdout()
    {
        var (exitCode, output, error) = RunTool("hook", [0xFF, 0xFE, 0x00, 0x7B]);
        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, output);
        Assert.StartsWith("uno-lint:", error);
    }
}
