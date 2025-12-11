using System.Text.Json.Serialization;

namespace MTGProxyApp.Dtos;

public class CardDto
{
    public int Count { get; set; }
    public int LineIndex { get; set; }
    public bool IsToken { get; set; }
    public byte[]? PreLoadedCardImageFront { get; set; }
    public byte[]? PreLoadedCardImageBack { get; set; }
    public bool Flip { get; set; }
    [JsonPropertyName("set")] public string? Set { get; set; }
    [JsonPropertyName("oracle_id")] public string? OracleId { get; set; }

    [JsonPropertyName("type_line")] public string? TypeLine { get; set; }
    [JsonPropertyName("collector_number")] public string? CollectorNumber { get; set; }
    [JsonPropertyName("highres_image")] public bool HighresImage { get; set; }
    [JsonPropertyName("image_uris")] public CardPngDto? ImageUris { get; set; }
    [JsonPropertyName("name")] public required string Name { get; set; }
    [JsonPropertyName("card_faces")] public CardFaceDto[]? CardFaces { get; set; }
    public class CardPngDto { [JsonPropertyName("png")] public Uri? Png { get; set; } }

    public class CardFaceDto
    {
        [JsonPropertyName("image_uris")] public CardPngDto? ImageUris { get; set; } 
        [JsonPropertyName("oracle_id")] public string? OracleId { get; set; }
    }
}