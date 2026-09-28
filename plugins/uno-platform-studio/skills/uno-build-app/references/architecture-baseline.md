# App Architecture and Quality Baseline

These are working defaults the agent applies as ordinary engineering judgement, proportionally to the app. They do not authorise extra scope, dependencies, backend provisioning or existing-app rewrites, and they are not a questionnaire for the developer. Ask only under rule 3 of the skill.

## 1. Architecture that serves the app

Start with the standard Uno template, the defaults in [new-app.md](new-app.md) and the requested design. Write the blueprint. Choose a first journey that exercises the app's risky boundaries, not an easy static page, and implement it through view, presentation state, rules, service, persistence or integration, and checks before repeating its pattern. Keep the blueprint current when requirements or decisions change and update the affected checks with it.

## 2. Validation and consistent mutations

Give immediate input feedback in the UI. Enforce business rules at their UI-independent owner so no screen can bypass them. Respect backend authority for server-owned rules; client validation is not a substitute. Map failures to clear field or form errors and preserve the user's input; never show raw exceptions or treat a failed save as success.

Confirmed mutations are the baseline. Use optimistic interaction only when rollback and reconciliation are defined and tested. For async operations handle repeated activation, cancellation, late results, ordering and owner lifetime with a concrete strategy for the feature, not generic concurrency infrastructure.

## 3. Real data and external integrations

Client development is the default scope. Integrate existing contracts; propose missing backend work separately; never invent production endpoints or quietly turn a networked requirement into a local-only feature.

Development data implementations are fine for progress and tests when isolated and labelled. A fake login, simulated purchase, no-op action or seed-only dataset never satisfies a required real integration. Keep fixtures out of Release output.

For networked features apply timeouts and cancellation, useful error mapping, and retry that respects operation semantics; never blindly replay a possibly-completed non-idempotent action. Page where scale requires it. Separate transport from UI and domain types when their meanings differ, without mechanical duplicate types.

Keep a short list of incomplete journeys and dependencies. Report missing implementation separately from missing verification.

## 4. Durability and offline behaviour

Persist what the app presents as saved. Distinguish deliberate temporary drafts and disposable development data. Choose storage to suit the data, platform and existing conventions; a setting does not justify a database.

Handle loading, missing or corrupt data, save failures and interrupted writes where applicable. Test restoration against real persistent storage, not a new model over the same memory. Preserve user data across schema changes and test the migration when one is introduced.

Networked apps handle disconnection explicitly. Offline editing, queued writes and synchronisation are features, not defaults; when in scope, define authority, freshness, conflicts, retry and visible pending state.

## 5. Identity and sensitive operations

When authentication is required, use the project's supported identity integration and real flows where configured; label any development substitute as incomplete integration. Server authorisation is separate from UI visibility; hiding a control enforces nothing.

Use established platform or project mechanisms for sensitive storage. Keep credentials and sensitive data out of source, logs, fixtures and handoff notes. Handle session expiry, logout and account switching so one user's state never appears under another.

Never invent payment, access, destructive-operation or data-ownership policy; ask under rule 3 of the skill when the missing policy changes behaviour. This is not a compliance claim.

## 6. Startup, lifecycle and platform behaviour

Make startup dependencies explicit: settings hydration, session restoration, initial navigation, recoverable initialisation errors. Do not present partially initialised state as ready.

Define what survives navigation, suspension, restart and browser reload; make any loss of unsaved work deliberate and visible; do not promise restoration the app has not implemented.

Exercise back navigation and dialog dismissal on applicable targets. Keep browser history and refresh coherent with the routing approach. Skia does not remove OS or browser lifecycle and permission differences.

Put platform operations behind services with useful denied or unavailable states. Ship no control that has no effect on an intended target. Record which device, emulator or browser actually ran; a simulator run is not device coverage.

## 7. Coherent and usable UI

Establish a small visual system before multiplying pages: type hierarchy, spacing, colour roles, reusable controls with their interaction states, responsive behaviour. Derive it from the supplied design or the fallback stack, using the existing theme and Toolkit skills rather than a new catalogue.

Include loading, empty, error, pending, disabled and success states where relevant. Check keyboard operation, semantic names and states, focus order, contrast, text scaling, screen-reader semantics and reduced motion where motion exists. Claim only what the available tools checked.

Preserve design intent while making routine usability corrections; explain material departures; ask only when the conflict changes a consequential requirement or the developer's explicit intent.

Use the project's string, resource and culture conventions and resilient layouts. New-app content is localisable without translated content or a language selector. Test long text and narrow/wide layouts; add RTL work when the audience needs it.

## 8. Meaningful verification and diagnostics

Choose tests by behaviour and risk: domain and service rules, persistence and integration boundaries, critical journeys through the real UI. Respect existing frameworks and package policy; for a new app with no tests, establish the minimal setup under that policy rather than skipping tests or adding several frameworks.

Exercise failure paths: invalid input, empty data, storage or network failure, interrupted work, expired sessions, rapid actions, late results. Outcomes, not coverage numbers.

Use representative data sizes; look at startup, list virtualisation, expensive UI work, images and subscription lifetime where relevant; measure real risks with existing tooling. No universal budgets, no benchmarking product.

Use the existing logging approach with actionable diagnostics and no automatic telemetry. Keep fixture switches out of Release.

For a new app include a Release build on available toolchains, checking target-specific trimming, AOT, resources or packaging where the build uses them. Record what could not run. No signing, store submission or deployment unless requested. Never disable a requested platform or build property to make a failing check disappear.

## 9. Completion means complete journeys

Before delivery compare every requested journey and visible action with implemented behaviour: durability, validation, navigation, failure paths, integration status, visual result. A passing recipe sample is not evidence its integration works.

For new apps and substantive changes run a scoped architecture review: dependency direction, rule ownership, shared-state authority, service lifetimes, repeated patterns, platform boundaries. Use an independent reviewer where the host supports it; otherwise apply the same rubric directly. The coordinating agent owns the assessment. A self-review inside a build task may repair; a standalone review request may not.

Report implementation gaps, failing checks, unavailable verification and authorised waivers distinctly, with the closing line from the report template. Provide run and test instructions, needed configuration, the blueprint and known limitations, reusing the existing README rather than generating a documentation tree. When blocked, leave coherent completed slices and a checkpoint naming the incomplete journeys and the precise next step.
