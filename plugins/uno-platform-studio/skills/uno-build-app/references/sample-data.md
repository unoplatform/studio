# Sample Data Behind the Service Interface

When the brief asks for an app that runs offline, on sample data, or before its backend is reachable, build the sample data as a second implementation of the data service, not as a separate code path in the Models. The live implementation stays real and complete (rule 8 of the skill): sample mode is a labelled mode the user picks, not a stand-in for a required integration.

## The shape

- **One interface for the data the app uses.** `ISalesDataSource` with `LoadAsync`, `SaveOpportunityAsync`, `DeleteOpportunityAsync`... Models and rules see only this interface, through a store or repository that picks the active source.
- **Two implementations.** The live one over the generated client (the `uno-kiota` skill); the sample one over a seed set in code with fixed ids, so screens, tests and UI checks can name the same records every run.
- **The sample source persists.** What the app presents as saved survives a relaunch: write the set as JSON to a file in the app's local data folder (`<LocalAppData>/<App>/sample-data.json`), through a temporary file then a move, so an interrupted write never leaves half a file. Take the path as a constructor parameter so tests use their own file.
- **A missing or unreadable file is reseeded.** Catch the read failure, keep the bad file aside (`sample-data.json.bad`), log a warning and start from the seed. Never crash on it, never show an empty app.
- **Reset.** A "Reset sample data" action in Settings rewrites the seed and refreshes the lists. The user can always get back to a known state.
- **The sample source behaves like the live one.** It assigns ids on create, applies the same cascades (deleting a parent removes its children if the server does) and rejects what the server would reject, so a journey checked on sample data means something.

## The data mode

A runtime setting, persisted with the user's preferences:

```csharp
public enum DataMode { Sample, Live, Auto }
```

- **Sample is the safe default.** A first launch with no credentials, and any launch the user did not switch, runs on sample data.
- **Live only when credentials are present.** Read them from the environment or the platform's secure store, never from source. With none, the Live choice is disabled and says why.
- **Auto, if offered, means Live when credentials are present.** Say so in Settings: a machine that has them set starts against the live environment.
- **Show the mode.** The shell or Settings names the active source ("Sample data (offline)", or the environment's host) so nobody mistakes one for the other. Switching mode, signing out or resetting refreshes every list.

## Writes and verification

- **Writes are tested on sample data.** Unit tests cover the sample source over a temporary file: seeding, a change surviving a new instance over the same file, the corrupt-file fallback, reset, cascades.
- **Never send a write to a live environment the user has not cleared.** Exercise live mode read-only (lists, details) unless the developer has explicitly allowed writes to that environment, and then restore what you changed. A live write path that was not run is reported as unverified, not passed.
- **UI checks start in an explicit mode** (the `uno-testing` skill, `references/without-app-mcp.md`): clear the credential variables for a sample run, and reset the sample file first.
