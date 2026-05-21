using System.Text.Json.Serialization;

namespace MTGProxyApp.Models;

public class BulkDataMetadata
{
    [JsonPropertyName("FileName")] public string FileName { get; set; } = "";
    [JsonPropertyName("UpdatedAt")] public DateTimeOffset UpdatedAt { get; set; }
}
