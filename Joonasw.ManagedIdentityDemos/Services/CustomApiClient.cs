using Azure.Core;
using Joonasw.ManagedIdentityDemos.Options;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Joonasw.ManagedIdentityDemos.Services;

public class CustomApiClient
{
    private static AccessToken CachedToken;
    private readonly HttpClient _httpClient;
    private readonly DemoSettings _settings;
    private readonly TokenCredential _tokenCredential;

    public CustomApiClient(
        HttpClient httpClient,
        IOptionsSnapshot<DemoSettings> settings,
        TokenCredential tokenCredential)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _tokenCredential = tokenCredential;
    }

    public async Task<Dictionary<string, string>> Request()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"{_settings.CustomApiBaseUrl}/api/test");
        var accessToken = await GetAccessToken();
        req.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage res = await _httpClient.SendAsync(req);
        string resJson = await res.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<Dictionary<string, string>>(resJson);
    }

    private async ValueTask<string> GetAccessToken()
    {
        if (CachedToken.ExpiresOn > DateTime.UtcNow.AddMinutes(4))
        {
            return CachedToken.Token;
        }

        var scopes = new[] { $"{_settings.CustomApiApplicationIdUri}/.default" };
        CachedToken = await _tokenCredential.GetTokenAsync(new TokenRequestContext(scopes), default);
        return CachedToken.Token;
    }
}
