using System.Runtime.InteropServices.Marshalling;
using System.Text.RegularExpressions;

namespace MTGProxyApp.Models;

public record DeckLineModel(int Count, string Name, string? SetCode, string? CollectorNumber)
{
    public static bool TryParse(string line, out DeckLineModel? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(line)) return false;

        var tryMatch = Regex.Match(line.Trim(),
            @"^\s*(?:(?<count>\d+)\s+)?(?<name>.+?)(?:\s+\((?<set>[A-Za-z0-9]{3,5})\)(?:\s+(?<num>[A-Za-z0-9]+))?(?:\s+\*F\*)?)?\s*$",
            RegexOptions.IgnorePatternWhitespace);
        if (!tryMatch.Success)
            return false;

        var countGroup = tryMatch.Groups["count"];
        var name = tryMatch.Groups["name"].Value.Trim();
        var set = tryMatch.Groups["set"].Success ? tryMatch.Groups["set"].Value.Trim() : null;
        var num = tryMatch.Groups["num"].Success ? tryMatch.Groups["num"].Value.Trim() : null;

        var count = countGroup.Success ? int.Parse(countGroup.Value) : 1;

        result = new DeckLineModel(count, name, set, num);
        return true;
    }
}