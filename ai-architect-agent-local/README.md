# ai-architect-agent-local

Local single-purpose AI Architect Agent built with Next.js (App Router), TypeScript, Tailwind CSS, Zod, Mermaid, and Ollama.

## What it does

- Accepts one prompt describing a software/app/system idea.
- Calls local Ollama (`http://localhost:11434`) with strict JSON schema output.
- Validates output with Zod.
- If invalid, performs **one automatic repair attempt** with Ollama.
- If Mermaid diagrams are invalid, performs **one Mermaid repair attempt** with Ollama.
- Returns architecture JSON only.
- Renders Mermaid diagrams locally in the browser.

## Requirements

- Node.js 18+
- npm
- Ollama installed and running locally

## Install and run

```bash
npm install
npm run dev
```

Open: `http://localhost:3000`

## Ollama setup

In another terminal:

```bash
ollama serve
ollama pull qwen2.5:7b-instruct
```

Optional model override:

```bash
OLLAMA_MODEL=llama3.1:8b npm run dev
```

Optional timeout override for very large prompts:

```bash
OLLAMA_TIMEOUT_MS=180000 OLLAMA_MODEL=llama3.1:8b npm run dev
```

## API contract

- Route: `POST /api/architect`
- Request body:

```json
{ "prompt": "your system request" }
```

- Success: `200` with strict architecture JSON.
- Errors:
  - `400` invalid/empty prompt
  - `502` Ollama unreachable or request failure
  - `404` model not found
  - `422` invalid JSON even after one repair attempt

## Example prompts

- `Хочу приложение с рецептами: поиск, категории, избранное.`
- `Добавь: роли (админ/юзер), подписка, платежи, рекомендации, офлайн доступ.`
- `Напиши стих о весне.` (should produce `scope_check.in_scope = false` in JSON)

## Troubleshooting

### Ollama not running

Symptoms:
- API returns `502`
- UI shows Ollama unreachable

Fix:

```bash
ollama serve
```

Ensure `http://localhost:11434` is reachable.

### Model not found

Symptoms:
- API returns `404`
- Error message says model is not found

Fix:

```bash
ollama pull qwen2.5:7b-instruct
```

Or set `OLLAMA_MODEL` to an available local model.

### JSON invalid

Symptoms:
- API returns `422`
- Message says invalid architecture JSON after retry

What happens:
- The server already did one automatic repair call.

Fix ideas:
- Retry request.
- Use a stronger local model.
- Make prompt more explicit about system architecture scope.

### Timeout / large prompts

Symptoms:
- API returns `502`
- Error contains `timed out`

Fix:
- Increase timeout with `OLLAMA_TIMEOUT_MS` (example: `180000` or `300000`).
- Warm up the model once before testing:

```bash
ollama run llama3.1:8b "warmup"
```

## Notes

- No cloud APIs, no API keys, no commercial endpoints.
- Mermaid is installed from npm and rendered client-side.
- This app is intentionally single-purpose and not a general chat app.
