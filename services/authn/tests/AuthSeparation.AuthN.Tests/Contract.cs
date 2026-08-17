using YamlDotNet.Serialization;

namespace AuthSeparation.AuthN.Tests;

/// <summary>
/// Reads the AuthN OpenAPI contract at test time.
///
/// The tests derive their expectations from the specification rather than from a
/// hand-copied list, so that adding an operation or a schema to the contract and
/// forgetting to regenerate the stub fails the suite. That coupling is the point:
/// this repository's claim is that the specifications produce the code.
/// </summary>
internal static class Contract
{
    private const string SpecPath = "specs/auth-separation_authn-api_v1.yaml";

    private static readonly Dictionary<object, object> Document = Load();

    /// <summary>Server path prefix, taken from the contract's server URL (e.g. "/v1").</summary>
    internal static string BasePath { get; } = DeriveBasePath();

    /// <summary>Every operation in the contract, as (HTTP method, path) pairs.</summary>
    internal static IEnumerable<(string Method, string Path)> Operations()
    {
        foreach (var (path, item) in Map(Document, "paths"))
        {
            foreach (var (method, _) in (Dictionary<object, object>)item)
            {
                // `parameters` and `summary` are siblings of the operations, not operations.
                var verb = (string)method;
                if (verb is "get" or "put" or "post" or "delete" or "patch" or "head" or "options")
                {
                    yield return (verb.ToUpperInvariant(), (string)path);
                }
            }
        }
    }

    /// <summary>Every entry under components.schemas, by name.</summary>
    internal static IEnumerable<(string Name, Dictionary<object, object> Schema)> Schemas()
    {
        var components = (Dictionary<object, object>)Document["components"];
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
    internal static IReadOnlyCollection<string> OwnProperties(Dictionary<object, object> schema)
    {
        var names = new SortedSet<string>(StringComparer.Ordinal);

        if (schema.TryGetValue("properties", out var direct))
        {
            foreach (var (name, _) in (Dictionary<object, object>)direct)
            {
                names.Add((string)name);
            }
        }

        if (schema.TryGetValue("allOf", out var allOf))
        {
            foreach (Dictionary<object, object> branch in (List<object>)allOf)
            {
                if (branch.ContainsKey("$ref"))
                {
                    continue;
                }

                if (branch.TryGetValue("properties", out var branchProperties))
                {
                    foreach (var (name, _) in (Dictionary<object, object>)branchProperties)
                    {
                        names.Add((string)name);
                    }
                }
            }
        }

        return names;
    }

    private static Dictionary<object, object> Map(Dictionary<object, object> parent, string key) =>
        (Dictionary<object, object>)parent[key];

    private static string DeriveBasePath()
    {
        var servers = (List<object>)Document["servers"];
        var url = (string)((Dictionary<object, object>)servers[0])["url"];
        return new Uri(url).AbsolutePath.TrimEnd('/');
    }

    private static Dictionary<object, object> Load()
    {
        var deserializer = new DeserializerBuilder().Build();
        return deserializer.Deserialize<Dictionary<object, object>>(File.ReadAllText(Locate()));
    }

    /// <summary>
    /// Walks up from the test binaries to the repository root. Test runners and CI
    /// disagree about the working directory; the specification's location relative
    /// to the repository root does not.
    /// </summary>
    private static string Locate()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, SpecPath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find {SpecPath} in any ancestor of {AppContext.BaseDirectory}.");
    }
}
