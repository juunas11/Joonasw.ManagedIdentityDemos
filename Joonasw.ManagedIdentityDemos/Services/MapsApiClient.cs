using Azure.Core;
using Joonasw.ManagedIdentityDemos.Models.AzureMaps;
using Joonasw.ManagedIdentityDemos.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Joonasw.ManagedIdentityDemos.Services;

public class MapsApiClient
{
    private static AccessToken CachedToken;
    private readonly HttpClient _httpClient;
    private readonly DemoSettings _settings;
    private readonly TokenCredential _tokenCredential;

    public MapsApiClient(
        HttpClient httpClient,
        IOptionsSnapshot<DemoSettings> settings,
        TokenCredential tokenCredential)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _tokenCredential = tokenCredential;
    }

    public async Task<MapsPoiResults> SearchPointsOfInterest(string query)
    {
        var queryString = QueryString.Create(new Dictionary<string, string>
        {
            ["api-version"] = "1.0",
            ["query"] = query,
            ["countrySet"] = "BE,FR,NL,LU,GB,DE"
        });
        var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://atlas.microsoft.com/search/poi/json{queryString.Value}");
        var accessToken = await GetAccessToken();
        req.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        req.Headers.Add("x-ms-client-id", _settings.MapsClientId);

        HttpResponseMessage res = await _httpClient.SendAsync(req);
        string resJson = await res.Content.ReadAsStringAsync();

        var results = JsonSerializer.Deserialize<MapsPoiResults>(resJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return results;
    }

    private async ValueTask<string> GetAccessToken()
    {
        if (CachedToken.ExpiresOn > DateTime.UtcNow.AddMinutes(4))
        {
            return CachedToken.Token;
        }

        var scopes = new[] { "https://atlas.microsoft.com/.default" };
        CachedToken = await _tokenCredential.GetTokenAsync(new TokenRequestContext(scopes), default);
        return CachedToken.Token;
    }
}
