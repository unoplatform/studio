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
     Use the first only if every required check on every declared target ran and passed.
     Use the second when implementation is done and any required check failed to run, was not run, or is pending.
     Use the third when any requested journey or integration is not implemented. -->

**Implementation complete; verification complete.**
**Implementation complete; verification incomplete.** Missing: <checks, targets and reasons>.
**Implementation incomplete.** Gaps: <journeys or integrations>.
