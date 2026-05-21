using System.Text.Json;
using MTGProxyApp.Dtos;
using MTGProxyApp.Models;

namespace MTGProxyApp.Services;

public class BulkDataService : BackgroundService
{
    private const string BulkDataApiUrl = "https://api.scryfall.com/bulk-data";
    private const string AllCardsType = "all_cards";
    private const string MetadataFileName = "metadata.json";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BulkDataService> _logger;
    private readonly string _dataDirectory;

    private Dictionary<string, List<CardDto>> _nameIndex = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, List<CardDto>> _oracleIndex = new(StringComparer.OrdinalIgnoreCase);

    public bool IsReady { get; private set; }
    public string Status { get; private set; } = "Initializing…";

    public BulkDataService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<BulkDataService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _dataDirectory = configuration["BulkData:DataDirectory"] ?? "./bulk-data";
        Directory.CreateDirectory(_dataDirectory);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await InitializeAsync(stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await CheckForUpdateAsync(stoppingToken);
        }
        catch (OperationCanceledException) { }
    }

    private async Task InitializeAsync(CancellationToken ct)
    {
        var metadata = LoadMetadata();
        var bulkDataPath = metadata != null ? Path.Combine(_dataDirectory, metadata.FileName) : null;

        if (bulkDataPath != null && File.Exists(bulkDataPath))
        {
            Status = "Loading card data…";
            await BuildIndexesAsync(bulkDataPath, ct);
            IsReady = true;
            Status = "Ready";
            _logger.LogInformation("Bulk data loaded from {Path}", bulkDataPath);
            _ = CheckForUpdateAsync(ct);
        }
        else
        {
            await DownloadAndIndexAsync(ct);
        }
    }

    private async Task CheckForUpdateAsync(CancellationToken ct)
    {
        try
        {
            var item = await GetAllCardsBulkItemAsync(ct);
            if (item == null) return;

            var metadata = LoadMetadata();
            if (metadata != null && metadata.UpdatedAt >= item.UpdatedAt)
            {
                _logger.LogInformation("Bulk data is up to date (last updated {UpdatedAt})", metadata.UpdatedAt);
                return;
            }

            _logger.LogInformation("Bulk data update available: {UpdatedAt}", item.UpdatedAt);
            await DownloadAndIndexAsync(ct, item);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to check for bulk data update");
        }
    }

    private async Task DownloadAndIndexAsync(CancellationToken ct, BulkDataItemDto? knownItem = null)
    {
        try
        {
            Status = "Fetching bulk data info…";
            var item = knownItem ?? await GetAllCardsBulkItemAsync(ct);
            if (item?.DownloadUri == null)
            {
                Status = "Failed to retrieve bulk data info";
                _logger.LogError("Could not retrieve All Cards bulk data item from Scryfall");
                return;
            }

            var oldMetadata = LoadMetadata();
            var newFileName = Path.GetFileName(item.DownloadUri.LocalPath);
            var newFilePath = Path.Combine(_dataDirectory, newFileName);

            Status = "Downloading card data…";
            _logger.LogInformation("Downloading bulk data from {Uri}", item.DownloadUri);
            await DownloadFileAsync(item.DownloadUri, newFilePath, ct);

            Status = "Building card index…";
            await BuildIndexesAsync(newFilePath, ct);

            if (oldMetadata != null)
            {
                var oldPath = Path.Combine(_dataDirectory, oldMetadata.FileName);
                if (File.Exists(oldPath) && oldPath != newFilePath)
                {
                    File.Delete(oldPath);
                    _logger.LogInformation("Deleted old bulk data file: {Path}", oldPath);
                }
            }

            SaveMetadata(new BulkDataMetadata { FileName = newFileName, UpdatedAt = item.UpdatedAt });
            IsReady = true;
            Status = "Ready";
            _logger.LogInformation("Bulk data ready (updated {UpdatedAt})", item.UpdatedAt);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Status = "Failed to download card data";
            _logger.LogError(ex, "Failed to download and index bulk data");
        }
    }

    private async Task BuildIndexesAsync(string filePath, CancellationToken ct)
    {
        var newNameIndex = new Dictionary<string, List<CardDto>>(StringComparer.OrdinalIgnoreCase);
        var newOracleIndex = new Dictionary<string, List<CardDto>>(StringComparer.OrdinalIgnoreCase);

        await using var stream = File.OpenRead(filePath);
        await foreach (var card in JsonSerializer.DeserializeAsyncEnumerable<CardDto>(stream, cancellationToken: ct))
        {
            if (card == null) continue;

            if (!newNameIndex.TryGetValue(card.Name, out var nameList))
                newNameIndex[card.Name] = nameList = [];
            nameList.Add(card);

            var oracleId = card.OracleId ?? card.CardFaces?[0].OracleId;
            if (oracleId != null)
            {
                if (!newOracleIndex.TryGetValue(oracleId, out var oracleList))
                    newOracleIndex[oracleId] = oracleList = [];
                oracleList.Add(card);
            }
        }

        _nameIndex = newNameIndex;
        _oracleIndex = newOracleIndex;
        _logger.LogInformation("Indexed {NameCount} card names and {OracleCount} oracle IDs",
            newNameIndex.Count, newOracleIndex.Count);
    }

    private async Task DownloadFileAsync(Uri uri, string destPath, CancellationToken ct)
    {
        using var client = _httpClientFactory.CreateClient();
        client.Timeout = Timeout.InfiniteTimeSpan; // file is ~2 GB; cancellation is handled via ct
        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = File.Create(destPath);
        await contentStream.CopyToAsync(fileStream, ct);
    }

    private async Task<BulkDataItemDto?> GetAllCardsBulkItemAsync(CancellationToken ct)
    {
        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MTGProxyApp/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        using var response = await client.GetAsync(BulkDataApiUrl, ct);
        if (!response.IsSuccessStatusCode) return null;

        var list = await JsonSerializer.DeserializeAsync<BulkDataListDto>(
            await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return list?.Data.FirstOrDefault(d => d.Type == AllCardsType);
    }

    public List<CardDto> SearchByName(string name, string? setCode = null, string? collectorNumber = null, string? lang = null, bool highresOnly = false)
    {
        if (!_nameIndex.TryGetValue(name, out var cards)) return [];

        var result = cards.AsEnumerable();
        if (setCode != null)
            result = result.Where(c => string.Equals(c.Set, setCode, StringComparison.OrdinalIgnoreCase));
        if (collectorNumber != null)
            result = result.Where(c => string.Equals(c.CollectorNumber, collectorNumber, StringComparison.OrdinalIgnoreCase));
        if (lang != null)
            result = result.Where(c => string.Equals(c.Lang, lang, StringComparison.OrdinalIgnoreCase));
        if (highresOnly)
            result = result.Where(c => c.HighresImage);
        return [.. result];
    }

    public List<CardDto> GetByOracleId(string oracleId, string? lang = null, bool highresOnly = false)
    {
        if (!_oracleIndex.TryGetValue(oracleId, out var cards)) return [];
        var result = cards.AsEnumerable();
        if (lang != null)
            result = result.Where(c => string.Equals(c.Lang, lang, StringComparison.OrdinalIgnoreCase));
        if (highresOnly)
            result = result.Where(c => c.HighresImage);
        return [.. result];
    }

    private string MetadataPath => Path.Combine(_dataDirectory, MetadataFileName);

    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };

    private BulkDataMetadata? LoadMetadata()
    {
        if (!File.Exists(MetadataPath))
        {
            _logger.LogInformation("No metadata file found at {Path}", MetadataPath);
            return null;
        }
        try
        {
            return JsonSerializer.Deserialize<BulkDataMetadata>(File.ReadAllText(MetadataPath), CaseInsensitiveOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize metadata at {Path}", MetadataPath);
            return null;
        }
    }

    private void SaveMetadata(BulkDataMetadata metadata) =>
        File.WriteAllText(MetadataPath, JsonSerializer.Serialize(metadata));
}
