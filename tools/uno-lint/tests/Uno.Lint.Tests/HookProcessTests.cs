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
    public void A_bad_hook_option_value_still_exits_0()
    {
        var (exitCode, output, _) = RunTool("hook --profile strictest", Encoding.UTF8.GetBytes("{}"));
        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, output);
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
