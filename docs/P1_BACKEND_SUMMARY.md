# P1 Backend Summary

Date: 2026-07-16
Branch: `codex/p0-production-hardening`

## Scope Completed

P1 in this pass focused on production operability and maintainability without changing the product flow.

### 1. Audit trail for sensitive operations

Added persistent audit logging through the new `AuditLogs` table.

Audited backend actions:

- `auth.login`: success and invalid-credential attempts.
- `account.create`: admin-created account success/failure.
- `payment_config.create`
- `payment_config.update`
- `payment_config.activate`
- `payment_config.delete`

Audit records include actor id, username, role, action, entity type/id, IP address, user agent, success/failure state, failure reason, metadata, and timestamp.

Sensitive payment config metadata is bounded and masked:

- API key and webhook secret are logged only as presence flags.
- Bank account number is masked to the last four digits.

### 2. Rate limiting

Added ASP.NET Core rate limiting policies:

- `auth`: default 10 requests per client IP per minute.
- `webhook`: default 120 requests per client IP per minute.

Applied to:

- `POST /api/Auth/login`
- `POST /api/Auth/register`
- `POST /api/zalo/auth/login`
- `POST /api/webhooks/sepay`

Configuration keys:

```json
"RateLimiting": {
  "Auth": {
    "PermitLimit": 10,
    "WindowMinutes": 1
  },
  "Webhook": {
    "PermitLimit": 120,
    "WindowMinutes": 1
  }
}
```

Because `UseForwardedHeaders()` runs before rate limiting, production must keep `ReverseProxy:KnownProxies` accurate so client IP partitioning is correct behind Nginx/Caddy.

### 3. Safer operational logging

Updated request logging to avoid writing full user display names into ordinary request logs. Logs now focus on user id, role, method, path, status code, and latency.

### 4. Database migration

Added migration:

- `20260716112640_AddAuditLogs`

Apply after the P0 migration during deployment:

```bash
dotnet ef database update
```

The deployment process should still follow `docs/P0_DEPLOYMENT.md` first, including backup, duplicate-data checks, and secret rotation.

### 5. Test coverage

Added `AuditLogServiceTests` to verify that audit logs persist actor context and bound long header/error fields.

Verification run:

- `dotnet build QLS-Backend.sln --no-restore`
- `dotnet test QLS.Backend.Tests/QLS.Backend.Tests.csproj`

Current backend test result: 5 passing tests.

Known warning still present:

- `Services/Ziggbee/MqttListenerService.cs(179,80)` nullable dereference warning. This was pre-existing and should be cleaned in the next P1 maintenance pass.

## Cross-Repo P1 Notes

This branch also includes related P1 cleanup outside the backend:

- Web: removed unused legacy mock dashboard files and stale WattVision/Bolivia dashboard references; `npm run lint` and `npm run build` pass.
- Store app: fixed all Flutter analyzer warnings; `flutter analyze` reports no issues and `flutter test` passes.

## Remaining P1 Work

These are still recommended before a formal production launch:

- Add real refresh tokens with revocation/device session management for web and kiosk clients.
- Encrypt stored payment provider secrets at rest, with key rotation.
- Add integration tests for authorization scope, SePay retry/idempotency, and payment config audit events.
- Add production monitoring and alerting for failed webhooks, duplicate transactions, machine trigger failures, and 5xx spikes.
- Add backup/restore drills and a documented migration runbook for the VPS.
- Update local `dotnet ef` tooling to match runtime 10.0.x.
