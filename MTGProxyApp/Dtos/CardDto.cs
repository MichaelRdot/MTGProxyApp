using System.Text.Json.Serialization;

namespace MTGProxyApp.Dtos;

public class CardDto
{
    public int Count { get; set; }
    public int LineIndex { get; set; }
    public Byte[] PreLoadedCardImageFront { get; set; }
    public Byte[] PreLoadedCardImageBack { get; set; }
    [JsonPropertyName("set")] public required string Set { get; set; }
    [JsonPropertyName("prints_search_uri")] public required Uri PrintsSearchUri { get; set; }
    [JsonPropertyName("collector_number")] public string? CollectorNumber { get; set; }
    [JsonPropertyName("highres_image")] public bool HighresImage { get; set; }
    [JsonPropertyName("image_uris")] public CardPngDto? ImageUris { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("card_faces")] public CardFaceDto[]? CardFaces { get; set; }
    public class CardPngDto
    {
        [JsonPropertyName("png")] public Uri? Png { get; set; }
    }

    public class CardFaceDto
    {
        [JsonPropertyName("image_uris")] public CardPngDto? ImageUris { get; set; }
    }
}