# **Project:** *pokemon-locations*

## Group Members
* Jade Harlev
* Maha Bhatti
* Maksim Popov

## Description
*pokemon-locations* is a project for SE498, the Software Engineering Capstone course at Chapman University.

The project will focus on Pokemon Red and will stay within the content of the game entirely.

See `docs/` for further information regarding what the project is, tech stack, etc.

## Setup Instructions

Make sure you already have Docker (and Docker Compose) or Podman, along with the .NET SDK installed.

### Submodules

This repo clones the [SE498-Capstone](https://github.com/jsanderswp/SE498-Capstone) StarTrekWeather API as a git submodule at `external/SE498-Capstone` so the full-stack dev environment can talk to it. Either clone with submodules:

```bash
git clone --recurse-submodules <this-repo-url>
```

…or, if you already cloned without `--recurse-submodules`, initialize them:

```bash
git submodule update --init --recursive
```

To pull a newer version of the upstream API later: `git submodule update --remote external/SE498-Capstone` and commit the bump.

### HTTPS Setup (macOS)

`dotnet dev-certs https -ep ${HOME}/.aspnet/https/aspnetapp.pfx -p password`

`dotnet dev-certs https --trust`

### HTTPS Setup (Windows)

`dotnet dev-certs https -ep %USERPROFILE%\.aspnet\https\aspnetapp.pfx -p password`

`dotnet dev-certs https --trust`

## Run Instructions

Use `scripts/dev-up.sh` to bring up the dev stack. It wraps the right Compose files and profiles for each mode so you don't have to remember the flags. On first run it will prompt you to choose `docker` or `podman` and save the choice to `scripts/.runtime` (gitignored). To override later, set `RUNTIME=docker|podman` (one-off or `export`-ed) or edit/delete `scripts/.runtime`.

* **Full stack (default):** our `pokemon-locations-api` + `db` + `cache` + `webserver` plus the SE498-Capstone `api` + `api-db`, all on a shared Docker network. The web server reaches our API at `http://pokemon-locations-api:8080` and the StarTrekWeather API at `http://api:8080`.
* **Backend only (`--backend-only` / `-b`):** just our `pokemon-locations-api` + `db`. The capstone stack and the web server/cache stay down.

The `db` container hosts both the API database (`pokemonlocations`) and the web server database (`pokemonlocations_webserver`).

### Full stack (API + web server + frontend + other API)

```bash
./scripts/dev-up.sh
```

> The frontend runs at [http://localhost:3001/](http://localhost:3001/). The web server serves static files from `wwwroot/` and proxies API requests with per-user state merging. The StarTrekWeather API is reachable from the host at [http://localhost:5002](http://localhost:5002) and from inside containers at `http://api:8080` (its service name in the capstone compose file).

### Backend only

```bash
./scripts/dev-up.sh --backend-only
```

> The API runs at [https://localhost:8081](https://localhost:8081), with Swagger at [https://localhost:8081/swagger](https://localhost:8081/swagger).

### Other commands

Any extra args are forwarded to `docker compose`, with the same file/profile selection applied. Examples:

```bash
./scripts/dev-up.sh down                  # tear down the full stack
./scripts/dev-up.sh logs -f webserver     # tail webserver logs
./scripts/dev-up.sh -b down               # tear down backend-only mode
```

> ⚠️ **Warning:** Compose only acts on services whose profile is currently active, so always tear down with the same mode you brought up with. If you ran `./scripts/dev-up.sh` (full stack), use `./scripts/dev-up.sh down`, **not** `./scripts/dev-up.sh -b down` — otherwise the web server and cache containers will be left running and Compose will fail to remove the project network. Symptom: `Network pokemonlocations_default  Resource is still in use`.

### Getting an API token

All API endpoints except `GET /health/db` require an HS256 JWT in the `Authorization: Bearer <token>` header. For local development, use the `issue-token.sh` helper, which sets the dev signing key and runs the `PokemonLocations.TokenIssuer` console tool:

```bash
./issue-token.sh --client team-alpha
```

`--client` is required and becomes the `sub` claim. `--days` is optional (default `90`). The signed JWT is written to stdout. The script's hardcoded `Jwt__Key` matches the dev key in `docker-compose.debug.yml`, so the resulting token validates against the locally-running API. For non-dev environments, run the issuer directly with the appropriate `Jwt__Key` exported:

```bash
Jwt__Key="<production-key>" \
  dotnet run --project Backend/PokemonLocations.TokenIssuer -- --client team-alpha
```

### Running the API outside the container

To run `PokemonLocations.Api` directly via `dotnet run`, first start the Postgres container it depends on:

```bash
./scripts/dev-up.sh -b up -d db
```

The API runs its database migrations automatically on startup (via DbUp), so no manual migration step is required. The API test suite brings up its own Postgres via Testcontainers and does not need the compose `db` service.

## Troubleshooting

### API container crashes on startup (macOS)

If the `PokemonLocations-Api` container exits immediately after starting, check its logs:

```bash
docker/podman logs PokemonLocations-Api
```

If you see `System.UnauthorizedAccessException: Access to the path '/https/aspnetapp.pfx' is denied` or `System.IO.IOException: Permission denied`, the HTTPS dev certificate is either missing or has incorrect file permissions.

**1. Generate the certificate** (if it doesn't exist):

```bash
dotnet dev-certs https -ep ${HOME}/.aspnet/https/aspnetapp.pfx -p password --trust
```

**2. Fix file permissions** (the cert is often created with owner-only `600` permissions, but the container runs as a non-root user and needs to read the mounted file):

```bash
chmod 644 ~/.aspnet/https/aspnetapp.pfx
```

**3. Restart the API container:**

```bash
docker/podman restart PokemonLocations-Api
```

Verify it's running by checking the logs again — you should see `Now listening on: http://[::]:8080`.

Subject to the terms in `LICENSE`.
