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

### Worker
- `OPENAI_API_KEY`
- `GEMINI_API_KEY`
- `LLM__OpenAI__Model` (opcional, default `gpt-4.1-mini`)
- `LLM__Gemini__Model` (opcional, default `gemini-2.5-flash`)
- `Rabbit__HostName`
- `Rabbit__QueueName`
- `Mongo__ConnectionString`
- `Mongo__DatabaseName`
- `Mongo__LogsCollectionName`

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

- Confiabilidade de fila: `manual ack`, retry e DLQ.
- Observabilidade: OpenTelemetry + métricas por provedor/modelo.
- Testes automatizados para orquestrador e integração API/Worker.
- Política de roteamento por custo/latência/qualidade (não só keyword).
