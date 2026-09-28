# <Task>: completion report

<!-- Fill in every section. Write "None" rather than deleting one.
     Check states are exactly: passed, failed, not run (with reason), waived (with who waived it). -->

## What was done

<!-- The journeys and features delivered, in plain language. -->

## Checks

| Check | Target | State | Evidence |
|---|---|---|---|
| Build | <net10.0-desktop> | <passed / failed / not run: reason / waived: by whom> | <command or link> |
| Unit and service tests | any | <state> | <command, counts> |
| <Runtime check, e.g. setting survives relaunch> | <target> | <state> | <tool and result> |
| Visual: contrast, light and dark | <target> | <state> | <method, lowest ratio> |
| Visual: screenshots, narrow and wide | <target> | <state> | <files> |

### Pending runtime checks by feature

<!-- Only when a pattern was reused provisionally. One line per feature, naming the checks still owed. -->

## Implementation gaps

<!-- Journeys or integrations that are not implemented or are simulated, e.g. a missing backend contract. Separate from checks that could not run. -->

## Failures not caused by this change

<!-- Pre-existing failures, with the baseline evidence. -->

## Decisions and deviations

<!-- Dependencies added and why, fixes made outside the requested scope and why they were needed, design adaptations. -->

## Findings not acted on

<!-- Problems noticed but not repaired because they were outside the request. -->

## How to run

<!-- Commands to build, test and launch; configuration the developer must supply. -->

Checkpoint: <path, or "none">

<!-- Keep exactly ONE of the following three lines, verbatim, and delete the other two.
     Use the first only if every required check on every task-relevant target passed or was explicitly waived; keep waivers visible above, never count them as passes.
     Use the second when no implementation gap is known but a required check failed without establishing an implementation defect, was not run, or is pending. Include pre-existing failures here when they prevent verification.
     Use the third when any requested journey or integration is missing, simulated instead of implemented, or has an unresolved defect demonstrated by a failed check. This takes precedence over the second line. -->

**Implementation complete; verification complete.**
**Implementation complete; verification incomplete.** Missing: <checks, targets and reasons>.
**Implementation incomplete.** Gaps: <journeys or integrations>.
