# Dataverse

Dataverse (Dynamics 365, Power Apps) is an OData service: generate and call its client with `references/odata.md`, `references/odata-calling.md` and `references/odata-writing.md`. This page is what its `$metadata` doesn't say. Every query below is a GET on `<org>/api/data/v9.2` with the app's token.

## Read the environment before you design

Everything below is read from the environment, and an app built without it has guessed its field rules, form fields, list columns and choice values. Read it first, with the developer's own sign-in, when the brief allows reading:

- **The token stays inside the command that uses it.** `T=$(az account get-access-token --resource <org> --query accessToken -o tsv)` then `curl -H "Authorization: Bearer $T" ...` in the same command, or `AzureCliCredential` inside the snapshot script. Never print a token, pipe it to the screen, or write it to a file: a command that would is refused, and rightly. That refusal is about the printing, not about reading; run the form that keeps the token in a variable.
- **GET only.** The snapshot script and every probe read; a write to try something out belongs on sample data.
- **When it cannot be read at all** (no sign-in on this machine, no network), write the snapshot script anyway, build from `$metadata`, and put the consequence first in the report: which rules, fields, columns and choice values are inferred, and the one command that replaces them with the environment's.

## Sign-in while developing

For development and prototyping, Microsoft publishes a sample app registration that works against any Dataverse environment: client id `51f81489-12ee-4a9e-aaae-a2591f45987d`, redirect URI `http://localhost` with MSAL.NET, scope `<org>/user_impersonation`, tenant `organizations`. When the developer gave no client id, ship it as the default so sign-in works on first run, and report it as a development default: production needs the organisation's own registration (the `uno-authentication` skill, `references/msal.md`).

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
- **Every table that has a form is in the snapshot, with every column the form edits.** A required flag, a length or a range typed from memory or copied from a mock-up is a guess: the validator reads its limits from the snapshot, or the script generates the validator from it.
- **A test holds the validators to the snapshot.** It walks the snapshot and, for each column a form edits, checks that the validator rejects an empty value when the column is required, a text one character over `MaxLength`, and a number below `MinValue` or above `MaxValue`. A column with no rule in the validator fails the test.
- **Choice lists come from the snapshot too**: the values and their order from `OptionSet.Options`, never typed in. The label of each value is an app resource keyed by the value, one per language the app ships (below), and a test checks that every value has one.

## A form's fields come from the table's main form

The columns a Dataverse user fills in for a table, and how they are grouped, are in its main form (`type eq 2`):

```http
GET systemforms?$select=name,formxml&$filter=objecttypecode eq 'contact' and type eq 2
```

`formxml` is XML: `form/tabs/tab/columns/column/sections/section/rows/row/cell/control`. Each `control`'s `datafieldname` is a column, and the tabs and sections are in the order the user sees. `contact`'s form named "Contact" has 28 fields in 9 sections, starting with `fullname`, `jobtitle`, `parentcustomerid`, `emailaddress1`, `telephone1`, `mobilephone`.

- **Pick the form named after the table** when there are several ("Contact", "Account"), not a portal or web form, and say which you took.
- **Snapshot it with the same script**, as sections with their fields, beside the rules. The app's form for that table shows those fields in those sections, each with its rule from the attribute metadata. A field the brief excludes is left out and reported; a mock-up that draws one table's form doesn't shorten the others.
- **A custom table's main form can be nearly empty**: one held only the name and the owner. Then the field list is the table's own editable columns: `Attributes?$filter=IsCustomAttribute eq true and IsValidForUpdate eq true`, the required ones first.
- **Add the form's columns to the client's scope** and regenerate (`references/odata.md`) when the generated model lacks one. Don't drop the field, and don't edit the generated model.
- **A lookup's target table joins the client's scope too.** A main-form lookup to a table the brief never named (a country, a province, a price list) still gets its picker: add that table's entity set to `scope.json`, regenerate, and read its key and name columns. A form field is never left out because its table was outside the scope.
- **A record's related lists are the table's one-to-many relationships**: `EntityDefinitions(LogicalName='account')/OneToManyRelationships?$select=SchemaName,ReferencingEntity,ReferencingAttribute`, kept to the tables the app shows and to columns a user fills in. A table can point at itself: `account_parent_account` (`ReferencingEntity` `account`, `ReferencingAttribute` `parentaccountid`) is the list of child accounts on an account. `masterid` records a merge and is no list.
- **A field whose attribute is a `Lookup` or `Customer` is a picker over the target table**, with server search (`references/odata-calling.md`): the `uno-navigation` skill's picker page. A `Picklist` is a `ComboBox` over the snapshot's options. Reading and writing the lookup are below.
- **A lookup the maker filters by related rows says so in its control.** The `control`'s `parameters` then carry `FilterRelationshipName`, a many-to-one relationship of the picked table whose `ReferencingAttribute` is the column to filter on; `DependentAttributeType`, the table that column points at; and `DependentAttributeName`, where the row to match comes from: `account.<prefix>_country` is another lookup on the form, and when the type is the form's own table it is the record itself. `account`'s `primarycontactid` has `contact_customer_accounts`, `contact.parentcustomerid` and `account`: its picker lists the contacts whose `_parentcustomerid_value` is this account. The snapshot records each such lookup's filter column and the field it follows. The app's picker for that field adds `_<column>_value eq <id>` to its `$filter` and lists only the chosen parent's rows, or every row while the parent is empty; choosing or clearing the parent empties the field (the `uno-navigation` skill's picker page, *A picker that takes a filter*).

## A list's columns, order and search come from the table's views

The columns a Dataverse user sees in a table's list, the order it opens in and what its search box searches are in the table's views:

```http
GET savedqueries?$select=name,querytype,isdefault,isquickfindquery,layoutxml,fetchxml&$filter=returnedtypecode eq 'contact' and statecode eq 0 and (querytype eq 0 or querytype eq 4 or querytype eq 64)
```

- **`querytype` says what a view is for.** 0 is a public view, and the one with `isdefault` true is the list the user opens on. 4 is the quick find view, whose filter names the columns the search box searches. 64 is the lookup view, the columns a picker over this table shows.
- **`layoutxml` has the columns, in order**: the `name` of each `grid/row/cell`. A name with a dot (`accountprimarycontactidcontactcontactid.emailaddress1` in `account`'s) is a column of a related table: the `link-entity` with that `alias` in `fetchxml` names the table and, in `to`, the lookup that leads to it.
- **`fetchxml` has the order and the searched columns.** Each `entity/order` is a sort column (`attribute`, with `descending="true"` when it is). In the quick find view, the `condition`s of the `filter` marked `isquickfindfields="1"` are the columns searched.
- **Snapshot it with the same script**, beside the forms: per table, the default view's columns and order, the quick find columns and the lookup view's columns. The app's list shows those columns, the table's primary name column first because it is the row's title in a narrow list, and opens in that order. Its search sends `contains()` over the quick find columns that hold text, and a picker shows the lookup view's columns. A table with no usable view shows its name, then its text and date columns, and searches the name; say so in the report.
- **A list reads the active rows**: `statecode eq 0` in the `$filter` of every list and picker, unless the list is of closed rows.
- **`contains()` reads `%`, `_` and `[` as wildcards.** After doubling quotes, put each in brackets (`[%]`, `[_]`, `[[]`), so the text typed matches only itself.
- **End every `$orderby` with the key** (`fullname asc,contactid`): rows that tie on the sort column otherwise repeat or go missing between pages.

## Labels

- **Option labels** are in `Options[].Label.LocalizedLabels`, one per provisioned language (`LanguageCode` 1033 is English), and in `Label.UserLocalizedLabel` in the caller's language. An org with no French language pack has no French labels: the app's resources carry them, keyed by the option's value, with the org's labels as the text for the languages it has.
- **Match on the value, never the label.** A label changes with the user's language. If you must match a name, match the base-language label (`organizations?$select=languagecode`).
- **Display values come as annotations.** `Prefer: odata.include-annotations="OData.Community.Display.V1.FormattedValue"` adds `<column>@OData.Community.Display.V1.FormattedValue` beside each choice, lookup, money and date column; read it from `AdditionalData`. Send several preferences in one header, comma-separated: `odata.maxpagesize=25,odata.include-annotations="..."`.

## Lookups

- **Read** a lookup as `_<column>_value`, the related row's key. Its name is the formatted-value annotation, and `_<column>_value@Microsoft.Dynamics.CRM.associatednavigationproperty` names the navigation property that writes it (`include-annotations="*"` returns both).
- **Write** through the navigation property (`references/odata-writing.md`). Its name comes from the relationship, not the column:

  ```http
  GET EntityDefinitions(LogicalName='task')/ManyToOneRelationships?$select=ReferencingAttribute,ReferencedEntity,ReferencingEntityNavigationPropertyName&$filter=ReferencingAttribute eq 'regardingobjectid'
  ```

- **Sort by a lookup with its own column.** `$orderby=_parentcustomerid_value` orders the rows by the related row's name, not by its id. `$orderby=parentcustomerid_account/name` answers 400 unless the same request also expands that navigation property (`$expand=parentcustomerid_account($select=name)`).
- **A polymorphic lookup has one navigation property per target table.** A contact's `parentcustomerid` is written as `parentcustomerid_account` or `parentcustomerid_contact`. A task's regarding is `regardingobjectid_account_task`, with the activity table's name at the end; `regardingobjectid_account` is declared only on `activitypointer`, so a write to `tasks` with it fails.

## Money

- **Every row with a money column names its currency**: `_transactioncurrencyid_value`, whose formatted value is the currency's name. `transactioncurrencies?$select=isocurrencycode,currencysymbol,currencyprecision` lists the organisation's currencies; read it once and keep it by id.
- **Show an amount in its row's currency.** Format the number in the user's culture with that currency's symbol (a clone of the culture's `NumberFormatInfo` with `CurrencySymbol` set), or show its ISO code beside it. The formatted-value annotation of a money column (`CA$285,000.00`) already has the symbol, but in the service user's format, not the app's language.
- **Each money column has a twin in the base currency**: `revenue_base`, `<prefix>_amount_base`. A total over rows sums the `_base` column (`aggregate(revenue_base with sum as total)`, `references/odata-calling.md`), because rows in different currencies can't be added in their own column, and it is labelled with the base currency: `organizations?$select=_basecurrencyid_value`.
- **Add `transactioncurrencies` to the client's scope** when the app shows money (`references/odata.md`); the `_base` columns come with their table.

## Status

A row's status is a pair: `statecode` (Active, Completed, Canceled) and `statuscode`, a reason that belongs to one state. Read the valid pairs instead of hard-coding them:

```http
GET EntityDefinitions(LogicalName='task')/Attributes(LogicalName='statuscode')/Microsoft.Dynamics.CRM.StatusAttributeMetadata?$select=LogicalName&$expand=OptionSet($select=Options)
```

Each option has a `Value` and its `State`: a task's Completed is 5 in state 1, Canceled 6 in state 2. A custom table's reasons are custom values such as 100000000. To close a row, PATCH both columns; to reopen it, as an Undo does, PATCH the pair it had before.

Snapshot the pairs with the same script as the rules. The values the app tests and writes (open, won, lost, completed, a stage) and their labels come from that snapshot, never from constants typed in code: a custom table's values differ from one environment to the next.

A maker can add or rename a status reason after the app ships. Once per session the app reads the pairs again in the background, with its own token, and uses them over the snapshot; when that read fails the snapshot stands. Tests run on the snapshot.

## Keys and query options

- **Keys go in parentheses:** `accounts(<id>)`. `accounts/<id>` answers 500.
- **`$skip` answers 400 and `$search` 501:** list both in `unsupported`, page with the next link, and search with `contains()` in `$filter`.

Verified against Dataverse Web API v9.2 on 2026-11-04; the main-form query on 2026-11-10; the lookup sort and the money columns on 2026-11-12; the one-to-many relationships on 2026-11-14.
