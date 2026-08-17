using YamlDotNet.Serialization;

namespace AuthSeparation.Testing;

/// <summary>
/// Reads one of the OpenAPI contracts at test time.
///
/// Each service's tests derive their expectations from its specification rather
/// than from a hand-copied list, so that adding an operation or a schema to a
/// contract and forgetting to regenerate the stub fails the suite. That coupling
/// is the point: this repository's claim is that the specifications produce the
/// code.
///
/// Shared rather than copied per service. `AUTH-020` had one copy, which was
/// fine; `AUTH-021` would have made two and `AUTH-022` three, all differing only
/// in a filename.
/// </summary>
public sealed class Contract
{
    private readonly Dictionary<object, object> _document;

    /// <param name="specPath">Repository-relative path, e.g. `specs/auth-separation_authz-api_v1.yaml`.</param>
    public Contract(string specPath)
    {
        SpecPath = specPath;
        _document = new DeserializerBuilder().Build()
            .Deserialize<Dictionary<object, object>>(File.ReadAllText(Locate(specPath)));
        BasePath = DeriveBasePath();
    }

    public string SpecPath { get; }

    /// <summary>Server path prefix, taken from the contract's server URL (e.g. "/v1").</summary>
    public string BasePath { get; }

    /// <summary>
    /// Every operation in the contract. <paramref name="Path"/> is the contract's
    /// own template; <paramref name="RequestPath"/> has its parameters filled in
    /// so it can actually be requested.
    /// </summary>
    public IEnumerable<(string Method, string Path, string RequestPath)> Operations()
    {
        foreach (var (path, item) in Map(_document, "paths"))
        {
            var operations = (Dictionary<object, object>)item;
            foreach (var (method, _) in operations)
            {
                // `parameters` and `summary` are siblings of the operations, not operations.
                var verb = (string)method;
                if (verb is "get" or "put" or "post" or "delete" or "patch" or "head" or "options")
                {
                    var template = (string)path;
                    yield return (verb.ToUpperInvariant(), template, FillPathParameters(template, operations));
                }
            }
        }
    }

    /// <summary>Every entry under components.schemas, by name.</summary>
    public IEnumerable<(string Name, Dictionary<object, object> Schema)> Schemas()
    {
        var components = (Dictionary<object, object>)_document["components"];
        foreach (var (name, schema) in (Dictionary<object, object>)components["schemas"])
        {
            yield return ((string)name, (Dictionary<object, object>)schema);
        }
    }

    /// <summary>
    /// The property names a schema declares in its own right — its own `properties`,
    /// plus those of any inline `allOf` branch. A `$ref` branch is excluded: it
    /// becomes a base class, so its properties are declared there.
    /// </summary>
    public static IReadOnlyCollection<string> OwnProperties(Dictionary<object, object> schema)
    {
        var names = new SortedSet<string>(StringComparer.Ordinal);
        Collect(schema, names);

        if (schema.TryGetValue("allOf", out var allOf))
        {
            foreach (Dictionary<object, object> branch in (List<object>)allOf)
            {
                if (!branch.ContainsKey("$ref"))
                {
                    Collect(branch, names);
                }
            }
        }

        return names;
    }

    private static void Collect(Dictionary<object, object> schema, SortedSet<string> into)
    {
        if (schema.TryGetValue("properties", out var properties))
        {
            foreach (var (name, _) in (Dictionary<object, object>)properties)
            {
                into.Add((string)name);
            }
        }
    }

    /// <summary>
    /// Substitutes `{name}` segments with values the router will accept.
    ///
    /// Only the generated parameter *type* constrains routing: a `uuid` becomes a
    /// C# `System.Guid` and will not bind unless the segment parses as one. String
    /// patterns in the contract are not route constraints, so any non-empty token
    /// serves — validating them is the business logic this slice deliberately
    /// does not build.
    /// </summary>
    private string FillPathParameters(string template, Dictionary<object, object> operations)
    {
        if (!template.Contains('{'))
        {
            return template;
        }

        var filled = template;
        foreach (var (name, format) in PathParameters(operations))
        {
            var value = format == "uuid" ? "00000000-0000-0000-0000-000000000000" : "sample";
            filled = filled.Replace("{" + name + "}", value, StringComparison.Ordinal);
        }

        return filled;
    }

    private IEnumerable<(string Name, string? Format)> PathParameters(Dictionary<object, object> pathItem)
    {
        // Path parameters may be declared on the path item itself or on each
        // operation; both are legal OpenAPI, and this contract set uses the
        // path-item form with `$ref`s into components.parameters. Reading only the
        // operations misses them entirely — and the miss is silent, because a URL
        // containing a literal "{roleKey}" still matches the route template
        // "roles/{roleKey}", so every request would appear to succeed.
        foreach (var (key, node) in pathItem)
        {
            var parameters = (string)key == "parameters"
                ? node
                : (node as Dictionary<object, object>)?.GetValueOrDefault("parameters");

            if (parameters is null)
            {
                continue;
            }

            foreach (Dictionary<object, object> parameter in (List<object>)parameters)
            {
                var resolved = Resolve(parameter);
                if (resolved.TryGetValue("in", out var location) && (string)location == "path")
                {
                    yield return ((string)resolved["name"], FormatOf(resolved));
                }
            }
        }
    }

    private string? FormatOf(Dictionary<object, object> parameter)
    {
        if (!parameter.TryGetValue("schema", out var schema))
        {
            return null;
        }

        var resolved = Resolve((Dictionary<object, object>)schema);
        return resolved.TryGetValue("format", out var format) ? (string)format : null;
    }

    /// <summary>Follows a local `$ref` one hop; returns the node unchanged otherwise.</summary>
    private Dictionary<object, object> Resolve(Dictionary<object, object> node)
    {
        if (!node.TryGetValue("$ref", out var reference))
        {
            return node;
        }

        var current = _document;
        foreach (var segment in ((string)reference).Split('/').Skip(1))
        {
            current = (Dictionary<object, object>)current[segment];
        }

        return current;
    }

    private static Dictionary<object, object> Map(Dictionary<object, object> parent, string key) =>
        (Dictionary<object, object>)parent[key];

    private string DeriveBasePath()
    {
        var servers = (List<object>)_document["servers"];
        var url = (string)((Dictionary<object, object>)servers[0])["url"];
        return new Uri(url).AbsolutePath.TrimEnd('/');
    }

    /// <summary>
    /// Walks up from the test binaries to the repository root. Test runners and CI
    /// disagree about the working directory; a specification's location relative
    /// to the repository root does not.
    /// </summary>
    private static string Locate(string specPath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, specPath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find {specPath} in any ancestor of {AppContext.BaseDirectory}.");
    }
}
