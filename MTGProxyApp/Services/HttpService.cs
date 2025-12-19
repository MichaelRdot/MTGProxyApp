using System.Net.Http.Headers;
using System.Text.Json;

namespace MTGProxyApp.Services;

public class HttpService
{
    private readonly HttpClient _client;

    public HttpService(HttpClient client)
    {
        _client = client;
        _client.DefaultRequestHeaders.Accept.Clear();
        _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MTGProxyApp", "1.0"));
    }

    public async Task<T?> GetResponse<T>(Uri uri, CancellationToken ct = default)
    {
        using var response = await _client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        return await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: ct);
    }

    public async Task<byte[]> LoadCardImage(string cardImage)
    {
        return await _client.GetByteArrayAsync(cardImage);
    }
}