using System;
using System.Collections.Generic;
using System.IO;

namespace ArchAgent.Config;

public sealed class AppConfig
{
    public string InputPath { get; private set; } = string.Empty;
    public string OutputRoot { get; private set; } = "artifacts";
    public string Mode { get; private set; } = "auto";
    public string Model { get; private set; } = "llama3.1";

    public static (bool Success, string ErrorMessage, AppConfig? Value) FromArgs(string[] args)
    {
        if (args.Length == 0 || Array.Exists(args, a => a is "-h" or "--help"))
        {
            return (false, "", null);
        }

        var config = new AppConfig();
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                return (false, $"Unknown argument: {arg}", null);
            }

            var key = arg[2..];
            if (i + 1 >= args.Length)
            {
                return (false, $"Missing value for --{key}", null);
            }

            map[key] = args[i + 1];
            i++;
        }

        if (!map.TryGetValue("input", out var input))
        {
            return (false, "Missing required --input", null);
        }

        config.InputPath = input;

        if (map.TryGetValue("out", out var outDir))
        {
            config.OutputRoot = outDir;
        }

        if (map.TryGetValue("mode", out var mode))
        {
            mode = mode.ToLowerInvariant();
            if (mode is not ("auto" or "ollama" or "heuristic"))
            {
                return (false, "Invalid --mode. Use auto|ollama|heuristic", null);
            }
            config.Mode = mode;
        }

        if (map.TryGetValue("model", out var model))
        {
            config.Model = model;
        }

        return (true, "", config);
    }

    public static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project src/ArchAgent -- --input <path> --out <dir> --mode <auto|ollama|heuristic> --model <name>");
        Console.WriteLine();
        Console.WriteLine("Defaults:");
        Console.WriteLine("  --out artifacts");
        Console.WriteLine("  --mode auto");
        Console.WriteLine("  --model llama3.1");
    }
}
