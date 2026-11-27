# Hot Testing: tests written inside the app

Hot Testing tests are MSTest tests that live in the app's own project and run inside the running app, on its UI thread: the tool hosting the app runs them, so they need no separate test project and no App MCP. Use them when the user asks for tests that stay with the app; use the `uno_app_*` workflow in `SKILL.md` to check a change once.

**Contents:** [Turn the feature on](#turn-the-feature-on) · [Where tests go](#where-tests-go) · [Control or app test](#control-or-app-test) · [A control test](#a-control-test) · [An app test](#an-app-test)

## Turn the feature on

1. Read `<UnoFeatures>` in the app's `.csproj`. If `HotTesting` is missing, add it to the list.
2. Never add `Uno.HotTesting`, `MSTest`, `MSTest.TestFramework` or `MSTest.TestAdapter` as a `PackageReference`, and never `dotnet add package` them. The feature references them in Debug builds only, with `PrivateAssets="all"`; a hand-added reference ships them in Release.
3. If the project sets `<UnoDisableHotTesting>true</UnoDisableHotTesting>`, in the `.csproj` or a `Directory.Build.props`, the feature is off: ask the user before removing it.

The Windows (WinAppSDK) target never gets Hot Testing. Build and run the tests on desktop, WebAssembly, iOS or Android.

## Where tests go

- Every test, fake and test helper goes in the project's `Tests/` folder, or the folder `HotTestingTestsFolder` names.
- Release builds leave that folder out, and the package with it. A test type anywhere else breaks the Release build with `CS0246` or `CS0234` on `TestClass`, `TestContext` or `Microsoft.VisualStudio.TestTools`: move it into the folder.
- App code never references a type from the tests folder.
- Everything in that folder is left out of Release, not only tests. If `Tests/` already holds app code, set `<HotTestingTestsFolder>` to another name before turning the feature on.
- A file added with an explicit `<Compile Include="...">` still compiles in Release. Don't add one for a test.

## Control or app test

| The test | Kind | Class attribute | One method |
|---|---|---|---|
| Shows controls by themselves: a control, a `UserControl`, a page with its data set by the test | Control test | `[ControlTestClass]` | `[ControlTestMethod]` |
| Starts the whole app and goes through it as a user does | App test | `[AppTestClass]` | `[AppTestMethod]` |

- Use these attributes in place of `[TestClass]` and `[TestMethod]`. A plain `[TestMethod]` that touches the UI runs off the UI thread, and nothing it showed is cleaned up.
- Under `[ControlTestClass]` or `[AppTestClass]`, a method's plain `[TestMethod]`, with or without `[DataRow]`, runs as that kind.
- Keep plain `[TestClass]` / `[TestMethod]` for logic with no UI, such as a converter or a model's validation.
- `HotTestingContext.Current` gives the test its context: `.Canvas` for control tests, `.App` for app tests. Use the class's `public TestContext TestContext { get; set; }` property for `TestContext.CancellationToken`.

Add `using Uno.HotTesting;` and `using Microsoft.VisualStudio.TestTools.UnitTesting;` to each test file.

## A control test

```csharp
[ControlTestClass]
public sealed class Given_PriceTag
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task When_price_is_zero_shows_free()
    {
        var tag = new PriceTag { Price = 0m };

        await using var shown = await HotTestingContext.Current.Canvas.ShowAsync(
            tag,
            new CanvasShowOptions { Size = new Size(360, 120), Theme = ElementTheme.Dark },
            TestContext.CancellationToken);

        await Assert.IsTrueAsync(() => tag.DisplayText == "Free", TestContext.CancellationToken);
    }
}
```

- Create the element in the test. `ShowAsync` adds it to the window and waits for `Loaded`, so it is measured and arranged.
- `Size` and `Theme` on `CanvasShowOptions` are optional. Without them, the element is sized to its content in the app's theme.
- One element is shown at a time. Dispose it (`await using`) before showing the next. Whatever the test leaves shown is removed when it ends.
- Wait for a state with `Assert.IsTrueAsync(() => ..., ct)`, which retries until the condition holds or times out. Never use `Task.Delay`.

## An app test

```csharp
[AppTestClass]
public sealed class Given_App
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task When_started_shows_the_home_page()
    {
        await using var app = await HotTestingContext.Current.App.RunAsync(
            new AppRunOptions(), TestContext.CancellationToken);

        await Assert.IsTrueAsync(
            () => FindNamed<TextBlock>(app.RootElement, "WelcomeText") is { Text.Length: > 0 },
            TestContext.CancellationToken);
    }

    private static T? FindNamed<T>(DependencyObject root, string name) where T : FrameworkElement
    {
        if (root is T element && element.Name == name)
        {
            return element;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            if (FindNamed<T>(VisualTreeHelper.GetChild(root, i), name) is { } found)
            {
                return found;
            }
        }

        return null;
    }
}
```

- `RunAsync` returns once the app's first navigation has finished. Dispose the result (`await using`) before the next run.
- Find elements from `app.RootElement` by `x:Name`, and use `app.Window`, `app.Instance` and `app.Services` rather than `Application.Current`, which can be the host's.
- Act as a user does, through automation peers: `new ButtonAutomationPeer(button).Invoke()`.
- If the app keeps static state, such as a cache, a counter or a singleton, clear it in `AppRunOptions.Cleanup`, which runs once the app exits. Otherwise the next test's app starts with it.
- A host that doesn't run apps, such as plain `dotnet test`, reports app tests as skipped. They are not failures.
