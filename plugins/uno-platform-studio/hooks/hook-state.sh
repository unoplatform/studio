# Shared by the hooks: a small per-session state file, so a PreToolUse gate can act on what the PostToolUse
# hooks saw. Sourced, not run. Expects $input to hold the hook JSON from stdin.
# Keys: file (last edited .xaml/.cs), verdict (last Hot Reload verdict class), started (an app start or
# restart succeeded in this session). Every read tolerates a missing file: no state means allow.
state_dir=${UNO_HOOK_STATE_DIR:-${TMPDIR:-/tmp}}
session=$(printf '%s' "$input" | sed -En 's/.*"session_id" *: *"([^"]*)".*/\1/p')
state_file=$state_dir/uno-hook-$(printf '%s' "${session:-default}" | tr -c 'A-Za-z0-9_.-' '_')

state_get() { [ -f "$state_file" ] && sed -En "s/^$1=(.*)$/\1/p" "$state_file" | tail -1; }
# state_set key value: replaces the key, keeps the others.
state_set() {
	tmp=$state_file.$$
	{ [ -f "$state_file" ] && grep -v "^$1=" "$state_file"; printf '%s=%s\n' "$1" "$2"; } > "$tmp" 2>/dev/null
	mv -f "$tmp" "$state_file" 2>/dev/null || rm -f "$tmp"
}
# What identifies a launch: the target framework and the app arguments of a uno_app_start, whitespace removed.
# A start that repeats both after an applied edit is a restart by another name; one that changes either
# (another platform, a data mode, a window size) is a new launch.
launch_key() {
	tfm=$(printf '%s' "$1" | sed -En 's/.*"targetFramework" *: *"([^"]*)".*/\1/p')
	args=$(printf '%s' "$1" | sed -En 's/.*"args" *: *(\[[^]]*\]).*/\1/p' | tr -d ' \t')
	printf '%s|%s' "$tfm" "$args"
}
# Code that only runs at startup, or project configuration: Hot Reload cannot carry it, so a restart is right.
is_startup_file() {
	printf '%s' "$1" | tr '\\' '/' | grep -Eq '(App\.xaml\.cs|Program\.cs|Main\.[A-Za-z]+\.cs|MainActivity[^/]*\.cs|AppDelegate[^/]*\.cs|\.csproj|Directory\.[A-Za-z]+\.(props|targets)|appsettings[^/]*\.json|launchSettings\.json|global\.json)$'
}
