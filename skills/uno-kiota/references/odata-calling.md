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

- **A list the user scrolls is a paginated feed.** Read the `uno-mvux` skill's `references/pagination.md` before writing it, and feed it with cursor pagination, the next link as the cursor: `ListFeed<T>.PaginatedByCursorAsync`. Don't loop the next link into one list in memory and search it on the client.
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

## Filter, sort and search are OData strings

```csharp
var text = query.Replace("'", "''"); // a quote inside an OData string literal is doubled
r.QueryParameters.Filter = $"contains(name,'{text}') or contains(emailaddress1,'{text}')";
r.QueryParameters.Orderby = ["name desc"];
```

- **Build a search with `$filter`** when `$search` is in the recipe's `unsupported` list, which is true for Dataverse and for TripPin. Probe the filter functions you use too, as `references/odata.md` describes, before you rely on them.
- **Use the schema's property names**: lower case for Dataverse (`name`, `emailaddress1`), Pascal case for TripPin (`FirstName`).
- **Ask for the columns you show with `Select`.** Dataverse's `account` has 152 columns, and `contact` has 225.

## Get by key

```csharp
var account = await client.AccountsWithAccountid(id).GetAsync(cancellationToken: ct);
var person = await client.PeopleWithUserName(userName.Replace("'", "''")).GetAsync(cancellationToken: ct);
```

- **The key's type comes from the schema.** It's a `Guid` for Dataverse (`AccountsWithAccountid(Guid)`) and a `string` for TripPin (`PeopleWithUserName(string)`).
- **Double a quote in a text key.** The script puts a text key in quotes in the path, `People('{UserName}')`; Kiota percent-encodes the value but doesn't double a quote in it, so `o'brien` goes as `o''brien`.

## Errors carry the service's message

```csharp
catch (ODataError e)
{
    var message = e.Message; // Dataverse: "... exceeded the maximum allowed length of '160'."
    var status = e.ResponseStatusCode;
}
```

`ODataError` is generated into `<Namespace>.Models.ODataErrors`. It derives from `ApiException`, and its `Message` is the service's own (`Error.Message`). Catch it before `ApiException` and show that message: the service's validation messages are specific.
