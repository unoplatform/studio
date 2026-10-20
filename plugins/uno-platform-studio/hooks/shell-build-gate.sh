#!/bin/sh
# PreToolUse on Bash|PowerShell. When the App MCP route is available (the DevServer CLI is installed), a shell
# `dotnet build` of a project is the same build uno_app_start is about to do, and its output stays in context for
# the rest of the session: skill-ab catalog sessions ran 5-6 of them each, 10-14k tokens on every later call
# (unoplatform/private#1217). Deny it and say what to do instead. `# build-only` in the command is the one
# documented escape, for a target that cannot run on this machine. Anything the hook cannot parse is allowed.
input=$(cat)
cmd=$(printf '%s' "$input" | sed -En 's/.*"command" *: *"(.*)".*/\1/p')
[ -n "$cmd" ] || exit 0
printf '%s' "$cmd" | grep -Eq '(^|[^A-Za-z0-9_./-])dotnet +build([^A-Za-z0-9_-]|$)' || exit 0
printf '%s' "$cmd" | grep -q 'build-only' && exit 0
command -v "${UNO_DEVSERVER_CLI:-uno-devserver}" >/dev/null 2>&1 || exit 0

printf '%s' '{"hookSpecificOutput":{"hookEventName":"PreToolUse","permissionDecision":"deny","permissionDecisionReason":"Do not dotnet build an Uno app in the shell: uno_app_start(projectPath, targetFramework, stdoutFile) builds that target and launches it in one call, a compile error fails the start with its diagnostics, and stdoutFile holds the full log. A shell build is the same build twice, and its output stays in context for the rest of the session. While the app runs, an edit is compiled by Hot Reload and uno_app_hot_reload_status(file) reports the diagnostics. Only for a target that cannot run on this machine, add the comment # build-only to the command."}}'
exit 0
