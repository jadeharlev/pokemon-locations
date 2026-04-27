using Microsoft.Extensions.Configuration;
using PokemonLocations.TokenIssuer;

const string usage = "Usage: dotnet run --project Backend/PokemonLocations.TokenIssuer -- --client <id> [--days <n>]";
const string defaultIssuer = "pokemon-locations-api";
const string defaultAudience = "pokemon-locations-clients";
const int defaultDays = 90;

string? client = null;
int days = defaultDays;

for (int i = 0; i < args.Length; i++) {
    switch (args[i]) {
        case "--client":
            if (i + 1 >= args.Length) {
                Console.Error.WriteLine("--client requires a value");
                return 1;
            }
            client = args[++i];
            break;
        case "--days":
            if (i + 1 >= args.Length || !int.TryParse(args[++i], out days) || days <= 0) {
                Console.Error.WriteLine("--days requires a positive integer value");
                return 1;
            }
            break;
        case "--help":
        case "-h":
            Console.WriteLine(usage);
            return 0;
        default:
            Console.Error.WriteLine($"Unknown argument: {args[i]}");
            Console.Error.WriteLine(usage);
            return 1;
    }
}

if (string.IsNullOrWhiteSpace(client)) {
    Console.Error.WriteLine("--client <id> is required");
    Console.Error.WriteLine(usage);
    return 1;
}

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var key = configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(key)) {
    Console.Error.WriteLine("Jwt:Key is not configured. Set the Jwt__Key environment variable.");
    return 1;
}

var issuer = configuration["Jwt:Issuer"] ?? defaultIssuer;
var audience = configuration["Jwt:Audience"] ?? defaultAudience;

var token = TokenBuilder.Build(client, days, key, issuer, audience);
Console.WriteLine(token);
return 0;
