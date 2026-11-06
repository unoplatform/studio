# OData Services

An OData v4 service (Dataverse and Dynamics 365, Business Central, an ASP.NET Core OData server) publishes its schema as CSDL at `$metadata`, not as an OpenAPI document. Generate its client from that schema with the recipe below. Never hand-write an OpenAPI document for it, and don't generate from a vendor's OpenAPI export. Microsoft Graph is OData too, but it has its own SDK: use that.

## Recognize it

- A URL ending in `$metadata`, or an XML file whose root is `<edmx:Edmx>`.
- Responses carrying `@odata.context`, `@odata.nextLink` or `@odata.count`.
- Dataverse: `https://<org>.crm<N>.dynamics.com/api/data/v9.2/`, where the `crm` part varies by region (`crm`, `crm3`, …).
- A vendor OpenAPI export with paths like `/accounts({accountid})`. Dataverse's is a conversion of its schema with settings the app didn't choose. It has no typed next link and declares query options the service rejects. Generating from it takes minutes and fails. Fetch `$metadata` instead.

## The recipe

It needs the .NET 10 SDK. Run every step from the solution folder.

1. Add `hidi`, Microsoft's CSDL-to-OpenAPI converter, to the local tool manifest next to Kiota (`references/generate.md`): `dotnet tool install Microsoft.OpenApi.Hidi`.
2. Copy this skill's `assets/odata-scope.cs` to `odata/odata-scope.cs`. **Never put it in a project folder.** A `.cs` file there compiles into the app, its top-level statements replace the app's entry point, and the build still succeeds with only warning CS7022.
3. Fetch the schema to `odata/<Service>/metadata.xml` with the service's credentials. For Dataverse, that's `GET <org>/api/data/v9.2/$metadata` with a bearer token. Add `odata/*/metadata.xml` to `.gitignore`: Dataverse's is 6 MB.
4. Probe the query options the app will use (below).
5. Write `odata/<Service>/scope.json`. Entity-set names are the schema's `<EntitySet Name>`, usually plural:

   ```json
   { "entitySets": ["accounts", "contacts"], "unsupported": ["skip", "search"] }
   ```

6. Run `dotnet run odata/odata-scope.cs -- odata/<Service>`. It writes `scoped.xml`, the schema cut to those sets, and `openapi.yaml`, the derived document.
7. Generate the client once, with no `--include-path`:

   ```bash
   dotnet kiota generate -l CSharp -c DataverseClient -n MyApp.Dataverse \
     -d odata/Dataverse/openapi.yaml -o MyApp/Content/Client/DataverseClient
   ```

8. Add `"client": "MyApp/Content/Client/DataverseClient"` to `scope.json`, relative to the solution folder. To regenerate after the service changes, re-fetch `metadata.xml` and re-run step 6. The script rewrites both files, then runs `kiota update` on the client. Until step 7 has made the client, it says so and skips the update.

Check in `odata/` (except `metadata.xml`), the client with its `kiota-lock.json`, and `dotnet-tools.json`. After cloning, a teammate runs `dotnet tool restore` first. Never edit `scoped.xml`, `openapi.yaml` or the client: change `scope.json` and re-run.

## Probe the query options

Services rarely declare which query options they support, and the converter offers every option the schema doesn't rule out. Before writing `scope.json`, call each option the app will use once against a kept set, and compare with the same call without it:

| Result | Meaning | Example |
|---|---|---|
| 4xx or 5xx | Unsupported | Dataverse: `$skip` returns 400, `$search` 501 |
| The same rows as without it | Ignored, which is worse: the feature looks like it works | TripPin: `$search=Russell` returns all 20 people |

List both kinds in `unsupported` (`skip`, `top`, `search`, `count`, `filter`, `orderby`). The client then doesn't offer them, and the app gets the feature another way (`references/odata-calling.md`). Known so far: Dataverse `skip` and `search`, and TripPin `search`. Probe any other service.

Probe paging too:

- **Page size:** `$top=2` with `Prefer: odata.maxpagesize=1`. One row and an `@odata.nextLink` means the service pages. Two rows and no link means it's ignored (TripPin): the whole set comes back in one call.
- **`$skip` really moves:** `$top=1&$skip=1&$orderby=<key>` must return the second row of `$top=2&$orderby=<key>`. A 200 with the first row again means it's ignored.

## What the script keeps

- The named entity sets, the types they reach through properties and base types, and the navigation properties between kept sets. Everything else goes: other sets, actions, functions, and navigation properties to other types, on every type including base types. Dataverse's activities derive from `activitypointer`, whose 46 navigation properties would otherwise pull in 622 of its 1,427 types.
- No empty base types. Kiota rejects Dataverse's `crmbaseentity` ("The type does not contain any information").
- `hidi` settings that type `@odata.nextLink` and `@odata.count`, and generate no navigation, operation or `/$count` paths. A `/$count` path answers `text/plain`, which `AddKiotaClient` can't parse.
- It doesn't keep derived entity types or bound actions and functions. A derived type's extra properties land in `AdditionalData`.

## Traps

- `hidi` writes keyed paths in quotes (`'/accounts({accountid})':`), so a search for lines starting with `/` finds none.
- Kiota names a keyed path's builder `<Set>With<Key>`: `client.AccountsWithAccountid(id)`, not an indexer. A text key's builder puts its argument in the path as it is (`/People({UserName})`): pass an OData string literal (`references/odata-calling.md`).
- The derived document's server is `http://localhost`. Always pass the service URL (`references/register.md`). For Dataverse, that's `<org>/api/data/v9.2`.

Verified with `hidi` 3.10.2, Kiota 1.38.0 and .NET SDK 10.0.400, against Dataverse Web API v9.2 and TripPin, and in two Dataverse apps built on Uno.Sdk 6.8.0-dev.51 and 7.0.0-dev.132.
