#!/bin/sh
# Guards uno_app_restart. `pre`: PreToolUse on the App MCP's uno_execute_tool. When every edit since the last start
# was applied by Hot Reload (the state hot-reload-edit.sh records for this session) and none touched startup code,
# the restart is denied with the reason: the running app already has the edits, and a restart proves nothing.
# After a RudeEdit, an InternalError, a compile failure, an edit the DevServer ignored, an edit to App.xaml.cs,
# Program.cs, a project, props, appsettings or manifest file, or with no edit since the last start, it is allowed.
# Only uno_app_restart is guarded: uno_app_close then uno_app_start is the way to relaunch with other arguments,
# data or platform, and stays open. `post`: PostToolUse on the same tool; a start or restart clears the state.
# Reads the hook JSON on stdin; the state lives in ${TMPDIR:-/tmp}/uno-studio-hooks/<session_id>.state.
mode=$1
input=$(cat)
tool=$(printf '%s' "$input" | grep -oE '"toolName" *: *"[^"]*"' | head -1 | sed -E 's/.*"([^"]*)"$/\1/')
session=$(printf '%s' "$input" | grep -oE '"session_id" *: *"[^"]*"' | head -1 | sed -E 's/.*"([^"]*)"$/\1/')
state="${TMPDIR:-/tmp}/uno-studio-hooks/${session:-default}.state"

case $mode in
post)
	case $tool in uno_app_start|uno_app_restart) rm -f "$state" ;; esac
	exit 0 ;;
pre)
	[ "$tool" = uno_app_restart ] || exit 0
	[ -f "$state" ] || exit 0
	verdict=$(sed -n 's/^verdict=//p' "$state" | tail -1)
	startup=$(sed -n 's/^startup=//p' "$state" | tail -1)
	last=$(sed -n 's/^last=//p' "$state" | tail -1)
	[ "$startup" = 1 ] && exit 0
	case $verdict in
	applied|nochanges|superseded)
		why="Hot Reload applied every edit since the last start (last: $last), and none touched code that runs at startup. The running app already has them, so a restart proves nothing here, and it costs a launch and the state under test: re-enter that state and assert with uno_app_find_elements(waitForMs). A restart is allowed after a RudeEdit or InternalError verdict, a compile failure, or an edit to App.xaml.cs, Program.cs, a .csproj, .props, appsettings or manifest file. To relaunch with other arguments, data or platform, uno_app_close then uno_app_start with them." ;;
	pending)
		why="The last edit is still on its way through Hot Reload (last: $last): a restart now would rebuild what is about to apply. Call uno_app_hot_reload_status(file, waitForMs: 10000) first; restart only if it reports RudeEdit or an internal error." ;;
	*) exit 0 ;;
	esac
	esc=$(printf '%s' "$why" | sed -e 's/\\/\\\\/g' -e 's/"/\\"/g')
	printf '%s' "{\"hookSpecificOutput\":{\"hookEventName\":\"PreToolUse\",\"permissionDecision\":\"deny\",\"permissionDecisionReason\":\"$esc\"}}"
	exit 0 ;;
esac
exit 0
