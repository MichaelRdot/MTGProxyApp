using System.Text.RegularExpressions;

namespace MTGProxyApp.Models;

public record DeckLineModel(int Count, string Name, string? SetCode, string? CollectorNumber)
{
    public static bool TryParse(string line, out DeckLineModel? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(line)) return false;

        var withSet = Regex.Match(line.Trim(),
            @"^\s*(?:(?<count>\d+)\s+)?(?<name>.+?)(?:\s+\((?<set>[A-Za-z0-9]{3,5})\)(?:\s+(?<num>[A-Za-z0-9]+))?)?\s*$", 
            RegexOptions.IgnorePatternWhitespace);
        if (!withSet.Success)
            return false;

        var countGroup = withSet.Groups["count"];
        var name = withSet.Groups["name"].Value.Trim();
        var set = withSet.Groups["set"].Success ? withSet.Groups["set"].Value.Trim() : null;
        var num = withSet.Groups["num"].Success ? withSet.Groups["num"].Value.Trim() : null;

        var count = countGroup.Success ? int.Parse(countGroup.Value) : 1;

        result = new DeckLineModel(count, name, set, num);
        return true;
    }
}