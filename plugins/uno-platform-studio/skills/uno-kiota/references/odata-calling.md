# Calling an OData Service

The client comes from `references/odata.md`. Register it like any Kiota client (`references/register.md`), with the service root as the Url: `<org>/api/data/v9.2` for Dataverse. The service enforces the rules below at runtime; the document can't express them. Creating, updating and deleting are in `references/odata-writing.md`, and Dataverse's own rules in `references/dataverse.md`.

The access token comes from the authentication handler: the `uno-authentication` skill (`references/http.md`) for a user's sign-in, or your own `DelegatingHandler` registered in `UseHttp` for a service identity such as an app registration with a client secret.

## Pages follow the next link

```csharp
var page = await client.Accounts.GetAsync(r =>
{
    r.Headers.Add("Prefer", "odata.maxpagesize=25");
    r.QueryParameters.Select = ["accountid", "name"];
    r.QueryParameters.Orderby = ["name"];
    r.QueryParameters.Count = true;
}, ct);
var total = page?.OdataCount;   // every matching row, not just this page
var next = page?.OdataNextLink; // null on the last page

var more = await client.Accounts.WithUrl(next!)
    .GetAsync(r => r.Headers.Add("Prefer", "odata.maxpagesize=25"), ct);
```

- **A list the user scrolls is a paginated feed.** Read the `uno-mvux` skill's `references/pagination.md` before writing it, and feed it with cursor pagination, the next link as the cursor: `ListFeed<T>.PaginatedByCursorAsync`. Don't loop the next link into one list in memory and search it on the client. A table that holds 20 rows today is paged like one that holds 20,000: the app is built for the table, not for the test environment.
- **Each list reads its own table.** The accounts page queries `accounts` and the contacts page `contacts`, each with the filter that says what this user may see. Don't build a list from the rows another list loaded (the accounts of the opportunities already in memory): a row with no such relation never shows, and every page waits on every table.
- **The service that wraps the client mirrors these calls**: a method per list that takes the search text, filter, sort and next link and returns one page, its count and the next link; a method per total (below); a get by key. Not one `LoadAsync` that returns every table.
- **Only a view that shows a filtered set at once reads it to the end**: a board of the open records, one month of a calendar. It follows the next link with its filter and a `Select` of the columns it shows, for that view alone. It is not the source of any other list, count or total.
- **Send `Prefer: odata.maxpagesize` on every request, including the one that follows the next link.** The link doesn't carry the page size. Without the header, Dataverse returned all 38 remaining rows as one page.
- **Follow the link with `WithUrl`, and pass only the header.** The link already carries the query options and the service's paging token.
- **When the probe found the page size ignored** (`references/odata.md`), there is never a next link: page with `$top` and `$skip` instead, in a stable order. A page shorter than `$top` is the last.

  ```csharp
  var page = await client.People.GetAsync(r =>
  {
      r.QueryParameters.Top = 25;
      r.QueryParameters.Skip = pageIndex * 25;
      r.QueryParameters.Orderby = ["UserName"]; // the key, so pages don't overlap
  }, ct);
  ```

- **Don't page with `$skip`** unless your probe showed it moves. Dataverse rejects it with a 400.

## The count is in the collection response

Set `QueryParameters.Count = true` and read `OdataCount`. The recipe generates no `/$count` path: it answers `text/plain`, which `AddKiotaClient` can't parse (`references/content-types.md`).

## Totals and counts per group come from `$apply`

A dashboard total, the number of rows per stage or status, and the count beside a filter are each one request the service answers. Don't load a table to add it up. The generated builders have no `$apply` parameter, so take the collection's URL from its builder and pass the whole URL to `WithUrl`:

```csharp
var accounts = client.Accounts.ToGetRequestInformation().URI.ToString(); // <service root>/accounts

// Active accounts per industry: the sum of a column and the number of rows.
var groups = await client.Accounts
    .WithUrl($"{accounts}?$apply=filter(statecode eq 0)/groupby((industrycode),aggregate(revenue with sum as total,$count as n))")
    .GetAsync(cancellationToken: ct);
foreach (var g in groups?.Value ?? [])
{
    var industry = g.Industrycode;                             // a grouped column fills its typed property
    var total = Convert.ToDecimal(g.AdditionalData["total"]);  // an alias is in AdditionalData, as a decimal
    var count = Convert.ToInt32(g.AdditionalData["n"]);
}

// One number for a table or a filter.
var one = await client.Accounts
    .WithUrl($"{accounts}?$apply=filter(statecode eq 0)/aggregate($count as n)")
    .GetAsync(cancellationToken: ct);
var active = Convert.ToInt32(one!.Value![0].AdditionalData["n"]);
```

- **The response is the collection's own type**: one item per group, the other properties null. Map each item to your own record (`StageTotal(stage, total, count)`) in the service; don't pass half-empty models to the UI.
- **`filter(...)/` comes first** to narrow the rows, then `groupby((columns),aggregate(...))` or `aggregate(...)` alone. The functions are `sum`, `average`, `min`, `max`, `countdistinct` and `$count`.
- **Expose each as its own feed** (`Feed.Async` over the service method), refreshed after a write that changes it. A total doesn't need the list, and the list doesn't need the total.
- **Probe `$apply` before relying on it**, as `references/odata.md` describes for the other query options: not every OData service implements it. Dataverse does, and refuses an aggregate over more than 50,000 rows: narrow it with `filter` first.
- **For the count of one filtered list**, `QueryParameters.Count = true` on the list's own request is enough (above).
- **Dataverse does not group by a lookup column in `$apply`.** `groupby((_parentcustomerid_value),...)`, the lookup's own name and its navigation property each answer 400. Grouping by a column of the related row (`groupby((primarycontactid/fullname),...)`) works for some lookups and answers 400 for others, so probe the one you need. When it is refused, send the same total as a FetchXML aggregate on the collection, still one request: `.WithUrl($"{accounts}?fetchXml={Uri.EscapeDataString(fetch)}")`, where `fetch` is `<fetch aggregate="true"><entity name="account"><attribute name="primarycontactid" alias="contact" groupby="true" /><attribute name="revenue" alias="total" aggregate="sum" /></entity></fetch>`. Every alias arrives in `AdditionalData`: the grouped lookup as the related row's id in a string, the totals as numbers. A group whose lookup is empty has no key for it, so read it with `TryGetValue`. Don't read every row to add them up in the app.

## Filter, sort and search are OData strings

```csharp
var text = query.Replace("'", "''"); // a quote inside an OData string literal is doubled
r.QueryParameters.Filter = $"contains(name,'{text}') or contains(emailaddress1,'{text}')";
r.QueryParameters.Orderby = ["name desc"];
```

- **Build a search with `$filter`** when `$search` is in the recipe's `unsupported` list, which is true for Dataverse and for TripPin. Probe the filter functions you use too, as `references/odata.md` describes, before you rely on them.
- **Use the schema's property names**: lower case for Dataverse (`name`, `emailaddress1`), Pascal case for TripPin (`FirstName`).
- **Every read names its columns with `Select`**: a list page, a related list, a picker and a get by key (below). A request without it returns every column: Dataverse's `account` has 152, and `contact` has 225. The service's tests can hold this: every GET for rows carries `$select`.
- **Sort a column that shows a related row by that row's name**, not by the key the row holds. How the service spells that differs, so probe it (`references/odata.md`); Dataverse's way is in `references/dataverse.md`.

## Get by key

```csharp
var account = await client.AccountsWithAccountid(id)
    .GetAsync(r => r.QueryParameters.Select = ["accountid", "name", "telephone1"], ct);
var person = await client.PeopleWithUserName(TextKey(userName)).GetAsync(cancellationToken: ct);

static string TextKey(string value) => "'" + value.Replace("'", "''") + "'"; // an OData string literal
```

- **A get by key takes `Select` too**: the columns the record page shows and edits, not the whole row.
- **The key's type comes from the schema.** It's a `Guid` for Dataverse (`AccountsWithAccountid(Guid)`) and a `string` for TripPin (`PeopleWithUserName(string)`).
- **Pass a text key as an OData string literal.** The generated builder puts its argument in the path as it is, so wrap it in quotes and double any quote inside; Kiota percent-encodes the result (`People(%27o%27%27brien%27)`). A bare key fails: TripPin answers with an HTML 500, which surfaces as `InvalidOperationException`, not `ApiException`. Don't edit the generated builder or the OpenAPI document to add the quotes.

## Errors carry the service's message

```csharp
catch (ODataError e)
{
    var message = e.Message; // Dataverse: "... exceeded the maximum allowed length of '160'."
    var status = e.ResponseStatusCode;
}
```

`ODataError` is generated into `<Namespace>.Models.ODataErrors`. It derives from `ApiException`, and its `Message` is the service's own (`Error.Message`). Catch it before `ApiException` and show that message: the service's validation messages are specific.
