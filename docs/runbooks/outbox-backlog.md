# Runbook: Outbox Backlog

## Symptoms

- Domain events are not being processed (no `published_at` on old rows in `domain_event_outbox`).
- Downstream consumers are not receiving events.
- `domain_event_outbox` table has rows older than the configured poll interval (`DomainEvents:Publisher:PollInterval`, default 15s) with `published_at IS NULL`.

## Diagnostic Steps

### 1. Check for unprocessed outbox rows

```sql
SELECT COUNT(*), MIN(created_at), MAX(created_at)
FROM domain_event_outbox
WHERE published_at IS NULL;
```

If count is growing and the oldest row is more than a few minutes old, the publisher is likely stalled.

### 2. Check publisher configuration

The publisher worker is controlled by `DomainEvents:Publisher:Enabled` (env var `DomainEvents__Publisher__Enabled`). If `false`, no processing occurs.

```bash
kubectl exec <pod> -- printenv DomainEvents__Publisher__Enabled
```

### 3. Check application logs for publisher errors

```bash
kubectl logs <pod> --since=1h | jq 'select(.SourceContext == "PortaBox.Infrastructure.Events.DomainEventOutboxPublisher")'
```

Look for exceptions, connection errors, or crash loops.

### 4. Check for long-running DB transactions blocking the outbox update

```sql
SELECT pid, now() - pg_stat_activity.query_start AS duration, query, state
FROM pg_stat_activity
WHERE state <> 'idle'
  AND query_start < now() - interval '1 minute'
ORDER BY duration DESC;
```

A long-running transaction can prevent the `published_at` UPDATE from committing.

### 5. Inspect specific failing events

```sql
SELECT id, event_type, aggregate_id, created_at, published_at, error_message
FROM domain_event_outbox
WHERE published_at IS NULL
ORDER BY created_at ASC
LIMIT 10;
```

If `error_message` is populated, it gives the exception that prevented processing.

## Resolution Actions

| Situation | Action |
|-----------|--------|
| Publisher disabled | Set `DomainEvents__Publisher__Enabled=true` and redeploy / restart. |
| Publisher pod crashed | Check pod logs, fix the underlying issue, restart the pod. |
| DB connection issue | Restore connectivity; the publisher will resume on next poll. |
| Specific event failing repeatedly | Inspect `error_message`; if unrecoverable, mark as published manually: `UPDATE domain_event_outbox SET published_at = now(), error_message = 'manually-skipped' WHERE id = '<id>';` |
| Backlog is large | The worker processes in batches of `DomainEvents:Publisher:BatchSize` (default 100); backlog will clear automatically once the issue is resolved. |

## Alerting Recommendation

Alert when:
- Any row in `domain_event_outbox` has `published_at IS NULL AND created_at < now() - interval '5 minutes'`
- Row count of unpublished events exceeds 500

Configure alert on the PostgreSQL exporter metrics or via a periodic SQL check job.

## Escalation

If the backlog exceeds 10 000 rows and the publisher cannot clear it, consider increasing `BatchSize` temporarily and/or scaling the publisher replica (ensure idempotent consumers).
