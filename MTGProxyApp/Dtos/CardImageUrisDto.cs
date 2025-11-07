using System.Text.Json.Serialization;

namespace MTGProxyApp.Dtos;

public class CardImageUrisDto
{
    [JsonPropertyName("png")] public Uri? Png { get; set; }
}