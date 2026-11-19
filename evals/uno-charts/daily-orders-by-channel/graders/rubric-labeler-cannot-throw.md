---
type: llm
---
The reply puts 90 days of order counts on a LiveCharts2 line chart in an Uno Platform app.
Facts the judge needs: on a date axis LiveCharts2 passes the axis's tick values, as `double` ticks, to the axis `Labeler`, including values outside any valid date while it pads its limits (negative numbers, or above `DateTime.MaxValue.Ticks`). `new DateTime((long)v)` throws `ArgumentOutOfRangeException` on those, and the chart then draws nothing. A range check before the conversion, a `try`/`catch` around it, or a Labeler that never builds a `DateTime` from the raw value all pass. Axis category labels (`Axis.Labels`) have no Labeler of their own and fail the criterion.
PASS if the date axis Labeler returns a string for every tick value without throwing.
FAIL if it builds a `DateTime` from the raw tick value with no range check or `try`/`catch`, or the dates go on the axis as one string per day in `Axis.Labels`.
