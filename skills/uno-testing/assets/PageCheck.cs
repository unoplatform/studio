// PageCheck.cs: the page check of the uno-testing skill (references/page-check.md).
//
// Copy this file into the app project (any folder) and add one line to App.xaml.cs, at the end of
// OnLaunched: after the first navigation (InitializeNavigationAsync), or after MainWindow.Activate().
//
//     _ = PageCheck.RunIfAskedAsync(MainWindow);
//
// It does nothing unless the PAGECHECK environment variable names a folder, and outside a Debug desktop
// build (net10.0-desktop) it compiles to an empty stub. When asked, it waits for the first page, opens
// every section the shell offers (the items of a NavigationView or of an Uno Toolkit TabBar, and the tabs
// a section shows), measures each screen, writes its files and exits the app. assets/page-check.cs runs
// it once per language and width.
//
// Your own walk adds the screens the shell does not reach (a record, a dialog, an empty list, an error):
//
//     await PageCheck.SnapAsync("accounts-empty");                       // from code, at any step
//     _ = PageCheck.RunIfAskedAsync(MainWindow, MyChecks.WalkAsync);     // a walk to run after the sections
//
// or, with no code, start the app with PAGECHECK_STAY=1 and write a step name into the file
// <folder>/snap from a check script: the screen is measured as it is, and the file is removed when the
// step is done. Create <folder>/end to finish.
//
// Variables: PAGECHECK the output folder; PAGECHECK_WIDTH and PAGECHECK_HEIGHT the window's client size
// (not set: the size the app opens at); PAGECHECK_CULTURE the language to run in. An app that starts in
// another language has the one asked for saved for its next start and exits with code 3: start it again.
//
// Writes into the folder: a PNG per step (open flyouts, dialogs and menus included), findings.txt (one
// line per finding: pass, step, kind, detail, tab-separated), texts.txt (every string shown, for a
// language review), steps.txt and summary.txt. Exit code 0: the pass finished. 3: start again (language).
// Anything else: it did not finish.
#if DEBUG && __DESKTOP__
using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Path = System.IO.Path;
using Point = Windows.Foundation.Point;
using Rect = Windows.Foundation.Rect;
using Size = Windows.Foundation.Size;
using Task = System.Threading.Tasks.Task;

internal static class PageCheck
{
    private static readonly List<string> Findings = [];
    private static readonly HashSet<string> Seen = [];
    private static readonly List<string> Texts = [];
    private static readonly List<string> Steps = [];
    private static Window? _window;
    private static string? _folder;
    private static string _pass = "";
    private static string _language = "";
    private static int _exceptions;

    /// <summary>Runs the pass when PAGECHECK is set: the sections, then <paramref name="then"/> (the app's own walk), then exit.</summary>
    public static async Task RunIfAskedAsync(Window window, Func<Task>? then = null)
    {
        if (Environment.GetEnvironmentVariable("PAGECHECK") is not { Length: > 0 } folder)
        {
            return;
        }

        _window = window;
        _folder = Path.GetFullPath(folder);
        _pass = Path.GetFileName(_folder);
        Directory.CreateDirectory(_folder);
        Application.Current.UnhandledException += (_, e) => { Thrown(e.Exception); e.Handled = true; };
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, e) => Thrown(e.Exception.InnerException ?? e.Exception);

        // An exception that leaves an async void method (most event handlers) is thrown again on the UI
        // thread, where Uno Platform's dispatcher drops it without raising UnhandledException. It is seen
        // here on its way: Task.ThrowAsync is how .NET throws it again.
        AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
        {
            if (new System.Diagnostics.StackTrace(1, false).GetFrames().Take(6).Select(f => f.GetMethod())
                .Any(m => m?.Name.Contains("ThrowAsync", StringComparison.Ordinal) == true && m.DeclaringType?.FullName?.StartsWith("System.Threading.Tasks.Task", StringComparison.Ordinal) == true))
            {
                Thrown(e.Exception);
            }
        };

        var code = 1;
        try
        {
            // The first page: something to read on screen, then nothing still loading.
            for (var waited = 0; waited < 30000 && !Shown<TextBlock>().Any(t => IsWords(t.Text)); waited += 250)
            {
                await Task.Delay(250);
            }

            await SettleAsync(15000);
            if (!await LanguageAsync())
            {
                code = 3;
                return;
            }

            if (Environment.GetEnvironmentVariable("PAGECHECK_LANGUAGE_ONLY") is { Length: > 0 })
            {
                code = 0;
                return;
            }

            await SizeAsync(window);
            _pass = $"{_language}-{window.Bounds.Width:0}";
            await SnapAsync("start");
            var sections = Steps.Count;
            await SectionsAsync([], 1);
            if (Steps.Count == sections)
            {
                Report("start", "check", "no NavigationView or TabBar item found: only the first screen was measured; reach the others from your own walk and call PageCheck.SnapAsync at each");
            }

            if (then is not null)
            {
                await then();
            }

            if (Environment.GetEnvironmentVariable("PAGECHECK_STAY") is { Length: > 0 })
            {
                await RequestsAsync(_folder);
            }

            code = 0;
        }
        catch (Exception ex)
        {
            Report("check", "check", $"the pass stopped: {ex.GetType().Name}: {ex.Message}");
            Console.Out.WriteLine($"PAGECHECK failed {ex}");
        }
        finally
        {
            var summary = $"{(code == 0 ? "finished" : code == 3 ? "restart" : "failed")} pass={_pass} steps={Steps.Count} findings={Findings.Count} exceptions={_exceptions}";
            Save();
            File.WriteAllText(Path.Combine(_folder, "summary.txt"), summary);
            Console.Out.WriteLine("PAGECHECK " + summary);
            Environment.ExitCode = code;
            Application.Current.Exit();
            // An app that keeps its window open on Exit (a closing handler) still ends the pass.
            _ = Task.Delay(5000).ContinueWith(_ => Environment.Exit(code), System.Threading.Tasks.TaskScheduler.Default);
        }
    }

    /// <summary>
    /// Measures the screen as it is now, saves its PNG and adds its findings. Wait for the state you want
    /// first. Does nothing when the check was not asked for.
    /// </summary>
    public static Task SnapAsync(string name)
    {
        if (_window is not { } window || _folder is null)
        {
            return Task.CompletedTask;
        }

        if (window.DispatcherQueue.HasThreadAccess)
        {
            return SnapOnUiAsync(window, name);
        }

        var done = new System.Threading.Tasks.TaskCompletionSource();
        window.DispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                await SnapOnUiAsync(window, name);
                done.SetResult();
            }
            catch (Exception ex)
            {
                done.SetException(ex);
            }
        });
        return done.Task;
    }

    private static async Task SnapOnUiAsync(Window window, string name)
    {
        var step = $"{Steps.Count + 1:00}-{Slug(name)}";

        // A window with no size measures every text as past its edge: one finding says so instead.
        for (var waited = 0; waited < 5000 && window.Bounds.Width <= 0; waited += 250)
        {
            await Task.Delay(250);
        }

        Steps.Add(step);
        if (window.Bounds.Width <= 0 || window.Content is not FrameworkElement root)
        {
            Report(step, "check", "the window had no size or no content, so this screen was not measured");
            return;
        }

        Measure(window, step);
        await PictureAsync(root, Path.Combine(_folder!, step + ".png"));
        Save();
        Console.Out.WriteLine($"PAGECHECK [{step}] {window.Bounds.Width:0}x{window.Bounds.Height:0}");
    }

    // ---- The walk: the shell's sections ----

    /// <summary>Every item of each NavigationView and TabBar on screen, picked as a person picks it, then the tabs that section shows.</summary>
    private static async Task SectionsAsync(HashSet<object> visited, int depth)
    {
        var shells = Shown<FrameworkElement>().Where(e => e is NavigationView || IsTabBar(e)).Where(visited.Add).ToList();
        foreach (var shell in shells)
        {
            var items = (shell is NavigationView nav ? NavigationItems(nav) : TabItems((ItemsControl)shell)).ToList();
            for (var i = 0; i < items.Count; i++)
            {
                Pick(shell, items[i]);
                await SettleAsync();
                await SnapAsync(SectionName(items[i], i));
                if (depth < 2)
                {
                    await SectionsAsync(visited, depth + 1);
                }
            }
        }
    }

    private static IEnumerable<object> NavigationItems(NavigationView nav)
    {
        var menu = nav.MenuItemsSource is IEnumerable source ? source.Cast<object>() : nav.MenuItems;
        var footer = nav.FooterMenuItemsSource is IEnumerable footerSource ? footerSource.Cast<object>() : nav.FooterMenuItems;
        var items = Flatten(menu.Concat(footer));
        if (nav.IsSettingsVisible && nav.SettingsItem is { } settings)
        {
            items = items.Append(settings);
        }

        // Headers and separators are not sections; neither is an item that only runs a command.
        return items.Where(i => i is not NavigationViewItemBase || i is NavigationViewItem { IsEnabled: true, Visibility: Visibility.Visible, SelectsOnInvoked: true });
    }

    private static IEnumerable<object> Flatten(IEnumerable<object> items) =>
        items.SelectMany(i => i is NavigationViewItem { MenuItems.Count: > 0 } parent ? Flatten(parent.MenuItems).Prepend(i) : [i]);

    // Uno Toolkit's TabBar, found by name so the check needs no package reference.
    private static bool IsTabBar(object element)
    {
        for (var type = element.GetType(); type is not null; type = type.BaseType)
        {
            if (type.FullName == "Uno.Toolkit.UI.TabBar")
            {
                return element is ItemsControl;
            }
        }

        return false;
    }

    private static IEnumerable<object> TabItems(ItemsControl bar) =>
        bar.Items.Where(i => i is not Control { IsEnabled: false } and not UIElement { Visibility: Visibility.Collapsed }
            && i.GetType().GetProperty("IsSelectable")?.GetValue(i) is not false);

    private static void Pick(FrameworkElement shell, object item)
    {
        if (shell is NavigationView nav)
        {
            // A NavigationViewItem's peer has no Invoke pattern on Uno Platform, so this calls the method a
            // click calls: it raises ItemInvoked, then selects. Setting SelectedItem alone skips ItemInvoked.
            var invoked = typeof(NavigationView).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == "OnNavigationViewItemInvoked" && m.GetParameters() is [{ ParameterType.Name: "NavigationViewItem" }]);
            if (item is NavigationViewItem navItem && invoked is not null)
            {
                invoked.Invoke(nav, [navItem]);
            }
            else
            {
                nav.SelectedItem = item;
            }
        }
        else
        {
            shell.GetType().GetProperty("SelectedItem")?.SetValue(shell, item);
        }
    }

    // The step's name: the item's automation id or x:Name (the same in every language), else its text.
    private static string SectionName(object item, int index) =>
        item is DependencyObject element && AutomationProperties.GetAutomationId(element) is { Length: > 0 } id ? id
        : item is FrameworkElement { Name.Length: > 0 } named ? named.Name
        : item is ContentControl control && Label(control) is { Length: > 0 } label ? label
        : $"section-{index + 1}";

    /// <summary>Until the texts on screen have stopped changing for most of a second and nothing shows progress, or <paramref name="atMost"/>.</summary>
    private static async Task SettleAsync(int atMost = 10000)
    {
        var last = "";
        var steady = 0;
        for (var waited = 0; waited < atMost && steady < 3; waited += 250)
        {
            await Task.Delay(250);
            var busy = Shown<ProgressRing>().Any(r => r.IsActive) || Shown<ProgressBar>().Any(b => b.IsIndeterminate);
            var now = string.Join("\n", Shown<TextBlock>().Select(t => t.Text));
            steady = !busy && now == last ? steady + 1 : 0;
            last = now;
        }
    }

    /// <summary>With PAGECHECK_STAY: a step name written into the file "snap" measures the screen; a file "end" finishes.</summary>
    private static async Task RequestsAsync(string folder)
    {
        var (snap, end) = (Path.Combine(folder, "snap"), Path.Combine(folder, "end"));
        Console.Out.WriteLine($"PAGECHECK waiting: write a step name into {snap}, create {end} to finish");
        while (!File.Exists(end))
        {
            await Task.Delay(200);
            if (File.Exists(snap))
            {
                // A second look, so a name still being written is read whole.
                await Task.Delay(100);
                await SnapAsync(File.ReadAllText(snap).Trim() is { Length: > 0 } name ? name : "snap");
                File.Delete(snap);
            }
        }

        File.Delete(end);
    }

    // ---- Language and size ----

    /// <summary>False when the app runs in another language than asked: it is saved for the next start.</summary>
    private static async System.Threading.Tasks.Task<bool> LanguageAsync()
    {
        var current = CultureInfo.CurrentUICulture;
        _language = current.Name;
        if (Environment.GetEnvironmentVariable("PAGECHECK_CULTURE") is not { Length: > 0 } asked)
        {
            return true;
        }

        // "fr" is met by fr-CA; "fr-CA" only by itself.
        if (string.Equals(current.Name, asked, StringComparison.OrdinalIgnoreCase)
            || (!asked.Contains('-') && string.Equals(current.TwoLetterISOLanguageName, asked, StringComparison.OrdinalIgnoreCase)))
        {
            _language = asked;
            return true;
        }

        if (Environment.GetEnvironmentVariable("PAGECHECK_RESTARTED") is { Length: > 0 })
        {
            Report("start", "check", $"asked for {asked} but the app runs in {current.Name}, also after its language was saved and it was started again: "
                + "the app keeps its language its own way, so pass its switch to the runner (--env NAME={language})");
            return true;
        }

        // The language before the first change, so the runner can put it back.
        var before = Path.Combine(_folder!, "language-before.txt");
        if (!File.Exists(before))
        {
            File.WriteAllText(before, current.Name);
        }

        // An app hosted by Uno.Extensions keeps its language in ILocalizationService: found by name, so
        // the check needs no package reference. Any other app follows the override below.
        var service = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("Uno.Extensions.Localization.ILocalizationService")).FirstOrDefault(t => t is not null);
        var localization = service is null ? null : Application.Current.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Where(p => p.Name == "Host" && p.GetMethod is not null)
            .Select(p => p.GetValue(p.GetMethod!.IsStatic ? null : Application.Current))
            .Select(host => host?.GetType().GetProperty("Services")?.GetValue(host) as IServiceProvider)
            .Select(services => services?.GetService(service)).FirstOrDefault(s => s is not null);
        if (localization is not null && service!.GetMethod("SetCurrentCultureAsync")?.Invoke(localization, [new CultureInfo(asked)]) is Task saved)
        {
            await saved;
        }
        else
        {
            Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = asked;
        }

        Console.Out.WriteLine($"PAGECHECK language {asked} saved ({(localization is null ? "PrimaryLanguageOverride" : "ILocalizationService")}), was {current.Name}: start again");
        return false;
    }

    /// <summary>Gives the window the client size asked for, in the units layout uses, and waits until it has kept it.</summary>
    private static async Task SizeAsync(Window window)
    {
        await SteadyAsync(window);
        var width = int.TryParse(Environment.GetEnvironmentVariable("PAGECHECK_WIDTH"), out var w) ? w : 0;
        var height = int.TryParse(Environment.GetEnvironmentVariable("PAGECHECK_HEIGHT"), out var h) ? h : 0;
        if (width <= 0 && height <= 0)
        {
            return;
        }

        var wanted = new Size(width > 0 ? width : window.Bounds.Width, height > 0 ? height : window.Bounds.Height);
        var scale = window.Content?.XamlRoot?.RasterizationScale ?? 1;
        var ask = wanted;
        bool Reached() => Math.Abs(window.Bounds.Width - wanted.Width) <= 1 && Math.Abs(window.Bounds.Height - wanted.Height) <= 1;

        // A resize lands late on a busy machine, can be dropped while the window comes up, and sets the
        // outer size on some platforms: ask, wait for it to land, then correct the next ask by what it missed.
        for (var attempt = 0; attempt < 6 && !Reached(); attempt++)
        {
            var before = window.Bounds;
            window.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = (int)Math.Round(ask.Width * scale), Height = (int)Math.Round(ask.Height * scale) });
            for (var waited = 0; waited < 3000 && window.Bounds == before; waited += 250)
            {
                await Task.Delay(250);
            }

            await SteadyAsync(window);

            // An ask that changed nothing was dropped: the same ask again can be taken for done, so the next one differs.
            ask = window.Bounds == before
                ? new Size(ask.Width + 16, ask.Height)
                : new Size(Math.Max(1, ask.Width + wanted.Width - window.Bounds.Width), Math.Max(1, ask.Height + wanted.Height - window.Bounds.Height));
        }

        if (Math.Abs(window.Bounds.Width - wanted.Width) > 1)
        {
            Report("start", "check", $"asked for a window {wanted.Width:0} wide, got {window.Bounds.Width:0}: the app or the screen sets another limit, and this pass measured that width");
        }

        await SettleAsync();
    }

    // A resize before the window is up can be dropped, and a busy machine lays the window out late.
    private static async Task SteadyAsync(Window window)
    {
        var (last, steady) = (new Rect(), 0);
        for (var waited = 0; waited < 10000 && steady < 4; waited += 250)
        {
            await Task.Delay(250);
            steady = window.Bounds.Width > 0 && window.Bounds == last ? steady + 1 : 0;
            last = window.Bounds;
        }
    }

    // ---- What every screen is measured for ----

    private static void Measure(Window window, string step)
    {
        var bounds = window.Bounds;
        var texts = Shown<TextBlock>().Where(t => IsWords(t.Text)).ToList();
        foreach (var text in texts)
        {
            var value = text.Text;
            Texts.Add(value);

            // raw-key: a resource key or a format hole shown as text.
            if (KeyPattern.IsMatch(value) || value.Contains("{0}", StringComparison.Ordinal) || value.Contains("ResourceNotFound", StringComparison.Ordinal))
            {
                Report(step, "raw-key", value);
            }

            if (Within<TextBox>(text))
            {
                // What a person typed scrolls inside its box.
                continue;
            }

            var box = Box(text);
            var scrollsSideways = Within<ScrollViewer>(text, s => s.HorizontalScrollMode != ScrollMode.Disabled && s.ScrollableWidth > 0);

            // off-screen: past the window's right edge, where nothing scrolls it into view.
            if (!scrollsSideways && box.Right > bounds.Width + 1)
            {
                Report(step, "off-screen", $"\"{value}\" runs {box.Right - bounds.Width:0} px past the window's right edge");
            }

            // clipped: one line (NoWrap, or MaxLines 1) with no ellipsis, given less width than it needs.
            if (text.TextTrimming == TextTrimming.None && (text.TextWrapping == TextWrapping.NoWrap || text.MaxLines == 1) && text.Inlines.Count <= 1 && !scrollsSideways)
            {
                var twin = new TextBlock { Text = value, FontSize = text.FontSize, FontFamily = text.FontFamily, FontWeight = text.FontWeight, FontStyle = text.FontStyle, FontStretch = text.FontStretch, CharacterSpacing = text.CharacterSpacing };
                twin.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                if (twin.DesiredSize.Width > box.Width + 2)
                {
                    Report(step, "clipped", $"\"{value}\" needs {twin.DesiredSize.Width:0} px, has {box.Width:0}");
                }
            }

            Cut(step, text, box);
        }

        // unnamed-input: nothing a screen reader can call it (a Header, AutomationProperties.Name or LabeledBy; a placeholder is not a name).
        foreach (var input in Shown<Control>().Where(c => IsInput(c) && !Within<Control>(c, IsInput)))
        {
            var header = input.GetType().GetProperty("Header")?.GetValue(input);
            if (header is null or "" && AutomationProperties.GetName(input) is not { Length: > 0 } && AutomationProperties.GetLabeledBy(input) is null)
            {
                Report(step, "unnamed-input", Locate(input));
            }
        }

        // unnamed-button: no text and no AutomationProperties.Name (an icon alone). Buttons inside a framework control's template are its own.
        var buttons = Shown<ButtonBase>().Where(b => b is not RepeatButton && b.Name != "HotReloadIcon" && !Within<Control>(b, c => IsInput(c) || c is ScrollBar or CalendarView or FlipView)).ToList();
        foreach (var button in buttons.Where(b => NameOf(b) is null))
        {
            Report(step, "unnamed-button", Locate(button));
        }

        // overlap-button, overlap-text: drawn over each other. Nested elements, icons and text inside a button don't count.
        Overlaps(step, "overlap-button", buttons, bounds, b => NameOf(b) ?? Locate(b));
        Overlaps(step, "overlap-text", texts.Where(t => !Within<TextBox>(t) && !Within<ButtonBase>(t) && !Within<IconElement>(t)).ToList(), bounds, t => t.Text);

        // h-scroll: a page that scrolls sideways. A strip that only scrolls sideways (tabs, filters) is meant to.
        foreach (var scroller in Shown<ScrollViewer>().Where(s => !Within<TextBox>(s) && s.VerticalScrollMode != ScrollMode.Disabled && s.HorizontalScrollMode != ScrollMode.Disabled && s.ViewportWidth > 0 && s.ExtentWidth > s.ViewportWidth + 1))
        {
            Report(step, "h-scroll", $"{Locate(scroller)} is {scroller.ExtentWidth:0} px wide in a {scroller.ViewportWidth:0} px viewport");
        }

        // too-wide: a page laid out wider than the window.
        foreach (var page in Shown<Page>().Where(p => p.ActualWidth > bounds.Width + 1))
        {
            Report(step, "too-wide", $"{page.GetType().Name} is {page.ActualWidth:0} px wide in a {bounds.Width:0} px window");
        }
    }

    private static void Overlaps<T>(string step, string kind, List<T> elements, Rect window, Func<T, string> name) where T : FrameworkElement
    {
        // Only within one layer: a dialog is meant to cover the page behind it.
        var popups = _window?.Content is UIElement root ? Popups(root).Select(p => (DependencyObject)p.Child).ToHashSet() : [];
        var boxes = elements.Select(e => (Element: e, Layer: Layer(e, popups), Box: Visible(e, window))).Where(b => b.Box is { Width: > 0, Height: > 0 }).ToList();
        for (var i = 0; i < boxes.Count; i++)
        {
            for (var j = i + 1; j < boxes.Count; j++)
            {
                var (a, b) = (boxes[i], boxes[j]);
                var x = Math.Min(a.Box.Right, b.Box.Right) - Math.Max(a.Box.Left, b.Box.Left);
                var y = Math.Min(a.Box.Bottom, b.Box.Bottom) - Math.Max(a.Box.Top, b.Box.Top);
                if (x > 2 && y > 2 && ReferenceEquals(a.Layer, b.Layer) && !IsAncestor(a.Element, b.Element) && !IsAncestor(b.Element, a.Element))
                {
                    Report(step, kind, $"\"{name(a.Element)}\" and \"{name(b.Element)}\" overlap by {x:0}x{y:0} px");
                }
            }
        }
    }

    private static void Report(string step, string kind, string detail)
    {
        // Each problem once per pass, with the first step that showed it. Locked: an exception can be reported from any thread.
        detail = detail.ReplaceLineEndings(" ");
        lock (Findings)
        {
            if (Seen.Add(kind + "|" + detail))
            {
                Findings.Add($"{_pass}\t{step}\t{kind}\t{detail}");
                Console.Out.WriteLine($"PAGECHECK finding {_pass} [{step}] {kind}: {detail}");
            }
        }
    }

    // exception: reported with the last step taken before it was seen.
    private static void Thrown(Exception? exception)
    {
        System.Threading.Interlocked.Increment(ref _exceptions);
        Report(Steps.LastOrDefault() ?? "start", "exception", exception is null ? "unknown" : $"{exception.GetType().Name}: {exception.Message}");
    }

    // After every step, so a pass that is stopped keeps what it found.
    private static void Save()
    {
        lock (Findings)
        {
            File.WriteAllLines(Path.Combine(_folder!, "findings.txt"), Findings);
        }

        File.WriteAllLines(Path.Combine(_folder!, "texts.txt"), Texts.Distinct());
        File.WriteAllLines(Path.Combine(_folder!, "steps.txt"), Steps);
    }

    // ---- The tree ----

    // The window's tree and every open popup's, where dialogs, flyouts and menus show.
    private static IEnumerable<T> Find<T>() where T : DependencyObject =>
        _window?.Content is UIElement root
            ? Descendants(root).Prepend(root).Concat(Popups(root).SelectMany(p => Descendants(p.Child).Prepend(p.Child))).OfType<T>()
            : [];

    private static IEnumerable<T> Shown<T>() where T : UIElement => Find<T>().Where(IsShown);

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    // The app's open popups (none until the content is loaded). Uno Platform's diagnostics overlay
    // (Hot Reload's status, in Debug) is the developer's tooling, not the app.
    private static IEnumerable<Popup> Popups(UIElement root) =>
        root.XamlRoot is not { } xamlRoot ? [] : VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot)
            .Where(p => p.Child is { } child && child.GetType().Namespace?.StartsWith("Uno.Diagnostics", StringComparison.Ordinal) != true);

    private static IEnumerable<DependencyObject> Ancestors(DependencyObject element)
    {
        for (var node = VisualTreeHelper.GetParent(element); node is not null; node = VisualTreeHelper.GetParent(node))
        {
            yield return node;
        }
    }

    private static bool IsShown(UIElement element) =>
        element is not FrameworkElement { ActualWidth: 0 } and not FrameworkElement { ActualHeight: 0 }
        && !Ancestors(element).Prepend(element).Any(node => node is UIElement { Visibility: Visibility.Collapsed } or UIElement { Opacity: 0 });

    private static bool Within<T>(DependencyObject element, Func<T, bool>? matches = null) where T : DependencyObject =>
        Ancestors(element).OfType<T>().Any(found => matches is null || matches(found));

    private static bool IsAncestor(DependencyObject ancestor, DependencyObject element) => Within<DependencyObject>(element, node => ReferenceEquals(node, ancestor));

    // The element's layer: the open popup it shows in (a dialog, a flyout, a menu), or null for the window's content.
    private static DependencyObject? Layer(DependencyObject element, HashSet<DependencyObject> popups) =>
        Ancestors(element).Prepend(element).FirstOrDefault(popups.Contains);

    /// <summary>The element's bounds in the window; for text, the bounds of what is drawn.</summary>
    private static Rect Box(FrameworkElement element)
    {
        var (left, width, height) = (0d, element.ActualWidth, element.ActualHeight);
        if (element is TextBlock text)
        {
            // A TextBlock's ActualWidth and ActualHeight are the size its text asked for, margins included,
            // whatever it was given: what is drawn is that without the margins, inside the size it was arranged at.
            var given = text.RenderSize;
            width = Math.Min(given.Width, Math.Max(0, text.DesiredSize.Width - text.Margin.Left - text.Margin.Right));
            height = Math.Min(given.Height, Math.Max(0, text.DesiredSize.Height - text.Margin.Top - text.Margin.Bottom));
            left = text.HorizontalTextAlignment switch
            {
                TextAlignment.Center => (given.Width - width) / 2,
                TextAlignment.Right => given.Width - width,
                _ => 0,
            };
        }

        return element.TransformToVisual(null).TransformBounds(new Rect(left, 0, width, height));
    }

    /// <summary>The part of the element a person can see: its bounds cut to the window, to every scroll viewer's viewport, and to the room layout gave each parent.</summary>
    private static Rect Visible(FrameworkElement element, Rect window)
    {
        var box = Box(element);
        box.Intersect(new Rect(0, 0, window.Width, window.Height));
        foreach (var node in Ancestors(element).OfType<FrameworkElement>().TakeWhile(_ => !box.IsEmpty))
        {
            if (node is ScrollViewer { ActualWidth: > 0 })
            {
                box.Intersect(Box(node));
            }

            if (Room(node) is { } room)
            {
                box.Intersect(room);
            }
        }

        return box;
    }

    /// <summary>
    /// Where layout cuts the element: the room its parent gave it, when the element is larger than that
    /// (a row too narrow for its content, a closed pane's content). Null when it has the room it takes.
    /// </summary>
    private static Rect? Room(FrameworkElement element)
    {
        var slot = LayoutInformation.GetLayoutSlot(element);
        var (width, height) = (slot.Width - element.Margin.Left - element.Margin.Right, slot.Height - element.Margin.Top - element.Margin.Bottom);
        if (element is TextBlock || width <= 0 || height <= 0 || (element.ActualWidth <= width + 1 && element.ActualHeight <= height + 1))
        {
            // Text is measured into the room it has: one line with no ellipsis that does not fit is "clipped".
            return null;
        }

        var at = Box(element);
        return new Rect(at.X, at.Y, Math.Min(at.Width, width), Math.Min(at.Height, height));
    }

    /// <summary>cut-right, cut-bottom: something the text sits in was given less room than it takes, and the text in the rest is not drawn.</summary>
    private static void Cut(string step, TextBlock text, Rect box)
    {
        // A closed pane (a NavigationView showing only its icons) hides its texts on purpose.
        if (Within<SplitView>(text, split => !split.IsPaneOpen && split.Pane is { } pane && (ReferenceEquals(pane, text) || IsAncestor(pane, text))))
        {
            return;
        }

        foreach (var node in Ancestors(text).OfType<FrameworkElement>())
        {
            if (node is ScrollViewer)
            {
                // Scrolled out of view is not cut.
                box.Intersect(Box(node));
            }

            if (box.IsEmpty || Room(node) is not { } room)
            {
                continue;
            }

            var (right, bottom) = (Math.Min(box.Width, box.Right - room.Right), Math.Min(box.Height, box.Bottom - room.Bottom));
            if (right > 2 || bottom > 2)
            {
                var (kind, lost, size, side) = right > 2 ? ("cut-right", right, box.Width, "wide") : ("cut-bottom", bottom, box.Height, "tall");
                Report(step, kind, $"\"{text.Text}\" loses {lost:0} of its {size:0} px {side}: its {node.GetType().Name} takes {node.ActualWidth:0}x{node.ActualHeight:0} and has room for {room.Width:0}x{room.Height:0} ({Locate(node)})");
                return;
            }

            box.Intersect(room);
        }
    }

    private static bool IsInput(Control control) =>
        control is TextBox or PasswordBox or ComboBox or AutoSuggestBox or NumberBox or CalendarDatePicker or DatePicker or TimePicker or Slider or ToggleSwitch or RatingControl;

    // What a screen reader calls the button: its AutomationProperties.Name, else its text.
    private static string? NameOf(ButtonBase button) =>
        AutomationProperties.GetName(button) is { Length: > 0 } name ? name
        : button is AppBarButton { Label.Length: > 0 } bar ? bar.Label
        : Label(button);

    private static string? Label(ContentControl control) => control.Content switch
    {
        string text when IsWords(text) => text,
        _ => Descendants(control).OfType<TextBlock>().FirstOrDefault(t => IsWords(t.Text) && !Within<IconElement>(t))?.Text,
    };

    // Text a person reads: an icon's glyph and a bullet are not.
    private static bool IsWords(string? text) => text is not null && text.Any(char.IsLetterOrDigit);

    /// <summary>Where the element is: its type and automation id or x:Name (else the named elements around it), and its page.</summary>
    private static string Locate(DependencyObject element)
    {
        var page = Ancestors(element).OfType<Page>().FirstOrDefault() is { } found ? " on " + found.GetType().Name : "";
        if (Id(element) is { } own)
        {
            return $"{element.GetType().Name} {own}{page}";
        }

        var around = Ancestors(element).Select(Id).OfType<string>().Take(2).Reverse().ToList();
        return element.GetType().Name + (around.Count > 0 ? " in " + string.Join("/", around) : "") + page;
    }

    private static string? Id(DependencyObject element) =>
        AutomationProperties.GetAutomationId(element) is { Length: > 0 } id ? id : element is FrameworkElement { Name.Length: > 0 } named ? named.Name : null;

    private static string Slug(string name) => Regex.Replace(name, "[^A-Za-z0-9]+", "-").Trim('-').ToLowerInvariant() is { Length: > 0 } slug ? slug : "step";

    // A resource key shown as text: Tasks_Title, Pages_Save.
    private static readonly Regex KeyPattern = new(@"^[A-Z][A-Za-z]+_[A-Za-z0-9_]+$");

    // ---- The picture ----

    /// <summary>The window's content, then each open popup (a flyout, a dialog, a menu) where it shows.</summary>
    private static async Task PictureAsync(FrameworkElement root, string path)
    {
        var (page, width, height) = await RenderAsync(root);
        if (width == 0 || height == 0)
        {
            return;
        }

        var scale = root.ActualWidth > 0 ? width / root.ActualWidth : 1;
        var shade = Application.Current.RequestedTheme == ApplicationTheme.Dark ? (byte)0 : (byte)255;
        var canvas = new byte[width * height * 4];
        Array.Fill(canvas, shade);
        Draw(canvas, width, height, page, width, height, 0, 0);
        foreach (var popup in Popups(root))
        {
            if (popup.Child is FrameworkElement { ActualWidth: > 0, ActualHeight: > 0 } child)
            {
                var (layer, layerWidth, layerHeight) = await RenderAsync(child);
                var at = child.TransformToVisual(null).TransformPoint(new Point(0, 0));
                Draw(canvas, width, height, layer, layerWidth, layerHeight, (int)Math.Round(at.X * scale), (int)Math.Round(at.Y * scale));
            }
        }

        WritePng(path, canvas, width, height);
    }

    private static async System.Threading.Tasks.Task<(byte[] Pixels, int Width, int Height)> RenderAsync(UIElement element)
    {
        var bitmap = new RenderTargetBitmap();
        await bitmap.RenderAsync(element);
        return ((await bitmap.GetPixelsAsync()).ToArray(), bitmap.PixelWidth, bitmap.PixelHeight);
    }

    // Source over, both BGRA with the alpha already multiplied in, as RenderTargetBitmap gives them.
    private static void Draw(byte[] canvas, int width, int height, byte[] layer, int layerWidth, int layerHeight, int left, int top)
    {
        for (var y = Math.Max(0, -top); y < layerHeight && y + top < height; y++)
        {
            for (var x = Math.Max(0, -left); x < layerWidth && x + left < width; x++)
            {
                var (from, to) = ((y * layerWidth + x) * 4, ((y + top) * width + x + left) * 4);
                var rest = 255 - layer[from + 3];
                for (var channel = 0; channel < 4; channel++)
                {
                    canvas[to + channel] = (byte)Math.Min(255, layer[from + channel] + canvas[to + channel] * rest / 255);
                }
            }
        }
    }

    // A PNG by hand (8-bit RGB), so the check needs no imaging package.
    private static void WritePng(string path, byte[] bgra, int width, int height)
    {
        using var data = new MemoryStream();
        using (var zlib = new ZLibStream(data, CompressionLevel.Fastest, leaveOpen: true))
        {
            var row = new byte[1 + width * 3];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var from = (y * width + x) * 4;
                    (row[1 + x * 3], row[2 + x * 3], row[3 + x * 3]) = (bgra[from + 2], bgra[from + 1], bgra[from]);
                }

                zlib.Write(row);
            }
        }

        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        (header[8], header[9]) = (8, 2);
        using var file = File.Create(path);
        file.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        Chunk(file, "IHDR", header);
        Chunk(file, "IDAT", data.ToArray());
        Chunk(file, "IEND", []);
    }

    private static void Chunk(Stream file, string type, byte[] data)
    {
        byte[] body = [.. Encoding.ASCII.GetBytes(type), .. data];
        var crc = 0xFFFFFFFFu;
        foreach (var value in body)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
            }
        }

        var number = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(number, (uint)data.Length);
        file.Write(number);
        file.Write(body);
        BinaryPrimitives.WriteUInt32BigEndian(number, ~crc);
        file.Write(number);
    }
}
#else
// Not a Debug desktop build: the one line in App.xaml.cs still compiles, and does nothing.
internal static class PageCheck
{
    public static System.Threading.Tasks.Task RunIfAskedAsync(object? window, System.Func<System.Threading.Tasks.Task>? then = null) => System.Threading.Tasks.Task.CompletedTask;

    public static System.Threading.Tasks.Task SnapAsync(string name) => System.Threading.Tasks.Task.CompletedTask;
}
#endif
