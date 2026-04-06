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
| Password Hashing | *pgcrypto `crypt()`* |
| Containerization | *Docker/Podman* |
| Caching | *Redis* |

## 2. Architecture

The backend will sit at the center of the app:

```
Browser ──(HTTP w/ Basic Auth)──▶ Web Server ──(REST w/ Bearer Token)──▶ API Server
                                      │                                      │
                                      ▼                                      ▼
                                 Web Server DB                           API Database
                              (users, favorites)                (locations, buildings, gyms, images)
```

The frontend (nginx) provies static HTML/CSS/JS files. The browser's JS makes backend requests, which authenticates the user, then sends content requests to the API. The backend owns all **user-specific data** while the API owns all **content/domain data**.

### 2.1 Database Separation

Two PostgreSQL databases, each running in its own container:

| **Database** | **Owns** | **Notes** |
| --- | --- | --- |
| Web Server DB | User-specific data | Users, favorite locations, favorite buildings |
| API DB | Content/domain data | Locations, buildings, gyms, location images |

The favorites tables in the backend DB reference IDs from the API DB (`location_id`, `building_id`). These are not enforced by foreign key constraints as they live in separate databases. The backend checks existence by calling the API before creating a favorite.

## 3. Authentication

### 3.1 Frontend → Backend: Basic Authentication

The frontend sends credentials to the backend via the `Authorization` header using the Basic scheme:

```
Authorization: Basic <base64(email:password)>
```

Endpoints that **do not** require Basic Auth:

- `POST /account/register`
- `POST /account/login`
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
| `password_hash` | `VARCHAR(255)` | `NOT NULL` | Hashed with pgcrypto `crypt()` |
| `created_at` | `TIMESTAMP` | `NOT NULL` `DEFAULT NOW()` | |

### 4.2 `FavoriteLocation`

| **Column** | **Type** | **Constraints** | **Notes** |
| --- | --- | --- | --- |
| `favorite_id` | `SERIAL` | `PRIMARY KEY` | |
| `user_id` | `INT` | `NOT NULL` | Foreign Key → `User` |
| `location_id` | `INT` | `NOT NULL` | References API location |
| `created_at` | `TIMESTAMP` | `NOT NULL` `DEFAULT NOW()` | |
| | | `UNIQUE (user_id, location_id)` | Prevents duplicates |

### 4.3 `FavoriteBuilding`

| **Column** | **Type** | **Constraints** | **Notes** |
| --- | --- | --- | --- |
| `favorite_id` | `SERIAL` | `PRIMARY KEY` | |
| `user_id` | `INT` | `NOT NULL` | Foreign Key → `User` |
| `building_id` | `INT` | `NOT NULL` | References API building |
| `created_at` | `TIMESTAMP` | `NOT NULL` `DEFAULT NOW()` | |
| | | `UNIQUE (user_id, building_id)` | Prevents duplicates |

## 5. Endpoints

**Base URL:** `http://localhost:8082` (dev), `TBD` (prod)

All request and response bodies use `application/json`.

### 5.1 Health

No auth required.

#### `GET /health/db`

Check the backend database connection.

| **Status** | **Response** |
| --- | --- |
| `200 OK` | `"Database connected"` |
| `500 Internal Server Error` | Database unreachable |

### 5.2 Account

No authentication required for registration or login.

#### `POST /account/register`

Create a new user account.

**Request:**
```json
{
  "email": "ash@pokemon.com",
  "password": "pikachu123"
}
```

| **Status** | **Response** |
| --- | --- |
| `201 Created` | `{ "userId": 1, "email": "ash@pokemon.com" }` |
| `400 Bad Request` | Validation errors |
| `409 Conflict` | Email already registered |

#### `POST /account/login`

Authenticate a user. Validates the provided credentials against the database.

**Request:**
```json
{
  "email": "ash@pokemon.com",
  "password": "pikachu123"
}
```

| **Status** | **Response** |
| --- | --- |
| `200 OK` | `{ "userId": 1, "email": "ash@pokemon.com" }` |
| `401 Unauthorized` | Invalid email or password |

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

### 5.6 Favorite Locations

All endpoints require Basic Auth. Favorites are stored in the backend database. The backend validates that the referenced `locationId` exists by calling the API before creating a favorite.

#### `GET /favorites/locations`

Retrieve the current user's favorited locations. The backend joins its local favorites data with location details fetched from the API.

**Response `200 OK`:**
```json
[
  {
    "favoriteId": 1,
    "locationId": 1,
    "name": "Pallet Town",
    "description": "The player's small hometown...",
    "createdAt": "2025-03-30T12:00:00Z"
  }
]
```

#### `POST /favorites/locations/{locationId}`

Add a location to the current user's favorites.

| **Status** | **Response** |
| --- | --- |
| `201 Created` | `{ "favoriteId": 1, "locationId": 1, "createdAt": "..." }` |
| `404 Not Found` | Location does not exist (API returned 404) |
| `409 Conflict` | Already favorited |

#### `DELETE /favorites/locations/{locationId}`

Remove a location from the current user's favorites.

| **Status** | **Response** |
| --- | --- |
| `204 No Content` | Removed |
| `404 Not Found` | Location not in favorites |

### 5.7 Favorite Buildings

All endpoints require Basic Auth. Same pattern as favorite locations.

#### `GET /favorites/buildings`

Retrieve the current user's favorited buildings. The backend joins its local favorites data with building details fetched from the API.

**Response `200 OK`:**
```json
[
  {
    "favoriteId": 1,
    "buildingId": 4,
    "locationId": 2,
    "name": "Pewter City Gym",
    "buildingType": "gym",
    "createdAt": "2025-03-30T12:00:00Z"
  }
]
```

#### `POST /favorites/buildings/{buildingId}`

Add a building to the current user's favorites.

| **Status** | **Response** |
| --- | --- |
| `201 Created` | `{ "favoriteId": 1, "buildingId": 4, "createdAt": "..." }` |
| `404 Not Found` | Building does not exist (API returned 404) |
| `409 Conflict` | Already favorited |

#### `DELETE /favorites/buildings/{buildingId}`

Remove a building from the current user's favorites.

| **Status** | **Response** |
| --- | --- |
| `204 No Content` | Removed |
| `404 Not Found` | Building not in favorites |

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
| `200` | OK | Successful read, update, or login |
| `201` | Created | Successful account or favorite creation |
| `204` | No Content | Successful deletion |
| `400` | Bad Request | Validation failure or bad body |
| `401` | Unauthorized | Missing/invalid Basic Auth |
| `404` | Not Found | Resource does not exist |
| `409` | Conflict | Duplicate (e.g., email, favorite) |
| `500` | Internal Server Error | Unexpected failure or API unreachable |

When the API returns an error, the backend translates it into an appropriate response for the frontend. API `404` responses are passed through as `404`. API `401` responses (invalid bearer token) are translated to `500` since token management is internal to the backend.

## 7. Data Validation Rules

These rules are enforced by the backend and covered by unit tests.

**Account Registration:**
- `email` — required, valid email format, max 255 characters, must be unique
- `password` — required, minimum 8 characters

**Account Login:**
- `email` — required
- `password` — required

**Favorites:**
- Target information (`locationId` or `buildingId`) must exist (validated via API call)
- A user cannot favorite the same resource twice (enforced by `UNIQUE` constraint)

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

### 8.2 Favorites Behavior

For favorite endpoints, the backend:

1. Authenticates the incoming request via Basic Auth
2. **Creating** (`POST`): Calls the API to verify the resource exists, then stores the favorite in the backend DB
3. **Listing** (`GET`): Reads favorites from the backend DB, then fetches resource details from the API to enrich the response with names, descriptions, etc.
4. **Removing** (`DELETE`): Removes the favorite from the backend DB directly

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

- **Authentication events:** Login attempts (success and failure), registration, invalid Basic Auth headers
- **API proxy calls:** Outbound URL, HTTP method, response status code, response time
- **Favorites operations:** Creation, deletion, duplicate attempts
- **Errors:** Stack traces for unhandled exceptions, API timeout/failure details, database errors
- **Startup:** Configuration loaded, database connection established, API connectivity verified

### 9.2 What Does NOT Get Logged

- Passwords or password hashes
- Full Bearer tokens (log only a prefix for correlation)
- Raw Basic Auth header values

Logging output is directed to the container console (`stdout`/`stderr`) so it can be viewed via `docker logs`.

## 10. Unit Testing

Unit tests cover backend logic and are run via `dotnet test` in CI (GitHub Actions).

### 10.1 Test Coverage Areas

- **Account controller logic:** Registration (success, duplicate email, validation failure), login (success, wrong password, nonexistent user)
- **Favorites controller logic:** Creating a favorite (success, duplicate, nonexistent resource), listing favorites, deleting a favorite (success, not found)
- **Auth middleware:** Valid Basic Auth header parsing, missing header rejection, malformed header rejection, credential validation
- **API proxy logic:** Successful proxy pass-through, API returning 404 (forwarded), API returning 500 (handled gracefully), API unreachable (timeout handling)

### 10.2 Testing Approach

- Controllers are tested using mocked repository interfaces (`NSubstitute`)
- API calls are mocked via `HttpMessageHandler` substitution so tests do not require a running API
- Database access is abstracted behind interfaces so repository logic can be tested independently

## 11. Future Considerations

- Finalize token issuance strategy between the backend and the API (JWT, opaque tokens, etc.)
- Redis caching for frequently accessed API data (location lists, building lists)
- Integration with another team's API (additional proxy endpoints, potentially a second bearer token)
- Rate limiting on login attempts
