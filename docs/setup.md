# Setup

## Prerequisites

- .NET SDK 8.0 (any 8.0.1xx or later feature band; `global.json` pins `8.0.100` with `rollForward: latestFeature`)
- Node.js 22.14.0 and npm 10.9.2 (see `.nvmrc`)
- Docker, for PostgreSQL 16 (optional; the tests use an in-memory database)

## Backend

```bash
dotnet tool restore                      # dotnet-ef 8.0.11
dotnet restore --use-lock-file           # writes/updates packages.lock.json; CI uses --locked-mode
dotnet build                             # warnings are errors
dotnet test --collect:"XPlat Code Coverage"
```

### Configuration (never commit real values)

```bash
cd backend/src/WestCoastFitness.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "<32+ random characters>"
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Database=westcoastfitness;Username=fitness;Password=<local password>"
dotnet user-secrets set "Admin:Email" "admin@example.com"        # optional first administrator
dotnet user-secrets set "Admin:Password" "<12+ characters>"
dotnet run                                # http://localhost:5080, Swagger at /swagger
```

The same keys can be supplied as environment variables using double underscores
(`Jwt__SigningKey`, `ConnectionStrings__Postgres`, ...).

## Database

`database/schema/001-initial-schema.sql` creates the schema and `002-seed-catalogue.sql` adds plans,
trainers and a rolling class schedule. `docker-compose.yml` mounts both into PostgreSQL 16-alpine:

```bash
cp .env.example .env        # then edit the values
docker compose up --build   # postgres on :5432, API on :5080
```

To re-check the schema against the EF model:
`dotnet ef dbcontext script --project backend/src/WestCoastFitness.Infrastructure --startup-project backend/src/WestCoastFitness.Api`.

## Frontend (Webpack)

```bash
cd frontend
npm ci
npm start               # webpack serve, http://localhost:5173, proxies /api to :5080
npm run build           # tsc --build && webpack --mode production -> dist/
npm run lint
npm run test:coverage   # Vitest + v8, Cobertura in coverage/
npm run duplication     # jscpd on src/
```

## Whole-repository checks

```bash
bash ci/build.sh && bash ci/test.sh && bash ci/scan.sh
npx jscpd .             # run from the repository root; reads .jscpd.json
```

`SCAN_MODE=strict` (default) makes `ci/scan.sh` fail on any finding; `SCAN_MODE=report` records
findings and exits 0.
