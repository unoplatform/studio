#!/bin/sh
# PostToolUse on Edit|Write|MultiEdit. After an edit to a .xaml or .cs file, tell the agent that a running
# Uno app picks the change up through Hot Reload, so it asserts instead of calling uno_app_start.
# Reads the hook JSON on stdin; prints nothing for other files.
input=$(cat)
printf '%s' "$input" | grep -Eq '"file_path" *: *"[^"]*\.(xaml|cs)"' || exit 0
printf '%s' '{"hookSpecificOutput":{"hookEventName":"PostToolUse","additionalContext":"An Uno app source file changed. If the app is running under the App MCP, Hot Reload applies this edit within a few seconds (a C# edit recreates the current page): confirm it with uno_app_hot_reload_status(file, waitForMs: 10000) when that tool is available, then assert with uno_app_find_elements(waitForMs). Do not dotnet build, uno_app_close or uno_app_start for it, and not to be safe either: the status result carries the compiler diagnostics and says RudeEdit when a start is really needed. Start again only for launch arguments, platform, app data, a Release build, a .csproj or package change, code that already ran at startup, or a reported RudeEdit."}}'
