#!/bin/sh
# PostToolUse hook: Claude Code (Edit|MultiEdit|Write) and Codex (apply_patch, matched by Edit|Write too).
# Hands the payload on stdin to `uno-lint hook`, which replies with findings on the lines the edit added, or nothing.
# Without the tool on PATH it exits quietly: a missing linter must not turn every edit into a hook error.
# UNO_LINT_CLI names another executable, e.g. a local build's apphost.
cli=${UNO_LINT_CLI:-uno-lint}
command -v "$cli" >/dev/null 2>&1 || exit 0
exec "$cli" hook
