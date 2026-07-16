# P0 deployment checklist

Run this checklist from a controlled administrator machine before deploying the P0 release to production.

## 1. Rotate exposed credentials

The old repository configuration contained production-like credentials. Rotate the database password, JWT signing key, LG API key, Zalo app secret, MQTT password, payment-provider keys, Docker Hub credentials, and VPS SSH credentials before the deployment.

Create the VPS `.env` from `.env.example`. It must contain real values for every required variable. Keep it outside Git and restrict it to the deployment account.
`LOYALTY_MINI_APP_CLAIM_URL_TEMPLATE` has a safe Compose default, but set it explicitly when the production Zalo Mini App claim URL differs from `https://zalo.me/s/miniapp?claimToken={token}`.
`CORS_ORIGIN_0` to `CORS_ORIGIN_4` also have Compose defaults for the current production/admin/Zalo domains; set them explicitly only when the allowed frontend origins change.
`ALLOWED_HOSTS` defaults to the API domains plus `localhost` and `127.0.0.1` so the deploy health check can call `/health` locally.
`REVERSE_PROXY_IP` defaults to the common Docker bridge gateway `172.17.0.1`; set it explicitly when the reverse proxy source IP seen by the backend is different.
`LG_API_KEY` defaults to `dev-placeholder` only so development deployments can boot without LG integration. Production startup rejects this placeholder, so set the real rotated LG API key before production deploy.

The reverse proxy must terminate TLS and proxy to `127.0.0.1:<BACKEND_PORT>`; the backend and PostgreSQL ports are intentionally no longer public. Set `REVERSE_PROXY_IP` to the source IP the backend sees for that proxy (commonly the Docker bridge gateway when the proxy runs on the host). Do not use a public or untrusted client IP.

## 2. Check data before the migration

The migration `20260716105609_AddSecurityConstraints` adds unique indexes. Resolve every row returned by these queries before running it:

```sql
SELECT "Username", COUNT(*)
FROM "Accounts"
GROUP BY "Username"
HAVING COUNT(*) > 1;

SELECT "PaymentCode", COUNT(*)
FROM "MachineSessions"
WHERE "PaymentCode" IS NOT NULL
GROUP BY "PaymentCode"
HAVING COUNT(*) > 1;

SELECT "GatewayTransactionId", COUNT(*)
FROM "PaymentTransactions"
WHERE "GatewayTransactionId" IS NOT NULL
GROUP BY "GatewayTransactionId"
HAVING COUNT(*) > 1;
```

Take and verify a database backup before changing duplicate records.

## 3. Apply the migration explicitly

Production startup no longer runs migrations automatically. From a trusted machine with the production connection string and the .NET SDK installed:

```powershell
dotnet ef database update --project QLS.Backend.csproj --startup-project QLS.Backend.csproj
```

Confirm the new migration appears in `__EFMigrationsHistory`, then deploy the image.

## 4. Post-deploy verification

Verify `https://<api-host>/health` through the TLS reverse proxy, authenticate through each supported client, submit one controlled payment-provider webhook, and confirm a duplicate delivery does not create a second payment transaction.
