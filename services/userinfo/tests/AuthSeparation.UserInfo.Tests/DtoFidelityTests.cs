using System.Reflection;
using AuthSeparation.Testing;
using Newtonsoft.Json;

namespace AuthSeparation.UserInfo.Tests;

/// <summary>
/// `AUTH-022` criterion 2: the generated DTOs match `components.schemas` exactly.
///
/// The smallest of the three contracts — nine schemas — and the last one needed to
/// close the slice.
/// </summary>
public sealed class DtoFidelityTests
{
    private static readonly Contract Spec = new("specs/auth-separation_userinfo-api_v1.yaml");
    private static readonly Assembly Stub = typeof(Generated.UserInfoController).Assembly;
    private const string Namespace = "AuthSeparation.UserInfo.Generated";

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
            // `UserId` (uuid) and `ConsentKey` (patterned string) are scalar aliases.
            // C# correctly inlines them to primitives, so the absence of a type is
            // the right outcome, not a gap.
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

    [Fact]
    public void ValidationError_inherits_from_Error_rather_than_repeating_it()
    {
        // The one composition in this contract, as in AuthN's.
        var validationError = Stub.GetType($"{Namespace}.ValidationError");
        var error = Stub.GetType($"{Namespace}.Error");

        Assert.NotNull(validationError);
        Assert.NotNull(error);
        Assert.Equal(error, validationError!.BaseType);
    }

    [Fact]
    public void The_contract_declares_the_schemas_ADR_0006_measured()
    {
        // ADR-0006 records 9 named schemas for User Info. Two are scalar aliases.
        var schemas = Spec.Schemas().ToArray();
        var aliases = schemas.Count(s => Contract.OwnProperties(s.Schema).Count == 0);

        Assert.Equal(9, schemas.Length);
        Assert.Equal(2, aliases);
    }
}
