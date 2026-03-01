# Test Coverage Matrix

_Last updated: 2026-03-01_

## Frontend (login-app)

| Area | Test Type | Test File(s) | Coverage |
|---|---|---|---|
| Auth token utilities | Unit | login-app/src/lib/token.unit.test.ts | Token persistence helpers and token parsing behavior in isolation. |
| Login page flow | Integration | login-app/src/pages/LoginPage.integration.test.tsx | End-to-end page behavior for login UI: form interactions, submit behavior, and integration-level page outcomes. |
| Register page flow | Integration | login-app/src/pages/RegisterPage.integration.test.tsx | End-to-end page behavior for registration UI: form interactions, submit behavior, and integration-level page outcomes. |

---

## Services (Backend APIs)

| Service | Test Type | Test File(s) | Coverage |
|---|---|---|---|
| LoginAPI | Unit | tests/LoginAPI.UnitTests/AuthServiceTests.cs | `AuthService` behavior: duplicate email guard, registration success (email normalization + BCrypt hashing), login failure paths (missing user, invalid password), login success (roles returned), missing user by id, and list mapping. |
| LoginAPI | Integration | tests/LoginAPI.IntegrationTests/AuthEndpointsIntegrationTests.cs | `/api/auth/register`, `/api/auth/login`, `/api/auth/me/{userId}`, `/api/auth/users/internal`; internal API key authorization; high-priority negative paths: duplicate register (`400`), unknown-user login (`401`), missing user lookup (`404`), invalid payload validation (`400`) for register/login. |
| AuthAPI | Unit | tests/AuthAPI.UnitTests/JwtTokenServiceTests.cs | `JwtTokenService` claim extraction and token generation behavior, role deduplication, required-claim failure path, user-id extraction success/failure cases. |
| AuthAPI | Unit | tests/AuthAPI.UnitTests/DownstreamProxyServiceTests.cs | `DownstreamProxyService` wrapping and passthrough behavior: refresh envelope success path, invalid downstream login payload (`502`), missing downstream base URL (`500`), register + best-effort activity recording path and header forwarding constraints. |
| AuthAPI | Integration | tests/AuthAPI.IntegrationTests/AuthGatewayIntegrationTests.cs | Auth gateway endpoint behavior: unauthorized `/api/me` without token, refreshed envelope for valid `/api/me`, forbidden `/api/users` for non-admin, proxy register behavior, login token+payload issuance behavior. |
| AuthAPI | Integration | tests/AuthAPI.IntegrationTests/UnitTest1.cs | `/health` endpoint returns `200 OK`. |
| ActivityAPI | Unit | tests/ActivityAPI.UnitTests/ActivityControllerTests.cs; tests/ActivityAPI.UnitTests/InternalApiKeyAuthenticationHandlerTests.cs | `ActivityController` validation and persistence behavior (invalid user/event type, normalization + save, count bounds + recent ordering/cap), plus `InternalApiKeyAuthenticationHandler` authentication outcomes (missing config/header, invalid key, valid key success principal). |
| ActivityAPI | Integration | tests/ActivityAPI.IntegrationTests/ActivityEndpointsIntegrationTests.cs | `/api/activity` creation success + normalization checks, invalid create (`400`), `/api/activity/{count}` cap-to-200 behavior, invalid count (`400`), internal API key authorization checks (`401` without key). |


