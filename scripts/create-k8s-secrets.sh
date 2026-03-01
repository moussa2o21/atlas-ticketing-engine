#!/usr/bin/env bash
# create-k8s-secrets.sh — Idempotent secret creation for atlas-desk namespace
# Usage: bash scripts/create-k8s-secrets.sh
#
# Required environment variables:
#   DESK_DB_CONNECTIONSTRING         — PostgreSQL connection string
#   DESK_EVENTBUS_CONNECTIONSTRING   — RabbitMQ connection string
#   DESK_REDIS_CONNECTIONSTRING      — Redis connection string
#   DESK_AIGATEWAY_APIKEY            — AI Gateway API key

set -euo pipefail

NAMESPACE="atlas-desk"

echo "=== Creating K8s secrets in namespace: ${NAMESPACE} ==="

# Validate required env vars
for var in DESK_DB_CONNECTIONSTRING DESK_EVENTBUS_CONNECTIONSTRING DESK_REDIS_CONNECTIONSTRING DESK_AIGATEWAY_APIKEY; do
  if [ -z "${!var:-}" ]; then
    echo "ERROR: ${var} is not set"
    exit 1
  fi
done

# atlas-desk-db-secret
kubectl create secret generic atlas-desk-db-secret \
  --namespace="${NAMESPACE}" \
  --from-literal=DB__CONNECTIONSTRING="${DESK_DB_CONNECTIONSTRING}" \
  --dry-run=client -o yaml | kubectl apply -f -
echo "  ✓ atlas-desk-db-secret"

# atlas-desk-eventbus-secret
kubectl create secret generic atlas-desk-eventbus-secret \
  --namespace="${NAMESPACE}" \
  --from-literal=EVENTBUS__CONNECTIONSTRING="${DESK_EVENTBUS_CONNECTIONSTRING}" \
  --dry-run=client -o yaml | kubectl apply -f -
echo "  ✓ atlas-desk-eventbus-secret"

# atlas-desk-redis-secret
kubectl create secret generic atlas-desk-redis-secret \
  --namespace="${NAMESPACE}" \
  --from-literal=REDIS__CONNECTIONSTRING="${DESK_REDIS_CONNECTIONSTRING}" \
  --dry-run=client -o yaml | kubectl apply -f -
echo "  ✓ atlas-desk-redis-secret"

# atlas-desk-ai-secret
kubectl create secret generic atlas-desk-ai-secret \
  --namespace="${NAMESPACE}" \
  --from-literal=AIGATEWAY__APIKEY="${DESK_AIGATEWAY_APIKEY}" \
  --dry-run=client -o yaml | kubectl apply -f -
echo "  ✓ atlas-desk-ai-secret"

echo "=== All secrets created/updated in ${NAMESPACE} ==="
