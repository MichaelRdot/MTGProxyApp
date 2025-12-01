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

    public async Task<PaginatedListDto<CardDto?>?> GetCardsBySearchQuery(string searchQuery)
    {
        searchQuery = searchQuery.Replace($"{(char)92}{(char)34}", $"{(char)34}");
        searchQuery = searchQuery.Replace(" ", "+");
        var uri = new Uri($"{_client.BaseAddress}search?q={searchQuery}");
        var cardList = await _httpService.GetResponse<PaginatedListDto<CardDto?>>(uri);
        return cardList ?? throw new Exception("Could not get anything from scryfall");
    }
}