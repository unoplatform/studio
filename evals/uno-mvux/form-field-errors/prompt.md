---
description: A form's field errors come from MVUX validation and are shown by Uno's Input validation on each bound input, with Save refused by the command's validation, not by a hand-written error reader or a validity check.
tags: [uno-mvux, validation]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
My Uno Platform app has an MVUX contact edit page: the model holds the contact being edited and a Save that calls the contact service. Last name is required and email must look like an email. I want each field's error message shown right under that field, a "fix the highlighted fields" line when anything is wrong, and Save must not go through while the form is invalid. Show the validator, the model and the XAML.
