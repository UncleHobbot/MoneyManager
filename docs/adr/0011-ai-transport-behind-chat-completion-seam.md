---
status: accepted
---

# AI transport behind an `IChatCompletion` seam; one OpenAI-compatible adapter for compatible providers

`AIService` previously fused three concerns: prompt assembly (system persona +
per-type user prompt + CSV), response parsing (`choices`/`usage`), and the raw
HTTP call against a `static HttpClient`. Two problems followed:

- **Untestable.** There was no seam — the only way to exercise prompt assembly or
  result mapping was a live HTTP call to an OpenAI endpoint. No `AIServiceTests`
  existed.
- **A latent race.** Each call did `httpClient.DefaultRequestHeaders.Clear()` then
  `.Add("Authorization", ...)` on the **shared static** client. Concurrent analyses
  could interleave and send the wrong key. The "never throws" contract was also
  leaky: transport exceptions (timeout, DNS) were uncaught.

## Decision

Introduce a transport seam `IChatCompletion` with provider-neutral domain types:

```
record ChatRequest(Endpoint, ApiKey, Model, SystemPrompt, UserPrompt, Data, Temperature);
record ChatResult(bool Success, string Content, int TotalTokens);
interface IChatCompletion { Task<ChatResult> CompleteAsync(ChatRequest request); }
```

- `AIService` keeps prompt assembly, provider resolution, and `ChatResult →
  AnalysisResult` mapping. It no longer knows the OpenAI wire format.
- `OpenAiCompatibleChatCompletion` owns the OpenAI wire format end to end (the
  `OpenAi*` types are now `internal` to the adapter). It **never throws**: non-2xx
  responses and transport exceptions both map to `ChatResult(false, …)`. The
  `Authorization` header is set per request, not on the shared client — killing the
  race.
- The seam is justified by **two adapters**: the prod HTTP adapter and an in-memory
  fake in tests (consistent with ADR-0003's "two = real" rule).

### One adapter for all OpenAI-compatible providers

OpenAI, **DeepSeek** (`https://api.deepseek.com`), and **Z.AI GLM**
(`https://api.z.ai/api/paas/v4/`) all speak the same wire format: `/chat/completions`,
`Authorization: Bearer`, identical `messages`/`temperature`/`choices`/`usage`. They
differ only in base URL and model name — both already stored per `AiProvider`
(`ApiUrl`, `Model`). So **a single `OpenAiCompatibleChatCompletion` serves all
three**; adding DeepSeek or Z.AI GLM is a new `AiProvider` row, not new code.

We deliberately did **not** build a provider factory / per-`ProviderType` adapter
registry. Three identical adapters behind a factory would be the "one adapter =
hypothetical seam" anti-pattern ADR-0003 rejects. `AiProvider.ProviderType` is
reserved as the **future discriminator**: a provider that needs a genuinely
different wire format (e.g. Anthropic with `x-api-key`, or a non-OpenAI response
shape) gets its own `IChatCompletion` adapter selected by `ProviderType` at that
point — and only then.

### Resilience opt-out

The AI client opts out of the global `StandardResilienceHandler`
(`RemoveAllResilienceHandlers`) and uses a plain 100s timeout. The standard
handler's 10s-per-attempt / 30s-total defaults are too short for LLM latency, and
retrying a paid, non-idempotent completion is undesirable.

## Consequences

- `AIService` is testable against a fake; `OpenAiCompatibleChatCompletion` is tested
  via a stub `HttpMessageHandler` (per-request auth, absolute endpoint, parsing,
  never-throws). 11 new tests.
- The static `HttpClient` is gone; the client comes from `IHttpClientFactory`.
- The over-modelled `OpenAIMessages.cs` (and dead `OpenAISettings`) are deleted;
  only the fields the adapter reads/writes remain, `internal` to the adapter.
- Adding an OpenAI-compatible provider is configuration. A non-compatible provider
  is a second adapter — see CONTEXT.md ("AI Transport / Chat Completion").
- The `RemoveAllResilienceHandlers` API is experimental (`EXTEXP0001`, suppressed
  narrowly). If it changes, the opt-out is the one place to revisit.
