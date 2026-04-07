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

Run the project with `docker/podman compose -f docker-compose.debug.yml up -d`

> The backend's spec will run at [https://localhost:8081/swagger](localhost:8081/swagger), and the backend will run at [https://localhost:8081](localhost:8081).
> The frontend will run at [http://localhost:3000/](localhost:3000)

Stop the project with `docker/podman compose -f docker-compose.debug.yml down`

## Troubleshooting

### API container crashes on startup (macOS)

If the `PokemonLocations-Api` container exits immediately after starting, check its logs:

```
docker/podman logs PokemonLocations-Api
```

If you see `System.UnauthorizedAccessException: Access to the path '/https/aspnetapp.pfx' is denied` or `System.IO.IOException: Permission denied`, the HTTPS dev certificate is either missing or has incorrect file permissions.

**1. Generate the certificate** (if it doesn't exist):

```
dotnet dev-certs https -ep ${HOME}/.aspnet/https/aspnetapp.pfx -p password --trust
```

**2. Fix file permissions** (the cert is often created with owner-only `600` permissions, but the container runs as a non-root user and needs to read the mounted file):

```
chmod 644 ~/.aspnet/https/aspnetapp.pfx
```

**3. Restart the API container:**

```
docker/podman restart PokemonLocations-Api
```

Verify it's running by checking the logs again — you should see `Now listening on: http://[::]:8080`.

Subject to the terms in `LICENSE`.
