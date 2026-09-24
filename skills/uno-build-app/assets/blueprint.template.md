# <App name>: architecture blueprint

<!-- Fill in every section, proportional to the app. A two-page app gets a sentence or two per section.
     Write "None" rather than deleting a section. Keep this file current as decisions change. -->

**Brief**: <one-sentence summary of what the app is for, and who uses it>
**Stack**: <presentation (MVUX / MVVM)>, <navigation>, <theme>, Uno.Sdk <version>
**Targets**: <declared targets>

## Journeys

<!-- The user journeys the brief asks for, in priority order. Mark the first journey to build and why it exercises the risky boundaries. -->

1. <journey> **(first: <why>)**
2. <journey>

## Domain

| Entity | Identity | Rules and invariants | Allowed transitions |
|---|---|---|---|
| <Entity> | <key> | <rule> | <from → to> |

<!-- Only rules established by the brief or the developer. Missing rules that change behaviour are questions, listed under Open questions. -->

## Services and boundaries

| Service | Responsibility | Backed by | Consumed by |
|---|---|---|---|
| <IService> | <what it owns> | <storage / API / platform> | <models> |

## Shared state ownership

| State | Owner | Observed through | Mutated through | Invalidation or refresh |
|---|---|---|---|---|
| <entity, session, preference or selection> | <service> | <feed / message / event> | <method> | <when and how> |

## Persistence

<!-- What is saved, where, when it loads, and what happens on a missing, corrupt or failed read or write. -->

## Navigation

<!-- Routes, what data each route receives, dialogs and the results they return, back behaviour. -->

## Integrations

| Integration | Contract | Status |
|---|---|---|
| <API or service> | <existing contract / link> | <available / missing: reported as a gap> |

## Risks

<!-- What is most likely to go wrong, and how the first journey or a test will catch it. -->

## Open questions

<!-- Only questions about business rules, access, data ownership, payments, destructive actions or integration contracts. Note what work continues meanwhile. -->
