using System.Text.Json.Serialization;
using MTGProxyApp.Dtos;

namespace MTGProxyApp.Services;

public class ScryfallService
{
    private readonly HttpClient _client;
    private readonly HttpService _httpService;

    public ScryfallService(HttpService httpService, HttpClient client)
    {
        _client = client;
        _httpService = httpService;
        _client.BaseAddress = new Uri("https://api.scryfall.com/cards/");
    }

    // Named search: nameSearchType: "exact" or "fuzzy"
    public async Task<CardDto?> GetCardByName(string nameSearchType, string cardName)
    {
        var escaped = Uri.EscapeDataString(cardName);
        var card = await _httpService.GetResponse<CardDto?>(
            new Uri($"{_client.BaseAddress}named?{nameSearchType}={escaped}"));
        return card;
    }

    // When user specifies set + collector, fetch that exact printing
    public async Task<CardDto?> GetCardBySetAndCollector(string setCode, string collectorNumber)
    {
        var uri = new Uri($"{_client.BaseAddress}{setCode.ToLowerInvariant()}/{collectorNumber}");
        return await _httpService.GetResponse<CardDto?>(uri);
    }

    public async Task<ScryfallListDto<CardDto>> GetPrintsAsync(Uri printsSearchUri)
    {
        var all = await _httpService.GetResponse<ScryfallListDto<CardDto>>(printsSearchUri);
        return all;
    }
    public class ScryfallList<T>
    {
        [JsonPropertyName("data")] public List<T>? Data { get; set; }
        [JsonPropertyName("has_more")] public bool HasMore { get; set; }
        [JsonPropertyName("next_page")] public string? NextPage { get; set; }
    }

// Small return type for prints pages
    public record PrintsPage(List<CardDto> Data, string? NextPage);

    public async Task<CardDto?> GetCardBySetAndCollectorAsync(string set, string collector)
    {
        var path = $"cards/{set}/{collector}".ToLowerInvariant();
        return await _httpService.GetResponse<CardDto>(new Uri(_client + path));
    }

    public async Task<CardDto?> GetBestPrintingForNameAsync(string name)
    {
        // Try exact, then fuzzy
        var exact = new Uri(_client + $"cards/named?exact={Uri.EscapeDataString(name)}");
        var card = await _httpService.GetResponse<CardDto>(exact);
        if (card is not null) return card;

        var fuzzy = new Uri(_client + $"cards/named?fuzzy={Uri.EscapeDataString(name)}");
        return await _httpService.GetResponse<CardDto>(fuzzy);
    }

    public async Task<PrintsPage> GetPrintsPageAsync(Uri uri)
    {
        var page = await _httpService.GetResponse<ScryfallList<CardDto>>(uri);
        var data = page?.Data ?? new List<CardDto>();
        var next = (page?.HasMore ?? false) ? page?.NextPage : null;
        return new PrintsPage(data, next);
    }
}