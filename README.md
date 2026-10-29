# AiMultiAgent

Arquitetura multi-agente com API + Worker assíncrono usando RabbitMQ e MongoDB.

## O que este projeto faz

1. A API recebe um prompt (`POST /api/ai`) e publica na fila RabbitMQ.
2. O Worker consome a fila e roteia o prompt para um provedor LLM (OpenAI ou Gemini).
3. A resposta é persistida no MongoDB para rastreabilidade.

## Segurança (Fase 1)

- Chaves de OpenAI e Gemini não ficam mais no código-fonte.
- A API exige `X-API-Key` para aceitar prompts.
- `docker-compose` usa variáveis de ambiente para segredos.

> **Ação obrigatória**: se as chaves antigas já foram versionadas, faça rotação/revogação imediata nos provedores.

## Confiabilidade de fila (Fase 2)

- `manual ack` no Worker (`autoAck: false`).
- Retry automático por mensagem com header `x-retry-count`.
- Limite configurável de tentativas (`Rabbit:MaxRetryCount`, default 3).
- Mensagem excedendo tentativas vai para **DLQ**:
  - exchange: `ai_dlx`
  - queue: `ai_queue.dlq`

## Observabilidade (Fase 3)

### Instrumentação OpenTelemetry

A API e o Worker exportam traces e métricas via OTLP.

Service names:
- `AiMultiAgent.API`
- `AiMultiAgent.Worker`

Métricas customizadas principais:
- `ai_api_prompts_received_total`
- `ai_api_prompt_validation_errors_total`
- `ai_worker_messages_processed_total`
- `ai_worker_messages_failed_total`
- `ai_worker_messages_retried_total`
- `ai_worker_messages_dlq_total`
- `ai_worker_message_processing_ms`
- `ai_orchestrator_routing_total` (com tag `provider`)
- `ai_agent_requests_total` (com tags `provider` e `model`)
- `ai_agent_errors_total` (com tags `provider` e `model`)
- `ai_agent_duration_ms` (com tags `provider` e `model`)

### Stack local de observabilidade

Arquivos adicionados:
- `docker-compose.observability.yml`
- `observability/otel-collector-config.yaml`
- `observability/prometheus.yml`
- `observability/grafana/provisioning/datasources/datasources.yml`
- `observability/grafana/provisioning/dashboards/dashboards.yml`
- `observability/grafana/dashboards/queue-and-agents-overview.json`

Suba tudo (app + observabilidade):

```bash
docker compose -f docker-compose.yml -f docker-compose.observability.yml up -d
```

Acessos locais:
- Grafana: `http://localhost:3000` (admin/admin)
- Jaeger: `http://localhost:16686`
- Prometheus: `http://localhost:9090`

Dashboard provisionado automaticamente:
- `AiMultiAgent / AiMultiAgent - Queue & Agents Overview`

## Configuração rápida

1. Copie `.env.example` para `.env` e preencha os valores:

```bash
cp .env.example .env
```

2. Suba os serviços:

```bash
docker compose up -d
```

3. Chame a API com a chave:

```bash
curl -X POST http://localhost:5000/api/ai \
  -H "Content-Type: application/json" \
  -H "X-API-Key: change-this-api-key" \
  -d '{"message":"Explique o que é programação orientada a objetos"}'
```

## Variáveis principais

### API
- `AI_API_KEY`: chave exigida no header `X-API-Key`.
- `Rabbit__HostName`
- `Rabbit__QueueName`
- `Rabbit__DeadLetterExchange`
- `Rabbit__DeadLetterQueueName`
- `Rabbit__MaxRetryCount`
- `OTEL_EXPORTER_OTLP_ENDPOINT`
- `OTEL_EXPORTER_OTLP_PROTOCOL` (`grpc` ou `http/protobuf`)

### Worker
- `OPENAI_API_KEY`
- `GEMINI_API_KEY`
- `LLM__OpenAI__Model` (opcional, default `gpt-4.1-mini`)
- `LLM__Gemini__Model` (opcional, default `gemini-2.5-flash`)
- `Rabbit__HostName`
- `Rabbit__QueueName`
- `Rabbit__DeadLetterExchange`
- `Rabbit__DeadLetterQueueName`
- `Rabbit__MaxRetryCount`
- `Mongo__ConnectionString`
- `Mongo__DatabaseName`
- `Mongo__LogsCollectionName`
- `OTEL_EXPORTER_OTLP_ENDPOINT`
- `OTEL_EXPORTER_OTLP_PROTOCOL` (`grpc` ou `http/protobuf`)

## Estrutura do projeto

- `AiMultiAgent.API`: entrada HTTP e publicação em fila.
- `AiMultiAgent.Worker`: consumo de fila, orquestração e persistência.
- `AiMultiAgent.Application`: agentes e orquestrador.
- `AiMultiAgent.Infrastructure`: RabbitMQ e MongoDB.

## CI/CD

Pipeline GitHub Actions em `.github/workflows/ci.yml` com:

1. `dotnet restore`
2. `dotnet build --configuration Release --no-restore`
3. `dotnet test` (auto-skip se não houver projeto de teste)

## Próximas evoluções recomendadas

- Testes automatizados para orquestrador e integração API/Worker.
- Política de roteamento por custo/latência/qualidade (não só keyword).
- Endpoint para inspeção da DLQ (admin/ops).
- Persistência de metadados de execução (provider/model/latência/status) no MongoDB.
