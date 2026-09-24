# Verification

Every check ends in exactly one of four states. Use these words in the checkpoint and the report:

| State | Meaning |
|---|---|
| **passed** | It ran, and the result shows the expected behaviour |
| **failed** | It ran, and the result does not. A failure is never reported as "unavailable" |
| **not run** | It could not run here. Always give the reason: missing workload, no emulator, no Uno App MCP, and so on |
| **waived** | The developer, or a standing project policy, explicitly said not to run it. Record who. Never report it as passed |

## Which targets

Task-relevant targets are the declared targets the change can affect. Shared code, XAML, models and services affect all of them. Build and exercise every task-relevant target that can run on this machine, and report each remaining one as **not run** with the reason. Never remove a target from the project to make the list shorter.

| Target | Build | Runtime |
|---|---|---|
| Skia Desktop | `dotnet build -f net10.0-desktop` | The Uno App MCP, or the app's runtime tests |
| WebAssembly | `dotnet build -f net10.0-browserwasm` | The Uno App MCP with a browser |
| Android | `dotnet build -f net10.0-android` (needs the Android workload and SDK) | An emulator or device through the Uno App MCP |
| iOS | `dotnet build -f net10.0-ios` (needs macOS and Xcode) | A simulator or device through the Uno App MCP |

Use the target framework monikers the project actually declares; the table shows the .NET 10 names. A shared Skia renderer does not make one platform's pass count for another. Record which device, emulator or browser actually ran; a simulator run is not device coverage.

`uno-check` reports missing workloads and SDKs. Installing them changes the machine, so it follows the host's approval policy; otherwise report the target as not run and give the developer the command.

## Levels of evidence

1. **Build** proves the code compiles and the XAML parses for that target. Nothing more.
2. **Unit and service tests** prove rules, ordering, identity and failure handling without the UI. Run them with `dotnet test`.
3. **Runtime checks** prove that the rendered controls invoke and update the intended state. They need the app running and something that can drive it: the Uno App MCP through the `uno-testing-ui` and `uno-testing-assertions` skills, or runtime tests the project already has.

A screenshot proves rendering, not behaviour. A persistence check changes the value, closes the app, relaunches it, and asserts the value came back. Recreating a model over the same in-memory data is not a persistence check.

## Without the Uno App MCP

Do not launch the app just to "have a look". A window you cannot screenshot or click verifies nothing. Instead:

1. Move decisions into plain methods and cover them with unit tests.
2. Use runtime tests if the project already has them.
3. Report every behaviour that was never seen on screen as a runtime check **not run**. "Tests pass" and "it works" are different claims.

## Making tests trustworthy

- **See it fail first.** Before trusting a new test, break the behaviour it guards (comment out the fix, or return the wrong value), run the test, watch it fail, then restore. A test never seen to fail has not been shown to test anything.
- **Await conditions, not time.** Fixed delays are not evidence. MVUX generated wrappers can take several dispatcher turns to reflect a feed change, so poll the property with a bounded timeout instead of reading it once.
- **Isolate data.** Tests use their own storage location and never touch the developer's real settings or a production service.

## Practical traps

- **`uno_app_start` builds before launching.** A build-time configuration change must be in place at launch time, not reverted before it.
- **A running app locks its output.** Build errors `MSB3027` or `MSB3021` usually mean an earlier instance is still running. Close it (`uno_app_close`) before building again.
- **Stop what you start.** Close every app instance, emulator session or server your checks launched.

## Provisional reuse

The first representative feature is exercised before its pattern is repeated. When runtime checks are not available but the build and relevant service tests pass, you may reuse the pattern provisionally. Record, per feature, which runtime checks are pending. When runtime checks become available, verify the representative feature first and carry any repair to the features that copied it. Never repeat a pattern that has an observed, unresolved runtime failure.

## Failures you did not cause

Compare against the baseline you recorded during discovery. A failure that was there before your change is reported as pre-existing, with that evidence, separately from anything your change broke. Do not repair it unless the requested work needs it. If you cannot tell, say the attribution is uncertain.

## Release build

For a new app, also build `-c Release` for the targets available, since trimming, AOT and resource packaging only show up there. Record what could not run. No signing, store submission or deployment unless asked.
