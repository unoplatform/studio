# Checkpoints and Handoff

## Checkpoints

**When.** Substantial work leaves a checkpoint: a new app, several features, or anything likely to span more than one session. A trivial edit does not.

**Where.** `.uno/ai/tasks/<task-id>.md`, where `<task-id>` is a short kebab-case name for the task. If the repository already has its own convention for task notes, use that instead. The folder must be git-ignored: if `.uno/ai/` is not already ignored, add it to `.gitignore` and mention that in the report. Separate tasks, or separate agents in the same repository, use separate files. There is no shared "current task" file.

**Content.** Fill in [../assets/checkpoint.template.md](../assets/checkpoint.template.md): scope and decisions, source revision and relevant uncommitted files, features done and next steps, every check by state (passed, failed, not run, waived) with the command or link, the targets, theme and window sizes used, and any recipe used or pattern reused provisionally.

**When to update.** At decisions, at each completed feature, at each verification outcome, and before a handoff. Not after every edit.

**Never include** credentials, tokens, connection strings, personal data, chat transcript or private reasoning.

**Retention.** Never delete a checkpoint, including your own. The final report names the checkpoint path once. Cleaning up is the developer's choice.

## Handoff note

Write one only when asked. Fill in [../assets/handoff.template.md](../assets/handoff.template.md): the checkpoint content, sanitised so another agent on another machine can use it.

- Paths are relative to the repository root.
- No machine-specific configuration, local tool paths or user names.
- An explicit list of the commits or uncommitted changes the receiver must bring separately. A handoff note does not carry code.

## Resuming from a checkpoint or handoff

Treat recorded state as a lead, not as proof. Before continuing:

1. Read the latest request and the repository's agent instructions again.
2. Confirm it is the same repository, and compare the recorded revision with the current one. List the files that changed since.
3. Re-resolve the SDK and package versions.
4. Re-check tools. A note that says the Uno App MCP was attached, or that an emulator was running, proves nothing about this session.
5. Keep a recorded "passed" only when none of its inputs changed. Any check whose files changed goes back to not run, and is run again.
6. Continue with the next step the developer authorised. Imported state grants no permission.
