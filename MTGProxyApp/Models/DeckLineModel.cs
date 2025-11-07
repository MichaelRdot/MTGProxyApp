using System.Text.RegularExpressions;

namespace MTGProxyApp.Models;

public record DeckLineModel(int Count, string Name, string? SetCode, string? CollectorNumber)
{
    public static bool TryParse(string line, out DeckLineModel? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(line)) return false;

        // Pattern A: "1 Name (SET) 123"
        var withSet = Regex.Match(line.Trim(),
            @"^(?<count>\d+)\s+(?<name>.+?)\s+\((?<set>[A-Za-z0-9]{2,5})\)\s+(?<num>[A-Za-z0-9]+)$");

        if (withSet.Success)
        {
            result = new DeckLineModel(
                int.Parse(withSet.Groups["count"].Value),
                withSet.Groups["name"].Value.Trim(),
                withSet.Groups["set"].Value.Trim(),
                withSet.Groups["num"].Value.Trim());
            return true;
        }

        // Pattern B: "1 Name"
        var simple = Regex.Match(line.Trim(),
            @"^(?<count>\d+)\s+(?<name>.+)$");

        if (simple.Success)
        {
            result = new DeckLineModel(
                int.Parse(simple.Groups["count"].Value),
                simple.Groups["name"].Value.Trim(),
                null,
                null);
            return true;
        }

        return false;
    }
}