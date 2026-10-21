#!/bin/sh
# PostToolUse on Edit|Write|MultiEdit. After an edit to a .xaml or .cs file, ask the DevServer whether the
# running Uno app applied it through Hot Reload (`uno-devserver app hot-reload-status`), and tell the agent the
# result: applied -> assert; compile failure -> block with the diagnostics; rude edit or DevServer internal error ->
# restart the app after the related edits; still processing, no changes and the like -> what the DevServer says.
# Without the CLI, or a solution file to point it at, it falls back to the reminder that Hot Reload applies the
# edit; with the CLI but no connected app (before the first start) it prints nothing. Reads the hook JSON on stdin; prints nothing for other files.
input=$(cat)
file=$(printf '%s' "$input" | sed -En 's/.*"file_path" *: *"([^"]*\.(xaml|cs))".*/\1/p' | sed 's/\\\\/\\/g')
[ -n "$file" ] || exit 0

cli=${UNO_DEVSERVER_CLI:-uno-devserver}
json_escape() { printf '%s' "$1" | tr -d '\r' | sed -e 's/\\/\\\\/g' -e 's/"/\\"/g' -e 's/\t/\\t/g' | sed -e ':a' -e '$!N' -e '$!ba' -e 's/\n/\\n/g'; }
remind() {
	printf '%s' '{"hookSpecificOutput":{"hookEventName":"PostToolUse","additionalContext":"An Uno app source file changed. If the app is running under the App MCP, Hot Reload applies this edit within a few seconds (a C# edit recreates the current page): confirm it with uno_app_hot_reload_status(file, waitForMs: 10000) when that tool is available, then assert with uno_app_find_elements(waitForMs). Do not dotnet build, uno_app_close or uno_app_start for it, and not to be safe either: the status result carries the compiler diagnostics and says RudeEdit when a start is really needed. Start again only for the cases the uno-build-app running-app rule allows: launch arguments, platform or app data; a Release build; the .csproj or packages; code that already ran at startup; and a RudeEdit, InternalError or uncovered edit the status reported."}}'
	exit 0
}

command -v "$cli" >/dev/null 2>&1 || remind

# The solution the edited file belongs to: the nearest folder above it with a .sln or .slnx. Without one the CLI
# has no --solution-dir, so the agent gets the reminder rather than silence.
has_sln() { for f in "$1"/*.sln "$1"/*.slnx; do [ -e "$f" ] && return 0; done; return 1; }
dir=$(dirname "$(printf '%s' "$file" | tr '\\' '/')")
sln_dir=
while [ -n "$dir" ]; do
	if has_sln "$dir"; then sln_dir=$dir; break; fi
	parent=$(dirname "$dir")
	[ "$parent" = "$dir" ] && break
	dir=$parent
done
[ -n "$sln_dir" ] || remind

# One CLI call per edit. --no-start: a hook never starts a DevServer, and with it the CLI does not wait for a
# tool the host does not list (the status tool is only offered by App MCP 2.0 and later): no host or no tool is
# exit code 2. 5 s covers an applied edit (0.3 to 4 s); a slower one is reported as not covered, and re-saving
# asks again.
out=$("$cli" --solution-dir "$sln_dir" app hot-reload-status --no-start --file "$file" --waitForMs 5000 2>/dev/null)
code=$?
verdict=$(printf '%s' "$out" | tr -d '\r' | grep '^edit to ' | head -1)
# A host with no connected app answers without a verdict (and exit code 0): nothing ran the edit, so there is nothing
# to say. Before the first start every scaffolding edit lands here (14 to 20 a session), and a reminder each time only
# taught the agent to skim the hook's notes.
[ -n "$verdict" ] || exit 0

# After a rude edit, or an internal error in the DevServer, no later edit applies until the app is rebuilt
# (fieldcheck-20261012-180709: 11 internal errors, each run of them ended only by a restart), so the restart
# comes after the edits that belong together.
restart='finish the edits that belong with this one, then uno_app_restart once (uno_app_close and uno_app_start when it is not offered)'
context() { printf '%s' "{\"hookSpecificOutput\":{\"hookEventName\":\"PostToolUse\",\"additionalContext\":\"Hot Reload $(json_escape "$verdict") $1\"}}"; }
# Code that already ran is not run again by Hot Reload, however cleanly the edit applies.
startup=
case $file in *App.xaml.cs) startup=" If the edit changed code that already ran at startup (OnLaunched, host configuration, DI registrations), the app runs it only after a restart: $restart." ;; esac

# Exit code 0 is applied, no changes, superseded or ignored (the verdict word tells them apart); 4 is still on its
# way; 3 is a rude edit; 1 covers every other verdict, and only a compile failure is the file's to fix.
case $code:$verdict in
	0:*"': applied"*) context "The running app now runs this edit (a C# edit recreated the current page: navigate back to the state under test). Assert with uno_app_find_elements(waitForMs); do not dotnet build, uno_app_close or uno_app_start.$startup" ;;
	*"': no changes"*) context "An earlier operation already compiled this file, as happens when edits follow each other within seconds: assert with uno_app_find_elements(waitForMs), and restart only if the app lacks the change." ;;
	*"': ignored"*) context "The DevServer does not watch this file (not in the app's solution, or a file type Hot Reload does not handle): the running app does not have this edit." ;;
	*"': superseded"*) context "A later operation recompiled the whole solution and usually includes this edit: assert with uno_app_find_elements(waitForMs), and restart only if the app lacks the change." ;;
	1:*" Failed in "*) printf '%s' "{\"decision\":\"block\",\"reason\":\"Hot Reload did not apply this edit to the running app. Fix the file and save again; do not close or start the app for it. If no diagnostics are listed below, call uno_app_hot_reload_status(file, waitForMs: 10000) to see them. The DevServer reported:\\n$(json_escape "$out")\"}" ;;
	1:*InternalError*) context "The DevServer failed internally, not on this file: do not save it again for this. Until a restart no later edit applies: $restart, then re-check the state under test." ;;
	1:*) context "Not applied yet, for a reason that is not in the file. Do not build, close or start the app for it, and do not assert yet: act on what the DevServer reported, and confirm with uno_app_hot_reload_status(file, waitForMs: 10000):\\n$(json_escape "$out")" ;;
	3:*) context "This is the one case where a restart is needed, and until it no later edit applies: $restart, then re-check the state under test." ;;
	4:*) context "Still on its way: call uno_app_hot_reload_status(file, waitForMs: 10000) before asserting; do not build, close or start the app for it." ;;
	*) remind ;;
esac
exit 0
