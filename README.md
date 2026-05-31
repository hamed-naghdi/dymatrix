# DyMatrix Notification Service

A .NET 10 HTTP notification ingestion service that receives structured notifications, analyzes them using an LLM,
and forwards alerts to Discord when the severity level is `warning` or above.

---

## Architecture

Clean Architecture with Domain, Application, Infrastructure, and API layers,
based on [jasontaylordev/CleanArchitecture](https://github.com/jasontaylordev/CleanArchitecture).

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A Discord webhook URL ([guide](https://support.discord.com/hc/en-us/articles/228383668))
- An OpenAI API key **or** a locally running [Ollama](https://ollama.com) instance

### Required Configuration

> ⚠️ The application will **not start** without these values set. Add them to `appsettings.json` or via user-secrets before running.

| Key | Description |
|---|---|
| `Llm:Provider` | `openai` or `ollama` |
| `Llm:ModelId` | e.g. `gpt-4o` or `qwen3` |
| `Llm:ApiKey` | Required when provider is `openai` |
| `Ollama:Endpoint` | Required when provider is `ollama` (default: `http://localhost:11434`) |
| `Discord:WebhookUrl` | Your Discord incoming webhook URL |

### Run

```bash
# Set secrets
dotnet user-secrets set "Llm:ApiKey" "sk-..." --project src/Presentation/DyMatrix.Api
dotnet user-secrets set "Discord:WebhookUrl" "https://discord.com/api/webhooks/..." --project src/Presentation/DyMatrix.Api

dotnet run --project src/Presentation/DyMatrix.Api
```

- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:7000`
- Scalar UI: `https://localhost:7000/scalar`

### Run with Ollama (no API key needed)

```bash
ollama pull qwen3
dotnet run --project src/Presentation/DyMatrix.Api
```

`appsettings.Development.json` already sets `Provider=ollama`.

### Run Tests

```bash
dotnet test
```

---

## API Reference

### `POST /notifications`

**Request body:**

```json
{
  "title": "Database connection failed",
  "message": "Connection to primary DB timed out after 30s.",
  "level": "error",
  "source": "OrderService",
  "timestamp": "2026-05-31T10:00:00Z"
}
```

| Field | Type | Required | Notes |
|---|---|---|---|
| `title` | string | ✅ | Max 200 characters |
| `message` | string | ✅ | Max 2000 characters |
| `level` | string | ✅ | `information`, `warning`, `error`, `critical` (case-insensitive) |
| `source` | string | ❌ | Max 100 characters. Defaults to `unknown` |
| `timestamp` | datetime | ❌ | Cannot be in the future. Defaults to `UtcNow` |

**Responses:**

| Status | Condition | Body |
|---|---|---|
| `202 Accepted` | Information level — logged only | `{ "id": "...", "forwarded": false }` |
| `202 Accepted` | Warning+ — forwarded to Discord | `{ "id": "...", "forwarded": true }` |
| `202 Accepted` | Warning+ but Discord failed | `{ "id": "...", "forwarded": false }` |
| `400 Bad Request` | Validation failure | ProblemDetails with `errors` dictionary |
| `429 Too Many Requests` | Rate limit exceeded | ProblemDetails + `Retry-After: 60` header |

---

## Rate Limiting

Max **10 messages per minute** forwarded to Discord. Applies only to `warning` and above — `information` notifications
are never counted. Uses .NET's built-in `SlidingWindowRateLimiter`. Returns `429` with `Retry-After: 60` when exceeded.

---

## LLM Integration

Uses `Microsoft.Extensions.AI`'s `IChatClient` abstraction — switch between OpenAI and Ollama via configuration only,
no code changes required. If the LLM call fails, a structured fallback message is forwarded instead. The caller always receives `202`.

---

## Architecture Notes (Production Considerations)

### 1. Background Job for Forwarding

The current implementation processes LLM and Discord calls synchronously within the HTTP request. In production,
notifications should be persisted immediately and a `BackgroundService` with the Outbox pattern should handle forwarding
asynchronously — decoupling ingestion latency from downstream availability and providing guaranteed delivery with retry logic.

### 2. Distributed Rate Limiting

The current `RateLimiterService` is in-process only. In a horizontally scaled deployment, each instance maintains its
own counter. A **Redis-backed distributed rate limiter** (atomic sliding window via `ZADD` + `ZREMRANGEBYSCORE` + `ZCARD`)
would be required to enforce the limit across all instances.

### 3. Multi-Provider LLM Routing via Keyed DI

If requirements called for routing different severity levels to different models, .NET's Keyed DI would be the clean
solution — the `IChatClient` abstraction is already in place and the change would be isolated to the DI registration.

### 4. Additional production concerns

Persistence (PostgreSQL + EF Core for audit trail and replay), observability (OpenTelemetry metrics and distributed tracing),
and endpoint authentication (API key or mTLS) are outside the scope of this task but would be required before production deployment.

### 5. Testing

For production-grade test coverage, more robust approaches would be considered — such as **Testcontainers**
for spinning up real infrastructure (Redis, PostgreSQL) in integration tests, **contract testing** with Pact
for the Discord webhook integration, and **chaos/resilience testing** with tools like Polly's chaos strategies to verify
fallback behavior under real failure conditions.
For the scope of this task, `WebApplicationFactory` with faked dependencies was chosen for simplicity and speed.