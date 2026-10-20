#!/bin/sh
# PreToolUse on the App MCP's uno_execute_tool (and a direct uno_app_restart). A restart after an edit Hot Reload
# reported as applied changes nothing the app runs and costs the build, the launch and Hot Reload's readiness:
# skill-ab catalog sessions restarted 7-13 times each "for a clean state" with the status at applied
# (unoplatform/private#1217). Deny a uno_app_restart unless the session's last Hot Reload verdict says Hot
# Reload could not carry the edit (RudeEdit, InternalError, not covered, ignored) or the last edited file is
# startup code or project configuration. No state, or anything unparseable, is allowed.
input=$(cat)
tool=$(printf '%s' "$input" | sed -En 's/.*"tool_name" *: *"([^"]*)".*/\1/p')
case $tool in
	*uno_execute_tool) name=$(printf '%s' "$input" | sed -En 's/.*"toolName" *: *"([^"]*)".*/\1/p') ;;
	*) name=${tool##*__} ;;
esac
[ "$name" = "uno_app_restart" ] || exit 0

. "$(dirname "$0")/hook-state.sh"
[ -f "$state_file" ] || exit 0
verdict=$(state_get verdict)
file=$(state_get file)
case $verdict in
	rude|internal|uncovered|ignored|'') exit 0 ;;
esac
[ -n "$file" ] && is_startup_file "$file" && exit 0

printf '%s' "{\"hookSpecificOutput\":{\"hookEventName\":\"PreToolUse\",\"permissionDecision\":\"deny\",\"permissionDecisionReason\":\"No restart: the last Hot Reload verdict for this session was '$verdict', so the running app already runs the current code, and a restart only costs the build, the launch and Hot Reload's readiness. Get back to the state under test by navigating or through the app's own refresh, then assert with uno_app_find_elements(waitForMs). If the app really lacks an edit, call uno_app_hot_reload_status(file) on it: a RudeEdit, InternalError or 'not covered' result unlocks the restart. An edit to startup code (App.xaml.cs, Program.cs, the .csproj) unlocks it too.\"}}"
exit 0
