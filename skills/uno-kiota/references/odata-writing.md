# Writing to an OData Service

Create, update and delete through the client from `references/odata.md`. The samples use the illustrative `Orders` and `Customers` schema of `references/odata-calling.md`. A vendor's own names and write rules are in its page (`references/dataverse.md` for Dataverse).

```csharp
var created = await client.Orders.PostAsync(new Order { Number = number },
    r => r.Headers.Add("Prefer", "return=representation"), ct);
var etag = (string)order.AdditionalData["@odata.etag"]; // read with the row the form shows
await client.OrdersWithOrderId(id).PatchAsync(changes, r => r.Headers.Add("If-Match", etag), ct);
await client.OrdersWithOrderId(id).DeleteAsync(cancellationToken: ct);
```

- **A record form's update sends the etag of the row it loaded**, so a change someone else saved since is refused with 412 instead of overwritten: say so and offer to reload. Keep the etag with the loaded record, and take the new one from the read after the save. `If-Match: *` is for a write made without the row in hand, such as a status change from a list (*Concurrency and upserts*, below).

- **A create returns `null` without `Prefer: return=representation`.** The service answers 204 with no body. Send the header whenever you need the new entity or its key.
- **An update returns nothing.** The generated `PatchAsync` is a `Task`, so `return=representation` on it is read by no one. Read the row again with `GetAsync` when the form shows values the server computes.

## Send what changed, and clear with an explicit null

Kiota's JSON writer skips a property whose value is `null`: `new Customer { Phone = null }` serializes as `{}`, and the old phone number stays. So the PATCH body is the changed fields only, and a cleared field goes in `AdditionalData`, where a null is written:

```csharp
var changes = new Customer();
if (draft.Name != saved.Name) changes.Name = draft.Name;
if (draft.Phone != saved.Phone)
{
    if (draft.Phone is null) changes.AdditionalData["Phone"] = null!; // "Phone": null
    else changes.Phone = draft.Phone;
}
```

- **Use the schema's property name as the key**: the `<Property Name>` in `$metadata`, in its casing, not the generated C# name.
- **Don't send every field.** It overwrites a change someone made since you read the row, and it runs whatever server logic is attached to those columns.

## Lookups

A lookup is a single-valued navigation property. Set it with `@odata.bind` and the target's entity-set path, key in parentheses:

```csharp
changes.AdditionalData["Customer@odata.bind"] = $"/Customers({customerId})";
```

Clear it by deleting the reference, `<set>(<key>)/<navigation>/$ref`. `WithUrl` takes the absolute URL, the registered service root plus that path:

```csharp
await client.OrdersWithOrderId(id)
    .WithUrl($"{serviceRoot}/Orders({id})/Customer/$ref")
    .DeleteAsync(cancellationToken: ct);
```

- **The navigation property's name is the schema's, not the key column's.** Read it from `$metadata`; a vendor can name it in a way neither obvious nor the same across tables, and its page says how to find it.
- **Some services also clear a lookup in the same PATCH**, with a null bind. That saves a request when the form saves other fields too, but use it only where the vendor's page or a probe on sample data shows it works.

## Concurrency and upserts

- **A service may treat PATCH as an upsert.** A PATCH to a key that no longer exists, because someone deleted the row, then creates it again. `If-Match: *` makes the update fail instead. The vendor's page or documentation says whether it upserts; when neither does, try it on sample data.
- **For optimistic concurrency, send the row's own etag.** Read `@odata.etag` from `AdditionalData` with the row, and send it as `If-Match`: the service answers 412 when the row changed since. `*` only checks that the row exists.
- **A set that declares `Org.OData.Core.V1.OptimisticConcurrency`** in `$metadata` expects that etag on every update and delete. A set that declares none may refuse `If-Match` altogether: send it only where the schema, the service's documentation or a probe shows it is accepted. The public TripPin sample has both kinds, `Airlines` and `People`.

Verified with Kiota 1.38.0 and Microsoft.Kiota.Serialization.Json 1.22: a typed `null` is left out of the JSON, and a null in `AdditionalData` is written. The lookup bind and the `$ref` clear were run against Dataverse Web API v9.2, under its own schema.
