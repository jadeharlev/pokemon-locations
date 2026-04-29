# Web Server (BFF) Spec - pokemon-locations

## 1. Overview

The web server is an ASP.NET 10 application that acts as the **Backend-for-Frontend (BFF)** layer. It serves the static frontend from `wwwroot/`, handles user authentication, manages per-user state, proxies content requests to the API, and merges user-specific data into API responses.

| **Function** | **Choice** |
| --- | --- |
| Framework | *ASP.NET 10* |
| Static Files | *`wwwroot/` via `UseStaticFiles()`* |
| Auth (browser → web server) | *HTTP Basic Auth (`idunno.Authentication.Basic`)* |
| Auth (web server → API) | *Bearer JWT (self-minted, HS256)* |
| Database | *PostgreSQL (separate from API DB)* |
| Data Access | *Dapper* |
| Caching | *Redis (`IDistributedCache`)* |
| Password Hashing | *BCrypt (`BCrypt.Net-Next`)* |

## 2. Architecture

```
Browser ──(HTTP w/ Basic Auth)──▶ Web Server ──(REST w/ Bearer Token)──▶ API Server
     ▲                                │                                      │
     │                                ▼                                      ▼
     │                          Web Server DB                           API Database
  wwwroot/                  (users, per-user state)          (locations, buildings, gyms)
(static files)                        │
                                      ▼
                                   Redis
                             (API response cache)
```

The web server has three responsibilities:
1. **Serve the frontend** — static HTML/CSS/JS from `wwwroot/` (no auth required)
2. **Proxy API reads** — forward content requests to the API and merge per-user state
3. **Own user data** — accounts, visited flags, badges, notes

## 3. Endpoints

**Base URL:** `http://localhost:3001` / `https://localhost:3002` (dev)

### 3.1 Static Files (Anonymous)

| Path | Description |
|------|-------------|
| `/` | `index.html` (via `UseDefaultFiles`) |
| `/*.html` | Frontend pages |
| `/script.js` | Frontend JavaScript |

### 3.2 Health (Anonymous)

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/health/db` | Database connectivity check → `{ "status": "ok" }` |

### 3.3 Account

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `POST` | `/account/signup` | Anonymous | Create account → `201` |
| `GET` | `/api/me` | Basic | Get authenticated user profile → `200` |
| `DELETE` | `/account` | Basic | Delete account (cascades) → `204` |

### 3.4 API Proxy (with merging)

All require Basic Auth. Responses are proxied from the API with per-user state merged in.

| Method | Path | Merging |
|--------|------|---------|
| `GET` | `/api/locations` | Adds `visited: bool` per location |
| `GET` | `/api/locations/{id}` | Adds `visited: bool` + `userImages: []` |
| `GET` | `/api/locations/{locationId}/buildings` | Adds `visited: bool` per building |
| `GET` | `/api/locations/{locationId}/buildings/{buildingId}` | Pure proxy (no merging) |
| `GET` | `/api/gyms` | Pure proxy (no merging) |

**Error propagation:** API `404` → web server `404`. API `5xx` → web server `502 Bad Gateway`.

### 3.5 Per-user State

All require Basic Auth.

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/me/badges` | List earned badges |
| `PUT` | `/api/me/badges/{badge}` | Earn a badge → `204` |
| `DELETE` | `/api/me/badges/{badge}` | Remove a badge → `204` |
| `PUT` | `/api/me/visited/locations/{id}` | Mark location visited → `204` |
| `DELETE` | `/api/me/visited/locations/{id}` | Unmark → `204` |
| `PUT` | `/api/me/visited/buildings/{locationId}/{buildingId}` | Mark building visited → `204` |
| `DELETE` | `/api/me/visited/buildings/{locationId}/{buildingId}` | Unmark → `204` |
| `GET` | `/api/me/notes/{locationId}` | Get note → `200` or `404` |
| `PUT` | `/api/me/notes/{locationId}` | Upsert note → `204` |
| `DELETE` | `/api/me/notes/{locationId}` | Delete note → `204` |
| `GET` | `/api/me/stats` | `{ gymsComplete, locationsVisited, buildingsVisited }` |

### 3.6 Validation

- **Notes:** empty/whitespace → `400 empty_note`; > 10,000 chars → `400 note_too_long`
- **Writes to API resources:** location/building existence verified via API before accepting; API `404` → web server `404`
- **All `PUT`/`DELETE` per-user endpoints** are idempotent (composite PKs prevent duplicates)

## 4. Caching

The `CachingApiClientDecorator` wraps `IPokemonLocationsApiClient` and caches API responses in Redis.

| Setting | Value |
|---------|-------|
| Cache store | Redis (`IDistributedCache`) |
| Key | API path (e.g., `/locations`, `/locations/1`) |
| TTL | 5 minutes |
| Scope | Shared across all users (upstream API data only) |

**Important:** Per-user merging happens **after** cache resolution. The merged response is never cached — only the raw API response is. This prevents cross-user data leakage.

Non-200 responses from the API are **not** cached, so transient errors don't stick.

## 5. Database Schema

See [SPEC_BACKEND.md](SPEC_BACKEND.md) for the full schema. Key tables:

- `users` — accounts with BCrypt-hashed passwords
- `user_badges` — earned badges (composite PK `user_id, badge`)
- `user_visited_locations` — visited locations (composite PK `user_id, location_id`)
- `user_visited_buildings` — visited buildings (composite PK `user_id, building_id`)
- `user_location_notes` — per-location notes with 10k char cap

All tables cascade on user deletion.

## 6. Docker Compose

The web server runs under the `frontend` Compose profile alongside Redis:

```bash
docker compose -f docker-compose.debug.yml --profile frontend up -d
```

| Service | Container | Port | Profile |
|---------|-----------|------|---------|
| `webserver` | `PokemonLocations-WebServer` | `3001` / `3002` | `frontend` |
| `cache` | `PokemonLocations-Cache` | `6379` | `frontend` |

The `webserver_uploads` named volume is mounted at `/app/uploads` for future image upload support (SE498-68).
