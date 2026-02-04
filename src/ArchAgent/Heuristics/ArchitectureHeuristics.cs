using System;
using System.Collections.Generic;
using System.Linq;
using ArchAgent.Model;

namespace ArchAgent.Heuristics;

public static class ArchitectureHeuristics
{
    public static SystemModel Normalize(SystemModel model, List<Assumption> assumptions)
    {
        if (string.IsNullOrWhiteSpace(model.Goal))
        {
            assumptions.Add(new Assumption
            {
                Field = "Goal",
                AssumptionText = "Provide a clear business outcome for the system.",
                Reason = "Input missing a goal section."
            });
            model.Goal = "Deliver the core business capabilities with a simple web-based experience.";
        }

        if (model.Actors.Count == 0)
        {
            assumptions.Add(new Assumption
            {
                Field = "Actors",
                AssumptionText = "Primary end users and system administrators.",
                Reason = "No actors specified."
            });
            model.Actors.AddRange(new[] { "End User", "Admin" });
        }

        if (model.Features.Count == 0)
        {
            assumptions.Add(new Assumption
            {
                Field = "Features",
                AssumptionText = "Basic CRUD and reporting features.",
                Reason = "No core features provided."
            });
            model.Features.AddRange(new[] { "Create, read, update, and delete records", "Search and filtering", "Basic reporting" });
        }

        if (model.DataEntities.Count == 0)
        {
            assumptions.Add(new Assumption
            {
                Field = "DataEntities",
                AssumptionText = "Primary domain entities and user accounts stored in a relational database.",
                Reason = "Data section missing."
            });
            model.DataEntities.AddRange(new[] { "User", "PrimaryDomainEntity", "AuditLog" });
        }

        if (model.Nfr.Count == 0)
        {
            assumptions.Add(new Assumption
            {
                Field = "NFR",
                AssumptionText = "Baseline security, performance, and availability requirements.",
                Reason = "No NFRs provided."
            });
            model.Nfr["Security"] = "JWT auth, RBAC, and audit logging.";
            model.Nfr["Availability"] = "Target 99.5% uptime.";
            model.Nfr["Performance"] = "Typical responses under 500ms.";
        }

        model.Actors = model.Actors.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(a => a).ToList();
        model.Features = model.Features.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(a => a).ToList();
        model.Integrations = model.Integrations.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(a => a).ToList();
        model.DataEntities = model.DataEntities.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(a => a).ToList();
        model.Constraints = model.Constraints.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(a => a).ToList();
        model.KeyFlows = model.KeyFlows.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(a => a).ToList();

        return model;
    }

    public static ArchitectureDecision Decide(SystemModel model)
    {
        var decision = new ArchitectureDecision();
        var integrationCount = model.Integrations.Count;
        var featureCount = model.Features.Count;

        if (integrationCount >= 3 || (integrationCount >= 2 && featureCount >= 8))
        {
            decision.Style = "microservices";
        }
        else
        {
            decision.Style = "modular monolith";
        }

        var keywordSource = string.Join(' ', model.Features).ToLowerInvariant() + " " + string.Join(' ', model.Integrations).ToLowerInvariant();
        if (keywordSource.Contains("event") || keywordSource.Contains("audit") || keywordSource.Contains("notification") || keywordSource.Contains("queue") || keywordSource.Contains("message"))
        {
            decision.UseBroker = true;
        }

        var nfrText = string.Join(' ', model.Nfr.Select(kv => $"{kv.Key} {kv.Value}")).ToLowerInvariant();
        if (nfrText.Contains("high availability") || nfrText.Contains("ha") || nfrText.Contains("99.9") || nfrText.Contains("redund"))
        {
            decision.UseLoadBalancer = true;
        }

        decision.Containers = new List<string>
        {
            "Web UI",
            "API",
            "Database"
        };
        if (decision.UseBroker)
        {
            decision.Containers.Add("Message Broker");
        }
        decision.Containers.AddRange(model.Integrations.Select(i => $"External: {i}"));

        decision.Tradeoffs.Add(decision.Style == "microservices"
            ? "Improved scalability and independent deployment at the cost of higher operational complexity."
            : "Simpler deployment and development flow at the cost of limited independent scaling.");

        return decision;
    }
}
