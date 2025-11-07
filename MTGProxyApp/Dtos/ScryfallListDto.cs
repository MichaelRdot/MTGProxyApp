using System.Text.Json.Serialization;

namespace MTGProxyApp.Dtos;

public class ScryfallListDto<T>
{
    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }
    [JsonPropertyName("next_page")]
    public string? NextPage { get; set; }
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = new();
}