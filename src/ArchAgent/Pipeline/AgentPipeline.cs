using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ArchAgent.Config;
using ArchAgent.Heuristics;
using ArchAgent.LLM;
using ArchAgent.Model;
using ArchAgent.Parsing;
using ArchAgent.Rendering;
using ArchAgent.Utils;
using ArchAgent.Validation;

namespace ArchAgent.Pipeline;

public sealed class AgentPipeline
{
    public async Task<PipelineResult> RunAsync(AppConfig config)
    {
        var assumptions = new List<Assumption>();
        var model = ParseInput(config.InputPath);
        model = ArchitectureHeuristics.Normalize(model, assumptions);

        ArchitectureDecision decision;
        string? prompt = null;
        string? response = null;
        bool ollamaUsed = false;

        if (config.Mode != "heuristic")
        {
            try
            {
                var client = new OllamaClient(TimeSpan.FromSeconds(60));
                prompt = BuildPrompt(model);
                response = await client.GenerateAsync(config.Model, prompt).ConfigureAwait(false);
                decision = ParseDecision(response);
                ollamaUsed = true;

                var heuristicDecision = ArchitectureHeuristics.Decide(model);
                decision = MergeDecisions(decision, heuristicDecision, model);
            }
            catch (Exception ex)
            {
                if (config.Mode == "ollama")
                {
                    throw new InvalidOperationException("Ollama mode required but call failed: " + ex.Message, ex);
                }
                decision = ArchitectureHeuristics.Decide(model);
                response = $"Ollama unavailable or invalid response. Falling back to heuristics. Error: {ex.Message}";
            }
        }
        else
        {
            decision = ArchitectureHeuristics.Decide(model);
        }

        var runId = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
        var outputRoot = Path.Combine(config.OutputRoot, runId);
        var docsDir = Path.Combine(outputRoot, "docs");
        var diagramsDir = Path.Combine(outputRoot, "diagrams");
        var metaDir = Path.Combine(outputRoot, "meta");

        FileSystem.EnsureDirectory(docsDir);
        FileSystem.EnsureDirectory(diagramsDir);
        FileSystem.EnsureDirectory(metaDir);

        FileSystem.WriteAllText(Path.Combine(diagramsDir, "c4_context.puml"), DiagramRenderer.RenderContextDiagram(model));
        FileSystem.WriteAllText(Path.Combine(diagramsDir, "c4_container.puml"), DiagramRenderer.RenderContainerDiagram(model, decision));
        FileSystem.WriteAllText(Path.Combine(diagramsDir, "sequence_key_flow.puml"), DiagramRenderer.RenderSequenceDiagram(model, decision));

        FileSystem.WriteAllText(Path.Combine(docsDir, "architecture_summary.md"), DocRenderer.RenderArchitectureSummary(model, decision, assumptions.ToArray()));
        FileSystem.WriteAllText(Path.Combine(docsDir, "adr_001.md"), DocRenderer.RenderAdr(model, decision));
        FileSystem.WriteAllText(Path.Combine(docsDir, "nfr_checklist.md"), DocRenderer.RenderNfrChecklist(model));
        FileSystem.WriteAllText(Path.Combine(docsDir, "api_contract_outline.md"), DocRenderer.RenderApiContract(model));
        FileSystem.WriteAllText(Path.Combine(docsDir, "risk_register.md"), DocRenderer.RenderRiskRegister(model));

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        FileSystem.WriteAllText(Path.Combine(metaDir, "extracted_model.json"), JsonSerializer.Serialize(model, jsonOptions));
        FileSystem.WriteAllText(Path.Combine(metaDir, "assumptions.json"), JsonSerializer.Serialize(assumptions, jsonOptions));

        var promptTrace = new StringBuilder();
        promptTrace.AppendLine($"Model: {config.Model}");
        promptTrace.AppendLine($"Mode: {config.Mode}");
        promptTrace.AppendLine($"Ollama Used: {ollamaUsed}");
        promptTrace.AppendLine();
        if (!string.IsNullOrWhiteSpace(prompt))
        {
            promptTrace.AppendLine("## Prompt");
            promptTrace.AppendLine(Redaction.Redact(prompt));
            promptTrace.AppendLine();
        }
        if (!string.IsNullOrWhiteSpace(response))
        {
            promptTrace.AppendLine("## Response");
            promptTrace.AppendLine(Redaction.Redact(response));
        }
        FileSystem.WriteAllText(Path.Combine(metaDir, "prompt_trace.md"), promptTrace.ToString());

        var requiredFiles = new List<string>
        {
            Path.Combine(diagramsDir, "c4_context.puml"),
            Path.Combine(diagramsDir, "c4_container.puml"),
            Path.Combine(diagramsDir, "sequence_key_flow.puml"),
            Path.Combine(docsDir, "architecture_summary.md"),
            Path.Combine(docsDir, "adr_001.md"),
            Path.Combine(docsDir, "nfr_checklist.md"),
            Path.Combine(docsDir, "api_contract_outline.md"),
            Path.Combine(docsDir, "risk_register.md"),
            Path.Combine(metaDir, "extracted_model.json"),
            Path.Combine(metaDir, "assumptions.json"),
            Path.Combine(metaDir, "prompt_trace.md")
        };

        var report = QualityValidator.Validate(outputRoot, requiredFiles, assumptions.Count);
        FileSystem.WriteAllText(Path.Combine(metaDir, "quality_report.json"), JsonSerializer.Serialize(report, jsonOptions));

        return new PipelineResult(outputRoot);
    }

    private static SystemModel ParseInput(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".md" || ext == ".markdown")
        {
            return MarkdownParser.Parse(path);
        }
        if (ext == ".json")
        {
            return JsonParser.Parse(path);
        }

        throw new InvalidOperationException("Unsupported input format. Use .md or .json");
    }

    private static string BuildPrompt(SystemModel model)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are a senior solution architect for an enterprise system.");
        sb.AppendLine("Your goal is to propose a BUILDABLE architecture that ADDS new engineering value beyond restating requirements.");
        sb.AppendLine("Return ONLY valid JSON (no markdown, no comments, no trailing commas).");
        sb.AppendLine();

        sb.AppendLine("JSON schema (MUST match):");
        sb.AppendLine("{");
        sb.AppendLine("  \"style\": \"modular_monolith\" | \"microservices\" | \"hybrid\",");
        sb.AppendLine("  \"styleRationale\": [\"...\"],");
        sb.AppendLine("  \"containers\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"name\": \"...\",");
        sb.AppendLine("      \"type\": \"web\" | \"api\" | \"worker\" | \"db\" | \"broker\" | \"storage\" | \"external\" | \"observability\",");
        sb.AppendLine("      \"responsibilities\": [\"...\"],");
        sb.AppendLine("      \"interfaces\": [\"REST\", \"Events\", \"SQL\", \"S3\"],");
        sb.AppendLine("      \"dataOwned\": [\"...\"],");
        sb.AppendLine("      \"scaling\": \"stateless_horizontal\" | \"vertical\" | \"managed\",");
        sb.AppendLine("      \"risks\": [\"...\"],");
        sb.AppendLine("      \"nfrTactics\": { \"security\": [\"...\"], \"performance\": [\"...\"], \"availability\": [\"...\"], \"observability\": [\"...\"] }");
        sb.AppendLine("    }");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"modules\": [");
        sb.AppendLine("    { \"name\": \"...\", \"purpose\": \"...\", \"keyEntities\": [\"...\"], \"keyAPIs\": [\"...\"] }");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"dataDesign\": {");
        sb.AppendLine("    \"entities\": [\"...\"],");
        sb.AppendLine("    \"relationships\": [\"...\"],");
        sb.AppendLine("    \"attachments\": { \"approach\": \"object_storage\" | \"db\" , \"notes\": \"...\" },");
        sb.AppendLine("    \"auditing\": { \"approach\": \"append_only\" | \"standard\" , \"notes\": \"...\" }");
        sb.AppendLine("  },");
        sb.AppendLine("  \"keyFlows\": [");
        sb.AppendLine("    { \"name\": \"...\", \"steps\": [\"...\"] , \"syncOrAsync\": \"sync\" | \"async\" | \"hybrid\" }");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"decisions\": [");
        sb.AppendLine("    { \"adrTitle\": \"...\", \"decision\": \"...\", \"options\": [\"...\"], \"why\": [\"...\"], \"consequences\": [\"...\"] }");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"risks\": [");
        sb.AppendLine("    { \"risk\": \"...\", \"likelihood\": \"Low\"|\"Med\"|\"High\", \"impact\": \"Low\"|\"Med\"|\"High\", \"mitigation\": [\"...\"] }");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"assumptions\": [");
        sb.AppendLine("    { \"field\": \"...\", \"assumption\": \"...\", \"reason\": \"...\" }");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine("System details:");
        sb.AppendLine($"Goal: {model.Goal}");
        sb.AppendLine($"Actors: {string.Join(", ", model.Actors)}");
        sb.AppendLine($"Features: {string.Join(", ", model.Features)}");
        sb.AppendLine($"Integrations: {string.Join(", ", model.Integrations)}");
        sb.AppendLine($"Data Entities: {string.Join(", ", model.DataEntities)}");
        sb.AppendLine($"Constraints: {string.Join(", ", model.Constraints)}");
        sb.AppendLine($"NFR: {string.Join("; ", model.Nfr.Select(kv => $"{kv.Key}={kv.Value}"))}");
        sb.AppendLine($"Key Flows: {string.Join(", ", model.KeyFlows)}");

        return sb.ToString();
    }

    private static ArchitectureDecision ParseDecision(string response)
    {
        var json = ExtractJson(response);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var decision = new ArchitectureDecision
        {
            Style = root.TryGetProperty("style", out var style) ? style.GetString() ?? "modular monolith" : "modular monolith",
            UseBroker = root.TryGetProperty("useBroker", out var broker) && broker.ValueKind == JsonValueKind.True,
            UseLoadBalancer = root.TryGetProperty("useLoadBalancer", out var lb) && lb.ValueKind == JsonValueKind.True
        };

        if (root.TryGetProperty("containers", out var containers) && containers.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in containers.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    decision.Containers.Add(item.GetString() ?? string.Empty);
                }
            }
        }

        if (root.TryGetProperty("tradeoffs", out var tradeoffs) && tradeoffs.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in tradeoffs.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    decision.Tradeoffs.Add(item.GetString() ?? string.Empty);
                }
            }
        }

        if (decision.Containers.Count == 0)
        {
            decision.Containers.AddRange(new[] { "Web UI", "API", "Database" });
        }

        return decision;
    }

    private static ArchitectureDecision MergeDecisions(ArchitectureDecision llm, ArchitectureDecision heuristic, SystemModel model)
    {
        llm.Style = llm.Style is "microservices" or "modular monolith" ? llm.Style : heuristic.Style;
        llm.UseBroker = llm.UseBroker || heuristic.UseBroker;
        llm.UseLoadBalancer = llm.UseLoadBalancer || heuristic.UseLoadBalancer;

        var containers = new HashSet<string>(llm.Containers, StringComparer.OrdinalIgnoreCase);
        containers.Add("Web UI");
        containers.Add("API");
        containers.Add("Database");
        if (llm.UseBroker)
        {
            containers.Add("Message Broker");
        }
        foreach (var integration in model.Integrations)
        {
            containers.Add($"External: {integration}");
        }

        llm.Containers = containers.OrderBy(c => c).ToList();
        if (llm.Tradeoffs.Count == 0)
        {
            llm.Tradeoffs.AddRange(heuristic.Tradeoffs);
        }

        return llm;
    }

    private static string ExtractJson(string input)
    {
        var start = input.IndexOf('{');
        var end = input.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return input[start..(end + 1)];
        }
        throw new InvalidOperationException("No JSON object found in LLM response.");
    }
}

public sealed record PipelineResult(string OutputRoot);
