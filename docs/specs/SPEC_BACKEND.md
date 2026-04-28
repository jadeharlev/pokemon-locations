# Backend (Web Server) Spec - pokemon-locations

## 1. Overview

The backend is an ASP.NET 10 web server that acts as the middleman between the frontend (browser) and the Pokémon Locations API. It handles user authentication, user-specific data (favorites), and sends content requests to the API.

The frontend communicates with the backend using **Basic Authentication** over HTTP. The backend then forwards content-related requests to the API using **Bearer Token** authentication.

| **Function** | **Choice** |
| --- | --- |
| Framework | *ASP.NET 10* |
| Communication Style | *RESTful* |
| Database | *PostgreSQL (separate from API DB, containerized)* |
| Data Access | *Dapper* |
| Password Hashing | *BCrypt (`BCrypt.Net-Next`)* |
| HTTP Basic Auth | *`idunno.Authentication.Basic`* |
| Containerization | *Docker/Podman* |
| Caching | *Redis* |

## 2. Architecture

The backend will sit at the center of the app:

```
Website (Browser) ──(HTTP w/ Basic Auth)──▶ Web Server ──(REST w/ Bearer Token)──▶ API Server
                                                │                                      │
                                                ▼                                      ▼
                                          Web Server DB                           API Database
                              (users; per-user state in follow-ups)     (locations, buildings, gyms, images)
```

> **Implementation status (as of SE498-58).** The web server project, JWT/Redis infra, `users` table, HTTP Basic Auth handler, and `GET /health/db` are shipped. Account endpoints, per-user state tables, API proxy, and image uploads are described in this spec as the eventual surface but are deferred to follow-up tickets (SE498-60, -65, -66, -67, -68, -69, -70).

The frontend (nginx) provides static HTML/CSS/JS files. The browser's JS makes backend requests, which authenticates the user, then sends content requests to the API. The backend owns all **user-specific data** while the API owns all **content/domain data**.

### 2.1 Database Separation

Two PostgreSQL databases, each running in its own container:

| **Database** | **Owns** | **Notes** |
| --- | --- | --- |
| Web Server DB | User-specific data | Users (with `display_name`, `theme`); per-user state in follow-ups: visited locations/buildings, earned badges, per-location notes, uploaded images |
| API DB | Content/domain data | Locations, buildings, gyms, location images |

Per-user rows in the backend DB reference IDs from the API DB (`location_id`, `building_id`). These are not enforced by foreign key constraints as they live in separate databases. The backend checks existence by calling the API before accepting writes.

## 3. Authentication

### 3.1 Frontend → Backend: Basic Authentication

The frontend sends credentials to the backend via the `Authorization` header using the Basic scheme:

```
Authorization: Basic <base64(email:password)>
```

Authentication is **stateless** — there is no session, no cookie, no signin/signout endpoint. Every authenticated request must carry the header. The backend validates by looking up the user by email (Dapper) and verifying the password against the stored BCrypt hash on every request. Failed lookups and bad passwords return `401` with `WWW-Authenticate: Basic realm="PokemonLocations"`.

Endpoints that **do not** require Basic Auth:

- `POST /account/signup`
- `GET /health/db`

All other endpoints require a valid Basic Auth header.

| **Scenario** | **Response** |
| --- | --- |
| Missing or bad header | `401 Unauthorized` |
| Invalid credentials | `401 Unauthorized` |

### 3.2 Backend → API: Bearer Token

When the backend needs to fetch or modify content data, it makes REST requests to the API with a Bearer token in the `Authorization` header:

```
Authorization: Bearer <token>
```

The backend is responsible for obtaining and managing API tokens. If the API returns `401`, the backend returns a `500 Internal Server Error` to the frontend, since tokens are managed internally.

## 4. Schema

The backend has its own PostgreSQL database, separate from the API database. This database stores **user-specific data** only.

### 4.1 `User`

| **Column** | **Type** | **Constraints** | **Notes** |
| --- | --- | --- | --- |
| `user_id` | `SERIAL` | `PRIMARY KEY` | Auto-incrementing |
| `email` | `VARCHAR(255)` | `NOT NULL` `UNIQUE` | Used as login identifier |
| `password_hash` | `VARCHAR(255)` | `NOT NULL` | Hashed with BCrypt (`BCrypt.Net-Next`, work factor 12) |
| `display_name` | `VARCHAR(50)` | `NOT NULL` | Shown in the UI; not unique |
| `theme` | `user_theme` | `NOT NULL` `DEFAULT 'bulbasaur'` | Postgres enum `('bulbasaur', 'charmander', 'squirtle')`; write endpoint deferred to SE498-64 |
| `created_at` | `TIMESTAMPTZ` | `NOT NULL` `DEFAULT now()` | |

### 4.2 Per-user state (deferred)

The following tables are part of the eventual schema but are **not** created by SE498-58. Each ticket below will add its migration and corresponding endpoint surface:

- **SE498-65** — `user_badges` (composite PK on `(user_id, badge)`, `badge` is a `badge_name` enum).
- **SE498-66** — `user_visited_locations` and `user_visited_buildings` (composite PKs on `(user_id, location_id)` / `(user_id, building_id)`).
- **SE498-67** — `user_location_notes` (composite PK on `(user_id, location_id)`, `note_text TEXT`).
- **SE498-68** — `user_uploaded_images` (per-user image records with disk-backed storage).

All per-user tables use `ON DELETE CASCADE` from `users.user_id`, so a `DELETE FROM users WHERE user_id = ?` hard-deletes everything the user owns.

## 5. Endpoints

**Base URL:** `http://localhost:3001` / `https://localhost:3002` (dev), `TBD` (prod)

All request and response bodies use `application/json`.

### 5.1 Health

No auth required.

#### `GET /health/db` ✅ shipped (SE498-58)

Check the backend database connection by issuing `SELECT 1`.

| **Status** | **Response** |
| --- | --- |
| `200 OK` | `{ "status": "ok" }` |
| `500 Internal Server Error` | Database unreachable |

### 5.2 Account

Sign-in is implicit: the browser sends Basic credentials with every request. There is no `POST /account/login` and no `POST /account/logout` — logging out is a client-side concern (clear in-memory credentials and prompt for re-entry).

#### `POST /account/signup` (deferred to SE498-60)

Create a new user account. Anonymous.

**Request:**
```json
{
  "email": "ash@pokemon.com",
  "password": "pikachu123",
  "displayName": "Ash"
}
```

| **Status** | **Response** |
| --- | --- |
| `201 Created` | `{ "userId": 1, "email": "ash@pokemon.com", "displayName": "Ash", "theme": "bulbasaur" }` |
| `400 Bad Request` | Validation errors |
| `409 Conflict` | Email already registered |

#### `GET /api/me` (deferred to SE498-60)

Returns the authenticated user's profile. Requires Basic Auth.

| **Status** | **Response** |
| --- | --- |
| `200 OK` | `{ "userId": 1, "email": "ash@pokemon.com", "displayName": "Ash", "theme": "bulbasaur" }` |
| `401 Unauthorized` | Missing/invalid Basic Auth header |

#### `DELETE /account` (deferred to SE498-60)

Hard-deletes the authenticated user and all owned per-user state (cascades).

| **Status** | **Response** |
| --- | --- |
| `204 No Content` | Account deleted |
| `401 Unauthorized` | Missing/invalid Basic Auth header |

### 5.3 Locations

All endpoints require Basic Auth. The backend forwards these requests to the API.

#### `GET /locations`

Retrieve all locations. Proxies to `GET /locations` on the API.

**Response `200 OK`:**
```json
[
  {
    "locationId": 1,
    "name": "Pallet Town",
    "description": "The player's small hometown...",
    "videoUrl": "/videos/pallet-town.mp4"
  }
]
```

#### `GET /locations/{locationId}`

Retrieve a single location with its images and buildings. Proxies to `GET /locations/{locationId}` on the API.

**Response `200 OK`:**
```json
{
  "locationId": 1,
  "name": "Pallet Town",
  "description": "The player's small hometown...",
  "videoUrl": "/videos/pallet-town.mp4",
  "images": [ ... ],
  "buildings": [ ... ]
}
```

| **Status** | **Response** |
| --- | --- |
| `200 OK` | Location object with images and buildings |
| `404 Not Found` | Location does not exist |

### 5.4 Buildings

All endpoints require Basic Auth. The backend forwards these requests to the API.

#### `GET /locations/{locationId}/buildings`

Retrieve all buildings for a location. Proxies to `GET /locations/{locationId}/buildings` on the API.

| **Status** | **Response** |
| --- | --- |
| `200 OK` | Array of building objects |
| `404 Not Found` | Location does not exist |

#### `GET /locations/{locationId}/buildings/{buildingId}`

Retrieve a single building. Proxies to `GET /locations/{locationId}/buildings/{buildingId}` on the API. Includes gym details if the building is a gym.

| **Status** | **Response** |
| --- | --- |
| `200 OK` | Building object (with `gym` if applicable) |
| `404 Not Found` | Location or building does not exist |

### 5.5 Gyms 

All endpoints require Basic Auth. The backend forwards these requests to the API.

#### `GET /gyms`

Retrieve all gyms in progression order. Proxies to `GET /gyms` on the API.

| **Status** | **Response** |
| --- | --- |
| `200 OK` | Array of gym objects ordered by `gymOrder` |

#### `GET /gyms/{gymId}`

Retrieve a single gym. Proxies to `GET /gyms/{gymId}` on the API.

| **Status** | **Response** |
| --- | --- |
| `200 OK` | Gym object |
| `404 Not Found` | Gym does not exist |

### 5.6 Per-user state (deferred)

The full per-user surface is decomposed across follow-up tickets. The shape below is the eventual contract; status reflects what's shipped.

| Method | Path | Purpose | Ticket |
|---|---|---|---|
| `GET` | `/api/me/badges` | List earned badges | SE498-65 |
| `PUT` | `/api/me/badges/{badge}` | Mark a badge earned | SE498-65 |
| `DELETE` | `/api/me/badges/{badge}` | Mark a badge unearned | SE498-65 |
| `PUT` | `/api/me/visited/locations/{id}` | Mark a location visited | SE498-66 |
| `DELETE` | `/api/me/visited/locations/{id}` | Unmark visited | SE498-66 |
| `PUT` | `/api/me/visited/buildings/{id}` | Mark a building visited | SE498-66 |
| `DELETE` | `/api/me/visited/buildings/{id}` | Unmark visited | SE498-66 |
| `GET` | `/api/me/notes/{locationId}` | Get the user's note for a location | SE498-67 |
| `PUT` | `/api/me/notes/{locationId}` | Upsert the user's note for a location | SE498-67 |
| `GET` | `/api/me/images/{locationId}` | List user-uploaded images for a location | SE498-68 |
| `POST` | `/api/me/images/{locationId}` | Upload a new image (multipart) | SE498-68 |
| `DELETE` | `/api/me/images/{imageId}` | Delete an uploaded image | SE498-68 |
| `GET` | `/api/me/stats` | Aggregate stats (badges, visited counts) | SE498-69 |

All require Basic Auth. Writes that target an API resource (a `locationId` or `buildingId`) validate existence by calling the API first; an API `404` propagates as a web-server `404`. Per-user uniqueness is enforced by composite primary keys, so repeated `PUT`s are idempotent.

## 6. Error Handling

All error responses use the following format:

```json
{
  "error": "A human readable error message"
}
```

**Status Codes:**

| **Code** | **Meaning** | **When Used** |
| --- | --- | --- |
| `200` | OK | Successful read or update |
| `201` | Created | Successful account creation |
| `204` | No Content | Successful deletion or successful idempotent write |
| `400` | Bad Request | Validation failure or bad body |
| `401` | Unauthorized | Missing/invalid Basic Auth |
| `404` | Not Found | Resource does not exist |
| `409` | Conflict | Duplicate (e.g., email already registered) |
| `500` | Internal Server Error | Unexpected failure or API unreachable |

When the API returns an error, the backend translates it into an appropriate response for the frontend. API `404` responses are passed through as `404`. API `401` responses (invalid bearer token) are translated to `500` since token management is internal to the backend.

## 7. Data Validation Rules

These rules are enforced by the backend and covered by unit tests.

**Account Signup:**
- `email` — required, valid email format, max 255 characters, must be unique
- `password` — required, minimum 8 characters
- `displayName` — required, max 50 characters

**Per-request authentication (every protected endpoint):**
- `Authorization: Basic <base64(email:password)>` — required and validated against the database on every request
- Failure mode: `401 Unauthorized` with `WWW-Authenticate: Basic realm="PokemonLocations"`

**Per-user writes:**
- Target information (`locationId` or `buildingId`) must exist (validated via API call)
- Repeated writes are idempotent (composite primary keys prevent duplicates)

## 8. API Integration

The backend communicates with the Pokémon Locations API via HTTP.

| **Setting** | **Value** |
| --- | --- |
| API Base URL (dev, internal) | `http://api:8080` (Docker network) |
| API Base URL (dev, external) | `https://localhost:8081` |
| Auth | Bearer token in `Authorization` header |

### 8.1 Proxy Behavior

For content endpoints (locations, buildings, gyms), the backend:

1. Authenticates the incoming request via Basic Auth
2. Makes the required request to the API with a Bearer token
3. Returns the API response to the frontend as-is without transforming or reshaping the data

### 8.2 Per-user state Behavior

For per-user endpoints, the backend:

1. Authenticates the incoming request via Basic Auth
2. **Writing** (`PUT` / `POST`): Calls the API to verify the referenced resource exists, then upserts into the per-user table. Composite PKs make repeats idempotent.
3. **Reading** (`GET`): Reads the per-user table directly. List endpoints that include API-owned details (location/building names, etc.) merge in fresh API data per request; the upstream API responses are read-through cached in Redis (default 5-minute TTL), but the merged-with-user-state response is **not** cached because it varies by user.
4. **Removing** (`DELETE`): Removes the row from the per-user table directly. Account deletion (`DELETE /account`) cascades through every per-user table.

## 9. Logging

Logging uses ASP.NET's built-in logging framework (`ILogger`) with the following levels:

| **Level** | **Usage** |
| --- | --- |
| `Trace` | Detailed diagnostic info (e.g., raw request/response payloads to the API) |
| `Debug` | Internal state during request processing (e.g., user ID resolved from auth, cache hit/miss) |
| `Information` | Normal operations (e.g., user registered, user logged in, favorite created, API proxy request made) |
| `Warning` | Unexpected but recoverable situations (e.g., API returned an unexpected status, failed login attempt) |
| `Error` | Failed operations (e.g., database query failed, API unreachable, unhandled exception in a request) |
| `Fatal` | Application-level failures (e.g., database connection pool exhausted, unable to start) |

### 9.1 What Gets Logged

- **Authentication events:** Basic Auth credential validation outcomes (success/failure, by email — never password), signup, malformed Basic Auth headers
- **API proxy calls:** Outbound URL, HTTP method, response status code, response time, cache hit/miss
- **Per-user operations:** Visited / badges / notes / images writes and deletes
- **Errors:** Stack traces for unhandled exceptions, API timeout/failure details, database errors
- **Startup:** Configuration loaded, database connection established, DbUp migration log, JWT minted, API connectivity verified

### 9.2 What Does NOT Get Logged

- Passwords or password hashes
- Full Bearer tokens (log only a prefix for correlation)
- Raw Basic Auth header values

Logging output is directed to the container console (`stdout`/`stderr`) so it can be viewed via `docker logs`.

## 10. Unit Testing

Unit tests cover backend logic and are run via `dotnet test` in CI (GitHub Actions).

### 10.1 Test Coverage Areas

- **Account controller logic:** Signup (success, duplicate email, validation failure), `GET /api/me`, `DELETE /account` (cascades through per-user tables)
- **Per-user controller logic:** Writes (success, idempotent repeat, API 404 propagation), reads (correct rows, merged details from cached API responses), deletes (success, not found)
- **Auth handler:** Valid Basic Auth header parsing, missing header rejection, malformed header rejection, credential validation against `UserRepository` + BCrypt
- **API proxy logic:** Successful proxy pass-through, cache hit vs miss, API returning 404 (forwarded), API returning 500 (handled gracefully), API unreachable (timeout handling)

### 10.2 Testing Approach

- Controllers are tested using mocked repository interfaces (`NSubstitute`)
- API calls are mocked via `HttpMessageHandler` substitution so tests do not require a running API
- Database access is abstracted behind interfaces so repository logic can be tested independently

## 11. Future Considerations

- Integration with another team's API (additional proxy endpoints, potentially a second bearer token)
- Rate limiting on signup and authentication failures
- Per-image thumbnails / blob storage migration
- Theme stylesheet wiring + write endpoint (SE498-64, deferred indefinitely)
