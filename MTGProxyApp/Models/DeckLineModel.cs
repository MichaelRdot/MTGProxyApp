using System.Text.RegularExpressions;

namespace MTGProxyApp.Models;

public record DeckLineModel(int Count, string Name, string? SetCode, string? CollectorNumber)
{
    public static bool TryParse(string line, out DeckLineModel? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(line)) return false;
        
        var withSet = Regex.Match(line.Trim(), @"^(?<count>\d*)\s+(?<name>.*?)\s+\((?<set>[A-Za-z0-9]{3})\)?\s+(?<num>[A-Za-z0-9]*)$");

        if (withSet.Success)
        {
            result = new DeckLineModel(
                int.Parse(withSet.Groups["count"].Value),
                withSet.Groups["name"].Value.Trim(),
                withSet.Groups["set"].Value.Trim(),
                withSet.Groups["num"].Value.Trim());
            return true;
        }
        return false;
    }
}