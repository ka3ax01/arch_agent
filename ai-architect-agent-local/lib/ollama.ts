import { ArchitectResponseJsonSchema, ArchitectResponseSchema, type ArchitectResponse } from "@/lib/schema";

type ChatMessage = {
  role: "system" | "user" | "assistant";
  content: string;
};

type OllamaChatResponse = {
  message?: {
    content?: string;
  };
  error?: string;
};

class ArchitectAgentError extends Error {
  status: number;
  constructor(message: string, status: number) {
    super(message);
    this.name = "ArchitectAgentError";
    this.status = status;
  }
}

export class InvalidInputError extends ArchitectAgentError {
  constructor(message: string) {
    super(message, 400);
    this.name = "InvalidInputError";
  }
}

export class OllamaUnreachableError extends ArchitectAgentError {
  constructor(message = "Ollama is unreachable. Ensure it is running on http://localhost:11434.") {
    super(message, 502);
    this.name = "OllamaUnreachableError";
  }
}

export class ModelNotFoundError extends ArchitectAgentError {
  constructor(message: string) {
    super(message, 404);
    this.name = "ModelNotFoundError";
  }
}

export class InvalidArchitectJsonError extends ArchitectAgentError {
  constructor(message: string) {
    super(message, 422);
    this.name = "InvalidArchitectJsonError";
  }
}

const OLLAMA_URL = "http://localhost:11434/api/chat";
const OLLAMA_MODEL = process.env.OLLAMA_MODEL ?? "qwen2.5:7b-instruct";
const OLLAMA_TIMEOUT_MS = Number(process.env.OLLAMA_TIMEOUT_MS ?? "180000");

export const SYSTEM_PROMPT = `You are AI Architect Agent — a strict system-architecture generator.
MISSION:
- For any user prompt that can reasonably be interpreted as building software, designing an app, service, platform, feature, or system: produce a system architecture deliverable.
- If the prompt is not reasonably mappable to software/system architecture (e.g., purely creative writing, general trivia with no system context, unrelated requests), refuse as out-of-scope.
OUTPUT RULES:
- Return JSON ONLY that conforms to the provided schema.
- Never include markdown fences.
- Never include additional keys.
- Keep Mermaid diagrams as raw mermaid text strings (no \`\`\` fences).
- Be concise but complete.
SCOPE CHECK:
- If in_scope=false: message must say it's out of scope and suggest asking for an app/system so you can produce architecture.
- If in_scope=true: always provide architecture.`;

const FIX_JSON_SYSTEM_PROMPT = `Fix the provided JSON so it exactly matches the required schema.
Rules:
- Return JSON ONLY.
- Keep the same intent as the original.
- Do not add extra keys.
- Ensure all required keys exist with correct types.`;

const FIX_MERMAID_SYSTEM_PROMPT = `Fix Mermaid diagram syntax in the provided JSON while keeping the same architecture intent.
Rules:
- Return JSON ONLY.
- Keep all top-level keys exactly as required by schema.
- Keep content concise.
- Fix diagrams.mermaid.c4_context and diagrams.mermaid.c4_component as valid Mermaid flowchart/graph syntax.
- Fix diagrams.mermaid.sequence as valid Mermaid sequenceDiagram syntax.
- Do not use markdown fences.
- Do not use standalone lines wrapped with |...| (invalid in Mermaid flowchart bodies).`;

function isModelNotFound(text: string): boolean {
  return /model/i.test(text) && /(not found|missing|pull)/i.test(text);
}

async function callOllama(messages: ChatMessage[]): Promise<string> {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), OLLAMA_TIMEOUT_MS);

  try {
    const response = await fetch(OLLAMA_URL, {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify({
        model: OLLAMA_MODEL,
        messages,
        stream: false,
        format: ArchitectResponseJsonSchema,
        options: { temperature: 0 }
      }),
      signal: controller.signal
    });

    const rawText = await response.text();

    if (!response.ok) {
      if (isModelNotFound(rawText)) {
        throw new ModelNotFoundError(`Model '${OLLAMA_MODEL}' not found. Pull it with: ollama pull ${OLLAMA_MODEL}`);
      }
      throw new OllamaUnreachableError(`Ollama request failed (${response.status}). ${rawText || "No error body returned."}`);
    }

    let parsed: OllamaChatResponse;
    try {
      parsed = JSON.parse(rawText) as OllamaChatResponse;
    } catch {
      throw new OllamaUnreachableError("Ollama returned a non-JSON response.");
    }

    const content = parsed.message?.content;
    if (!content || typeof content !== "string") {
      throw new OllamaUnreachableError("Ollama response did not include message.content.");
    }

    return content;
  } catch (error) {
    if (error instanceof ArchitectAgentError) {
      throw error;
    }
    if (error instanceof DOMException && error.name === "AbortError") {
      throw new OllamaUnreachableError(`Ollama request timed out after ${OLLAMA_TIMEOUT_MS}ms.`);
    }
    throw new OllamaUnreachableError();
  } finally {
    clearTimeout(timeout);
  }
}

function parseAndValidateArchitectResponse(text: string): ArchitectResponse {
  const parsedJson = JSON.parse(text) as unknown;
  const parsed = ArchitectResponseSchema.safeParse(parsedJson);
  if (!parsed.success) {
    throw new InvalidArchitectJsonError(`Schema validation failed: ${parsed.error.message}`);
  }
  return parsed.data;
}

function normalizeMermaidText(input: string): string {
  let value = input.trim();

  if (value.startsWith("```")) {
    value = value.replace(/^```[a-zA-Z]*\n?/, "").replace(/\n?```$/, "").trim();
  }

  value = value.replace(/^mermaid\s*\n/i, "").trim();
  return value;
}

function normalizeArchitectResponseMermaid(response: ArchitectResponse): ArchitectResponse {
  return {
    ...response,
    diagrams: {
      mermaid: {
        c4_context: normalizeMermaidText(response.diagrams.mermaid.c4_context),
        c4_component: normalizeMermaidText(response.diagrams.mermaid.c4_component),
        sequence: normalizeMermaidText(response.diagrams.mermaid.sequence)
      }
    }
  };
}

function validateFlowchartLikeMermaid(name: string, source: string): string[] {
  const errors: string[] = [];
  const value = source.trim();

  if (!value) {
    errors.push(`${name}: is empty`);
    return errors;
  }

  const firstLine = value.split("\n")[0]?.trim() ?? "";
  if (!/^(flowchart|graph)\b/i.test(firstLine)) {
    errors.push(`${name}: must start with 'flowchart' or 'graph'`);
  }

  const lines = value.split("\n");
  for (const line of lines) {
    if (/^\s*\|.*\|\s*$/.test(line)) {
      errors.push(`${name}: contains invalid standalone '|...|' line`);
      break;
    }
  }

  if (!/(-->|---|-.->|==>)/.test(value)) {
    errors.push(`${name}: missing relationship arrows`);
  }

  return errors;
}

function validateSequenceMermaid(source: string): string[] {
  const errors: string[] = [];
  const value = source.trim();

  if (!value) {
    errors.push("sequence: is empty");
    return errors;
  }

  const firstLine = value.split("\n")[0]?.trim() ?? "";
  if (!/^sequenceDiagram\b/i.test(firstLine)) {
    errors.push("sequence: must start with 'sequenceDiagram'");
  }

  if (!/(->>|-->>|->|-->)/.test(value)) {
    errors.push("sequence: missing message arrows");
  }

  return errors;
}

function validateMermaidDiagrams(response: ArchitectResponse): string[] {
  if (!response.scope_check.in_scope) {
    return [];
  }

  const { c4_context, c4_component, sequence } = response.diagrams.mermaid;
  return [
    ...validateFlowchartLikeMermaid("c4_context", c4_context),
    ...validateFlowchartLikeMermaid("c4_component", c4_component),
    ...validateSequenceMermaid(sequence)
  ];
}

async function generateArchitectResponseWithJsonRepair(prompt: string): Promise<ArchitectResponse> {
  const primaryRaw = await callOllama([
    { role: "system", content: SYSTEM_PROMPT },
    { role: "user", content: prompt }
  ]);

  try {
    return parseAndValidateArchitectResponse(primaryRaw);
  } catch (error) {
    const validationMessage =
      error instanceof InvalidArchitectJsonError
        ? error.message
        : "The first response was not valid JSON.";

    const repairRaw = await callOllama([
      { role: "system", content: SYSTEM_PROMPT },
      { role: "system", content: FIX_JSON_SYSTEM_PROMPT },
      {
        role: "user",
        content: `Original user prompt:\n${prompt}\n\nValidation issue:\n${validationMessage}\n\nInvalid JSON text:\n${primaryRaw}`
      }
    ]);

    try {
      return parseAndValidateArchitectResponse(repairRaw);
    } catch (secondError) {
      const reason = secondError instanceof Error ? secondError.message : "Unknown validation failure";
      throw new InvalidArchitectJsonError(
        `Model produced invalid architecture JSON after one repair attempt. ${reason}`
      );
    }
  }
}

export async function callArchitectAgent(prompt: string): Promise<ArchitectResponse> {
  const trimmedPrompt = prompt.trim();
  if (!trimmedPrompt) {
    throw new InvalidInputError("Prompt is required.");
  }

  const initialResponse = normalizeArchitectResponseMermaid(
    await generateArchitectResponseWithJsonRepair(trimmedPrompt)
  );
  const mermaidErrors = validateMermaidDiagrams(initialResponse);
  if (mermaidErrors.length === 0) {
    return initialResponse;
  }

  const mermaidRepairRaw = await callOllama([
    { role: "system", content: SYSTEM_PROMPT },
    { role: "system", content: FIX_MERMAID_SYSTEM_PROMPT },
    {
      role: "user",
      content: `Original user prompt:\n${trimmedPrompt}\n\nMermaid validation issues:\n${mermaidErrors.join(
        "\n"
      )}\n\nCurrent JSON:\n${JSON.stringify(initialResponse)}`
    }
  ]);

  const repairedResponse = normalizeArchitectResponseMermaid(parseAndValidateArchitectResponse(mermaidRepairRaw));
  const postRepairMermaidErrors = validateMermaidDiagrams(repairedResponse);
  if (postRepairMermaidErrors.length > 0) {
    throw new InvalidArchitectJsonError(
      `Mermaid diagrams are invalid after one Mermaid repair attempt: ${postRepairMermaidErrors.join("; ")}`
    );
  }

  return repairedResponse;
}
