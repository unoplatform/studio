# MVUX Mocking

`Uno.HotTesting.Reactive` carries a source generator that emits, for every MVUX model it finds:

- `record {Model}Mock` — its **required** members are the model's service-dependent feeds, its **optional** members are the feeds derived from them, and `{Model}Mock.Empty` pins every input to empty.
- `static partial class {Vm}Mock` — `Create()` and `Create({Model}Mock)` build the **real** view-model over the real model, with every constructor parameter null-injected but an `IMessenger`, which gets a fresh messenger, then apply the mock.
- `SetMock(this {Vm}, {Model}Mock)` — swaps the mocked feeds on a live view-model.

Only the members you set are replaced. A derived feed computes over the mocked inputs, so the required members are enough: set a derived one only to override what it computes.

**Contents:** [Workflow](#workflow) · [Critical Rules](#critical-rules) · [Key Principles (Stable)](#key-principles-stable)

## Workflow

### Step 1: Check that the package is there — don't add it

The Uno.Sdk references `Uno.HotTesting.Reactive` itself in every Debug build of a project with `MVUX` in `<UnoFeatures>`, at the app's Uno.Extensions version and with `PrivateAssets="all"`. Don't add a `PackageReference` or `dotnet add package` to such a project: it duplicates the SDK's, and without its condition it ships in Release builds.

- **The mocks exist only in Debug builds.** The SDK drops the package when the build is optimized (`Optimize=true`, as in Release), and every generated mock goes with it. Code that uses a mock compiles in Release only from `HotDesignPreviews/`, which Release builds also leave out, or from a project that Release never builds.
- **No `{Model}Mock` type in a Debug build?** Check the `.csproj` and `Directory.Build.props` for `<UnoDisableHotTesting>true</UnoDisableHotTesting>`, which turns the reference off, and for an Uno.Extensions version override older than 7.4, which makes the SDK skip it with `UNOB0028`.
- **A project without the `MVUX` feature**, such as a separate test project, gets no implicit reference. Reference the package there yourself, at the app's Uno.Extensions version (`dotnet list package --include-transitive` shows it).

### Step 2: Write against the generated names

The mock types are generated at build time, but you can write against them before building. Names follow the model:

| Model | View-model | Mock record | Factory class |
| --- | --- | --- | --- |
| `RecipeModel` | `RecipeViewModel` | `RecipeModelMock` | `RecipeViewModelMock` |

They are emitted in the model's namespace. Read the members off the model, with no trial build:

- A feed that uses a service, or any other constructor parameter, is a **required** member.
- A feed computed from another feed of the model is an **optional** (derived) member, even if it also uses a service.
- A feed that uses neither, such as a local `State.Value(this, ...)`, is not in the mock.

Then build once in Debug: a CS9035 error names any required member you missed. Only if a member is still unclear, build with `-p:EmitCompilerGeneratedFiles=true` and read the model's `*.Mock.g.cs` under `obj/<configuration>/<tfm>/generated/Uno.HotTesting.Reactive.Generator/`.

`{Name}ViewModel` is the default view-model naming. An app that sets `[assembly: BindableGenerationTool(1)]` gets the older `Bindable{Model}` names instead, which the generated mocks do not match.

### Step 3: Create a mocked view-model

```csharp
using Uno.HotTesting.Reactive;

// Given: public partial record RecipeModel(IRecipeService Service)
//   { public IListFeed<Step> Steps => ...;  public IFeed<int> StepsCount => Steps.Select(...); }

var vm = RecipeViewModelMock.Create(new RecipeModelMock
{
    Steps = ListFeedMock.Value(step1, step2, step3), // required: fed by the service
    // StepsCount is derived: it computes 3 over the mocked Steps
});
```

The feed states, on `FeedMock` (single value) and `ListFeedMock` (lists):

| State | `FeedMock` | `ListFeedMock` |
| --- | --- | --- |
| Data | `Value(x)` | `Value(a, b, c)` |
| Empty | `Empty<T>()` | `Empty<T>()` |
| Loading | `Loading<T>()` | `Loading<T>()` |
| Error | `Error<T>(exception)` | `Error<T>(exception)` |
| Refreshing | `Refreshing(stale)` | `Refreshing(a, b)` |
| Not yet emitted | `Undefined<T>()` | `Undefined<T>()` |

`Message<T>(msg => ...)` builds any other message. That is the whole API, so there is no need to inspect the package.

`Empty` is the no-data state — what a service returning nothing produces — not a value holding an empty list. A `FeedView` shows its `NoneTemplate` for it, and nothing at all when the page defines none, so the empty state of such a page is blank by design. When the user asks for that state, tell them, and offer to add a `NoneTemplate` — it changes their page, so don't add one unasked.

### Step 4: Walk through states

`Create` returns the view-model; `SetMock` changes what it shows. `with` over `Empty` sets only the members that differ:

```csharp
vm.SetMock(RecipeModelMock.Empty with
{
    Steps = ListFeedMock.Loading<Step>(),
});
```

## Critical Rules

- **A feed must reach its service through a lambda, not a method group.** `Feed.Async(Service.GetItems)` dereferences `Service` as soon as the property is read, and `Create` null-injects it — so `Create` throws, and inside a preview the only symptom is a blank page. Check every feed of the model before mocking it, rewrite each method group as `Feed.Async(async ct => await Service.GetItems(ct))` (same for `ListFeed.Async`), and tell the user you changed their model. The two forms behave the same at runtime.
- **Give the page the view-model, not the model.** `{Vm}Mock.Create(...)` returns the generated view-model, which is what the page binds to.
- **A state input is mocked as a feed** — assign `FeedMock.*` to an `IState<T>` and `ListFeedMock.*` to an `IListState<T>`, not a `State`.
- **A list with `.Selection(...)` can't be mocked yet** (unoplatform/uno.extensions#3247): whatever the mock sets, the list runs its real feed against the null-injected service and shows its error. Tell the user, and for that page offer the fallback below.
- **Use mocks only from previews and tests.** In an app project, use them from `HotDesignPreviews/` or from test code, never from the app's own pages or models: those fail to compile in Release, where the mocks don't exist.
- **`Create` null-injects every constructor parameter but an `IMessenger`.** A model that dereferences a service inside its constructor throws `NullReferenceException` from `Create`; keep model constructors to assignments, plus the messaging pattern's `messenger.Observe(...)`. The messenger `Create` passes is a fresh one, so a preview never receives the app's messages.
- **No mock generated?** Read the diagnostics before working around it:
  - `MOCK0001` (warning) — the view-model exposes no constructor `Create` can call.
  - `MOCK0002` (information) — the model has feeds, but none is fed by a constructor parameter, so there is nothing to mock. It is hidden at default `dotnet build` verbosity: look in the IDE error list or build with `-v normal`. If a feed does reach a service by a route the analysis cannot see, declare it: `[FeedDependency("Member", OnParameter = "service")]` (namespace `Uno.Extensions.Reactive.Config`).
  - `MOCK0003` (warning) — an `[assembly: ImplicitBindables(...)]` pattern is not a valid regular expression.
- **Fallback when no mock can exist:** construct the real view-model over a hand-written in-memory service — `new RecipeViewModel(new FakeRecipeService())`. This runs the real feeds, derived ones included, so it can show data and empty states but not loading or error.

## Key Principles (Stable)

- A mock replaces the members you set on a real view-model over a real model; every service is null-injected.
- Mocking is only active inside `Create`; nothing is wrapped outside it, so the runtime cost for the rest of the app is nil.
- `[assembly: EnableFeedMocking(IsEnabled = false)]` turns generation off entirely.
- A model nested inside another type is only mocked from a project that references it, not from its own project.

## Related Skills

- `uno-platform` (`references/previews.md`) — Put the mocked view-model in a preview
- `references/feed-basics.md` — What the mocked feeds stand in for (IFeed<T>)
- `references/state-basics.md` — IState<T>, mocked through its feed interface
- `references/feedview.md` — How a FeedView renders each mocked state
- `uno-testing` (`references/assertions.md`) — Asserting on the UI once it is driven by a mock
