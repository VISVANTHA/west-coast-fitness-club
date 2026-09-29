# West Coast Fitness Club

West Coast Fitness Club - Arizona-based fitness club management platform.

This repository holds four variants of the same application, one per branch. They exist to
exercise code-quality and dependency scanning on the Testable platform: two code-quality
polarities crossed with two frontend build toolchains.

| Branch | Code quality | Frontend bundler |
| --- | --- | --- |
| `feature/wcf-positive-vite` | Clean baseline | Vite 6.4.3 |
| `feature/wcf-positive-webpack` | Clean baseline | Webpack 5 |
| `feature/wcf-negative-vite` | Intentionally seeded, documented findings | Vite 6.4.3 |
| `feature/wcf-negative-webpack` | Intentionally seeded, documented findings | Webpack 5 |

"Negative" means the code contains deliberate, documented issues for scanners to detect. Every
branch still builds, tests, boots and runs its full CI pipeline. See `docs/metric-mapping.md`
and `docs/clarifications.md` on each branch.

`main` only carries this README; check out one of the branches above for the application.
