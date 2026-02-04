using System.Collections.Generic;

namespace ArchAgent.Model;

public sealed class SystemModel
{
    public string Goal { get; set; } = string.Empty;
    public List<string> Actors { get; set; } = new();
    public List<string> Features { get; set; } = new();
    public List<string> Integrations { get; set; } = new();
    public List<string> DataEntities { get; set; } = new();
    public List<string> Constraints { get; set; } = new();
    public Dictionary<string, string> Nfr { get; set; } = new();
    public List<string> KeyFlows { get; set; } = new();
}

public sealed class Assumption
{
    public string Field { get; set; } = string.Empty;
    public string AssumptionText { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class ArchitectureDecision
{
    public string Style { get; set; } = "modular monolith";
    public List<string> Containers { get; set; } = new();
    public bool UseBroker { get; set; }
    public bool UseLoadBalancer { get; set; }
    public List<string> Tradeoffs { get; set; } = new();
}

public sealed class QualityReport
{
    public int CompletenessScore { get; set; }
    public int ConsistencyScore { get; set; }
    public int AssumptionCount { get; set; }
    public List<string> Warnings { get; set; } = new();
}
