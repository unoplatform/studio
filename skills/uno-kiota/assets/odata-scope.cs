// Scopes an OData service's schema to the entity sets an app uses, converts it to OpenAPI, and regenerates the client.
// Copied into the app by the uno-kiota skill (references/odata.md) as <Solution>/odata/odata-scope.cs, never inside a project
// folder: a .cs file there compiles into the app and its top-level statements replace the app's entry point (warning CS7022).
// Run from the solution folder:
//
//   dotnet run odata/odata-scope.cs -- odata/<Service>
//
// odata/<Service>/scope.json:  { "entitySets": ["accounts", "contacts"], "unsupported": ["skip", "search"], "client": "<App>/Content/Client/<Client>" }
// odata/<Service>/metadata.xml: the service's $metadata, fetched beforehand (git-ignored).
// Writes scoped.xml and openapi.yaml next to them, then runs `dotnet kiota update -o <client>` when "client" is set.
using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;

if (args.Length != 1)
{
    Console.Error.WriteLine("usage: dotnet run odata-scope.cs -- <service folder>");
    return 2;
}

var dir = Path.GetFullPath(args[0]);
using var scope = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "scope.json")));
string[] Strings(string name) => scope.RootElement.TryGetProperty(name, out var p) ? [.. p.EnumerateArray().Select(e => e.GetString()!)] : [];
var wanted = Strings("entitySets");
var unsupported = Strings("unsupported");

XNamespace edm = "http://docs.oasis-open.org/odata/ns/edm";
var doc = XDocument.Load(Path.Combine(dir, "metadata.xml"));
var schemas = doc.Descendants(edm + "Schema").ToList();
string[] typeKinds = ["EntityType", "ComplexType", "EnumType", "TypeDefinition"];

var aliases = new Dictionary<string, string>();
var types = new Dictionary<string, XElement>();
foreach (var s in schemas)
{
    var ns = s.Attribute("Namespace")!.Value;
    aliases[ns] = ns;
    if (s.Attribute("Alias") is { } alias) aliases[alias.Value] = ns;
    foreach (var e in s.Elements().Where(e => e.Name.Namespace == edm && typeKinds.Contains(e.Name.LocalName)))
        types[$"{ns}.{e.Attribute("Name")!.Value}"] = e;
}

string? Resolve(string? name)
{
    if (string.IsNullOrEmpty(name)) return null;
    if (name.StartsWith("Collection(")) name = name["Collection(".Length..^1];
    if (name.StartsWith("Edm.")) return null;
    var dot = name.LastIndexOf('.');
    var prefix = dot < 0 ? "" : name[..dot];
    return $"{aliases.GetValueOrDefault(prefix, prefix)}.{name[(dot + 1)..]}";
}

bool IsEmpty(XElement e) => e.Name.LocalName is "EntityType" or "ComplexType"
    && !e.Elements().Any(c => c.Name.LocalName is "Property" or "NavigationProperty" or "Key");

var keptSets = new Dictionary<string, string>();
foreach (var set in schemas.SelectMany(s => s.Elements(edm + "EntityContainer")).SelectMany(c => c.Elements(edm + "EntitySet")))
    if (wanted.Contains(set.Attribute("Name")!.Value))
        keptSets[set.Attribute("Name")!.Value] = Resolve(set.Attribute("EntityType")!.Value)!;
var missing = wanted.Except(keptSets.Keys).Order().ToList();
if (missing.Count > 0)
{
    Console.Error.WriteLine($"no entity set named {string.Join(", ", missing)}");
    return 1;
}
var keptEntities = keptSets.Values.ToHashSet();

// Navigation properties that leave the kept entity types go first, so they don't pull their targets in. On every type, not only
// the kept ones: a kept type's base type has its own (Dataverse's activitypointer has 46), and they would pull most of the schema in.
foreach (var e in types.Values)
    e.Elements(edm + "NavigationProperty").Where(n => !keptEntities.Contains(Resolve(n.Attribute("Type")!.Value)!)).Remove();

// Everything the kept entity types reach. ponytail: no derived types or bound operations; add them when a scenario needs type casts or actions.
var reached = new HashSet<string>();
var todo = new Stack<string?>(keptEntities);
while (todo.Count > 0)
{
    var name = todo.Pop();
    if (name is null || !types.TryGetValue(name, out var e) || !reached.Add(name)) continue;
    todo.Push(Resolve(e.Attribute("BaseType")?.Value));
    foreach (var p in e.Elements().Where(p => p.Name.LocalName is "Property" or "NavigationProperty"))
        todo.Push(Resolve(p.Attribute("Type")?.Value));
    todo.Push(Resolve(e.Attribute("UnderlyingType")?.Value));
}

// Kiota rejects an allOf over a schema with no properties ("The type does not contain any information"): subtypes stop deriving from empty base types, which then go.
foreach (var name in reached)
    if (Resolve(types[name].Attribute("BaseType")?.Value) is { } b && types.TryGetValue(b, out var baseType) && IsEmpty(baseType))
        types[name].Attribute("BaseType")!.Remove();
reached.RemoveWhere(n => IsEmpty(types[n]) && !keptEntities.Contains(n));

foreach (var s in schemas)
{
    var ns = s.Attribute("Namespace")!.Value;
    s.Elements().Where(c => c.Name.LocalName switch
    {
        "EntityType" or "ComplexType" or "EnumType" or "TypeDefinition" => !reached.Contains($"{ns}.{c.Attribute("Name")!.Value}"),
        "Action" or "Function" or "Term" or "Annotations" => true,
        _ => false,
    }).Remove();
    foreach (var c in s.Elements(edm + "EntityContainer"))
    {
        c.Elements().Where(x => !(x.Name == edm + "EntitySet" && keptSets.ContainsKey(x.Attribute("Name")!.Value)) && x.Name != edm + "Annotation").Remove();
        c.Elements(edm + "EntitySet").Elements(edm + "NavigationPropertyBinding").Where(b => !keptSets.ContainsKey(b.Attribute("Target")!.Value)).Remove();
    }
}

// Query options the service rejects or ignores although its schema doesn't say so: the converter leaves them out.
var caps = new Dictionary<string, (string Term, string? Property)>
{
    ["skip"] = ("SkipSupported", null),
    ["top"] = ("TopSupported", null),
    ["search"] = ("SearchRestrictions", "Searchable"),
    ["count"] = ("CountRestrictions", "Countable"),
    ["filter"] = ("FilterRestrictions", "Filterable"),
    ["orderby"] = ("SortRestrictions", "Sortable"),
};
if (unsupported.FirstOrDefault(o => !caps.ContainsKey(o)) is { } unknown)
{
    Console.Error.WriteLine($"unknown query option '{unknown}'; use {string.Join(", ", caps.Keys)}");
    return 1;
}
foreach (var s in schemas)
    foreach (var c in s.Elements(edm + "EntityContainer").ToList())
        foreach (var set in unsupported.Length > 0 ? keptSets.Keys.ToList() : [])
            s.Add(new XElement(edm + "Annotations", new XAttribute("Target", $"{s.Attribute("Namespace")!.Value}.{c.Attribute("Name")!.Value}/{set}"),
                unsupported.Select(o =>
                {
                    var (term, prop) = caps[o];
                    var a = new XElement(edm + "Annotation", new XAttribute("Term", $"Org.OData.Capabilities.V1.{term}"));
                    a.Add(prop is null
                        ? new XAttribute("Bool", "false")
                        : new XElement(edm + "Record", new XElement(edm + "PropertyValue", new XAttribute("Property", prop), new XAttribute("Bool", "false"))));
                    return a;
                })));

var scoped = Path.Combine(dir, "scoped.xml");
doc.Save(scoped);
Console.WriteLine($"entity sets {string.Join(", ", keptSets.Keys.Order())}; types kept {reached.Count} of {types.Count}");

// Typed @odata.nextLink and @odata.count; no navigation, operation or /$count paths (a /$count answers text/plain, which AddKiotaClient can't parse).
var settings = Path.Combine(Path.GetTempPath(), $"odata-scope-{Environment.ProcessId}.json");
File.WriteAllText(settings, """
    { "OpenApiConvertSettings": { "EnablePagination": true, "EnableCount": true, "EnableDollarCountPath": false,
      "EnableNavigationPropertyPath": false, "EnableOperationPath": false, "EnableOperationImportPath": false } }
    """);
try
{
    var code = Run("hidi", "transform", "--csdl", scoped, "-o", Path.Combine(dir, "openapi.yaml"), "-f", "yaml", "-v", "3.0", "--co", "--ll", "Error", "--sp", settings);
    if (code != 0) return code;
}
finally { File.Delete(settings); }

if (!scope.RootElement.TryGetProperty("client", out var client)) return 0;
var clientDir = Path.GetFullPath(client.GetString()!);
if (!File.Exists(Path.Combine(clientDir, "kiota-lock.json")))
{
    Console.WriteLine($"no client in {clientDir} yet: generate it once (references/odata.md, step 7); later runs update it");
    return 0;
}
return Run("kiota", "update", "-o", clientDir);

static int Run(params string[] arguments)
{
    using var p = Process.Start(new ProcessStartInfo("dotnet", arguments))!;
    p.WaitForExit();
    return p.ExitCode;
}
