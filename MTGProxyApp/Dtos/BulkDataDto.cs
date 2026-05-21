using System.Text.Json.Serialization;

namespace MTGProxyApp.Dtos;

public class BulkDataListDto
{
    [JsonPropertyName("data")] public List<BulkDataItemDto> Data { get; set; } = [];
}

public class BulkDataItemDto
{
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("download_uri")] public Uri? DownloadUri { get; set; }
    [JsonPropertyName("updated_at")] public DateTimeOffset UpdatedAt { get; set; }
}
