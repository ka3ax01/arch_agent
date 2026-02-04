using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArchAgent.Model;

namespace ArchAgent.Parsing;

public static class MarkdownParser
{
    private static readonly Dictionary<string, string> SectionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["goal"] = "goal",
        ["users / actors"] = "actors",
        ["users/actors"] = "actors",
        ["users"] = "actors",
        ["actors"] = "actors",
        ["core features"] = "features",
        ["features"] = "features",
        ["integrations"] = "integrations",
        ["data"] = "data",
        ["constraints"] = "constraints",
        ["non-functional requirements"] = "nfr",
        ["nfr"] = "nfr",
        ["key flows"] = "flows",
        ["key flow"] = "flows"
    };

    public static SystemModel Parse(string path)
    {
        var lines = File.ReadAllLines(path);
        var sections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        string? current = null;

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.StartsWith("#"))
            {
                var heading = line.TrimStart('#').Trim();
                if (SectionMap.TryGetValue(heading, out var key))
                {
                    current = key;
                    if (!sections.ContainsKey(key))
                    {
                        sections[key] = new List<string>();
                    }
                    continue;
                }
                current = null;
                continue;
            }

            if (current is null || string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            sections[current].Add(line);
        }

        var model = new SystemModel
        {
            Goal = ExtractParagraph(sections, "goal"),
            Actors = ExtractList(sections, "actors"),
            Features = ExtractList(sections, "features"),
            Integrations = ExtractList(sections, "integrations"),
            DataEntities = ExtractList(sections, "data"),
            Constraints = ExtractList(sections, "constraints"),
            Nfr = ExtractNfr(sections, "nfr"),
            KeyFlows = ExtractList(sections, "flows")
        };

        return model;
    }

    private static string ExtractParagraph(Dictionary<string, List<string>> sections, string key)
    {
        return sections.TryGetValue(key, out var lines) ? string.Join(" ", lines) : string.Empty;
    }

    private static List<string> ExtractList(Dictionary<string, List<string>> sections, string key)
    {
        if (!sections.TryGetValue(key, out var lines))
        {
            return new List<string>();
        }

        var items = new List<string>();
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
            {
                items.Add(trimmed[2..].Trim());
            }
            else if (char.IsDigit(trimmed.FirstOrDefault()) && trimmed.Contains('.'))
            {
                var idx = trimmed.IndexOf('.');
                if (idx > 0)
                {
                    items.Add(trimmed[(idx + 1)..].Trim());
                }
            }
            else
            {
                items.Add(trimmed);
            }
        }

        return items.Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
    }

    private static Dictionary<string, string> ExtractNfr(Dictionary<string, List<string>> sections, string key)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!sections.TryGetValue(key, out var lines))
        {
            return result;
        }

        foreach (var line in lines)
        {
            var trimmed = line.Trim().TrimStart('-', '*').Trim();
            var parts = trimmed.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2)
            {
                result[parts[0]] = parts[1];
            }
            else if (!string.IsNullOrWhiteSpace(trimmed))
            {
                result[$"NFR-{result.Count + 1}"] = trimmed;
            }
        }

        return result;
    }
}
