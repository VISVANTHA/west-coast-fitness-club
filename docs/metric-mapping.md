# Metric mapping

## Which variant is this?

| | |
| --- | --- |
| Branch | `feature/wcf-positive-vite` |
| Polarity | **Positive** - clean baseline. Scanners should report no high or critical findings. |
| Frontend bundler | **Vite 6.4.3** (`tsc --build && vite build`) |
| Sibling branches | `feature/wcf-positive-webpack` (same code, Webpack), `feature/wcf-negative-vite` and `feature/wcf-negative-webpack` (seeded findings) |

## What each metric family is evidenced by

| Metric family | Evidence in this branch |
| --- | --- |
| Build system / version fingerprint | `global.json` (SDK `8.0.100`, `rollForward: latestFeature`); `packages.lock.json` in every backend project; `frontend/package.json` `engines.node` = `22.14.0` and `frontend/package-lock.json`; `WestCoastFitness.sln`; `Sdk="Microsoft.NET.Sdk.Web"` (Api) vs `Microsoft.NET.Sdk` (libraries); `frontend/vite.config.ts` |
| Cyclomatic / cognitive complexity | Low to moderate. The busiest method is `BookingRules.Evaluate` (five early returns, each rule in its own helper). ESLint enforces `complexity <= 10`, `max-depth <= 3`, `max-params <= 4` and passes. |
| Duplication | Exactly **one intentional, documented near-duplicate**: the `DescribeWindow` helper in `ClassAnnouncementText.cs` and `StaffClassAnnouncementText.cs` (21 lines, 0.38% of the scanned code). jscpd reports it as a real but minor clone; the configured threshold is 5%. |
| Lint / static analysis | Roslyn `latest-Recommended` analyzers with `TreatWarningsAsErrors=true`; ESLint 10 flat config with type-checked typescript-eslint rules. Both clean. |
| Dependency vulnerabilities | `dotnet list package --vulnerable --include-transitive` reports nothing. `npm audit` reports 3 *moderate* advisories, all in the pinned dev-only `vitest`/`@vitest/mocker` chain (see `clarifications.md`); none high or critical. |
| Injection | Every EF Core query is LINQ (parameterised). There is no raw SQL and no string-built query anywhere in `backend/src`. |
| XSS | React default escaping only. No `dangerouslySetInnerHTML`, `innerHTML` or `eval`; ESLint forbids `eval`, `new Function` and implied eval. |
| Object-level authorization (BOLA) | `AccessPolicy` on every member-scoped operation; API tests prove a second member gets `403` on profile, summary, bookings, payments, booking, charging and plan change. |
| Security headers, rate limiting, CORS | `SecurityHeadersMiddleware` (nosniff, frame deny, CSP, referrer policy, HSTS over HTTPS); built-in rate limiter (120/min per client, 10/min on `/api/auth/*`); CORS restricted to `Cors:AllowedOrigins`. All covered by API tests. |
| Secrets hygiene | `Jwt:SigningKey`, `ConnectionStrings:Postgres`, `Admin:*` come only from user-secrets or environment variables; the API refuses to start without a 32+ character signing key. `.env` is git-ignored and `docker-compose.yml` fails fast when a secret is unset. |
| Test coverage | Backend 97% and frontend 99% line coverage (target >= 80% on both sides, enforced in CI by `ci/coverage-summary.mjs`). 163 xUnit tests and 61 Vitest tests. |
| CI | GitHub Actions, GitLab CI and Bitbucket Pipelines all run `ci/build.sh`, `ci/test.sh` and `ci/scan.sh`; scan runs in `strict` mode here. |
