# Runbook: Magic Link Failures

## Symptoms

- Síndico reports not receiving the setup email after condomínio creation.
- `POST /auth/password-setup` returns 400 when a token is submitted.
- Operator sees an error when attempting to resend the magic link.

## Diagnostic Steps

### 1. Check email outbox for failed deliveries

```sql
SELECT id, recipient_email, subject, error_message, attempt_count, last_attempt_at, created_at
FROM email_outbox
WHERE sent_at IS NULL
ORDER BY created_at DESC
LIMIT 20;
```

If rows are present with `attempt_count > 0` and `sent_at IS NULL`, the SMTP relay is failing.

### 2. Check SMTP connectivity

```bash
# From within the container / pod:
openssl s_client -connect <smtp-host>:587 -starttls smtp
```

Verify that the TLS handshake succeeds and credentials are accepted.

### 3. Check for expired or consumed tokens

```sql
SELECT id, user_id, purpose, created_at, expires_at, consumed_at, invalidated_at
FROM magic_link
WHERE user_id = '<sindico-user-id>'
ORDER BY created_at DESC
LIMIT 5;
```

- `consumed_at IS NOT NULL` → token was already used; resend from backoffice.
- `expires_at < now()` → token expired (default TTL: 72h); resend from backoffice.
- `invalidated_at IS NOT NULL` → token was superseded by a resend; síndico should use the latest email.

### 4. Check structured logs

```bash
# Filter for magic link events for the user:
kubectl logs <pod> | jq 'select(.UserId == "<guid>" and .EventType == "magic-link.*")'
```

Look for `magic-link.issued` and `magic-link.consumed` events. The token hash is logged, never the raw token.

### 5. Verify rate-limit not exhausted for resend

The service allows up to 5 resends per user per 24h. If exceeded, `POST /resend-magic-link` returns 429. Check:

```sql
SELECT COUNT(*) FROM magic_link
WHERE user_id = '<sindico-user-id>'
  AND purpose = 'PasswordSetup'
  AND created_at > now() - interval '24 hours';
```

## Resolution Actions

| Situation | Action |
|-----------|--------|
| SMTP is down | Fix SMTP relay credentials/connectivity; the `EmailOutboxRetryWorker` will retry pending rows automatically on recovery. |
| Token expired | Operator uses backoffice `Reenviar link` action for the síndico. |
| Token consumed | Verify the síndico can log in. If not, reset password via Identity admin API (future feature). |
| Rate limit hit | Wait for the 24h window to reset, or manually clear rate-limit count via DB if urgent. |
| Email in spam | Ask síndico to check spam folder; configure DKIM/SPF/DMARC on the sender domain. |

## Escalation

If the outbox has growing backlog (`attempt_count >= 3`) after SMTP is restored, manually trigger a resend via the backoffice or call `POST /admin/condominios/{id}/sindicos/{userId}:resend-magic-link` with an operator token.
