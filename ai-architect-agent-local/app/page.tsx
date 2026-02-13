"use client";

import { useMemo, useState } from "react";
import MermaidBlock from "@/components/MermaidBlock";
import JsonCard from "@/components/JsonCard";
import type { ArchitectResponse } from "@/lib/schema";

export default function HomePage() {
  const [prompt, setPrompt] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<ArchitectResponse | null>(null);

  const jsonOutput = useMemo(() => (result ? JSON.stringify(result, null, 2) : ""), [result]);

  async function submitPrompt() {
    const trimmed = prompt.trim();
    if (!trimmed) {
      setError("Please enter a prompt.");
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response = await fetch("/api/architect", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ prompt: trimmed })
      });

      const data = (await response.json()) as ArchitectResponse | { error?: string };

      if (!response.ok) {
        const message = "error" in data && typeof data.error === "string" ? data.error : "Request failed.";
        throw new Error(message);
      }

      setResult(data as ArchitectResponse);
    } catch (requestError) {
      setResult(null);
      setError(requestError instanceof Error ? requestError.message : "Unexpected error");
    } finally {
      setLoading(false);
    }
  }

  async function copyText(value: string, label: string) {
    try {
      await navigator.clipboard.writeText(value);
      setError(`${label} copied to clipboard.`);
      setTimeout(() => {
        setError((current) => (current === `${label} copied to clipboard.` ? null : current));
      }, 1500);
    } catch {
      setError(`Failed to copy ${label.toLowerCase()}.`);
    }
  }

  return (
    <main className="mx-auto flex w-full max-w-7xl flex-col gap-6 px-4 py-8 md:px-6">
      <header className="space-y-2">
        <h1 className="text-2xl font-bold">AI Architect Agent (Local)</h1>
        <p className="text-sm text-slate-600">
          Single-purpose generator. Every successful response is strict architecture JSON.
        </p>
      </header>

      <section className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
        <label htmlFor="prompt" className="mb-2 block text-sm font-medium text-slate-700">
          Describe the app/system you want to design
        </label>
        <textarea
          id="prompt"
          className="min-h-36 w-full rounded-lg border border-slate-300 p-3 text-sm outline-none ring-offset-2 focus:border-slate-400 focus:ring-2 focus:ring-slate-200"
          placeholder="Example: Build a recipe app with search, favorites, subscriptions, and admin moderation."
          value={prompt}
          onChange={(event) => setPrompt(event.target.value)}
        />
        <div className="mt-3 flex flex-wrap gap-3">
          <button
            type="button"
            disabled={loading}
            onClick={submitPrompt}
            className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-semibold text-white transition hover:bg-slate-700 disabled:cursor-not-allowed disabled:bg-slate-400"
          >
            {loading ? "Generating..." : "Generate Architecture"}
          </button>
          <button
            type="button"
            disabled={!result}
            onClick={() => jsonOutput && copyText(jsonOutput, "JSON")}
            className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 transition hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-50"
          >
            Copy JSON
          </button>
          <button
            type="button"
            disabled={!result}
            onClick={() => result && copyText(result.diagrams.mermaid.c4_context, "Mermaid Context")}
            className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 transition hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-50"
          >
            Copy Mermaid: Context
          </button>
          <button
            type="button"
            disabled={!result}
            onClick={() => result && copyText(result.diagrams.mermaid.c4_component, "Mermaid Component")}
            className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 transition hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-50"
          >
            Copy Mermaid: Component
          </button>
          <button
            type="button"
            disabled={!result}
            onClick={() => result && copyText(result.diagrams.mermaid.sequence, "Mermaid Sequence")}
            className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 transition hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-50"
          >
            Copy Mermaid: Sequence
          </button>
        </div>
      </section>

      {error ? (
        <section className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-700">{error}</section>
      ) : null}

      {result ? (
        <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
          <JsonCard title="Scope Check" className="lg:col-span-2">
            <p>
              <span className="font-semibold">in_scope:</span> {String(result.scope_check.in_scope)}
            </p>
            <p>
              <span className="font-semibold">message:</span> {result.scope_check.message}
            </p>
          </JsonCard>

          <JsonCard title="Interpreted Product">
            <p>
              <span className="font-semibold">one_liner:</span> {result.interpreted_product.one_liner}
            </p>
            <p className="font-semibold">assumptions:</p>
            <ul className="list-disc space-y-1 pl-5">
              {result.interpreted_product.assumptions.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
          </JsonCard>

          <JsonCard title="Requirements">
            <p className="font-semibold">actors:</p>
            <ul className="list-disc space-y-1 pl-5">
              {result.requirements.actors.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
            <p className="font-semibold">features:</p>
            <ul className="list-disc space-y-1 pl-5">
              {result.requirements.features.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
            <p className="font-semibold">constraints:</p>
            <ul className="list-disc space-y-1 pl-5">
              {result.requirements.constraints.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
            <p className="font-semibold">integrations:</p>
            <ul className="list-disc space-y-1 pl-5">
              {result.requirements.integrations.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
            <p className="font-semibold">data_entities:</p>
            <ul className="list-disc space-y-1 pl-5">
              {result.requirements.data_entities.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
            <p className="font-semibold">key_flows:</p>
            <ul className="list-disc space-y-1 pl-5">
              {result.requirements.key_flows.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
          </JsonCard>

          <JsonCard title="Architecture Details" className="lg:col-span-2">
            <p>
              <span className="font-semibold">style:</span> {result.architecture.style}
            </p>
            <p className="font-semibold">rationale:</p>
            <ul className="list-disc space-y-1 pl-5">
              {result.architecture.rationale.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
            <p className="font-semibold">components:</p>
            <div className="space-y-2">
              {result.architecture.components.map((component) => (
                <div key={component.name} className="rounded-lg border border-slate-200 p-3">
                  <p>
                    <span className="font-semibold">name:</span> {component.name}
                  </p>
                  <p>
                    <span className="font-semibold">responsibility:</span> {component.responsibility}
                  </p>
                  <p>
                    <span className="font-semibold">tech_options:</span> {component.tech_options.join(", ")}
                  </p>
                  <p>
                    <span className="font-semibold">interfaces:</span> {component.interfaces.join(", ")}
                  </p>
                </div>
              ))}
            </div>
            <p className="font-semibold">data.storage:</p>
            <ul className="list-disc space-y-1 pl-5">
              {result.architecture.data.storage.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
            <p className="font-semibold">data.schema_notes:</p>
            <ul className="list-disc space-y-1 pl-5">
              {result.architecture.data.schema_notes.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
            <p className="font-semibold">data.events:</p>
            <ul className="list-disc space-y-1 pl-5">
              {result.architecture.data.events.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
            <p className="font-semibold">security:</p>
            <ul className="list-disc space-y-1 pl-5">
              {result.architecture.security.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
            <p className="font-semibold">observability:</p>
            <ul className="list-disc space-y-1 pl-5">
              {result.architecture.observability.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
            <p className="font-semibold">deployment:</p>
            <ul className="list-disc space-y-1 pl-5">
              {result.architecture.deployment.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
          </JsonCard>

          <JsonCard title="NFRs">
            {result.nfrs.map((nfr) => (
              <div key={nfr.category} className="rounded-lg border border-slate-200 p-3">
                <p className="font-semibold">{nfr.category}</p>
                <ul className="list-disc space-y-1 pl-5">
                  {nfr.items.map((item) => (
                    <li key={item}>{item}</li>
                  ))}
                </ul>
              </div>
            ))}
          </JsonCard>

          <JsonCard title="Recommendations">
            <p className="font-semibold">next_steps:</p>
            <ul className="list-disc space-y-1 pl-5">
              {result.recommendations.next_steps.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
            <p className="font-semibold">tradeoffs:</p>
            <ul className="list-disc space-y-1 pl-5">
              {result.recommendations.tradeoffs.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
            <p className="font-semibold">risks:</p>
            <ul className="list-disc space-y-1 pl-5">
              {result.recommendations.risks.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
            <p className="font-semibold">questions:</p>
            <ul className="list-disc space-y-1 pl-5">
              {result.questions.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
          </JsonCard>

          <JsonCard title="Diagrams" className="lg:col-span-2">
            <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
              <MermaidBlock chart={result.diagrams.mermaid.c4_context} title="C4 Context" />
              <MermaidBlock chart={result.diagrams.mermaid.c4_component} title="C4 Component" />
              <MermaidBlock chart={result.diagrams.mermaid.sequence} title="Sequence" />
            </div>
          </JsonCard>

          <JsonCard title="Raw JSON" className="lg:col-span-2">
            <pre className="overflow-x-auto rounded-lg bg-slate-950 p-4 text-xs text-slate-100">{jsonOutput}</pre>
          </JsonCard>
        </div>
      ) : null}
    </main>
  );
}
