# Dataverse

Dataverse (Dynamics 365, Power Apps) is an OData service: generate and call its client with `references/odata.md`, `references/odata-calling.md` and `references/odata-writing.md`. This page is what its `$metadata` doesn't say. Every query below is a GET on `<org>/api/data/v9.2` with the app's token.

## Field rules come from EntityDefinitions

`$metadata` has types and nullability only. The rules a form enforces are in the table's attribute metadata, one typed cast per kind of column:

```http
GET EntityDefinitions(LogicalName='account')/Attributes?$select=LogicalName,AttributeType,RequiredLevel,DisplayName&$filter=IsValidForUpdate eq true
GET EntityDefinitions(LogicalName='account')/Attributes/Microsoft.Dynamics.CRM.StringAttributeMetadata?$select=LogicalName,MaxLength,FormatName
GET .../Attributes/Microsoft.Dynamics.CRM.IntegerAttributeMetadata?$select=LogicalName,MinValue,MaxValue
GET .../Attributes/Microsoft.Dynamics.CRM.MoneyAttributeMetadata?$select=LogicalName,MinValue,MaxValue,Precision
GET .../Attributes/Microsoft.Dynamics.CRM.PicklistAttributeMetadata?$select=LogicalName&$expand=OptionSet($select=Options)
```

- **Required** is `RequiredLevel.Value` `ApplicationRequired` or `SystemRequired`. `Recommended` and `None` are optional.
- **`FormatName.Value`** is `Email`, `Url`, `Phone`, `Text` and so on. The server doesn't enforce it: it stored `not-an-email` in an Email column. The app validates formats itself.
- **Money `Precision`** is the number of decimals allowed (2 for `revenue`), beside its `MinValue` and `MaxValue`.
- **Snapshot the rules with a script** kept beside `odata/`, into a JSON file or validator classes, and re-run it when the table changes, as you re-run the scope script. Enforce them with the `uno-mvux` skill's validation reference, not with hand-rolled error state.

## Labels

- **Option labels** are in `Options[].Label.LocalizedLabels`, one per provisioned language (`LanguageCode` 1033 is English), and in `Label.UserLocalizedLabel` in the caller's language. An org with no French language pack has no French labels: the app's resources carry them.
- **Match on the value, never the label.** A label changes with the user's language. If you must match a name, match the base-language label (`organizations?$select=languagecode`).
- **Display values come as annotations.** `Prefer: odata.include-annotations="OData.Community.Display.V1.FormattedValue"` adds `<column>@OData.Community.Display.V1.FormattedValue` beside each choice, lookup, money and date column; read it from `AdditionalData`. Send several preferences in one header, comma-separated: `odata.maxpagesize=25,odata.include-annotations="..."`.

## Lookups

- **Read** a lookup as `_<column>_value`, the related row's key. Its name is the formatted-value annotation, and `_<column>_value@Microsoft.Dynamics.CRM.associatednavigationproperty` names the navigation property that writes it (`include-annotations="*"` returns both).
- **Write** through the navigation property (`references/odata-writing.md`). Its name comes from the relationship, not the column:

  ```http
  GET EntityDefinitions(LogicalName='task')/ManyToOneRelationships?$select=ReferencingAttribute,ReferencedEntity,ReferencingEntityNavigationPropertyName&$filter=ReferencingAttribute eq 'regardingobjectid'
  ```

- **A polymorphic lookup has one navigation property per target table.** A contact's `parentcustomerid` is written as `parentcustomerid_account` or `parentcustomerid_contact`. A task's regarding is `regardingobjectid_account_task`, with the activity table's name at the end; `regardingobjectid_account` is declared only on `activitypointer`, so a write to `tasks` with it fails.

## Status

A row's status is a pair: `statecode` (Active, Completed, Canceled) and `statuscode`, a reason that belongs to one state. Read the valid pairs instead of hard-coding them:

```http
GET EntityDefinitions(LogicalName='task')/Attributes(LogicalName='statuscode')/Microsoft.Dynamics.CRM.StatusAttributeMetadata?$select=LogicalName&$expand=OptionSet($select=Options)
```

Each option has a `Value` and its `State`: a task's Completed is 5 in state 1, Canceled 6 in state 2. A custom table's reasons are custom values such as 100000000. To close a row, PATCH both columns.

## Keys and query options

- **Keys go in parentheses:** `accounts(<id>)`. `accounts/<id>` answers 500.
- **`$skip` answers 400 and `$search` 501:** list both in `unsupported`, page with the next link, and search with `contains()` in `$filter`.

Verified against Dataverse Web API v9.2 on 2026-11-04.
