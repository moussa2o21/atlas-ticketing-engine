# AppLogica Desk — ATLAS Ticketing Engine
# CLAUDE.md — Claude Code Operational Context

Product:   AppLogica Desk (ATLAS-ITSM)
Sprint:    Sprint 15 — Phase 1 Foundation & Incident Core
Wave:      Wave 3 (alongside Pulse CRM and Nexus ERP)
Stack:     .NET 8, React 18, PostgreSQL 15, Redis, Hangfire, SignalR, RabbitMQ + MassTransit
Folder:    ATLAS\Dev\Atlas Ticketing Engine
Workspace: ATLAS.code-workspace

## Solution Structure

  Atlas Ticketing Engine/
  ├── CLAUDE.md                              ← this file
  ├── .claude/settings.json
  ├── src/
  │   ├── AppLogica.Desk.Domain/             ← Entities, enums, domain events, repository interfaces
  │   ├── AppLogica.Desk.Application/        ← Commands, queries, MediatR handlers, DTOs, validators
  │   ├── AppLogica.Desk.Infrastructure/     ← EF Core, repositories, Hangfire jobs, SignalR hubs, clients
  │   └── AppLogica.Desk.API/               ← ASP.NET Core Web API, controllers, middleware, Program.cs
  ├── tests/
  │   ├── AppLogica.Desk.Tests.Unit/
  │   └── AppLogica.Desk.Tests.Integration/
  ├── k8s/
  │   ├── namespace.yaml
  │   ├── deployment.yaml                    ← desk-api (min 2 pods)
  │   ├── deployment-worker.yaml             ← desk-worker Hangfire (no ingress)
  │   ├── deployment-signalr.yaml            ← desk-signalr (sticky sessions)
  │   ├── service.yaml
  │   ├── ingress.yaml
  │   ├── hpa.yaml
  │   ├── networkpolicy.yaml
  │   └── schema-migration-job.yaml
  ├── scripts/
  │   └── create-k8s-secrets.sh
  ├── docker-compose.yml
  └── .github/workflows/deploy.yml

## Services (3 deployments)

  desk-api        → Main REST API. Horizontally scalable. Exposed via ATLAS API Gateway.
  desk-worker     → Hangfire background jobs: SLA timer engine, escalation processor, KB indexer.
                    ClusterIP only — NO external ingress.
  desk-signalr    → Real-time SignalR hub for agent inbox. Requires sticky session routing.

## Database — desk schema (dedicated PostgreSQL instance)

  Schema:   desk (schema-per-tenant pattern — tenant schema resolved from JWT at runtime)
  Hangfire: hangfire schema (separate)

  Core tables:
    desk.incidents                 — ITIL incident lifecycle
    desk.incident_comments         — timeline entries and internal notes
    desk.sla_policies              — SLA targets per priority (response + resolution minutes)
    desk.sla_timers                — active SLA state per incident/request
    desk.business_hours_calendars  — per-tenant working hours + holiday calendars
    desk.queues                    — routing queues for incidents and requests
    desk.agents                    — agent profiles synced from ATLAS Identity
    desk.teams                     — team groupings
    desk.audit_log                 — APPEND-ONLY. No UPDATE. No DELETE. Ever.

  CRITICAL: audit_log is APPEND-ONLY. No UPDATE. No DELETE anywhere in codebase.
  CRITICAL: Every table must include tenant_id UUID NOT NULL — filter on every query.
  CRITICAL: Soft delete pattern — is_deleted BOOL, deleted_at TIMESTAMPTZ, deleted_by UUID.

## Multi-Tenancy Rules

  TenantId is ALWAYS resolved from the JWT claim (X-Tenant-Id or tenant_id claim).
  TenantId is NEVER accepted from the request body.
  Every repository method filters by TenantId automatically via ITenantContext.
  Schema-per-tenant: ITenantContext.SchemaName used in EF Core connection string at runtime.

## SLA Engine Rules

  SLA calculation NEVER runs inline in API handlers — always via SLA Engine service.
  SLA timers evaluated by Hangfire job on 60-second cycle.
  Redis used for real-time SLA warning pub/sub and breach alerts only.
  PostgreSQL stores all SLA timer state for durability.
  Business hours are per-tenant (MENA support: Fri-Sat weekend for GCC, Fri-only for Egypt).
  SLA pause/resume events published to ATLAS Event Bus.

## Domain Events Published (ATLAS Event Bus)

  desk.incident.created
  desk.incident.assigned
  desk.incident.escalated
  desk.incident.resolved
  desk.incident.closed
  desk.sla.warning          ← breach imminent (at 80% elapsed by default)
  desk.sla.breached
  desk.request.submitted
  desk.request.approved
  desk.request.fulfilled
  desk.change.approved
  desk.change.implemented

  Exchange: atlas.events (topic)
  Queue:    atlas.desk.events
  DLQ:      atlas.desk.dlq
  Routing:  desk.#

  Password source:
    kubectl get secret atlas-rabbitmq-secret -n atlas-platform \
      -o jsonpath='{.data.rabbitmq-password}' | base64 -d

## ATLAS Platform Integration

  Identity Service   → JWT validation, RBAC roles, agent profile sync
                       Internal JWKS: http://keycloak.atlas-identity.svc.cluster.local:8080/realms/applogica/protocol/openid-connect/certs
                       AUTH__AUTHORITY: http://keycloak.atlas-identity.svc.cluster.local:8080/realms/applogica

  API Gateway        → All external traffic routes through gateway. desk-api registers routes.
                       Route prefix: /api/desk/

  Event Bus          → RabbitMQ on atlas-platform namespace. MassTransit consumer.
                       EVENTBUS__CONNECTIONSTRING from atlas-rabbitmq-secret. Never hardcode.

  AI Gateway         → ATLAS AI Gateway for copilot, classification, RAG.
                       Endpoint: http://atlas-ai-gateway.atlas-platform.svc.cluster.local/v1/
                       Never call Azure OpenAI or Anthropic directly.

## Authorization Model

  atlas-super-admin   → all operations, all tenants
  atlas-system-admin  → manage SLA policies, queues, catalog for own tenant
  atlas-agent         → create/update/resolve incidents and requests
  atlas-viewer        → read-only access to incidents and reports
  Service tokens      → internal service-to-service calls only

## API Endpoints (Phase 1 scope)

  POST   /api/desk/incidents                  → create incident (AI classification triggered async)
  GET    /api/desk/incidents                  → list with filters (status, priority, queue, SLA risk)
  GET    /api/desk/incidents/{id}             → incident detail
  PATCH  /api/desk/incidents/{id}             → update mutable fields
  POST   /api/desk/incidents/{id}/assign      → assign to agent or queue
  POST   /api/desk/incidents/{id}/escalate    → escalate (tier 2 / tier 3)
  POST   /api/desk/incidents/{id}/resolve     → resolve with notes
  POST   /api/desk/incidents/{id}/close       → close
  GET    /api/desk/incidents/{id}/timeline    → full activity timeline
  POST   /api/desk/incidents/bulk-assign      → bulk assign (max 100)
  GET    /health/live                         → liveness
  GET    /health/ready                        → readiness (postgres + rabbitmq)
  /hubs/desk                                  → SignalR hub for real-time agent inbox

## K8s Secrets (all in atlas-desk namespace)

  atlas-desk-db-secret          → DB__CONNECTIONSTRING
  atlas-desk-eventbus-secret    → EVENTBUS__CONNECTIONSTRING
  atlas-desk-redis-secret       → REDIS__CONNECTIONSTRING
  atlas-desk-ai-secret          → AIGATEWAY__APIKEY

## Security Rules

  1. No connection strings or API keys in any file — K8s secrets only
  2. No credentials in git history — check before every push
  3. audit_log is APPEND-ONLY — no UPDATE or DELETE anywhere in codebase
  4. TenantId NEVER from request body — always from JWT
  5. SLA Engine never called inline in API handlers — always async via service
  6. AI Gateway is the ONLY entry point for LLM calls — no direct Azure OpenAI or Anthropic
  7. EVENTBUS__CONNECTIONSTRING from atlas-rabbitmq-secret — never hardcoded

## Auto-Commit Rule

  After every completed phase:
    git add .
    git commit -m "feat(desk): Phase N — [description]"
    git push
  Never ask permission to commit.

## Exit Criteria (Sprint 15 Complete When ALL Pass)

  1. dotnet build — 0 errors, 0 warnings across all 4 projects
  2. dotnet test — all unit + integration tests pass
  3. POST /api/desk/incidents → incident created with ticket number INC-YYYY-NNNNN
  4. SLA timer created automatically for new incident with correct priority targets
  5. GET /api/desk/incidents/{id}/timeline → returns creation and assignment entries
  6. desk.incident.created event appears in RabbitMQ atlas.desk.events queue
  7. /health/ready → postgres: Healthy, rabbitmq: Healthy or Degraded
  8. TenantId isolation verified — agent cannot see incidents from another tenant

## Folder Name

  This repo lives in: ATLAS\Dev\Atlas Ticketing Engine
  It is part of ATLAS.code-workspace alongside:
    Atlas API Gateway
    atlas audit service
    Atlas Event Bus
    Atlas Identity Platform
    Atlas Notify Hub
