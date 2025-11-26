using System.Text.Json.Serialization;

namespace MTGProxyApp.Dtos;

public class CardDto
{
    [JsonPropertyName("set")] public required string Set { get; set; }

    [JsonPropertyName("prints_search_uri")]
    public required Uri PrintsSearchUri { get; set; }

    [JsonPropertyName("collector_number")] public string? CollectorNumber { get; set; }
    [JsonPropertyName("highres_image")] public bool HighresImage { get; set; }
    [JsonPropertyName("image_uris")] public CardPngDto? ImageUris { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }

    public class CardPngDto
    {
        [JsonPropertyName("png")] public Uri? Png { get; set; }
    }
}