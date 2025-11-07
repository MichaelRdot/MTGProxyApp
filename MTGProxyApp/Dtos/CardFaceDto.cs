using System.Text.Json.Serialization;

namespace MTGProxyApp.Dtos;

public class CardFaceDto
{
    [JsonPropertyName("image_uris")] public CardImageUrisDto? ImageUris { get; set; }
}