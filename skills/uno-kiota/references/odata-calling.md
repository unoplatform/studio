# Calling an OData Service

The client comes from `references/odata.md`. Register it like any Kiota client (`references/register.md`), with the service root as the Url: the URL `$metadata` hangs off. The service enforces the rules below at runtime; the document can't express them. Creating, updating and deleting are in `references/odata-writing.md`. What one vendor's service does beyond the protocol is in its own page (`references/dataverse.md` for Dataverse).

**Contents:** [Pages follow the next link](#pages-follow-the-next-link) · [The count is in the collection response](#the-count-is-in-the-collection-response) · [Totals and counts per group come from `$apply`](#totals-and-counts-per-group-come-from-apply) · [Filter, sort and search are OData strings](#filter-sort-and-search-are-odata-strings) · [Get by key](#get-by-key) · [Errors carry the service's message](#errors-carry-the-services-message)

The samples use an illustrative schema, `Orders` and `Customers`. Use your schema's names, with their casing: the `<EntitySet Name>` and `<Property Name>` in `$metadata`.

The access token comes from the authentication handler: the `uno-authentication` skill (`references/http.md`) for a user's sign-in, or your own `DelegatingHandler` registered in `UseHttp` for a service identity such as an app registration with a client secret.

## Pages follow the next link

```csharp
var page = await client.Orders.GetAsync(r =>
{
    r.Headers.Add("Prefer", "odata.maxpagesize=25");
    r.QueryParameters.Select = ["OrderId", "Number", "Amount"];
    r.QueryParameters.Orderby = ["Number", "OrderId"];
    r.QueryParameters.Count = true;
}, ct);
var total = page?.OdataCount;   // every matching row, not just this page
var next = page?.OdataNextLink; // null on the last page

var more = await client.Orders.WithUrl(next!)
    .GetAsync(r => r.Headers.Add("Prefer", "odata.maxpagesize=25"), ct);
```

- **A list the user scrolls is a paginated feed.** Read the `uno-mvux` skill's `references/pagination.md` before writing it, and feed it with cursor pagination, the next link as the cursor: `ListFeed<T>.PaginatedByCursorAsync`. Don't loop the next link into one list in memory and search it on the client. A table that holds 20 rows today is paged like one that holds 20,000: the app is built for the table, not for the test environment.
- **Each list reads its own table.** The orders page queries `Orders` and the customers page `Customers`, each with the filter that says what this user may see. Don't build a list from the rows another list loaded (the customers of the orders already in memory): a row with no such relation never shows, and every page waits on every table.
- **The service that wraps the client mirrors these calls**: a method per list that takes the search text, filter, sort and next link and returns one page, its count and the next link; a method per total (below); a get by key. Not one `LoadAsync` that returns every table.
- **Only a view that shows a filtered set at once reads it to the end**: a board of the open records, one month of a calendar. It follows the next link with its filter and a `Select` of the columns it shows, for that view alone. It is not the source of any other list, count or total.
- **Send `Prefer: odata.maxpagesize` on every request, including the one that follows the next link.** The link doesn't have to carry the page size: without the header, a service can return every remaining row as one page.
- **Follow the link with `WithUrl`, and pass only the header.** The link already carries the query options and the service's paging token.
- **When the probe found the page size ignored** (`references/odata.md`), there is never a next link: page with `$top` and `$skip` instead, in a stable order. A page shorter than `$top` is the last.

  ```csharp
  var page = await client.Orders.GetAsync(r =>
  {
      r.QueryParameters.Top = 25;
      r.QueryParameters.Skip = pageIndex * 25;
      r.QueryParameters.Orderby = ["OrderId"]; // the key, so pages don't overlap
  }, ct);
  ```

- **Don't page with `$skip`** unless your probe showed it moves. Some services refuse it with a 400, and others ignore it.

## The count is in the collection response

Set `QueryParameters.Count = true` and read `OdataCount`. The recipe generates no `/$count` path: it answers `text/plain`, which `AddKiotaClient` can't parse (`references/content-types.md`).

## Totals and counts per group come from `$apply`

A dashboard total, the number of rows per stage or status, and the count beside a filter are each one request the service answers. Don't load a table to add it up. The generated builders have no `$apply` parameter, so take the collection's URL from its builder and pass the whole URL to `WithUrl`:

```csharp
var orders = client.Orders.ToGetRequestInformation().URI.ToString(); // <service root>/Orders

// Open orders per category: the sum of a column and the number of rows.
var groups = await client.Orders
    .WithUrl($"{orders}?$apply=filter(Status eq 'Open')/groupby((Category),aggregate(Amount with sum as total,$count as n))")
    .GetAsync(cancellationToken: ct);
foreach (var g in groups?.Value ?? [])
{
    var category = g.Category;                                 // a grouped column fills its typed property
    var total = Convert.ToDecimal(g.AdditionalData["total"]);  // an alias is in AdditionalData, as a decimal
    var count = Convert.ToInt32(g.AdditionalData["n"]);
}

// One number for a table or a filter.
var one = await client.Orders
    .WithUrl($"{orders}?$apply=filter(Status eq 'Open')/aggregate($count as n)")
    .GetAsync(cancellationToken: ct);
var open = Convert.ToInt32(one!.Value![0].AdditionalData["n"]);
```

- **The response is the collection's own type**: one item per group, the other properties null. Map each item to your own record (`CategoryTotal(category, total, count)`) in the service; don't pass half-empty models to the UI.
- **`filter(...)/` comes first** to narrow the rows, then `groupby((columns),aggregate(...))` or `aggregate(...)` alone. The functions are `sum`, `average`, `min`, `max`, `countdistinct` and `$count`.
- **Expose each as its own feed** (`Feed.Async` over the service method), refreshed after a write that changes it. A total doesn't need the list, and the list doesn't need the total.
- **Probe each `$apply` request before relying on it** (`references/odata.md`). It is an optional extension: a service may not implement it, may cap the rows one aggregate reads, or may refuse a grouping it doesn't like, such as a column that points at a related row. Narrow with `filter` first when there is a cap.
- **When the service refuses the request**, look for its own aggregate query before anything else: the vendor's page or documentation (`references/dataverse.md` has Dataverse's). Reading every row to add them up in the app is the last resort, for a set the app already reads to the end, and it goes in the report.
- **For the count of one filtered list**, `QueryParameters.Count = true` on the list's own request is enough (above).

## Filter, sort and search are OData strings

```csharp
var text = query.Replace("'", "''"); // a quote inside an OData string literal is doubled
r.QueryParameters.Filter = $"contains(Name,'{text}') or contains(Email,'{text}')";
r.QueryParameters.Orderby = ["Name desc", "CustomerId"];
```

- **Build a search with `$filter`** when `$search` is in the recipe's `unsupported` list. Probe the filter functions you use too, as `references/odata.md` describes, before you rely on them.
- **Use the schema's property names**, in the schema's casing. One service spells them in lower case and another in Pascal case, and a wrong case is a 400.
- **Every read names its columns with `Select`**: a list page, a related list, a picker and a get by key (below). A request without it returns every column, which is hundreds on a large business table. The service's tests can hold this: every GET for rows carries `$select`.
- **End every `$orderby` with the key** (`Name asc,CustomerId`): rows that tie on the sort column otherwise repeat or go missing between pages.
- **Sort a column that shows a related row by that row's name**, not by the key the row holds. The protocol's spelling is the navigation path (`$orderby=Customer/Name`), but a service can refuse it or want an `$expand` with it, so probe it (`references/odata.md`) and check the vendor's page.

## Get by key

```csharp
var order = await client.OrdersWithOrderId(id)
    .GetAsync(r => r.QueryParameters.Select = ["OrderId", "Number", "Amount"], ct);
var customer = await client.CustomersWithCode(TextKey(code)).GetAsync(cancellationToken: ct);

static string TextKey(string value) => "'" + value.Replace("'", "''") + "'"; // an OData string literal
```

- **A get by key takes `Select` too**: the columns the record page shows and edits, not the whole row.
- **The key's type comes from the schema**: a `Guid` here for `OrdersWithOrderId(Guid)`, a `string` for `CustomersWithCode(string)`.
- **Pass a text key as an OData string literal.** The generated builder puts its argument in the path as it is, so wrap it in quotes and double any quote inside; Kiota percent-encodes the result (`Customers(%27o%27%27brien%27)`). A bare key fails, and not always as an `ApiException`: a service that answers with an HTML 500 surfaces as `InvalidOperationException`. Don't edit the generated builder or the OpenAPI document to add the quotes.

## Errors carry the service's message

```csharp
catch (ODataError e)
{
    var message = e.Message; // the service's own text, such as a length limit with its number
    var status = e.ResponseStatusCode;
}
```

`ODataError` is generated into `<Namespace>.Models.ODataErrors`. It derives from `ApiException`, and its `Message` is the service's own (`Error.Message`). Catch it before `ApiException` and show that message: the service's validation messages are specific.

These calls were run against Dataverse Web API v9.2 and the public TripPin sample service, under their own schemas.
