# Writing to an OData Service

Create, update and delete through the client from `references/odata.md`. Dataverse's names for lookups and its status rules are in `references/dataverse.md`.

```csharp
var created = await client.Accounts.PostAsync(new Account { Name = name },
    r => r.Headers.Add("Prefer", "return=representation"), ct);
var etag = (string)account.AdditionalData["@odata.etag"]; // read with the row the form shows
await client.AccountsWithAccountid(id).PatchAsync(changes, r => r.Headers.Add("If-Match", etag), ct);
await client.AccountsWithAccountid(id).DeleteAsync(cancellationToken: ct);
```

- **A record form's update sends the etag of the row it loaded**, so a change someone else saved since is refused with 412 instead of overwritten: say so and offer to reload. Keep the etag with the loaded record, and take the new one from the read after the save. `If-Match: *` is for a write made without the row in hand, such as a status change from a list (*Concurrency and upserts*, below).

- **A create returns `null` without `Prefer: return=representation`.** The service answers 204 with no body. Send the header whenever you need the new entity or its key.
- **An update returns nothing.** The generated `PatchAsync` is a `Task`, so `return=representation` on it is read by no one. Read the row again with `GetAsync` when the form shows values the server computes.

## Send what changed, and clear with an explicit null

Kiota's JSON writer skips a property whose value is `null`: `new Account { Telephone1 = null }` serializes as `{}`, and the old phone number stays. So the PATCH body is the changed fields only, and a cleared field goes in `AdditionalData`, where a null is written:

```csharp
var changes = new Account();
if (draft.Name != saved.Name) changes.Name = draft.Name;
if (draft.Phone != saved.Phone)
{
    if (draft.Phone is null) changes.AdditionalData["telephone1"] = null!; // "telephone1": null
    else changes.Telephone1 = draft.Phone;
}
```

- **Use the schema's property name as the key**: the `<Property Name>` in `$metadata`, lower case on Dataverse.
- **Don't send every field.** It overwrites a change someone made since you read the row, and on Dataverse it runs every rule and plug-in registered on those columns.

## Lookups

A lookup is a single-valued navigation property. Set it with `@odata.bind` and the target's entity-set path, key in parentheses:

```csharp
changes.AdditionalData["parentcustomerid_account@odata.bind"] = $"/accounts({accountId})";
```

Clear it in one of two ways:

- **In the same PATCH**, on Dataverse: `changes.AdditionalData["parentcustomerid_account@odata.bind"] = null!;`. Dataverse answered 204 and cleared the value.
- **On any OData service**, `DELETE` the reference: `<set>(<key>)/<navigation>/$ref`. `WithUrl` takes the absolute URL, the registered service root plus that path:

  ```csharp
  await client.ContactsWithContactid(id)
      .WithUrl($"{serviceRoot}/contacts({id})/parentcustomerid_account/$ref")
      .DeleteAsync(cancellationToken: ct);
  ```

The navigation property's name is the schema's, not the lookup column's. On Dataverse it is often neither obvious nor the same across tables (`references/dataverse.md`).

## Concurrency and upserts

- **Dataverse's PATCH is an upsert.** A PATCH to a key that no longer exists, because someone deleted the row, creates it again. `If-Match: *` makes the update fail instead.
- **For optimistic concurrency, send the row's own etag.** Read `@odata.etag` from `AdditionalData` with the row, and send it as `If-Match`: the service answers 412 when the row changed since. `*` only checks that the row exists.
- **A set that declares `Org.OData.Core.V1.OptimisticConcurrency`** in `$metadata` (TripPin's `Airlines` does) expects that etag on every update and delete. A set that declares none may refuse `If-Match` altogether, as TripPin's `People` does: send it only where the schema or the service's documentation asks for it.

Verified with Kiota 1.38.0 and Microsoft.Kiota.Serialization.Json 1.22: a typed `null` is left out of the JSON, and a null in `AdditionalData` is written. The lookup bind and the `$ref` clear were run against Dataverse Web API v9.2.
