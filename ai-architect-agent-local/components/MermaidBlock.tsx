"use client";

import mermaid from "mermaid";
import { useEffect, useMemo, useRef, useState } from "react";

type MermaidBlockProps = {
  chart: string;
  title: string;
};

let mermaidInitialized = false;

function normalizeMermaid(input: string): string {
  let value = input.trim();

  // Remove fenced code block wrappers if the model included them.
  if (value.startsWith("```")) {
    value = value.replace(/^```[a-zA-Z]*\n?/, "").replace(/\n?```$/, "").trim();
  }

  // Some models prefix content with "mermaid" as a standalone first line.
  value = value.replace(/^mermaid\s*\n/i, "").trim();

  return value;
}

function sanitizeFlowchartLikeMermaid(input: string): string {
  const lines = input.split("\n");
  return lines
    .filter((line) => !/^\s*\|.*\|\s*$/.test(line))
    .join("\n")
    .trim();
}

function sanitizeSequenceMermaid(input: string): string {
  const lines = input.split("\n");
  if (lines.length === 0) {
    return input;
  }

  const first = lines[0].trim().replace(/;$/, "");
  if (!/^sequenceDiagram$/i.test(first)) {
    return input;
  }

  const body = lines.slice(1);
  const participants = new Set<string>();
  const fixedBody: string[] = [];

  for (const rawLine of body) {
    const line = rawLine.trim();
    if (!line) {
      continue;
    }

    const implicitParticipant = line.match(/^([A-Za-z_]\w*)\s+as\s+([A-Za-z_]\w*)$/);
    if (implicitParticipant) {
      // Convert invalid "Chief as user" form into a valid participant declaration.
      const displayName = implicitParticipant[1];
      const id = implicitParticipant[2];
      participants.add(id);
      fixedBody.push(`participant ${id} as ${displayName}`);
      continue;
    }

    const participantLine = line.match(/^participant\s+([A-Za-z_]\w*)(?:\s+as\s+(.+))?$/);
    if (participantLine) {
      participants.add(participantLine[1]);
      fixedBody.push(line);
      continue;
    }

    const messageLine = line.match(/^([A-Za-z_]\w*)\s*(?:->>|-->>|->|-->)\s*([A-Za-z_]\w*)\s*:?/);
    if (messageLine) {
      participants.add(messageLine[1]);
      participants.add(messageLine[2]);
    }

    fixedBody.push(line);
  }

  const hasAutonumber = fixedBody.some((line) => line === "autonumber");
  const participantLines = Array.from(participants)
    .sort()
    .map((id) => `participant ${id}`);

  const bodyWithoutParticipants = fixedBody.filter((line) => !/^participant\s+/.test(line));
  const mergedBody = hasAutonumber
    ? ["autonumber", ...participantLines, ...bodyWithoutParticipants.filter((line) => line !== "autonumber")]
    : [...participantLines, ...bodyWithoutParticipants];

  return ["sequenceDiagram", ...mergedBody].join("\n").trim();
}

function autoFixMermaid(input: string): string {
  const firstLine = input.split("\n")[0]?.trim().replace(/;$/, "").toLowerCase() ?? "";
  if (firstLine === "sequencediagram") {
    return sanitizeSequenceMermaid(input);
  }
  if (firstLine.startsWith("graph") || firstLine.startsWith("flowchart")) {
    return sanitizeFlowchartLikeMermaid(input);
  }
  return input;
}

export default function MermaidBlock({ chart, title }: MermaidBlockProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [error, setError] = useState<string | null>(null);

  const renderId = useMemo(
    () => `mermaid-${title.toLowerCase().replace(/[^a-z0-9]+/g, "-")}-${Math.random().toString(36).slice(2)}`,
    [title]
  );

  useEffect(() => {
    if (!mermaidInitialized) {
      mermaid.initialize({ startOnLoad: false, securityLevel: "strict" });
      mermaidInitialized = true;
    }

    let cancelled = false;

    async function renderChart() {
      if (!containerRef.current) {
        return;
      }

      const normalizedChart = normalizeMermaid(chart);

      if (!normalizedChart) {
        containerRef.current.innerHTML = "";
        setError("No diagram available.");
        return;
      }

      const repairedChart = autoFixMermaid(normalizedChart);
      const renderAttempts = repairedChart !== normalizedChart ? [normalizedChart, repairedChart] : [normalizedChart];

      let lastError: unknown = null;
      for (let index = 0; index < renderAttempts.length; index += 1) {
        try {
          const { svg } = await mermaid.render(`${renderId}-${index}`, renderAttempts[index]);
          if (!cancelled && containerRef.current) {
            containerRef.current.innerHTML = svg;
            setError(null);
          }
          return;
        } catch (error) {
          lastError = error;
        }
      }

      const details =
        lastError instanceof Error && lastError.message
          ? ` ${lastError.message}`
          : "";
      if (!cancelled) {
        setError(`Diagram rendering failed. Check Mermaid syntax in the returned JSON.${details}`);
        if (containerRef.current) {
          containerRef.current.innerHTML = "";
        }
      }
    }

    void renderChart();

    return () => {
      cancelled = true;
    };
  }, [chart, renderId]);

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <h3 className="mb-3 text-sm font-semibold text-slate-700">{title}</h3>
      {error ? (
        <p className="text-sm text-slate-500">{error}</p>
      ) : (
        <div className="overflow-x-auto" ref={containerRef} aria-label={`${title} Mermaid diagram`} />
      )}
    </div>
  );
}
