using System.Text.Json.Serialization;

namespace MTGProxyApp.Dtos;

public class PaginatedListDto<T>
{
    [JsonPropertyName("object")] public required string ObjectType { get; set; }

    [JsonPropertyName("data")] public required List<T> Data { get; set; }

    [JsonPropertyName("has_more")] public bool HasMore { get; set; }

    [JsonPropertyName("next_page")] public Uri? NextPage { get; set; }

    [JsonPropertyName("total_cards")] public int? Page { get; set; }

    [JsonPropertyName("warnings")] public Array? Warnings { get; set; }
}