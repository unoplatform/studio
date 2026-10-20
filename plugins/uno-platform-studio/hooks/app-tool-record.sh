#!/bin/sh
# PostToolUse on the App MCP's uno_execute_tool (and direct uno_app_* names). Records, for the restart gate, the
# two things the edit hook cannot see: a start or restart that succeeded (the app now runs the current code), and
# the verdict of an explicit uno_app_hot_reload_status call. Prints nothing.
input=$(cat)
tool=$(printf '%s' "$input" | sed -En 's/.*"tool_name" *: *"([^"]*)".*/\1/p')
case $tool in
	*uno_execute_tool) name=$(printf '%s' "$input" | sed -En 's/.*"toolName" *: *"([^"]*)".*/\1/p') ;;
	*) name=${tool##*__} ;;
esac
. "$(dirname "$0")/hook-state.sh"
response=$(printf '%s' "$input" | sed -En 's/.*"tool_response" *: *(.*)$/\1/p')
case $name in
	uno_app_start|uno_app_restart)
		case $response in *'"isError":true'*|*'"isError": true'*) exit 0 ;; esac
		state_set started 1
		state_set verdict started
		# Every launch of the session, by target and arguments: a start that repeats any of them is a restart.
		[ "$name" = uno_app_start ] && state_add launched "$(launch_key "$input")" ;;
	uno_app_hot_reload_status)
		case $response in
			*RudeEdit*) state_set verdict rude ;;
			*InternalError*) state_set verdict internal ;;
			*'not covered'*|*'no Hot Reload operation covered'*) state_set verdict uncovered ;;
			*': ignored'*) state_set verdict ignored ;;
			*': applied'*) state_set verdict applied ;;
		esac ;;
esac
exit 0
