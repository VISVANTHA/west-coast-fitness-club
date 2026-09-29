# Clarifications

Branch: `feature/wcf-negative-vite`. The application is the `feature/wcf-positive-vite` application plus the
seeded findings below. "Negative" means detectable issues, not a broken build: the solution compiles with
`TreatWarningsAsErrors`, all 163 xUnit tests and 72 Vitest tests pass, the API boots and the CI pipeline
completes. Scanner exit codes are non-zero by design and are reported as findings.

## Seeded findings

| # | Finding | Where | Detail |
| --- | --- | --- | --- |
| 1 | High cyclomatic / cognitive complexity | `backend/src/WestCoastFitness.Application/Scheduling/BookingRules.cs` (`Evaluate`); `.../Membership/ProrationCalculator.cs` (`Calculate`); `frontend/src/validation/booking.ts` (`classBadge`) | Booking-conflict and proration logic left as single nested methods with hand-written week and month tables. Behaviour is identical to the clean versions, so the existing tests pass. ESLint flags `classBadge` (complexity 15 > 10). |
| 2 | Real duplicated block across two files | `.../Application/Reporting/MemberActivityReport.cs` and `MemberActivityDigest.cs`; `frontend/src/reports/activityReport.ts` and `activityDigest.ts` | Each pair is a copy with only the type / export names changed. jscpd reports roughly 6% duplicated lines against the repository's 5% threshold (`.jscpd.json`), so it exits non-zero. |
| 3 | Vulnerable NuGet package | `Newtonsoft.Json 12.0.3` in `WestCoastFitness.Infrastructure` (used by `AuditLog.Format`) | **CVE-2024-21907** / GHSA-5crp-9r3c-p9vr: uncontrolled recursion (denial of service) on deeply nested JSON, fixed in 13.0.1. NuGet audit reports it as NU1903 (high). `Directory.Build.props` excludes NU1901-NU1904 from `TreatWarningsAsErrors` so the build still succeeds. |
| 4 | Vulnerable npm package | `lodash 4.17.20` (with `@types/lodash 4.17.20`), used by `frontend/src/reports/*.ts` | **CVE-2021-23337** / GHSA-35jh-r3h4-6jhm: command injection via `_.template` (high, fixed in 4.17.21). `npm audit` also lists ReDoS (GHSA-29mw-wpgm-hmr9), code injection via `_.template` keys (GHSA-r5fr-rjxr-66jc) and prototype pollution (GHSA-f23m-r3pf-42rh, GHSA-xxjr-mmjv-4gpg). The app never calls `_.template`, and only `lodash/groupBy` is imported. |
| 5 | Missing object-level authorization (BOLA gap) | `MembershipService.GetSummaryAsync`, served by `GET /api/members/{memberId}/summary` | Read-only and low risk (plan name, price, join date), but any authenticated member can read any member's summary. Commented `INTENTIONAL ISSUE (BOLA gap)` in code; asserted by `Summary_IsReadableByAnyAuthenticatedMember_KnownBolaGap`. Anonymous callers still get 401. |
| 6 | Hardcoded config value instead of an environment variable | `backend/src/WestCoastFitness.Infrastructure/DependencyInjection.cs` | Falls back to `Host=localhost;...;Password=changeme-local-only` when `ConnectionStrings:Postgres` is not set. A placeholder for a throwaway local database, never a real credential. |
| 7 | Low test coverage on one module | `Application/Reporting` (C#) and `frontend/src/reports` | No tests, so 0% line coverage for the module; every other module is covered as in the positive branch. Overall about 68% backend and 68% frontend. |

## Things that are unchanged on purpose

- The repository-detection fingerprint (`global.json`, `packages.lock.json` files, `engines.node`,
  `package-lock.json`, `.sln`, project SDK types, `vite.config.ts`) is complete. The seeded issues are code
  and dependency findings, never a missing or ambiguous build-system signal.
- Security headers, rate limiting, CORS, parameterised queries and secrets handling are as in the positive branch.

## Carried over from the positive branch

- **jsdom 26.1.0** (jsdom 30 needs Node >= 22.22.2). **Vitest 3.2.7** is pinned by the brief and carries three
  moderate dev-only advisories (GHSA-82fw-gwwq-j7x9); the fix needs Vitest 5.
- .NET SDK is pinned with `8.0.100` / `latestFeature`; development used 8.0.423.
- The schema is SQL (`database/schema/`), cross-checked with `dotnet ef dbcontext script`.
- Payments carry an opaque token, never a card number.

## What was not verified from the authoring environment

- The Testable platform coverage-preview (Ready / Partial / Blocked) cannot be run from here; the fingerprint
  that prevents `Blocked` is complete.
- GitLab CI and Bitbucket Pipelines files were not executed.
