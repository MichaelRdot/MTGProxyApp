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

    public async Task<List<CardDto>> GetPrintsAsync(Uri printsSearchUri)
    {
        var all = new List<CardDto>();
        var data = await _httpService.GetResponse<ScryfallListDto<CardDto>>(printsSearchUri);
        foreach (var cardArt in data.Data)
        { 
            all.AddRange(cardArt);
        }
        
        return all;
    }
}