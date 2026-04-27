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

### HTTPS Setup (macOS)

`dotnet dev-certs https -ep ${HOME}/.aspnet/https/aspnetapp.pfx -p password`

`dotnet dev-certs https --trust`

### HTTPS Setup (Windows)

`dotnet dev-certs https -ep %USERPROFILE%\.aspnet\https\aspnetapp.pfx -p password`

`dotnet dev-certs https --trust`

## Run Instructions

The Compose file is split into two stacks via Compose profiles:

* **API stack (default)**: `api`, `db`, `cache`. The `db` container hosts both the API database (`pokemonlocations`) and the web server database (`pokemonlocations_webserver`).
* **Frontend stack**: adds `frontend` (and, in the future, the ASP.NET web server). Activated with `--profile frontend`.

### API stack only

```bash
docker/podman compose -f docker-compose.debug.yml up -d
```

> The API will run at [https://localhost:8081](localhost:8081), with Swagger at [https://localhost:8081/swagger](localhost:8081/swagger).

### Full stack (API + frontend)

```bash
docker/podman compose -f docker-compose.debug.yml --profile frontend up -d
```

> The frontend will run at [http://localhost:3000/](localhost:3000).

Stop everything with `docker/podman compose -f docker-compose.debug.yml --profile frontend down` (the `--profile` flag is needed to also stop frontend services).

> ⚠️ **Warning:** Compose only acts on services whose profile is currently active. If you ran `up` with `--profile frontend`, you **must** pass `--profile frontend` to `down` as well — otherwise the frontend container will be left running, and Compose will fail to remove the project network because the frontend is still attached to it. Symptom: `Network pokemonlocations_default  Resource is still in use`.

### Running the API outside the container

To run `PokemonLocations.Api` directly via `dotnet run`, first start the Postgres container it depends on:

```bash
docker/podman compose -f docker-compose.debug.yml up -d db
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
