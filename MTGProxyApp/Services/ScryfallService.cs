using MTGProxyApp.Dtos;

namespace MTGProxyApp.Services;

public class ScryfallService(BulkDataService bulkDataService)
{
    public List<CardDto> SearchCards(string name, string? setCode, string? collectorNumber, string? lang = null, bool highresOnly = false)
    {
        var cards = bulkDataService.SearchByName(name, setCode, collectorNumber, lang, highresOnly);

        // When set+collector yields nothing (e.g. promo variants), fall back to any non-MDFC print
        // so the card still resolves rather than silently failing
        if (cards.Count == 0)
            cards = bulkDataService.SearchByName(name, lang: lang, highresOnly: highresOnly)
                .Where(c => c.CardFaces == null)
                .ToList();

        // Fall back to printed name (localized/non-English card names) when English name lookup fails
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
}
