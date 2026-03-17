# Pokémon Locations

Project for SE498 (Software Engineering Capstone) at Chapman University.

## Setup Instructions

Make sure you already have Docker (and Docker Compose) or Podman, along with the .NET SDK installed.

### HTTPS Setup (macOS)

`dotnet dev-certs https -ep ${HOME}/.aspnet/https/aspnetapp.pfx -p password`
`dotnet dev-certs https --trust`

### HTTPS Setup (Windows)

`dotnet dev-certs https -ep %USERPROFILE%\.aspnet\https\aspnetapp.pfx -p password`
`dotnet dev-certs https --trust`

## Run Instructions

Run the backend with `docker/podman compose -f docker-compose.debug.yml up -d`

> The backend's spec will run at localhost:8081/swagger, and the backend will run at localhost:8081.

Stop the project with `docker/podman compose -f docker-compose.debug.yml down`
