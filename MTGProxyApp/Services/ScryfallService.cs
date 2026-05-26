using MTGProxyApp.Dtos;

namespace MTGProxyApp.Services;

public class ScryfallService(BulkDataService bulkDataService, HttpService httpService)
{
    private const string ScryfallSearchUrl = "https://api.scryfall.com/cards/search";
    public List<CardDto> SearchCards(string name, string? setCode, string? collectorNumber, string? lang = null, bool highresOnly = false)
    {
        var cards = bulkDataService.SearchByName(name, setCode, collectorNumber, lang, highresOnly);

        // When set+collector yields nothing (e.g. promo variants), fall back to any non-MDFC print
        // so the card still resolves rather than silently failing
        if (cards.Count == 0)
            cards = bulkDataService.SearchByName(name, lang: lang, highresOnly: highresOnly)
                .Where(c => c.CardFaces == null)
                .ToList();

        // Fall back to flavor name (showcase/alternate art names like Godzilla, FF crossover, SLD alts)
        if (cards.Count == 0)
            cards = bulkDataService.SearchByFlavorName(name, setCode, collectorNumber, lang, highresOnly);

        if (cards.Count == 0)
            cards = bulkDataService.SearchByFlavorName(name, lang: lang, highresOnly: highresOnly)
                .Where(c => c.CardFaces == null)
                .ToList();

        // Fall back to printed name (localized/non-English card names)
        if (cards.Count == 0)
            cards = bulkDataService.SearchByPrintedName(name, setCode, collectorNumber, lang, highresOnly);

        if (cards.Count == 0)
            cards = bulkDataService.SearchByPrintedName(name, lang: lang, highresOnly: highresOnly)
                .Where(c => c.CardFaces == null)
                .ToList();

        return cards;
    }

    public List<CardDto> GetPrintsByOracleId(string oracleId, string? lang = null, bool highresOnly = false) =>
        bulkDataService.GetByOracleId(oracleId, lang, highresOnly);

    public async Task<HashSet<string>> GetIllustrationIdsByArtTagAsync(string artTag)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // unique=art returns one card per distinct illustration, giving us all illustration IDs
        // that carry this tag without the oracle_id constraint (which caused Scryfall's default
        // unique=cards deduplication to collapse all prints to a single result).
        // Client-side filtering against _allCards already scopes results to the right card.
        var query = Uri.EscapeDataString($"art:\"{artTag}\"");
        Uri? uri = new($"{ScryfallSearchUrl}?q={query}&unique=art");

        while (uri != null)
        {
            var response = await httpService.GetResponse<ScryfallSearchResponseDto>(uri);
            if (response == null) break;

            foreach (var card in response.Data)
                if (card.IllustrationId != null)
                    result.Add(card.IllustrationId);

            uri = response.HasMore && response.NextPage != null
                ? new Uri(response.NextPage)
                : null;
        }

        return result;
    }
}
