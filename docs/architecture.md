# Architecture

West Coast Fitness Club manages members, plans, classes, trainers, bookings and payments for an
Arizona-based club. This branch is **`feature/wcf-negative-vite`**: intentionally seeded, documented findings; Vite frontend. The architecture is
identical to `feature/wcf-positive-vite`; only the seeded items in `clarifications.md` differ.

```text
React 19 + TypeScript 6 + Vite 6            (frontend/)
        |  JSON over /api  (Vite dev proxy -> :5080)
ASP.NET Core 8 API   controllers, JWT bearer, rate limiting, security headers, CORS
        |
Application          services and rules (booking, proration, access policy), no framework types
        |
Domain               entities and enums, no dependencies
        ^
Infrastructure       EF Core 8 + Npgsql, repositories, password hashing, MockPaymentGateway
        |
PostgreSQL 16        database/schema/001-initial-schema.sql
```

Dependencies point inward: `Api -> Application, Infrastructure`; `Infrastructure -> Application`;
`Application -> Domain`. Infrastructure implements the interfaces declared in
`WestCoastFitness.Application/Abstractions.cs`.

## Projects

| Path | Role |
| --- | --- |
| `backend/src/WestCoastFitness.Domain` | `Member`, `MembershipPlan`, `FitnessClass`, `Trainer`, `ClassBooking`, `Payment`, `UserAccount`, enums |
| `backend/src/WestCoastFitness.Application` | `Reporting/` (`MemberActivityReport`, `MemberActivityDigest` - seeded duplicate, untested), `AccountService`, `CatalogService`, `BookingService`, `MembershipService`, `PaymentService`, `BookingRules`, `ProrationCalculator`, `AccessPolicy` |
| `backend/src/WestCoastFitness.Infrastructure` | `FitnessDbContext`, repositories, `IdentityPasswordHasher`, `MockPaymentGateway`, `AdminBootstrapper` |
| `backend/src/WestCoastFitness.Api` | controllers, `Program.cs`, `JwtTokenIssuer`, security-header and exception middleware, rate limiting |
| `backend/tests/*.Tests` | one xUnit project per layer; the API tests host the real pipeline on an in-memory database |
| `frontend/src` | pages, API client, session handling, validation and formatting |

## Authorization (object level)

`AccessPolicy.EnsureCanAccessMember(caller, memberId)` runs in every service method that takes a
member id or loads a record owned by a member: profile, plan change, bookings (list, book,
cancel), activity and payments (list, charge). **The one exception on this branch is the membership summary
route (seeded BOLA gap, see `clarifications.md`).** A `Member` caller may only reach the member profile named in
their own token; an `Administrator` may reach any. Anything else is a `403`. The API tests assert this
for every route except the seeded gap (see `MemberFlowTests`), which is the BOLA evidence for this branch.

## Payments

`MockPaymentGateway` runs in-process. It declines zero and negative amounts, blank tokens, raw card
numbers and the `tok_decline` test token; it keeps no state. The `payments` table has no column that
could hold card data: only the gateway reference and status are stored.

## Time

Instants are stored as UTC (`timestamptz`). Arizona does not observe daylight saving time (MST, UTC-7
all year), so the frontend renders every time in `America/Phoenix`.

## Request pipeline

`SecurityHeadersMiddleware` -> `ClubExceptionMiddleware` -> Swagger (Development only) -> rate limiter
-> CORS (`Cors:AllowedOrigins`) -> JWT authentication -> authorization -> controllers.
