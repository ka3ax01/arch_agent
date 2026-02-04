using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ArchAgent.Model;

namespace ArchAgent.Parsing;

public static class JsonParser
{
    public static SystemModel Parse(string path)
    {
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var model = new SystemModel
        {
            Goal = GetString(root, "goal"),
            Actors = GetStringList(root, "actors"),
            Features = GetStringList(root, "features"),
            Integrations = GetStringList(root, "integrations"),
            DataEntities = GetStringList(root, "dataEntities", "data_entities", "data"),
            Constraints = GetStringList(root, "constraints"),
            KeyFlows = GetStringList(root, "keyFlows", "key_flows"),
            Nfr = GetNfr(root)
        };

        return model;
    }

    private static string GetString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static List<string> GetStringList(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Array)
            {
                var items = new List<string>();
                foreach (var item in value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        items.Add(item.GetString() ?? string.Empty);
                    }
                }
                return items;
            }
            if (value.ValueKind == JsonValueKind.String)
            {
                return new List<string> { value.GetString() ?? string.Empty };
            }
        }

        return new List<string>();
    }

    private static Dictionary<string, string> GetNfr(JsonElement root)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("nfr", out var nfr) || root.TryGetProperty("nfrs", out nfr))
        {
            if (nfr.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in nfr.EnumerateObject())
                {
                    result[prop.Name] = prop.Value.GetString() ?? prop.Value.ToString();
                }
            }
            else if (nfr.ValueKind == JsonValueKind.Array)
            {
                int idx = 1;
                foreach (var item in nfr.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        result[$"NFR-{idx}"] = item.GetString() ?? string.Empty;
                        idx++;
                    }
                }
            }
        }
        return result;
    }
}
