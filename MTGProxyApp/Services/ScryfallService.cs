using MTGProxyApp.Dtos;

namespace MTGProxyApp.Services;

public class ScryfallService(BulkDataService bulkDataService)
{
    public Task<List<CardDto>> SearchCards(string name, string? setCode, string? collectorNumber)
    {
        var cards = bulkDataService.SearchByName(name, setCode, collectorNumber);

        if (cards.Count == 0)
            cards = bulkDataService.SearchByName(name)
                .Where(c => c.CardFaces == null)
                .ToList();

        return Task.FromResult(cards);
    }

    public Task<List<CardDto>> GetPrintsByOracleId(string oracleId) =>
        Task.FromResult(bulkDataService.GetByOracleId(oracleId));
}
