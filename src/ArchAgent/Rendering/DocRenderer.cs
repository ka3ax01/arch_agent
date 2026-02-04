using System;
using System.Linq;
using System.Text;
using ArchAgent.Model;

namespace ArchAgent.Rendering;

public static class DocRenderer
{
    public static string RenderArchitectureSummary(SystemModel model, ArchitectureDecision decision, Assumption[] assumptions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Architecture Summary");
        sb.AppendLine();
        sb.AppendLine("## Overview");
        sb.AppendLine(model.Goal);
        sb.AppendLine();
        sb.AppendLine("## Assumptions");
        if (assumptions.Length == 0)
        {
            sb.AppendLine("- None");
        }
        else
        {
            foreach (var assumption in assumptions)
            {
                sb.AppendLine($"- {assumption.Field}: {assumption.AssumptionText} ({assumption.Reason})");
            }
        }
        sb.AppendLine();
        sb.AppendLine("## Chosen Style + Rationale");
        sb.AppendLine($"- Style: **{decision.Style}**");
        foreach (var tradeoff in decision.Tradeoffs)
        {
            sb.AppendLine($"- {tradeoff}");
        }
        sb.AppendLine();
        sb.AppendLine("## Containers");
        sb.AppendLine("- Web UI: user experience, validation, and session handling.");
        sb.AppendLine("- API: core business logic, integrations, and orchestration.");
        sb.AppendLine("- Database: system of record for domain data.");
        if (decision.UseBroker)
        {
            sb.AppendLine("- Message Broker: asynchronous events for notifications and audit trails.");
        }
        foreach (var integration in model.Integrations)
        {
            sb.AppendLine($"- External Integration: {integration}");
        }
        sb.AppendLine();
        sb.AppendLine("## Data Flows");
        sb.AppendLine("- Client requests flow through the Web UI to the API.");
        sb.AppendLine("- The API enforces authn/authz, then reads/writes to the database.");
        if (decision.UseBroker)
        {
            sb.AppendLine("- The API publishes events to the broker for downstream processing.");
        }
        sb.AppendLine();
        sb.AppendLine("## Observability");
        sb.AppendLine("- Centralized logging, tracing for key flows, and dashboards for errors/latency.");
        sb.AppendLine();
        sb.AppendLine("## Security Baseline");
        sb.AppendLine("- JWT-based authentication, RBAC, and least-privilege access to data.");
        sb.AppendLine("- TLS for all external communication and secrets managed outside source control.");
        sb.AppendLine();
        sb.AppendLine("## Tradeoffs");
        foreach (var tradeoff in decision.Tradeoffs)
        {
            sb.AppendLine($"- {tradeoff}");
        }
        return sb.ToString();
    }

    public static string RenderAdr(SystemModel model, ArchitectureDecision decision)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# ADR 001: Architecture Style");
        sb.AppendLine();
        sb.AppendLine("## Status");
        sb.AppendLine("Proposed");
        sb.AppendLine();
        sb.AppendLine("## Context");
        sb.AppendLine(model.Goal);
        sb.AppendLine();
        sb.AppendLine("## Options");
        sb.AppendLine("- Modular monolith with clear modules");
        sb.AppendLine("- Microservices with independent deployment and data ownership");
        sb.AppendLine();
        sb.AppendLine("## Decision");
        sb.AppendLine($"Choose **{decision.Style}** based on current integrations and scope.");
        sb.AppendLine();
        sb.AppendLine("## Consequences");
        foreach (var tradeoff in decision.Tradeoffs)
        {
            sb.AppendLine($"- {tradeoff}");
        }
        return sb.ToString();
    }

    public static string RenderNfrChecklist(SystemModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# NFR Checklist");
        sb.AppendLine();
        sb.AppendLine("## Security");
        sb.AppendLine("- [ ] JWT auth + RBAC (Priority: High) - enforce least privilege");
        sb.AppendLine("- [ ] Secrets management (Priority: High) - use environment/secret store");
        sb.AppendLine("- [ ] Audit logging (Priority: Medium) - capture critical actions");
        sb.AppendLine();
        sb.AppendLine("## Performance");
        sb.AppendLine("- [ ] Target p95 latency < 500ms (Priority: Medium) - caching & pagination");
        sb.AppendLine("- [ ] Load test core flows (Priority: Medium) - baseline throughput");
        sb.AppendLine();
        sb.AppendLine("## Availability");
        sb.AppendLine("- [ ] Health checks + alerts (Priority: High) - proactive recovery");
        sb.AppendLine("- [ ] Backup/restore drills (Priority: Medium) - verify RPO/RTO");
        sb.AppendLine();
        sb.AppendLine("## Observability");
        sb.AppendLine("- [ ] Structured logs (Priority: High) - correlation IDs");
        sb.AppendLine("- [ ] Tracing for key flows (Priority: Medium) - end-to-end visibility");
        sb.AppendLine();
        sb.AppendLine("## Maintainability");
        sb.AppendLine("- [ ] Clear module boundaries (Priority: High) - ownership and testability");
        sb.AppendLine("- [ ] API versioning (Priority: Low) - backward compatibility");
        sb.AppendLine();
        if (model.Nfr.Count > 0)
        {
            sb.AppendLine("## Input NFRs");
            foreach (var kv in model.Nfr)
            {
                sb.AppendLine($"- {kv.Key}: {kv.Value}");
            }
        }
        return sb.ToString();
    }

    public static string RenderApiContract(SystemModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# API / Data Contract Outline");
        sb.AppendLine();
        sb.AppendLine("## Endpoints");
        sb.AppendLine("- GET /health");
        sb.AppendLine("- POST /auth/login");
        sb.AppendLine("- GET /entities");
        sb.AppendLine("- POST /entities");
        sb.AppendLine("- GET /entities/{id}");
        sb.AppendLine("- PUT /entities/{id}");
        sb.AppendLine("- DELETE /entities/{id}");
        sb.AppendLine();
        sb.AppendLine("## Entities");
        foreach (var entity in model.DataEntities)
        {
            sb.AppendLine($"- {entity}");
        }
        sb.AppendLine();
        sb.AppendLine("## Events");
        sb.AppendLine("- EntityCreated");
        sb.AppendLine("- EntityUpdated");
        sb.AppendLine("- EntityDeleted");
        sb.AppendLine();
        sb.AppendLine("## Error Model");
        sb.AppendLine("- { code, message, correlationId } with HTTP status codes");
        return sb.ToString();
    }

    public static string RenderRiskRegister(SystemModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Risk Register");
        sb.AppendLine();
        sb.AppendLine("| Risk | Likelihood | Impact | Mitigation |");
        sb.AppendLine("| --- | --- | --- | --- |");
        sb.AppendLine("| Scope creep | Medium | High | Keep backlog prioritized and approve changes | ");
        sb.AppendLine("| Integration delays | Medium | Medium | Early contract testing with partners | ");
        sb.AppendLine("| Data quality issues | Low | High | Validation and monitoring on ingestion | ");
        sb.AppendLine("| Security misconfig | Medium | High | Security reviews + automated scanning | ");
        sb.AppendLine("| Performance bottlenecks | Medium | Medium | Load testing + caching | ");
        sb.AppendLine("| Availability gaps | Low | High | HA design + backups | ");
        sb.AppendLine("| Unclear ownership | Medium | Medium | Clear module/service ownership | ");
        sb.AppendLine("| Observability blind spots | Medium | Medium | Tracing + logging standards | ");
        return sb.ToString();
    }
}
