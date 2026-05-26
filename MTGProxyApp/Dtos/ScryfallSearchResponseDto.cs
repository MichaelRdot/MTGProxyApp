using System.Text.Json.Serialization;

namespace MTGProxyApp.Dtos;

public class ScryfallSearchResponseDto
{
    [JsonPropertyName("data")] public List<CardDto> Data { get; set; } = [];
    [JsonPropertyName("has_more")] public bool HasMore { get; set; }
    [JsonPropertyName("next_page")] public string? NextPage { get; set; }
}
