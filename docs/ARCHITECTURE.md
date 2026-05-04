# Architecture - pokemon-locations

## Tech Stack

| **Function** | **Choice** |
| --- | --- |
| Frontend | *Raw HTML/CSS/JS* |
| Frontend Styling | *Bootstrap 5 (CDN)* |
| Frontend Hosting | *ASP.NET 10 Web Server (static files via `wwwroot/`)* |
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
                              (users; per-user state)              (locations, buildings, gyms, images)
                                                │
                                                ▼
                                             Redis
                                    (API response cache)
```

The application has three main components:

**Frontend (Website)** — Static HTML, CSS, and JavaScript served by the ASP.NET web server from `wwwroot/`. The browser makes `fetch()` calls to the same-origin web server using Basic Authentication. Bootstrap 5 is loaded externally for styling. The frontend never communicates with the API directly.

**Backend (Web Server)** — An ASP.NET 10 server that serves the frontend static files, handles user authentication (HTTP Basic Auth, stateless), manages user-specific data (accounts and per-user state such as visited locations, earned badges, and per-location notes), and proxies content requests to the API using a self-minted Bearer JWT. API responses are cached in Redis (5-minute TTL). Per-user state is merged into proxied responses per-request. Passwords are hashed with BCrypt. It has its own PostgreSQL database for user data.

**API Server** — An ASP.NET 10 RESTful API that provides CRUD operations for all content/domain data: locations, buildings, gyms, and location images. It has its own separate PostgreSQL database and exposes a Swagger view for documentation.

## Database Separation

Two PostgreSQL databases run inside a single `postgres:17` container:

| **Database** | **Owns** | **Accessed By** |
| --- | --- | --- |
| `pokemonlocations_webserver` | Users; per-user state (visited locations/buildings, earned badges, per-location notes) | Backend (Web Server) |
| `pokemonlocations` | Locations, buildings, gyms, location images | API Server |

Per-user rows in the web server DB reference IDs from the API DB but do not use foreign key constraints across databases. The backend validates existence by calling the API before accepting writes.

## Containers

All services run in Docker containers orchestrated by Docker Compose. The frontend stack (web server, Redis cache) is gated behind the `frontend` Compose profile — `docker compose up` brings up only the API stack by default.

| **Container** | **Image / Build** | **Port** | **Profile** |
| --- | --- | --- | --- |
| API Server | ASP.NET 10 (built from source) | `8080` / `8081` (HTTPS) | *(default)* |
| Database | `postgres:17` (hosts both `pokemonlocations` and `pokemonlocations_webserver`) | `5432` | *(default)* |
| Web Server | ASP.NET 10 (built from source); serves frontend + BFF endpoints | `3001` / `3002` (HTTPS) | `frontend` |
| Cache | `redis:8-alpine` (API response cache for web server) | `6379` | `frontend` |

> **Note:** The web server replaced the previous nginx-based frontend container as of SE498-70. Static files are now served directly from `wwwroot/` by the ASP.NET web server.

## Further Reading

For detailed specifications, see:

- [`docs/specs/SPEC_FRONTEND.md`](specs/SPEC_FRONTEND.md) — Frontend pages, behavior, style guide
- [`docs/specs/SPEC_WEBSERVER.md`](specs/SPEC_WEBSERVER.md) — Web server (BFF) endpoints, schema, caching, validation
- [`docs/specs/SPEC_API.md`](specs/SPEC_API.md) — API endpoints, schema, validation rules
