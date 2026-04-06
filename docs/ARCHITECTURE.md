# Architecture - pokemon-locations

## Tech Stack

| **Function** | **Choice** |
| --- | --- |
| Frontend | *Raw HTML/CSS/JS* |
| Frontend Styling | *Bootstrap 5 (CDN)* |
| Frontend Hosting | *nginx (containerized)* |
| Backend (Web Server) | *ASP.NET 10* |
| API | *ASP.NET 10* |
| Databases | *PostgreSQL (two separate instances, containerized)* |
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

The application follows a layered architecture with three main components:

**Frontend (Website)** — Static HTML, CSS, and JavaScript served by an nginx container. The browser makes `fetch()` calls to the backend using Basic Authentication. Bootstrap 5 is loaded from an external CDN for styling. The frontend never communicates with the API directly.

**Backend (Web Server)** — An ASP.NET 10 server that handles user authentication (Basic Auth), manages user-specific data (accounts, favorites), and proxies content requests to the API using Bearer Token authentication. It has its own PostgreSQL database for user data and connects to Redis for caching.

**API Server** — An ASP.NET 10 RESTful API that provides CRUD operations for all content/domain data: locations, buildings, gyms, and location images. It has its own separate PostgreSQL database and exposes a Swagger view for documentation.

## Database Separation

Two PostgreSQL databases run in separate containers:

| **Database** | **Owns** | **Accessed By** |
| --- | --- | --- |
| Web Server DB | Users, favorites | Backend (Web Server) |
| API Database | Locations, buildings, gyms, location images | API Server |

Favorites in the backend DB reference IDs from the API DB but do not use foreign key constraints across databases. The backend validates existence by calling the API.

## Containers

All services run in Docker containers orchestrated by Docker Compose:

| **Container** | **Image / Build** | **Port** |
| --- | --- | --- |
| Frontend | `nginx:alpine` | `3000` |
| Backend (Web Server) | ASP.NET 10 (built from source) | `8082` |
| API Server | ASP.NET 10 (built from source) | `8080` / `8081` (HTTPS) |
| Web Server DB | `postgres:17` | `5433` |
| API Database | `postgres:17` | `5432` |
| Cache | `redis:8-alpine` | `6379` |

## Further Reading

For detailed specifications, see:

- [`docs/specs/SPEC_FRONTEND.md`](specs/SPEC_FRONTEND.md) — Frontend pages, behavior, style guide
- [`docs/specs/SPEC_BACKEND.md`](specs/SPEC_BACKEND.md) — Backend endpoints, schema, auth, logging, testing
- [`docs/specs/SPEC_API.md`](specs/SPEC_API.md) — API endpoints, schema, validation rules
