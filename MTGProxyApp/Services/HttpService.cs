using System.Net.Http.Headers;
using System.Text.Json;

namespace MTGProxyApp.Services;

public class HttpService
{
    private readonly HttpClient _client;

    public HttpService(HttpClient client)
    {
        _client = client;
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MTGProxyApp", "1.0"));
    }

    public async Task<T?> GetResponse<T>(Uri uri)
    {
        try
        {
            var response = await _client.GetAsync(uri);

            if ((int)response.StatusCode == 429)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(10);
                Console.WriteLine($"Rate limited. Waiting {retryAfter.TotalSeconds}s before retry...");
                await Task.Delay(retryAfter);
                response = await _client.GetAsync(uri); 
            }

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return default;
        }

        return default;
    }

    public async Task<byte[]> LoadCardImage(string cardImage)
    {
        return await _client.GetByteArrayAsync(cardImage);
    }
}