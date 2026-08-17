// Host wiring for the AuthN stub. This file is hand-written and deliberately thin:
// everything that describes the API surface is generated from
// specs/auth-separation_authn-api_v1.yaml and lives under Generated/, which is
// never hand-edited (AUTH-020 criterion 4).
//
// AddNewtonsoftJson is required, not stylistic: the generated DTOs carry
// Newtonsoft.Json attributes, so without it request bodies would not bind as the
// contract specifies (ADR-0006, operational finding 1).

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddNewtonsoftJson();

var app = builder.Build();

app.MapControllers();

app.Run();

// Exposed so the test project can host the real application through
// WebApplicationFactory rather than re-declaring the wiring under test.
public partial class Program;
