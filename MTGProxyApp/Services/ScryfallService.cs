using MTGProxyApp.Dtos;
using System.Text.Json;

namespace MTGProxyApp.Services;

public class ScryfallService
{
    private readonly HttpService _httpService;
    private readonly HttpClient _client;

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
        var card = await _httpService.GetResponse<CardDto?>(new Uri($"{_client.BaseAddress}named?{nameSearchType}={escaped}"));
        return card;
    }

    // When user specifies set + collector, fetch that exact printing
    public async Task<CardDto?> GetCardBySetAndCollector(string setCode, string collectorNumber)
    {
        var uri = new Uri($"{_client.BaseAddress}{setCode.ToLowerInvariant()}/{collectorNumber}");
        return await _httpService.GetResponse<CardDto?>(uri);
    }

    // Scryfall lists use a standard "List" envelope: { has_more, data: [...] }
    public class ScryfallList<T>
    {
        public bool Has_More { get; set; }
        public string? Next_Page { get; set; }
        public List<T> Data { get; set; } = new();
    }

    public async Task<List<CardDto>> GetPrintsAsync(Uri printsSearchUri)
    {
        var all = new List<CardDto>();
        var next = printsSearchUri;
        while (true)
        {
            var page = await _httpService.GetResponse<ScryfallList<CardDto>>(next);
            if (page == null) break;

            all.AddRange(page.Data);
            if (page.Has_More && !string.IsNullOrWhiteSpace(page.Next_Page))
            {
                next = new Uri(page.Next_Page);
            }
            else break;
        }
        return all;
    }
}