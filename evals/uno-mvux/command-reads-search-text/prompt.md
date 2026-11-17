---
description: A search command stays disabled because its parameter does not match the state by name, so MVUX treats it as a CommandParameter that is null.
tags: [uno-mvux, commands]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
Uno Platform app, MVUX. My page model has the text box text in `public IState<string> Query => State<string>.Value(this, () => string.Empty);` and a `public async ValueTask Search(string text, CancellationToken ct)` method that calls my search service. The Search button is bound to it, but it stays greyed out and never runs, even after I've typed something. Why, and what's the fix?
