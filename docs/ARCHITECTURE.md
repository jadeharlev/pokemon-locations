# Architecture - pokemon-locations

## Tech Stack

| **Function** | **Choice** |
| --- | --- |
| Frontend | *Raw HTML/CSS/JS* |
| Frontend Styling | *Bootstrap 5 (CDN)* |
| Frontend Hosting | *nginx (containerized)* |
| Backend (Web Server) | *ASP.NET 10* |
| API | *ASP.NET 10* |
| Databases | *PostgreSQL (one container hosting two databases, containerized)* |
| Caching | *Redis* |
| Data Access | *Dapper* |
| Containerization | *Docker/Podman* |
| Orchestration | *Docker Compose* |
| CI | *GitHub Actions* |
| Testing | *xUnit, NSubstitute* |

## Architecture Overview

```
Website (Browser) ──(HTTP w/ Basic Auth)──▶ Web Server ──(REST w/ Bearer Token)──▶ API Server
                                                │                                      │
                                                ▼                                      ▼
                                          Web Server DB                           API Database
                                        (users, favorites)              (locations, buildings, gyms, images)
```

The application follows has layers with three main components:

**Frontend (Website)** — Static HTML, CSS, and JavaScript served by an nginx container. The browser makes `fetch()` calls to the backend using Basic Authentication. Bootstrap 5 is loaded externally for styling. The frontend never communicates with the API directly.

**Backend (Web Server)** — An ASP.NET 10 server that handles user authentication (Basic Auth), manages user-specific data (accounts, favorites), and proxies content requests to the API using Bearer Token authentication. It has its own PostgreSQL database for user data and connects to Redis for caching.

**API Server** — An ASP.NET 10 RESTful API that provides CRUD operations for all content/domain data: locations, buildings, gyms, and location images. It has its own separate PostgreSQL database and exposes a Swagger view for documentation.

## Database Separation

Two PostgreSQL databases run inside a single `postgres:17` container:

| **Database** | **Owns** | **Accessed By** |
| --- | --- | --- |
| `pokemonlocations_webserver` | Users, favorites | Backend (Web Server) |
| `pokemonlocations` | Locations, buildings, gyms, location images | API Server |

Favorites in the web server DB reference IDs from the API DB but do not use foreign key constraints across databases. The backend validates existence by calling the API.

## Containers

All services run in Docker containers orchestrated by Docker Compose. The frontend stack (frontend, web server) is gated behind the `frontend` Compose profile — `docker compose up` brings up only the API stack by default.

| **Container** | **Image / Build** | **Port** | **Profile** |
| --- | --- | --- | --- |
| API Server | ASP.NET 10 (built from source) | `8080` / `8081` (HTTPS) | *(default)* |
| Database | `postgres:17` (hosts both `pokemonlocations` and `pokemonlocations_webserver`) | `5432` | *(default)* |
| Cache | `redis:8-alpine` | `6379` | *(default)* |
| Backend (Web Server) | ASP.NET 10 (built from source) | `8082` | `frontend` |
| Frontend | `nginx:alpine` | `3000` | `frontend` |

## Further Reading

For detailed specifications, see:

- [`docs/specs/SPEC_FRONTEND.md`](specs/SPEC_FRONTEND.md) — Frontend pages, behavior, style guide
- [`docs/specs/SPEC_BACKEND.md`](specs/SPEC_BACKEND.md) — Backend endpoints, schema, auth, logging, testing
- [`docs/specs/SPEC_API.md`](specs/SPEC_API.md) — API endpoints, schema, validation rules
