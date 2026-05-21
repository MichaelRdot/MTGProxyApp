using MTGProxyApp.Dtos;

namespace MTGProxyApp.Services;

public class ScryfallService(BulkDataService bulkDataService)
{
    public Task<List<CardDto>> SearchCards(string name, string? setCode, string? collectorNumber, string? lang = null, bool highresOnly = false)
    {
        var cards = bulkDataService.SearchByName(name, setCode, collectorNumber, lang, highresOnly);

        if (cards.Count == 0)
            cards = bulkDataService.SearchByName(name, lang: lang, highresOnly: highresOnly)
                .Where(c => c.CardFaces == null)
                .ToList();

        return Task.FromResult(cards);
    }

    public Task<List<CardDto>> GetPrintsByOracleId(string oracleId) =>
        Task.FromResult(bulkDataService.GetByOracleId(oracleId));
}
