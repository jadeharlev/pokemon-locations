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

Subject to the terms in `LICENSE`.
