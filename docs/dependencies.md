# Dependencies

All versions are pinned exactly and locked (`packages.lock.json`, `frontend/package-lock.json`).

## Toolchain

| Tool | Version |
| --- | --- |
| .NET SDK | 8.0.x (`global.json`: `8.0.100`, `rollForward: latestFeature`); developed on 8.0.423 |
| C# | 12 (`LangVersion 12.0`), Nullable and ImplicitUsings enabled |
| Node.js / npm | 22.14.0 / 10.9.2 (`engines`, `packageManager`, `.nvmrc`) |
| PostgreSQL | 16-alpine (docker-compose) |
| Container images | build `mcr.microsoft.com/dotnet/sdk:8.0`, runtime `mcr.microsoft.com/dotnet/aspnet:8.0` |

## Backend (NuGet)

| Package | Version | Project |
| --- | --- | --- |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.31 | Api |
| Swashbuckle.AspNetCore | 6.9.0 | Api |
| Microsoft.EntityFrameworkCore | 8.0.11 | Infrastructure |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.11 | Infrastructure |
| Microsoft.EntityFrameworkCore.Design | 8.0.11 (design-time only) | Infrastructure, Api |
| Microsoft.Extensions.Identity.Core | 8.0.31 (password hashing only) | Infrastructure |
| dotnet-ef (local tool, `.config/dotnet-tools.json`) | 8.0.11 | tooling |
| xunit / xunit.runner.visualstudio | 2.9.2 / 2.8.2 | tests |
| Microsoft.NET.Test.Sdk | 17.11.1 | tests |
| coverlet.collector | 6.0.2 | tests |
| Microsoft.AspNetCore.Mvc.Testing | 8.0.11 | Api.Tests |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.11 | Infrastructure.Tests, Api.Tests |
| Microsoft.Extensions.Configuration | 8.0.0 | Infrastructure.Tests |

## Frontend (npm)

| Package | Version | Note |
| --- | --- | --- |
| react, react-dom | 19.3.0 | |
| react-router-dom | 7.18.4 | |
| typescript | 6.0.3 | |
| webpack | 5.111.1 | this branch's bundler |
| webpack-cli | 7.2.3 | |
| webpack-dev-server | 5.2.3 | 6.x needs Node >= 22.15 |
| html-webpack-plugin, mini-css-extract-plugin | 5.6.8, 2.10.2 | |
| ts-loader, css-loader, style-loader | 9.6.2, 7.1.5, 4.0.0 | |
| ts-node | 10.9.2 | loads `webpack.config.ts` |
| vite (transitive, npm override) | 6.4.3 | required by Vitest only; no Vite config or plugin is used |
| eslint | 10.11.0 | flat config |
| @eslint/js, typescript-eslint | 10.0.1, 8.71.0 | |
| eslint-plugin-react-hooks, eslint-plugin-react-refresh | 7.1.1, 0.5.7 | |
| vitest, @vitest/coverage-v8 | 3.2.7 | |
| jsdom | 26.1.0 | see `clarifications.md` |
| @testing-library/react, jest-dom, user-event | 16.3.3, 6.9.1, 14.6.7 | |
| jscpd | 4.0.5 | |
