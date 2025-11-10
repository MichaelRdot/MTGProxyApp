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
        // Keep this — we’ll use it to combine relative paths below.
        _client.BaseAddress = new Uri("https://api.scryfall.com/cards/");
    }

    public async Task<CardDto?> GetCardBySetAndCollectorAsync(string set, string collector)
    {
        // BaseAddress already ends with "/cards/"
        var uri = new Uri(_client.BaseAddress!, $"{set}/{collector}".ToLowerInvariant());
        return await _httpService.GetResponse<CardDto>(uri);
    }

    public async Task<CardDto?> GetBestPrintingForNameAsync(string name)
    {
        // Try exact, then fuzzy
        var exact = new Uri(_client.BaseAddress!, $"named?exact={Uri.EscapeDataString(name)}");
        var card = await _httpService.GetResponse<CardDto>(exact);
        if (card is not null) return card;

        var fuzzy = new Uri(_client.BaseAddress!, $"named?fuzzy={Uri.EscapeDataString(name)}");
        return await _httpService.GetResponse<CardDto>(fuzzy);
    }

    public async Task<PrintsPage> GetPrintsPageAsync(Uri uri)
    {
        // Scryfall’s prints_search_uri is absolute already — just pass it through.
        var page = await _httpService.GetResponse<ScryfallListDto<CardDto>>(uri);
        var data = page?.Data ?? new List<CardDto>();
        var next = page?.HasMore ?? false ? page?.NextPage : null;
        return new PrintsPage(data, next);
    }

    // Small return type for prints pages
    public record PrintsPage(List<CardDto> Data, string? NextPage);
}