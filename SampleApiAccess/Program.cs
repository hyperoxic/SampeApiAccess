using IdentityModel.Client;
using Microsoft.Extensions.Configuration;

namespace ConsoleClient;

class Program
{
    static async Task Main(string[] args)
    {
        // setup the configuration file
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        IConfiguration configuration = builder.Build();

        // retrieve variables from appsettings.json
        var clientId = configuration.GetSection("API")["ClientId"];
        var clientSecret = configuration.GetSection("API")["ClientSecret"];
        var apiScope = configuration.GetSection("API")["ApiScope"];
        var dbName = configuration.GetSection("API")["DbName"];
        var identityEndpoint = configuration.GetSection("API")["IdentityEndpoint"];
        var apiEndpoint = configuration.GetSection("API")["ApiEndpoint"];

        // create an HttpClient instance
        var httpClient = new HttpClient();

        // discover the endpoints using metadata
        var discoveryDocument = await httpClient.GetDiscoveryDocumentAsync(identityEndpoint);

        if (discoveryDocument.IsError)
        {
            Console.WriteLine(discoveryDocument.Error);
            return;
        }

        // request a token using client credentials
        var tokenResponse = await httpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = discoveryDocument.TokenEndpoint,
            ClientId = clientId,
            ClientSecret = clientSecret,
            Scope = apiScope
        });

        // test for token
        if (tokenResponse.IsError)
        {
            Console.WriteLine(tokenResponse.Error);
            return;
        }

        // write out token to console
        Console.WriteLine(tokenResponse.Json);

        // call the API using the access token
        var apiClient = new HttpClient();
        apiClient.SetBearerToken(tokenResponse.AccessToken);

        // build variables for api call
        var date = DateOnly.Parse("2023-04-18");
        var formattedDate = date.ToString("yyyy-MM-dd");
        var uri = new Uri($"{apiEndpoint}/GetSchedule/{formattedDate}?dbName={dbName}");

        // make api call
        var response = await apiClient.GetAsync(uri);

        // test for success
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine(response.StatusCode);
            return;
        }

        // write out response to console
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(content);
    }
}