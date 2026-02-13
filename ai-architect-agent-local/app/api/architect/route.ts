import { NextResponse } from "next/server";
import {
  callArchitectAgent,
  InvalidArchitectJsonError,
  InvalidInputError,
  ModelNotFoundError,
  OllamaUnreachableError
} from "@/lib/ollama";

export async function POST(request: Request) {
  try {
    const body = (await request.json()) as { prompt?: unknown };
    const prompt = typeof body.prompt === "string" ? body.prompt.trim() : "";

    if (!prompt) {
      return NextResponse.json({ error: "Prompt is required." }, { status: 400 });
    }

    const data = await callArchitectAgent(prompt);
    return NextResponse.json(data, { status: 200 });
  } catch (error) {
    if (error instanceof SyntaxError) {
      return NextResponse.json({ error: "Invalid JSON request body." }, { status: 400 });
    }

    if (error instanceof InvalidInputError) {
      return NextResponse.json({ error: error.message }, { status: 400 });
    }

    if (error instanceof ModelNotFoundError) {
      return NextResponse.json({ error: error.message }, { status: 404 });
    }

    if (error instanceof InvalidArchitectJsonError) {
      return NextResponse.json({ error: error.message }, { status: 422 });
    }

    if (error instanceof OllamaUnreachableError) {
      return NextResponse.json({ error: error.message }, { status: 502 });
    }

    const message = error instanceof Error ? error.message : "Unknown server error";
    return NextResponse.json({ error: message }, { status: 500 });
  }
}
