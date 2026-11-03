# Calling an OData Service

The client comes from `references/odata.md`. Register it like any Kiota client (`references/register.md`), with the service root as the Url: `<org>/api/data/v9.2` for Dataverse. The access token comes from the authentication handler (the `uno-authentication` skill, `references/http.md`). The service enforces these rules at runtime; the document can't express them.

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

- **Send `Prefer: odata.maxpagesize` on every request, including the one that follows the next link.** The link doesn't carry the page size. Without the header, Dataverse returned all 38 remaining rows as one page.
- **Follow the link with `WithUrl`, and pass only the header.** The link already carries the query options and the service's paging token.
- **Don't page with `$skip`** unless your probe showed the service supports it. Dataverse rejects it with a 400. To feed an MVUX list, use cursor pagination with the next link as the cursor: `ListFeed<T>.PaginatedByCursorAsync` in the `uno-mvux` skill (`references/pagination.md`).

## The count is in the collection response

Set `QueryParameters.Count = true` and read `OdataCount`. The recipe generates no `/$count` path: it answers `text/plain`, which `AddKiotaClient` can't parse (`references/content-types.md`).

## Filter, sort and search are OData strings

```csharp
var text = query.Replace("'", "''"); // a quote inside an OData string literal is doubled
r.QueryParameters.Filter = $"contains(name,'{text}') or contains(emailaddress1,'{text}')";
r.QueryParameters.Orderby = ["name desc"];
```

- **Build a search with `$filter`** when `$search` is in the recipe's `unsupported` list, which is true for Dataverse and for TripPin. Probe the filter functions you use too, as `references/odata.md` describes, before you rely on them.
- **Use the schema's property names**: lower case for Dataverse (`name`, `emailaddress1`), Pascal case for TripPin (`FirstName`).
- **Ask for the columns you show with `Select`.** Dataverse's `account` has 152 columns, and `contact` has 225.

## Get, create, update and delete by key

```csharp
var account = await client.AccountsWithAccountid(id).GetAsync(cancellationToken: ct);
var created = await client.Accounts.PostAsync(new Account { Name = name },
    r => r.Headers.Add("Prefer", "return=representation"), ct);
await client.AccountsWithAccountid(id).PatchAsync(new Account { Name = name }, cancellationToken: ct);
await client.AccountsWithAccountid(id).DeleteAsync(cancellationToken: ct);
```

- **A create returns `null` without `Prefer: return=representation`.** The service answers 204 with no body. Send the header whenever you need the new entity or its key.
- **The key's type comes from the schema.** It's a `Guid` for Dataverse (`AccountsWithAccountid(Guid)`) and a `string` for TripPin (`PeopleWithUserName("russellwhyte")`).

## Errors carry the service's message

```csharp
catch (ODataError e)
{
    var message = e.Message; // Dataverse: "... exceeded the maximum allowed length of '160'."
    var status = e.ResponseStatusCode;
}
```

`ODataError` is generated into `<Namespace>.Models.ODataErrors`. It derives from `ApiException`, and its `Message` is the service's own (`Error.Message`). Catch it before `ApiException` and show that message: the service's validation messages are specific.
