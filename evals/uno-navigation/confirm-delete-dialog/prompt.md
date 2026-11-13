---
description: A confirmation before deleting is shown from the model through the navigator's message dialog and its answer is acted on.
tags: [uno-navigation, dialogs]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
In my Uno Platform app (MVUX models, Uno.Extensions navigation) the ProjectModel has a `DeleteProject` method. Before deleting I want a "Delete this project? This can't be undone." prompt with Delete and Cancel buttons, and only delete if the user picks Delete. The logic lives in the model, which has no reference to the page. How do I show the prompt and read the answer?
