# arch_agent — AI Architect Agent (Assignment 6)

AI Architect Agent generates a practical architecture package from a short system description:
- C4 diagrams (Context + Container) in PlantUML
- Architecture Decision Records (ADR)
- NFR checklist (security, performance, availability, observability)
- API / data contract outline
- Risks & assumptions

## Why this exists
In real projects, early architecture is often stuck in chats and whiteboards.
This agent turns a messy idea into a structured, review-ready architecture draft.

## Input
Provide a system description as Markdown or JSON.

### Option A: Markdown (recommended)
Create a file: `inputs/system.md` with:
- Business goal / users
- Core features
- Constraints (budget/time/tech)
- Data and integrations
- Non-functional requirements (if known)

### Option B: JSON
`inputs/system.json` with:
- actors[]
- components[]
- data_stores[]
- data_flows[]
- constraints[]
- nfrs{}

## Output
The agent writes artifacts into `artifacts/<run_id>/`:
- `docs/architecture_summary.md`
- `docs/adr_001.md` (and more ADRs if needed)
- `docs/nfr_checklist.md`
- `docs/api_contract_outline.md`
- `diagrams/c4_context.puml`
- `diagrams/c4_container.puml`
- `meta/assumptions.json`

## Agent Pipeline
1) Parse & normalize the system description
2) Propose architecture style and justify it
3) Generate C4 Context + Container diagrams
4) Review NFRs and map them to design choices
5) Produce ADRs, risks, and implementation roadmap
6) Export artifacts for review

## Quality & Safety
- The agent explicitly marks assumptions when input is incomplete
- No secrets or credentials should be included in the input
- Output is a draft and requires human review

## Quickstart
1) Put your system description into `inputs/system.md`
2) Run the agent:
   - `python -m src.agent --input inputs/system.md --out artifacts`

## Testing
Test cases are stored in `tests/` and cover:
- monolith CRUD system
- microservices + queue
- external integrations
- incomplete input (assumption handling)

## Limitations
- Architecture quality depends on the completeness of the input
- Generated diagrams are best-effort and may need refinement
- The agent does not replace security reviews or performance testing

## Quickstart (C#)
1) Ensure Ollama is running (see note below).
2) Run the agent:
   - `dotnet run --project src/ArchAgent -- --input inputs/system.md --out artifacts --mode auto --model llama3.1`

### Ollama note
Start Ollama with your preferred model, for example:
- `ollama run llama3.1`

## Modes
- `auto`: try Ollama first; if unavailable, fall back to heuristic mode
- `ollama`: require Ollama; fail if unavailable
- `heuristic`: no LLM calls

## Output artifacts tree
```
artifacts/<run_id>/
  diagrams/
    c4_context.puml
    c4_container.puml
    sequence_key_flow.puml
  docs/
    architecture_summary.md
    adr_001.md
    nfr_checklist.md
    api_contract_outline.md
    risk_register.md
  meta/
    assumptions.json
    extracted_model.json
    quality_report.json
    prompt_trace.md
```

## How to switch model
Use `--model <name>` to select a local Ollama model, e.g. `llama3.1` or `mistral`.

## Testing approach
Run the CLI against the files in `tests/` and review the artifacts. Focus on:
- style choice (monolith vs microservices)
- assumptions for incomplete inputs
- presence and consistency of all artifacts

## Limitations (C# MVP)
- Heuristic mode is intentionally simple and conservative.
- LLM output is validated but may still need human review.
