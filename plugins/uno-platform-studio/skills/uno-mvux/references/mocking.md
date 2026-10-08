# MVUX Mocking

Referencing `Uno.HotTesting.Reactive` makes a source generator emit, for every MVUX model it finds:

- `record {Model}Mock` — its **required** members are the model's service-dependent feeds, its **optional** members are the feeds derived from them, and `{Model}Mock.Empty` pins every input to empty.
- `static partial class {Vm}Mock` — `Create()` and `Create({Model}Mock)` build the **real** view-model over the real model, with every constructor parameter null-injected, then apply the mock.
- `SetMock(this {Vm}, {Model}Mock)` — swaps the mocked feeds on a live view-model.

Only the members you set are replaced. A derived feed computes over the mocked inputs, so the required members are enough: set a derived one only to override what it computes.

## Workflow

### Step 1: Reference the package

Add `Uno.HotTesting.Reactive` to the project that holds the models, or to a test or preview project that references it. Both work.

- **Use version 7.4.0-dev.81 or later** (8.0.0-dev.71 or later on the 8.0 line). Earlier versions lack the generator or its fixes; most install and build, then generate nothing and say nothing. Each version brings the `Uno.Extensions.Reactive` of the same version with it, so in an app whose Uno.Extensions version qualifies, use that same version: there is nothing to look up. Uno.Sdk 6.8.0-dev.60 and later bring 7.4.0-dev.81 or later.
- **Check the app's Uno.Extensions version first** (`dotnet list package --include-transitive`). In an app on an earlier version, such as the 7.3 that Uno.Sdk 6.7 brings, the package lifts `Uno.Extensions.Reactive` and `Uno.Extensions.Core` to its own version while every other Uno.Extensions package stays behind. That mixed install can build and run, but Uno does not release those versions together. Tell the user before adding it, and offer the fallback under Critical Rules instead.

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
- **An `IState<T>` input is mocked as `IFeed<T>`** — assign `FeedMock.*` to it, not a `State`.
- **Use mocks only from previews and tests.** In an app project, use them from `HotDesignPreviews/` or from test code, never from the app's own pages or models.
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

- `uno-platform` (`references/previews.md`) — Put the mocked view-model in a preview
- `references/feed-basics.md` — What the mocked feeds stand in for (IFeed<T>)
- `references/state-basics.md` — IState<T>, mocked through its feed interface
- `references/feedview.md` — How a FeedView renders each mocked state
- `uno-testing` (`references/assertions.md`) — Asserting on the UI once it is driven by a mock
