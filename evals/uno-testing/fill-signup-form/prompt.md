---
description: Driving a form through the App MCP uses the right automation-peer action per control (setValue/type, toggle, expand) instead of the invoke default action, then waits on the result.
tags: [uno-testing, interaction]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
In my Uno Platform app the sign-up page has an email TextBox, an "I accept the terms" CheckBox, a Country ComboBox, and a Submit button that shows a "Welcome!" TextBlock when it succeeds. My agent has the Uno App MCP connected. Write the exact sequence of App MCP calls it should make to fill the form, submit it, and prove it worked. I just want the plan, not a run.
