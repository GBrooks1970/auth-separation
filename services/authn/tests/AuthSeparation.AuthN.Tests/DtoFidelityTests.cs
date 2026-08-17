using System.Reflection;
using AuthSeparation.Testing;
using Newtonsoft.Json;

namespace AuthSeparation.AuthN.Tests;

/// <summary>
/// `AUTH-020` criterion 2: the generated DTOs match `components.schemas` exactly.
///
/// The spike established this once by hand (17/17 object schemas, zero property
/// mismatches). Encoding it as a test makes it permanent — 47 schemas across 19
/// `allOf` compositions is where generators produce plausible-looking but wrong
/// output, and a silent generator regression would otherwise be invisible until
/// something tried to use a DTO.
/// </summary>
public sealed class DtoFidelityTests
{
    private static readonly Contract Spec = new("specs/auth-separation_authn-api_v1.yaml");
    private static readonly Assembly Stub = typeof(Generated.AuthNController).Assembly;
    private const string Namespace = "AuthSeparation.AuthN.Generated";

    public static TheoryData<string> SchemaNames()
    {
        var data = new TheoryData<string>();
        foreach (var (name, _) in Spec.Schemas())
        {
            data.Add(name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(SchemaNames))]
    public void Each_schema_generates_a_type_whose_properties_match_it(string name)
    {
        var schema = Spec.Schemas().Single(s => s.Name == name).Schema;
        var expected = Contract.OwnProperties(schema);
        var type = Stub.GetType($"{Namespace}.{name}");

        if (expected.Count == 0)
        {
            // A schema with no properties is a scalar alias — `UserId` is a
            // `string`/`uuid`, not an object. C# correctly inlines these to
            // primitives, so the absence of a type is the right outcome, not a gap.
            Assert.True(
                type is null,
                $"Schema '{name}' declares no properties, so it is a scalar alias and should inline to a primitive. " +
                $"A generated type named '{name}' means the generator started boxing aliases.");
            return;
        }

        Assert.True(type is not null, $"Schema '{name}' declares properties but no type was generated for it.");

        // DeclaredOnly: an `allOf` with a `$ref` becomes inheritance, so the base
        // class owns the referenced properties and this type declares only its own.
        // The `[JsonExtensionData]` bucket is excluded: none of these schemas sets
        // `additionalProperties: false`, so NSwag emits a catch-all that carries no
        // contract property of its own.
        var actual = type!
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(p => p.GetCustomAttribute<JsonExtensionDataAttribute>() is null)
            .Select(p => p.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? p.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected.OrderBy(n => n, StringComparer.Ordinal).ToArray(), actual);
    }

    [Fact]
    public void ValidationError_inherits_from_Error_rather_than_repeating_it()
    {
        // The one composition in this contract. If the generator flattened it, the
        // test above would still pass on ValidationError's own property while the
        // relationship the specification describes had been lost.
        var validationError = Stub.GetType($"{Namespace}.ValidationError");
        var error = Stub.GetType($"{Namespace}.Error");

        Assert.NotNull(validationError);
        Assert.NotNull(error);
        Assert.Equal(error, validationError!.BaseType);
    }

    [Fact]
    public void The_contract_declares_the_schemas_the_spike_measured()
    {
        // ADR-0006 records 21 named schemas for AuthN, of which three are scalar
        // aliases. A contract that shrank would leave every case above passing.
        var schemas = Spec.Schemas().ToArray();
        var aliases = schemas.Count(s => Contract.OwnProperties(s.Schema).Count == 0);

        Assert.Equal(21, schemas.Length);
        Assert.Equal(3, aliases);
    }
}
