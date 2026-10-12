#!/bin/sh
# PostToolUse on Edit|Write|MultiEdit. After an edit to a .xaml or .cs file, ask the DevServer whether the
# running Uno app applied it through Hot Reload (`uno-devserver app hot-reload-status`), and tell the agent the
# result: applied -> assert; not applied -> the compiler diagnostics; rude edit -> restart the app after the related edits.
# Without the CLI, a running DevServer, or the status tool, it falls back to the reminder that Hot Reload
# applies the edit. Reads the hook JSON on stdin; prints nothing for other files.
input=$(cat)
file=$(printf '%s' "$input" | sed -n 's/.*"file_path" *: *"\([^"]*\.\(xaml\|cs\)\)".*/\1/p' | sed 's/\\\\/\\/g')
[ -n "$file" ] || exit 0

cli=${UNO_DEVSERVER_CLI:-uno-devserver}
json_escape() { printf '%s' "$1" | tr -d '\r' | sed -e 's/\\/\\\\/g' -e 's/"/\\"/g' -e 's/\t/\\t/g' | sed -e ':a' -e 'N' -e '$!ba' -e 's/\n/\\n/g'; }
remind() {
	printf '%s' '{"hookSpecificOutput":{"hookEventName":"PostToolUse","additionalContext":"An Uno app source file changed. If the app is running under the App MCP, Hot Reload applies this edit within a few seconds (a C# edit recreates the current page): confirm it with uno_app_hot_reload_status(file, waitForMs: 10000) when that tool is available, then assert with uno_app_find_elements(waitForMs). Do not dotnet build, uno_app_close or uno_app_start for it, and not to be safe either: the status result carries the compiler diagnostics and says RudeEdit when a start is really needed. Start again only for launch arguments, platform, app data, a Release build, a .csproj or package change, code that already ran at startup, or a reported RudeEdit."}}'
	exit 0
}

command -v "$cli" >/dev/null 2>&1 || remind

# The solution the edited file belongs to: the nearest folder above it with a .sln or .slnx.
has_sln() { for f in "$1"/*.sln "$1"/*.slnx; do [ -e "$f" ] && return 0; done; return 1; }
dir=$(dirname "$(printf '%s' "$file" | tr '\\' '/')")
sln_dir=
while [ -n "$dir" ]; do
	if has_sln "$dir"; then sln_dir=$dir; break; fi
	parent=$(dirname "$dir")
	[ "$parent" = "$dir" ] && break
	dir=$parent
done
[ -n "$sln_dir" ] || exit 0

# --no-start: a hook never starts a DevServer. The status tool is only offered by App MCP 2.0 and later;
# the tools list answers at once, where a status call for a missing tool would wait for it.
tools=$("$cli" --solution-dir "$sln_dir" app tools --no-start 2>/dev/null)
printf '%s' "$tools" | grep -q uno_app_hot_reload_status || remind

# After a rude edit no later edit applies until the app is rebuilt, so the restart comes after the edits that
# belong together. uno_app_restart (App MCP 2.0.0-dev.8+) closes and starts with the last start's arguments.
if printf '%s' "$tools" | grep -q uno_app_restart; then
	restart='finish the edits that belong with this one, then uno_app_restart once (it closes and starts the app with the last start arguments)'
else
	restart='finish the edits that belong with this one, then uno_app_close and uno_app_start once'
fi

out=$("$cli" --solution-dir "$sln_dir" app hot-reload-status --no-start --file "$file" --waitForMs 8000 2>/dev/null)
code=$?
verdict=$(printf '%s' "$out" | tr -d '\r' | grep '^edit to ' | head -1)

case $code in
	0) printf '%s' "{\"hookSpecificOutput\":{\"hookEventName\":\"PostToolUse\",\"additionalContext\":\"Hot Reload $(json_escape "$verdict") The running app now runs this edit (a C# edit recreated the current page: navigate back to the state under test). Assert with uno_app_find_elements(waitForMs); do not dotnet build, uno_app_close or uno_app_start.\"}}" ;;
	1) printf '%s' "{\"decision\":\"block\",\"reason\":\"Hot Reload did not apply this edit to the running app. Fix the file and save again; do not build, close or start the app for it. The DevServer reported:\\n$(json_escape "$out")\"}" ;;
	3) printf '%s' "{\"hookSpecificOutput\":{\"hookEventName\":\"PostToolUse\",\"additionalContext\":\"Hot Reload $(json_escape "$verdict") This is the one case where a restart is needed, and until it no later edit applies: $restart, then re-check the state under test.\"}}" ;;
	*) remind ;;
esac
exit 0
