import { z } from "zod";
import { zodToJsonSchema } from "zod-to-json-schema";

const ScopeCheckSchema = z
  .object({
    in_scope: z.boolean(),
    message: z.string()
  })
  .strict();

const InterpretedProductSchema = z
  .object({
    one_liner: z.string(),
    assumptions: z.array(z.string())
  })
  .strict();

const RequirementsSchema = z
  .object({
    actors: z.array(z.string()),
    features: z.array(z.string()),
    constraints: z.array(z.string()),
    integrations: z.array(z.string()),
    data_entities: z.array(z.string()),
    key_flows: z.array(z.string())
  })
  .strict();

const ComponentSchema = z
  .object({
    name: z.string(),
    responsibility: z.string(),
    tech_options: z.array(z.string()),
    interfaces: z.array(z.string())
  })
  .strict();

const ArchitectureSchema = z
  .object({
    style: z.enum(["modular_monolith", "microservices"]),
    rationale: z.array(z.string()),
    components: z.array(ComponentSchema),
    data: z
      .object({
        storage: z.array(z.string()),
        schema_notes: z.array(z.string()),
        events: z.array(z.string())
      })
      .strict(),
    security: z.array(z.string()),
    observability: z.array(z.string()),
    deployment: z.array(z.string())
  })
  .strict();

const NfrSchema = z
  .object({
    category: z.string(),
    items: z.array(z.string())
  })
  .strict();

const DiagramsSchema = z
  .object({
    mermaid: z
      .object({
        c4_context: z.string(),
        c4_component: z.string(),
        sequence: z.string()
      })
      .strict()
  })
  .strict();

const RecommendationsSchema = z
  .object({
    next_steps: z.array(z.string()),
    tradeoffs: z.array(z.string()),
    risks: z.array(z.string())
  })
  .strict();

export const ArchitectResponseSchema = z
  .object({
    scope_check: ScopeCheckSchema,
    interpreted_product: InterpretedProductSchema,
    requirements: RequirementsSchema,
    architecture: ArchitectureSchema,
    nfrs: z.array(NfrSchema),
    diagrams: DiagramsSchema,
    recommendations: RecommendationsSchema,
    questions: z.array(z.string())
  })
  .strict();

export type ArchitectResponse = z.infer<typeof ArchitectResponseSchema>;

export const ArchitectResponseJsonSchema = zodToJsonSchema(ArchitectResponseSchema, {
  name: "ArchitectResponse",
  $refStrategy: "none",
  target: "jsonSchema7"
});
