// Runs the page check (PageCheck.cs, copied into the app: the uno-testing skill, references/page-check.md) once per
// language and window width, on Windows, macOS and Linux. Copy it beside the solution, for example as
// <Solution>/tests/ui/page-check.cs, never inside a project folder: a .cs file there compiles into the app and its
// top-level statements replace the app's entry point (warning CS7022).
//
//   dotnet run tests/ui/page-check.cs -- <App>/<App>.csproj [--languages en,fr] [--widths 1280,400] [--out <folder>]
//                                        [--env NAME=value]... [--timeout <seconds per pass>] [--framework <tfm>] [--no-build]
//
// --languages  default: the app's languages (appsettings.json LocalizationConfiguration:Cultures, else the folders of
//              its .resw files), else en.
// --widths     the window's client width per pass; "open" leaves the size the app opens at. Default 1280,400.
// --env        a variable for the app, repeatable: its sample-data switch, so no pass signs in or reaches a live
//              service. {language} and {width} in the value are replaced per pass, for an app that reads its language
//              from a variable of its own (--env MYAPP_LANGUAGE={language}).
// --out        default: <temp>/page-check/<App>. Each pass writes <out>/<language>-<width>/ (a PNG per step,
//              findings.txt, texts.txt, steps.txt, summary.txt, app.log); <out>/findings.txt has every pass's findings.
//
// Builds the Debug desktop target once, then starts the app once per pass (twice when the app has to change
// language: the first start saves it), stops a pass that outlasts the timeout by killing the process it started,
// and puts the app's language back at the end. On Linux the app needs an X display: this script uses DISPLAY and
// never starts or stops a display server. Exit code 0: every pass finished (findings or not). 1: a pass did not
// finish. 2: wrong arguments, no display, or the build failed.
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

string? project = null, outRoot = null, framework = null;
string[]? languages = null;
string[] widths = ["1280", "400"];
var variables = new List<(string Name, string Value)>();
var timeout = 300;
var build = true;
try
{
    for (var i = 0; i < args.Length; i++)
    {
        string Next() => ++i < args.Length ? args[i] : throw new ArgumentException($"{args[i - 1]} needs a value");
        switch (args[i])
        {
            case "--languages": languages = Next().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); break;
            case "--widths": widths = Next().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); break;
            case "--out": outRoot = Path.GetFullPath(Next()); break;
            case "--framework": framework = Next(); break;
            case "--timeout": timeout = int.Parse(Next()); break;
            case "--no-build": build = false; break;
            case "--env":
                var pair = Next().Split('=', 2);
                variables.Add(pair.Length == 2 ? (pair[0], pair[1]) : throw new ArgumentException("--env takes NAME=value"));
                break;
            default: project = project is null && !args[i].StartsWith('-') ? Path.GetFullPath(args[i]) : throw new ArgumentException($"unknown argument {args[i]}"); break;
        }
    }

    if (project is null || !File.Exists(project))
    {
        throw new ArgumentException(project is null ? "the app's .csproj is missing" : $"{project} does not exist");
    }

    if (widths.FirstOrDefault(w => w != "open" && !(int.TryParse(w, out var n) && n > 0)) is { } bad)
    {
        throw new ArgumentException($"--widths takes numbers or open, not {bad}");
    }

    if (widths.Length == 0 || languages is { Length: 0 })
    {
        throw new ArgumentException("--widths and --languages take at least one value");
    }
}
catch (Exception ex) when (ex is ArgumentException or FormatException)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine("usage: dotnet run page-check.cs -- <App.csproj> [--languages en,fr] [--widths 1280,400] [--out <folder>] [--env NAME=value]... [--timeout <seconds>] [--framework <tfm>] [--no-build]");
    return 2;
}

var projectFolder = Path.GetDirectoryName(project)!;
var appName = Path.GetFileNameWithoutExtension(project);
outRoot ??= Path.Combine(Path.GetTempPath(), "page-check", appName);
languages ??= AppLanguages(projectFolder);
framework ??= Regex.Match(File.ReadAllText(project), @"<TargetFrameworks?>([^<]+)<").Groups[1].Value
    .Split(';', StringSplitOptions.TrimEntries).FirstOrDefault(f => f.EndsWith("-desktop", StringComparison.Ordinal)) ?? "net10.0-desktop";

if (OperatingSystem.IsLinux() && !variables.Any(v => v.Name == "DISPLAY" && v.Value.Length > 0) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")))
{
    Console.Error.WriteLine("No display: on Linux the app needs an X display, and this script does not start one.");
    Console.Error.WriteLine("Start one and name it, for example:  Xvfb :97 -screen 0 1920x1200x24 &   then run this again with DISPLAY=:97 set.");
    return 2;
}

if (build)
{
    Console.WriteLine($"Building {appName} ({framework}, Debug)...");
    var (code, output) = Dotnet(["build", project, "-f", framework, "-c", "Debug", "--nologo", "-v", "q"]);
    if (code != 0)
    {
        Console.Error.WriteLine(output);
        Console.Error.WriteLine("The build failed: nothing was checked.");
        return 2;
    }
}

var (targetCode, target) = Dotnet(["msbuild", project, "-getProperty:TargetPath", $"-p:TargetFramework={framework}", "-p:Configuration=Debug"]);
target = target.Trim();
if (targetCode != 0 || !File.Exists(target))
{
    Console.Error.WriteLine($"The app's assembly was not found ({target}): build it first, or drop --no-build.");
    return 2;
}

Directory.CreateDirectory(outRoot);
Console.WriteLine($"{appName}: languages {string.Join(", ", languages)}; widths {string.Join(", ", widths)}; output {outRoot}");
string? languageBefore = null;
var passes = new List<(string Name, bool Finished, int Steps, int Findings, int Exceptions)>();
var allFindings = new List<string>();
foreach (var language in languages)
{
    foreach (var width in widths)
    {
        var name = $"{language}-{width}";
        var folder = Path.Combine(outRoot, name);
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }

        Directory.CreateDirectory(folder);
        var watch = Stopwatch.StartNew();

        // Exit code 3: the app started in another language, saved the one asked for and wants one more start.
        var exit = Start(folder, language, width, []);
        if (exit == 3)
        {
            exit = Start(folder, language, width, [("PAGECHECK_RESTARTED", "1")], timeout - (int)watch.Elapsed.TotalSeconds);
        }

        var before = Path.Combine(folder, "language-before.txt");
        languageBefore ??= File.Exists(before) ? File.ReadAllText(before).Trim() : null;
        // The app writes its files after every step, so a pass that was stopped still shows what it found.
        var summary = File.Exists(Path.Combine(folder, "summary.txt")) ? File.ReadAllText(Path.Combine(folder, "summary.txt")) : "";
        var finished = exit == 0 && summary.StartsWith("finished ", StringComparison.Ordinal);
        var steps = Lines(folder, "steps.txt").Length;
        var findings = Lines(folder, "findings.txt");
        var exceptions = findings.Count(f => f.Split('\t') is [_, _, "exception", ..]);
        allFindings.AddRange(findings);
        passes.Add((name, finished, steps, findings.Length, exceptions));
        var state = finished ? "finished" : exit is null ? $"DID NOT FINISH (stopped after {timeout} s)" : $"DID NOT FINISH (exit code {exit})";
        var ranAs = Regex.Match(summary, @"pass=(\S+)").Groups[1].Value is { Length: > 0 } pass && pass != name ? $" (ran as {pass})" : "";
        Console.WriteLine($"{name,-12} {state}{ranAs}  steps={steps} findings={findings.Length} exceptions={exceptions}  {(finished ? folder : Path.Combine(folder, "app.log"))}");
    }
}

// The app gets back the language it had before the first change.
if (languageBefore is not null && languageBefore != languages[^1])
{
    var folder = Path.Combine(outRoot, ".language");
    Directory.CreateDirectory(folder);
    var exit = Start(folder, languageBefore, "open", [("PAGECHECK_LANGUAGE_ONLY", "1")]);
    Console.WriteLine(exit is 0 or 3 ? $"The app's language is back to {languageBefore}." : $"The app's language was not put back to {languageBefore}: set it in the app.");
    Directory.Delete(folder, recursive: true);
}

File.WriteAllLines(Path.Combine(outRoot, "findings.txt"), allFindings);
var kinds = allFindings.Select(f => f.Split('\t')).Where(f => f.Length > 2).GroupBy(f => f[2]).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} {g.Count()}");
Console.WriteLine($"total: {passes.Count(p => p.Finished)} of {passes.Count} passes finished  steps={passes.Sum(p => p.Steps)} findings={passes.Sum(p => p.Findings)} exceptions={passes.Sum(p => p.Exceptions)}");
Console.WriteLine($"findings by kind: {(allFindings.Count == 0 ? "none" : string.Join(", ", kinds))}  ({Path.Combine(outRoot, "findings.txt")})");
return passes.All(p => p.Finished) ? 0 : 1;

// One start of the app for a pass. Null: it outlasted the time left and was stopped.
int? Start(string folder, string language, string width, (string Name, string Value)[] extra, int? seconds = null)
{
    var start = new ProcessStartInfo("dotnet") { WorkingDirectory = Path.GetDirectoryName(target)!, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
    start.ArgumentList.Add(target);
    start.Environment["PAGECHECK"] = folder;
    start.Environment["PAGECHECK_CULTURE"] = language;
    foreach (var name in new[] { "PAGECHECK_WIDTH", "PAGECHECK_RESTARTED", "PAGECHECK_LANGUAGE_ONLY", "PAGECHECK_STAY" })
    {
        start.Environment.Remove(name);
    }

    if (width != "open")
    {
        start.Environment["PAGECHECK_WIDTH"] = width;
    }

    foreach (var (name, value) in variables.Concat(extra))
    {
        start.Environment[name] = value.Replace("{language}", language).Replace("{width}", width);
    }

    using var log = new StreamWriter(Path.Combine(folder, "app.log"), append: true);
    using var app = new Process { StartInfo = start };
    void Write(string? line)
    {
        if (line is not null)
        {
            lock (log)
            {
                log.WriteLine(line);
            }
        }
    }

    app.OutputDataReceived += (_, e) => Write(e.Data);
    app.ErrorDataReceived += (_, e) => Write(e.Data);
    app.Start();
    app.BeginOutputReadLine();
    app.BeginErrorReadLine();
    if (!app.WaitForExit(TimeSpan.FromSeconds(Math.Max(5, seconds ?? timeout))))
    {
        // Only the process this script started, and what it started: by id, never by name.
        app.Kill(entireProcessTree: true);
        app.WaitForExit();
        return null;
    }

    app.WaitForExit();
    return app.ExitCode;
}

string[] Lines(string folder, string file) => File.Exists(Path.Combine(folder, file)) ? File.ReadAllLines(Path.Combine(folder, file)) : [];

(int Code, string Output) Dotnet(string[] arguments)
{
    var start = new ProcessStartInfo("dotnet") { WorkingDirectory = projectFolder, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
    foreach (var argument in arguments)
    {
        start.ArgumentList.Add(argument);
    }

    using var process = Process.Start(start)!;
    var error = process.StandardError.ReadToEndAsync();
    var output = process.StandardOutput.ReadToEnd();
    process.WaitForExit();
    return (process.ExitCode, output + error.Result);
}

// The languages the app ships: Uno.Extensions' list in appsettings.json, else one per folder of .resw files.
static string[] AppLanguages(string projectFolder)
{
    var settings = Path.Combine(projectFolder, "appsettings.json");
    if (File.Exists(settings))
    {
        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(settings), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            if (json.RootElement.TryGetProperty("LocalizationConfiguration", out var localization) && localization.TryGetProperty("Cultures", out var cultures)
                && cultures.EnumerateArray().Select(c => c.GetString()).OfType<string>().ToArray() is { Length: > 0 } listed)
            {
                return listed;
            }
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            // Not the shape expected: the .resw folders decide.
        }
    }

    var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
    var folders = Directory.EnumerateFiles(projectFolder, "*.resw", SearchOption.AllDirectories)
        .Where(f => !Path.GetRelativePath(projectFolder, f).Split(separators).Any(part => part is "bin" or "obj"))
        .Select(f => Path.GetFileName(Path.GetDirectoryName(f)!)).Distinct().Order().ToArray();
    return folders.Length > 0 ? folders : ["en"];
}
