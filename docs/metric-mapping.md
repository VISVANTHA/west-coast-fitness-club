# Metric mapping

## Which variant is this?

| | |
| --- | --- |
| Branch | `feature/wcf-negative-vite` |
| Polarity | **Negative** - the code contains intentionally detectable, documented issues. It still builds, tests, boots and runs its whole CI pipeline. |
| Frontend bundler | **Vite 6.4.3** (`tsc --build && vite build`) |
| Sibling branches | `feature/wcf-negative-webpack` (identical seeded issues, Webpack), `feature/wcf-positive-vite` and `feature/wcf-positive-webpack` (clean baselines) |

Everything not listed under "Seeded findings" in `clarifications.md` is identical to `feature/wcf-positive-vite`.

## What each metric family is evidenced by

| Metric family | Evidence in this branch |
| --- | --- |
| Build system / version fingerprint | Complete and identical to the positive branches: `global.json` (SDK `8.0.100`, `latestFeature`); a `packages.lock.json` per backend project; `frontend/package.json` `engines.node` `22.14.0` plus `frontend/package-lock.json`; `WestCoastFitness.sln`; Web vs library `Sdk` attributes; `frontend/vite.config.ts`. The seeded issues never touch these. |
| Cyclomatic / cognitive complexity | **Seeded.** `BookingRules.Evaluate` (booking conflicts) and `ProrationCalculator.Calculate` (proration) are unrefactored, deeply nested methods; `classBadge` in `frontend/src/validation/booking.ts` breaks ESLint's `complexity <= 10`. |
| Duplication | **Seeded.** `MemberActivityReport.cs` / `MemberActivityDigest.cs` and `activityReport.ts` / `activityDigest.ts` are copies. jscpd reports about 6% duplicated lines against its 5% threshold, so the run exits non-zero. |
| Lint / static analysis | Roslyn analyzers stay clean (warnings are still errors). ESLint reports one error, the `classBadge` complexity. |
| Dependency vulnerabilities | **Seeded.** `Newtonsoft.Json 12.0.3` (CVE-2024-21907, high) and `lodash 4.17.20` (CVE-2021-23337, high, plus others). Exact advisories are in `clarifications.md`. |
| Injection / XSS | Unchanged from the positive baseline: parameterised EF Core only; React default escaping only. |
| Object-level authorization (BOLA) | **Seeded gap** on `GET /api/members/{memberId}/summary` (read-only: plan name, price, join date). Every other member-scoped route still enforces `AccessPolicy`. |
| Security headers, rate limiting, CORS | Unchanged and still tested. |
| Secrets hygiene | **Seeded.** `DependencyInjection.AddInfrastructure` falls back to a hardcoded local development connection string with a placeholder password when `ConnectionStrings:Postgres` is unset. It is not a real credential. |
| Test coverage | **Seeded low module.** `Application/Reporting` and `frontend/src/reports` have no tests (0%). Everything else is tested as in the positive branches. Overall: backend about 68% and frontend about 68% line coverage. |
| CI | The same `ci/*.sh` scripts run with `SCAN_MODE=report` and `COVERAGE_ENFORCE=false`: every scanner runs to completion, findings are published in `scan-results/`, and no stage fails. |
