---
name: uno-mvux-mocking
description: "Generate typed mocks of MVUX view-models with Uno.HotTesting.Reactive, to show a page in any feed state without its real services."
when_to_use: "Use when a page or control bound to an MVUX view-model needs deterministic data without calling its real services — for a preview, a UI test, a screenshot, or a demo — or when asked to show a page loading, failing, empty, refreshing, or with specific data. Also use to explain the MOCK0001, MOCK0002 or MOCK0003 diagnostics. To put the mocked view-model in a preview, pair with `uno-previews`. For a page with no MVUX view-model, the `FeedMock` / `ListFeedMock` factories below are enough on their own."
metadata:
  author: uno-platform
  version: "1.0"
  category: mvux
---

# MVUX Mocking — Agent Skill

Referencing `Uno.HotTesting.Reactive` makes a source generator emit, for every MVUX model it finds:

- `record {Model}Mock` — its **required** members are the model's service-dependent feeds, its **optional** members are the feeds derived from them, and `{Model}Mock.Empty` pins every input to empty.
- `static partial class {Vm}Mock` — `Create()` and `Create({Model}Mock)` build the **real** view-model over the real model, with every constructor parameter null-injected, then apply the mock.
- `SetMock(this {Vm}, {Model}Mock)` — swaps the mocked feeds on a live view-model.

Only the members you set are replaced. A derived feed does not recompute from mocked inputs, so set the derived members the page shows as well.

## Workflow

### Step 1: Reference the package

Add `Uno.HotTesting.Reactive` to the project that holds the models, or to a test or preview project that references it. Both work.

- **Use version 8.0.0-dev.14 or later.** No earlier version — the whole 7.x line included — ships the generator: it installs and builds, then generates nothing and says nothing. Each version brings the `Uno.Extensions.Reactive` of the same version with it, so take the newest 8.0 version; there is nothing else to match.
- **Check the app's Uno.Extensions version first** (`dotnet list package --include-transitive`). In an app still on Uno.Extensions 7.x, the package lifts `Uno.Extensions.Reactive` and `Uno.Extensions.Core` to 8.0 while every other Uno.Extensions package stays on 7.x. That mixed install can build and run, but Uno does not release those versions together. Tell the user before adding it, and offer the fallback under Critical Rules instead.

### Step 2: Build, then use the generated names

The mock types exist only after a build. Names follow the model:

| Model | View-model | Mock record | Factory class |
| --- | --- | --- | --- |
| `RecipeModel` | `RecipeViewModel` | `RecipeModelMock` | `RecipeViewModelMock` |

They are emitted in the model's namespace. Let the compiler tell you the required members rather than guessing them.

`{Name}ViewModel` is the default view-model naming. An app that sets `[assembly: BindableGenerationTool(1)]` gets the older `Bindable{Model}` names instead, which the generated mocks do not match.

### Step 3: Create a mocked view-model

```csharp
using Uno.HotTesting.Reactive;

// Given: public partial record RecipeModel(IRecipeService Service)
//   { public IListFeed<Step> Steps => ...;  public IFeed<int> StepsCount => Steps.Select(...); }

var vm = RecipeViewModelMock.Create(new RecipeModelMock
{
    Steps = ListFeedMock.Value(step1, step2, step3), // required: fed by the service
    StepsCount = FeedMock.Value(3),                  // derived: set it too, see Critical Rules
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

`Empty` is the no-data state — what a service returning nothing produces — not a value holding an empty list. A `FeedView` shows its `NoneTemplate` for it, and nothing at all when the page defines none, so the empty state of such a page is blank by design. When the user asks for that state, tell them, and offer to add a `NoneTemplate` — it changes their page, so don't add one unasked.

### Step 4: Walk through states

`Create` returns the view-model; `SetMock` changes what it shows. `with` over `Empty` sets only the members that differ:

```csharp
vm.SetMock(RecipeModelMock.Empty with
{
    Steps = ListFeedMock.Loading<Step>(),
    StepsCount = FeedMock.Loading<int>(),
});
```

## Critical Rules

- **A feed must reach its service through a lambda, not a method group.** `Feed.Async(Service.GetItems)` dereferences `Service` as soon as the property is read, and `Create` null-injects it — so `Create` throws, and inside a preview the only symptom is a blank page. Check every feed of the model before mocking it, rewrite each method group as `Feed.Async(async ct => await Service.GetItems(ct))` (same for `ListFeed.Async`), and tell the user you changed their model. The two forms behave the same at runtime.
- **Give the page the view-model, not the model.** `{Vm}Mock.Create(...)` returns the generated view-model, which is what the page binds to.
- **An `IState<T>` input is mocked as `IFeed<T>`** — assign `FeedMock.*` to it, not a `State`.
- **Set every derived member the page shows, not only the required ones.** A derived feed left unset still reads the real input, not the mock, and fails on the null-injected service — the page shows nothing for it. Give it the value it would compute (`StepsCount = FeedMock.Value(3)`), and the matching state in a loading or error mock. `{Model}Mock.Empty` sets only the required members.
- **Keep mock usage out of shipping code.** In an app project, use it from `HotDesignPreviews/` (excluded from Release builds by the Uno SDK) or from test code. The generated `{Model}Mock` types are still compiled into the app itself.
- **`Create` null-injects every constructor parameter.** A model that dereferences a service inside its constructor throws `NullReferenceException` from `Create`; keep model constructors to assignments.
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

- [[uno-previews]] — Put the mocked view-model in a preview
- [[uno-mvux-feed-basics]] — What the mocked feeds stand in for (IFeed<T>)
- [[uno-mvux-state-basics]] — IState<T>, mocked through its feed interface
- [[uno-mvux-feedview]] — How a FeedView renders each mocked state
- [[uno-testing-assertions]] — Asserting on the UI once it is driven by a mock
