#!/bin/sh
# SessionStart on startup|resume|clear|compact. In an Uno app project, tell the agent before it plans that an app
# running under the App MCP is edited through Hot Reload, not rebuilt, and to look for a running app before
# the first build; after a compaction this is what brings the rule back. Anywhere else it prints nothing, so a
# non-Uno session pays no context. Reads the hook JSON on stdin for the project folder (`cwd`).
input=$(cat)
dir=$(printf '%s' "$input" | sed -En 's/.*"cwd" *: *"([^"]*)".*/\1/p' | sed 's/\\\\/\\/g')
[ -n "$dir" ] || dir=$(pwd)
[ -d "$dir" ] || exit 0

# An Uno app names Uno.Sdk either in global.json (msbuild-sdks) or on a project's Sdk attribute, with or without
# a version (discovery.md). Three folder levels cover src/App/App.csproj; bin and obj hold copies worth skipping.
is_uno() {
	[ -f "$dir/global.json" ] && grep -q '"Uno\.Sdk"' "$dir/global.json" && return 0
	[ -n "$(find "$dir" -maxdepth 3 -name '*.csproj' -not -path '*/bin/*' -not -path '*/obj/*' \
		-exec grep -l 'Sdk="Uno\.Sdk' {} + 2>/dev/null | head -n 1)" ]
}
is_uno || exit 0

printf '%s' '{"hookSpecificOutput":{"hookEventName":"SessionStart","additionalContext":"This is an Uno Platform app. While it runs under the App MCP, Hot Reload applies each .xaml or .cs edit: confirm it by selector, not with dotnet build, uno_app_close or uno_app_start; start again only if the change is missing after 10 s, or as the uno-build-app running-app rule allows. Before the first build, call uno_app_get_runtime_info: a connected instance is the app under test."}}'
exit 0
