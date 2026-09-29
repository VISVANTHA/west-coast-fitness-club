# Clarifications

Decisions and deviations a reviewer might otherwise have to guess at. Branch: `feature/wcf-positive-vite`.

## Pinned versions that needed an explanation

- **jsdom 26.1.0, not the latest 30.x.** jsdom 30 requires Node `^22.22.2 || ^24.15.0`, which conflicts with
  the pinned Node 22.14.0. jsdom 26 supports it.
- **`npm audit` shows 3 moderate advisories** (GHSA-82fw-gwwq-j7x9, `@vitest/mocker` redirect-mock path
  traversal, reached through `vitest`/`@vitest/coverage-v8`). Vitest 3.2.7 is the version the brief pins; the
  fix needs Vitest 5 (a breaking change). It is a dev-only test runner and is never shipped. No high or
  critical advisories exist, which is the bar for the scan stage.
- **.NET SDK.** `global.json` pins `8.0.100` with `rollForward: latestFeature`; CI resolves the newest 8.0.x
  SDK (developed on 8.0.423). The brief mentions an 8.0.420 toolchain; any 8.0.1xx+ satisfies the pin.
- **`Microsoft.EntityFrameworkCore.Design` is also referenced by the Api project** (`PrivateAssets=all`)
  because `dotnet ef` requires its startup project to reference it. It does not flow into the published app.

## Design decisions

- **Schema by SQL, not EF migrations.** `database/schema/001-initial-schema.sql` is the source of truth for the
  database and was cross-checked against `dotnet ef dbcontext script`. The API never creates or alters tables.
- **Enums travel as strings** in JSON and are stored as text with `CHECK` constraints.
- **Payments carry a token, never a card.** `MockPaymentGateway` refuses anything that looks like a card
  number. The UI sends the constant demo token `tok_demo_visa`; `tok_decline` forces a decline.
- **Object-level authorization lives in the Application layer** (`AccessPolicy`) so it cannot be forgotten by
  a new controller. Members reach only their own records; administrators reach all.
- **Weeks run Monday to Sunday in UTC** for the weekly class allowance. Arizona has no DST, so the local week
  boundary differs from UTC by a fixed seven hours.
- **Capacity races.** Capacity is checked before insert, so two simultaneous bookings for the last seat could
  both succeed. The unique index prevents duplicate bookings per member and class; a hard capacity guarantee
  would need a serializable transaction or row lock, which is out of scope.

## The one intentional finding

`ClassAnnouncementText.DescribeWindow` and `StaffClassAnnouncementText.DescribeWindow` are deliberately
identical (see the `NEAR-DUPLICATE` comments). jscpd reports one 21-line clone (0.38%). It is the "real but
minor" finding the brief asks for and stays well under the 5% threshold.

## What was not verified from the authoring environment

- The **Testable platform coverage-preview** (Ready / Partial / Blocked) cannot be run from here. The
  repository-detection fingerprint (see `metric-mapping.md`) is complete, which is what causes `Blocked`.
- GitLab CI and Bitbucket Pipelines files are syntactically straightforward equivalents but were not executed;
  GitHub Actions is the pipeline that runs for this repository.
