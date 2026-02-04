using System.Text.RegularExpressions;

namespace ArchAgent.Utils;

public static partial class Redaction
{
    private static readonly Regex KeyValuePattern =
        new(
            "(?i)(api_key|apikey|token|secret|password|passwd|bearer)\\s*[:=]\\s*([^\\s]+)",
            RegexOptions.Compiled
        );

    private static readonly Regex BearerPattern = MyRegex();

    public static string Redact(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var redacted = KeyValuePattern.Replace(input, "$1: [REDACTED]");
        redacted = BearerPattern.Replace(redacted, "Bearer [REDACTED]");
        return redacted;
    }

    [GeneratedRegex(@"(?i)bearer\s+[A-Za-z0-9\-\._~\+/]+=*", RegexOptions.Compiled, "en-KZ")]
    private static partial Regex MyRegex();
}
