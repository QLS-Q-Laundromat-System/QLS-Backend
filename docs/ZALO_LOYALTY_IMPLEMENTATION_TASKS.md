# Zalo Loyalty Implementation Tasks

## Scope

Implement loyalty points for QLS through Zalo Mini App with the flow:

1. Session paid by bank transfer.
2. SePay webhook confirms payment.
3. Backend triggers machine.
4. Backend creates claim token + QR link.
5. Customer opens Mini App and logs in with a Zalo access token verified by the backend.
6. Customer claims points by token.
7. Backend awards points and stores history.
8. If session later becomes `Error` or `Cancelled`, backend rolls back awarded points.

Business decisions confirmed:

- Loyalty is **per Brand**.
- Backend retrieves Zalo Profile using `access_token` and `appsecret_proof`.
- Rollback points when a paid session transitions to `Error` / `Cancelled`.
- `CustomerType` and `StudentVerificationStatus` default values are used until a dedicated verification flow exists.
- Use backend redirect claim link strategy (`/api/loyalty/claim-link/{token}`).

## Architecture Decisions

1. Add 3 new entities:
- `LoyaltyCustomer`
- `PointClaimToken`
- `LoyaltyPointTransaction`

2. Add brand boundary fields:
- `BrandId` on all loyalty tables.

3. Idempotency and constraints:
- `PointClaimToken.Token` unique.
- Unique earned points per machine session (via unique index on `(MachineSessionId, Type)`).

4. Claim token lifecycle:
- Created after successful SePay processing.
- Expires in 10 minutes (configurable).
- One-time use only.

5. Points rule:
- `points = floor(paidAmount / 10000)`.
- If points = 0 then claim is rejected.

6. Expiry rule:
- Earned points expire after 3 months (configurable).
- Background job creates `Expire` transactions daily.

7. Rollback rule:
- On session `Error` / `Cancelled`, rollback remaining earned points for that session with `Adjust` transaction.

## Deliverables

1. Database / Domain
- [x] Add enums: `CustomerType`, `StudentVerificationStatus`, `PointTransactionType`.
- [x] Add models: `LoyaltyCustomer`, `PointClaimToken`, `LoyaltyPointTransaction`.
- [x] Add `DbSet` + Fluent mappings + unique indexes.
- [x] Add EF migration for loyalty schema.

2. API Contracts
- [x] Add Zalo auth DTOs (`accessToken` login request/response and Graph profile).
- [x] Add loyalty DTOs (`claim`, `me`, `history`, `session loyalty`).

3. Service Layer
- [x] Add `IZaloAuthService` and `IZaloGraphApiClient`.
- [x] Add `ILoyaltyService`.
- [x] Implement Graph API verification, customer upsert and JWT issue.
- [x] Implement claim validation and award points transaction.
- [x] Implement rollback logic for failed/cancelled sessions.
- [x] Implement claim token creation from payment flow.
- [x] Implement daily points expiry processor.

4. API Layer
- [x] Add `POST /api/zalo/auth/login`.
- [x] Add `POST /api/loyalty/claim`.
- [x] Add `GET /api/loyalty/me`.
- [x] Add `GET /api/loyalty/points/history`.
- [x] Add `GET /api/loyalty/claim-link/{token}` redirect endpoint.
- [x] Extend `GET /api/v1/sessions/{id}` to include loyalty claim info.

5. Existing Flow Integration
- [x] Inject loyalty service into SePay webhook processing.
- [x] Create claim token right after payment success.
- [x] Trigger rollback when session status changes to `Error`/`Cancelled`.

6. Host / DI / Config
- [x] Register new services through existing Scrutor scan namespaces.
- [x] Register loyalty point expiry hosted service.
- [x] Add loyalty settings in `appsettings`.

7. Verification
- [x] Build compile success.
- [ ] Sanity check core scenarios:
  - login with backend-verified Zalo profile
  - create claim token
  - claim success
  - claim duplicate rejected
  - expired token rejected
  - rollback on session error/cancel
  - daily expiry handling

## API Notes

### Zalo Auth

See `ZALO_LOYALTY_FLOW_SPEC.md` for access token login and Graph API verification contracts.

Auth response:

```json
{
  "accessToken": "jwt_backend",
  "customerId": "uuid",
  "customerType": "Member",
  "studentVerificationStatus": "None",
  "totalPoints": 0
}
```

### Loyalty Claim

`POST /api/loyalty/claim` (Bearer loyalty token required)

Request:

```json
{
  "claimToken": "ABC123XYZ"
}
```

### Claim Link Redirect

`GET /api/loyalty/claim-link/{token}`

Backend redirects to configured Mini App deep link template.

## Config Proposal

Add section:

```json
"Loyalty": {
  "ClaimTokenTtlMinutes": 10,
  "PointUnitVnd": 10000,
  "PointExpiryMonths": 3,
  "MiniAppClaimUrlTemplate": "https://zalo.me/s/miniapp?claimToken={token}"
}
```

## Risks and Mitigation

1. Migration conflict on existing local DB:
- Keep loyalty migration independent from old `PaymentConfigs` conflict.
- Resolve previous migration mismatch separately before running full startup migrate.

2. Duplicate webhook retries:
- Existing transaction idempotency remains in SePay webhook.
- Loyalty earning idempotency enforced by DB unique index and token claimed state.

3. Concurrent claims:
- One-time token + unique earned transaction index prevent double award.
