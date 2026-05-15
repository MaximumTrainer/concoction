# LLM Chat Design: GCP, AWS, and Azure

## Objective

Implement production-grade chat functionality for Concoction using cloud LLM services from:

- **GCP Vertex AI** (Gemini)
- **AWS Bedrock** (Claude/Titan)
- **Azure OpenAI** (GPT)

The design preserves Concoction's hexagonal architecture by introducing provider adapters in Infrastructure and a provider-agnostic port in Application.

---

## Current State Summary

- `Concoction.Application.Chat.AgentChatService` exists and supports session management and tool invocation.
- `ITool` and `IToolRegistry` ports are already defined.
- Built-in tools (`discover_schema`, `generate_data`) are registered in DI.
- Chat currently supports manual `/tool` message commands; there is no external LLM provider integration yet.

---

## Proposed Architecture

### Layers

- **Domain**: provider-neutral LLM records/enums
- **Application**: orchestration logic, context composition, token budgeting, provider ports
- **Infrastructure**: cloud-specific adapters for GCP/AWS/Azure
- **API**: chat endpoints consuming `IAgentChatService`

### Core Flow

1. User sends chat message.
2. `AgentChatService` stores the message and composes instructions + history.
3. Service calls a selected `ILlmProvider`.
4. If provider returns tool calls, tools are executed through `IToolRegistry`.
5. Tool outputs are sent back to the provider.
6. Final assistant response is stored and returned.

---

## Domain Additions

Add provider-neutral models:

- `LlmProvider` enum
- `LlmFinishReason` enum
- `LlmMessage`
- `LlmToolCall`
- `LlmToolDefinition`
- `LlmCompletion`
- `LlmCompletionRequest`

These remain independent of any cloud SDK types.

---

## Application Additions

### New Ports

- `ILlmProvider`
- `ILlmProviderFactory`
- `ILlmContextBuilder`
- `ITokenBudgetEstimator`

### AgentChatService Enhancements

- Replace manual `/tool` dispatch path with full LLM agentic loop.
- Support repeated tool-call rounds with max-iteration guard.
- Persist tool invocation audit trail for each call.
- Enforce session authorization and workspace boundaries on each execution step.

---

## Infrastructure Adapters

Add adapters in `Concoction.Infrastructure/Llm/`:

- `GcpVertexAiAdapter`
- `AwsBedrockAdapter`
- `AzureOpenAiAdapter`
- `LlmProviderFactory`
- `FunctionCallSerializer`
- `NaiveTokenBudgetEstimator`

### Provider SDKs

- GCP: `Google.Cloud.AIPlatform.V1`
- AWS: `AWSSDK.BedrockRuntime`
- Azure: `Azure.AI.OpenAI`

---

## Configuration

Introduce `LlmOptions` with nested provider options:

- `DefaultProvider`
- `VertexAi` (project/location/model)
- `Bedrock` (region/model)
- `Azure` (endpoint/deployment)

Secrets must be resolved via `ISecretProvider` or native cloud identity (ADC/IAM/Managed Identity), never hardcoded.

---

## Security Requirements

- Keep system instructions separate from user input.
- Enforce tool allowlists per workspace.
- Block cross-workspace resource access.
- Do not log sensitive prompt payloads or secrets.
- Cap token usage and tool-loop iterations.
- Return safe fallback on content filtering/provider failures.

---

## API Enhancements

1. Update `POST /chat/sessions/{id}/messages` to return:
   - stored user message
   - assistant response
   - tool invocation summary
2. Add SSE endpoint for streaming responses:
   - `GET /chat/sessions/{id}/messages/stream`

---

## Testing Plan

### Unit Tests

- Context builder behavior and token trimming
- Agent chat orchestration with mock `ILlmProvider`
- Tool allowlist enforcement
- Loop guard and fallback behavior

### Integration Tests

- Provider adapter request/response mapping for GCP, AWS, Azure
- Streaming behavior
- Error translation and retry logic

### Security Tests

- Prompt-injection resistance
- Unauthorized tool invocation rejection
- Workspace isolation verification

---

## Implementation Checklist

- [ ] Add domain LLM models/enums
- [ ] Add application ports for provider abstraction
- [ ] Implement context builder and token estimator
- [ ] Refactor `AgentChatService` to LLM-first flow
- [ ] Add cloud adapters and provider factory
- [ ] Add DI wiring and configuration binding
- [ ] Update API response contracts and streaming endpoint
- [ ] Add unit/integration/security tests
- [ ] Update user guide with provider setup instructions

