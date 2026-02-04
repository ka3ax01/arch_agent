using System;
using System.IO;
using System.Threading.Tasks;
using ArchAgent.Config;
using ArchAgent.Pipeline;

namespace ArchAgent;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var configResult = AppConfig.FromArgs(args);
            if (!configResult.Success)
            {
                Console.Error.WriteLine(configResult.ErrorMessage);
                AppConfig.PrintUsage();
                return 2;
            }

            var config = configResult.Value!;
            if (!File.Exists(config.InputPath))
            {
                Console.Error.WriteLine($"Input file not found: {config.InputPath}");
                return 2;
            }

            var pipeline = new AgentPipeline();
            var result = await pipeline.RunAsync(config);

            Console.WriteLine($"Artifacts written to: {result.OutputRoot}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Fatal error: " + ex.Message);
            return 1;
        }
    }
}
