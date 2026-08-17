using System.Reflection;
using AuthSeparation.Testing;
using Newtonsoft.Json;

namespace AuthSeparation.AuthZ.Tests;

/// <summary>
/// `AUTH-021` criterion 2: the generated DTOs match `components.schemas` exactly.
///
/// Same assertions as AuthN's, against a contract with different shapes: seventeen
/// schemas, two scalar aliases, and **two** `allOf` compositions where AuthN had
/// one.
/// </summary>
public sealed class DtoFidelityTests
{
    private static readonly Contract Spec = new("specs/auth-separation_authz-api_v1.yaml");
    private static readonly Assembly Stub = typeof(Generated.AuthZController).Assembly;
    private const string Namespace = "AuthSeparation.AuthZ.Generated";

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
            // `UserId` (uuid) and `PermissionKey` (patterned string) are scalar
            // aliases. C# correctly inlines them to primitives, so the absence of a
            // type is the right outcome, not a gap.
            Assert.True(
                type is null,
                $"Schema '{name}' declares no properties, so it is a scalar alias and should inline to a primitive. " +
                $"A generated type named '{name}' means the generator started boxing aliases.");
            return;
        }

        Assert.True(type is not null, $"Schema '{name}' declares properties but no type was generated for it.");

        var actual = type!
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(p => p.GetCustomAttribute<JsonExtensionDataAttribute>() is null)
            .Select(p => p.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? p.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected.OrderBy(n => n, StringComparer.Ordinal).ToArray(), actual);
    }

    [Theory]
    [InlineData("Role", "RoleDefinition")]
    [InlineData("Policy", "PolicyDefinition")]
    public void A_composed_schema_inherits_rather_than_repeating_its_base(string derived, string expectedBase)
    {
        // Both of this contract's compositions are "definition plus server-assigned
        // timestamps". If the generator flattened them, the theory above would still
        // pass on the derived type's own properties while the relationship the
        // specification describes had been lost.
        var derivedType = Stub.GetType($"{Namespace}.{derived}");
        var baseType = Stub.GetType($"{Namespace}.{expectedBase}");

        Assert.NotNull(derivedType);
        Assert.NotNull(baseType);
        Assert.Equal(baseType, derivedType!.BaseType);
    }

    [Fact]
    public void The_contract_declares_the_schemas_ADR_0006_measured()
    {
        // ADR-0006 records 17 named schemas for AuthZ. Two are scalar aliases.
        var schemas = Spec.Schemas().ToArray();
        var aliases = schemas.Count(s => Contract.OwnProperties(s.Schema).Count == 0);

        Assert.Equal(17, schemas.Length);
        Assert.Equal(2, aliases);
    }
}
