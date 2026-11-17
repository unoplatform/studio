---
type: llm
---
The reply explains why a Button bound to an MVUX command on an Uno Platform Model stays disabled, and fixes it.
PASS if the fix stays in the Model and its generated command.
FAIL if it reads the TextBox in code-behind, replaces the command with a Click handler, or adds `INotifyPropertyChanged`.
