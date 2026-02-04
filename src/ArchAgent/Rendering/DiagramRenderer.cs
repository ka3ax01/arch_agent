using System;
using System.Linq;
using System.Text;
using ArchAgent.Model;

namespace ArchAgent.Rendering;

public static class DiagramRenderer
{
    public static string RenderContextDiagram(SystemModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("@startuml");
        sb.AppendLine("title C4 Context Diagram");
        sb.AppendLine("skinparam rectangle {" );
        sb.AppendLine("  BackgroundColor #F7F7F7");
        sb.AppendLine("  BorderColor #333333");
        sb.AppendLine("}");
        sb.AppendLine();
        foreach (var actor in model.Actors)
        {
            sb.AppendLine($"actor \"{Escape(actor)}\" as {Alias(actor)}");
        }
        sb.AppendLine("rectangle \"System Boundary\" as SystemBoundary {");
        sb.AppendLine("  rectangle \"AI Architected System\" as SystemBox");
        sb.AppendLine("}");
        sb.AppendLine();
        foreach (var actor in model.Actors)
        {
            sb.AppendLine($"{Alias(actor)} --> SystemBox : uses");
        }
        foreach (var integration in model.Integrations)
        {
            sb.AppendLine($"SystemBox ..> \"{Escape(integration)}\" : integrates");
        }
        sb.AppendLine("@enduml");
        return sb.ToString();
    }

    public static string RenderContainerDiagram(SystemModel model, ArchitectureDecision decision)
    {
        var sb = new StringBuilder();
        sb.AppendLine("@startuml");
        sb.AppendLine("title C4 Container Diagram");
        sb.AppendLine("skinparam rectangle {" );
        sb.AppendLine("  BackgroundColor #F0F8FF");
        sb.AppendLine("  BorderColor #333333");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("rectangle \"System\" as SystemBoundary {");
        sb.AppendLine("  rectangle \"Web UI\" as WebUI");
        sb.AppendLine("  rectangle \"API\" as Api");
        sb.AppendLine("  database \"Database\" as Db");
        if (decision.UseBroker)
        {
            sb.AppendLine("  rectangle \"Message Broker\" as Broker");
        }
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("WebUI --> Api : HTTPS");
        sb.AppendLine("Api --> Db : SQL");
        if (decision.UseBroker)
        {
            sb.AppendLine("Api --> Broker : publish/consume");
        }
        foreach (var integration in model.Integrations)
        {
            sb.AppendLine($"Api ..> \"{Escape(integration)}\" : REST/SMTP");
        }
        sb.AppendLine("@enduml");
        return sb.ToString();
    }

    public static string RenderSequenceDiagram(SystemModel model, ArchitectureDecision decision)
    {
        var flow = model.KeyFlows.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(flow))
        {
            flow = "Booking flow";
        }

        var sb = new StringBuilder();
        sb.AppendLine("@startuml");
        sb.AppendLine($"title Sequence Diagram - {Escape(flow)}");
        sb.AppendLine("actor User");
        sb.AppendLine("participant WebUI");
        sb.AppendLine("participant API");
        sb.AppendLine("database DB");
        if (decision.UseBroker)
        {
            sb.AppendLine("queue Broker");
        }
        sb.AppendLine();
        sb.AppendLine("User -> WebUI : Submit request");
        sb.AppendLine("WebUI -> API : POST /request");
        sb.AppendLine("API -> DB : Validate + persist");
        sb.AppendLine("DB --> API : Result");
        if (decision.UseBroker)
        {
            sb.AppendLine("API -> Broker : Publish event");
        }
        if (model.Integrations.Any())
        {
            sb.AppendLine($"API -> \"{Escape(model.Integrations.First())}\" : Notify/Integrate");
        }
        sb.AppendLine("API --> WebUI : Response");
        sb.AppendLine("WebUI --> User : Confirmation");
        sb.AppendLine("@enduml");
        return sb.ToString();
    }

    private static string Alias(string name)
    {
        var cleaned = new string(name.Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            cleaned = "Actor";
        }
        return cleaned;
    }

    private static string Escape(string value)
    {
        return value.Replace("\"", "'");
    }
}
