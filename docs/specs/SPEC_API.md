# API Spec - pokemon-locations

## 1. Overview

The Pokémon Locations API is a **read-only** RESTful service that exposes locations, buildings, gyms, and location images from the Pokémon Red world.

The **only consumer** is the web server (BFF). The frontend never calls the API directly — requests are proxied through the web server.

| **Function** | **Choice** |
| --- | --- |
| Database | *Postgres* |
| Data Access | *Dapper* |
| API Documentation/Spec | Swagger/OpenAPI (`/swagger`, with Bearer Authorize button) |

In line with the rest of the tech stack, everything runs on *ASP.NET 10* with containerization being handled by *Docker/Podman*.

> **Why read-only?** Content data (locations, buildings, gyms) is canonical to Pokémon Red and seeded entirely via SQL migrations. There is no use case for an HTTP client to mutate it, so Create/Update/Delete endpoints were removed (SE498-57) as defense-in-depth. New seed data is added by writing a new migration in `PokemonLocations.Api/Database/Migrations/`.

## 2. Authentication

All endpoints use **Bearer Token** auth via signed **HS256 JWT**. The token is supplied in the `Authorization` header:

```
Authorization: Bearer <token>
```

The only endpoint that does **not** require a token is `GET /health/db`.

| **Scenario** | **Response** |
| --- | --- |
| Missing token | `401 Unauthorized` |
| Invalid signature, issuer, or audience | `401 Unauthorized` |
| Expired token (`exp` past) | `401 Unauthorized` |

Swagger UI exposes an **Authorize** button that lets a developer paste a JWT and call protected endpoints from the browser; the bearer scheme is declared on every operation.

### 2.1 Expected claims

| **Claim** | **Value** |
| --- | --- |
| `iss` | Configured issuer (default `pokemon-locations-api`) |
| `aud` | Configured audience (default `pokemon-locations-clients`) |
| `sub` | Identifier of the consuming team/client (e.g. `team-alpha`) |
| `exp` | Unix timestamp; tokens past expiry are rejected |

The signing key, issuer, and audience are configured on the API via `Jwt:Key`, `Jwt:Issuer`, and `Jwt:Audience` (env vars `Jwt__Key` / `Jwt__Issuer` / `Jwt__Audience`). The key must be at least 32 bytes for HS256.

### 2.2 Token issuance

Tokens are minted offline using the `PokemonLocations.TokenIssuer` console tool, which signs with the same key the API validates against:

```bash
dotnet run --project Backend/PokemonLocations.TokenIssuer -- --client team-alpha --days 90
```

`--client` (required) becomes the `sub` claim. `--days` (optional, default `90`) sets the expiry window. The signed JWT is written to stdout. There is no in-API issuance endpoint.

For local development, `./issue-token.sh --client <id>` is a convenience wrapper that pre-sets `Jwt__Key` to the dev key in `docker-compose.debug.yml`.

## 3. Schema

The API maintains a PostgreSQL database containing content/domain data only. User-specific data is owned by the web server's separate database.

Schema is managed as a series of versioned SQL migration scripts under `PokemonLocations.Api/Database/Migrations/`, embedded into the API assembly and applied on application startup via [DbUp](https://dbup.readthedocs.io/). Each script runs at most once per database; re-running the API against an already-migrated database is a no-op. Data access from C# uses Dapper on top of a singleton `NpgsqlDataSource`.

### 3.1 **Enum:** `building_type`

```
gym
pokemon_center
poke_mart
residential
landmark
lab
```

This enum is enforced at the database level — inserts with any other value are rejected by Postgres.

### 3.2 `Location`

| **Column** | **Type** | **Constraints** | **Notes** |
| --- | --- | --- | --- |
| `location_id` | `INT` | `PRIMARY KEY` | |
| `name` | `VARCHAR(255)` | `NOT NULL` | |
| `description` | `TEXT` | | |
| `video_url` | `VARCHAR(500)` | | |

### 3.3 `LocationImage`

| **Column** | **Type** | **Constraints** | **Notes** |
| --- | --- | --- | --- |
| `image_id` | `INT` | `PRIMARY KEY` | |
| `location_id` | `INT` | `NOT NULL` | Foreign key to `locations.location_id` |
| `image_url` | `VARCHAR(500)` | `NOT NULL` | |
| `display_order` | `INT` | `NOT NULL` `DEFAULT 0` | |
| `caption` | `VARCHAR(255)` | | |

### 3.4 `Building`

| **Column** | **Type** | **Constraints** | **Notes** |
| --- | --- | --- | --- |
| `building_id` | `INT` | `PRIMARY KEY` | |
| `location_id` | `INT` | `NOT NULL` | Foreign key to `locations.location_id` |
| `name` | `VARCHAR(255)` | `NOT NULL` | |
| `building_type` | `building_type` | `NOT NULL` | See enum above |
| `description` | `TEXT` | | |
| `landmark_description` | `TEXT` | | Only populated when `building_type = 'landmark'` |

### 3.5 `Gym` (companion row for buildings with `building_type = gym`)

| **Column** | **Type** | **Constraints** | **Notes** |
| --- | --- | --- | --- |
| `gym_id` | `INT` | `PRIMARY KEY` | |
| `building_id` | `INT` | `NOT NULL` `UNIQUE` | Foreign key to `buildings.building_id` |
| `gym_type` | `VARCHAR(50)` | `NOT NULL` | e.g. Fire, Water, Rock |
| `badge_name` | `VARCHAR(100)` | `NOT NULL` | |
| `gym_leader` | `VARCHAR(100)` | `NOT NULL` | |
| `gym_order` | `INT` | `NOT NULL` | 1–8 gym progression order |

## 4. API Endpoints

**Base URL:** `https://localhost:8081` (dev), `TBD` (prod)

All response bodies use `application/json`. Every endpoint except `GET /health/db` requires a Bearer token; `POST`, `PUT`, and `DELETE` against any path return `405 Method Not Allowed`.

### 4.1 Health

No auth required.

#### `GET /health/db`

Check database connection.

| **Status** | **Response** |
| --- | --- |
| `200 OK` | `"Database Connected"` |
| `500 Internal Server Error` | Database unreachable |

### 4.2 Locations

#### `GET /Locations`

Retrieve all locations (flat list, no nested images/buildings).

**Response `200 OK`:**
```json
[
  {
    "locationId": 1,
    "name": "Pallet Town",
    "description": "A fairly new and quiet town...",
    "videoUrl": null
  }
]
```

#### `GET /Locations/{locationId}`

Retrieve a single location by ID.

**Response `200 OK`:**
```json
{
  "locationId": 1,
  "name": "Pallet Town",
  "description": "A fairly new and quiet town...",
  "videoUrl": null
}
```

| **Status** | **Response** |
|---|---|
| `200 OK` | Location object |
| `404 Not Found` | Location does not exist |

> The API returns the bare `Location` row; nested `images` and `buildings` are not included on this endpoint. Clients fetch them via the dedicated images/buildings endpoints below.

### 4.3 Location Images

#### `GET /locations/{locationId}/images`

Retrieve all images for a location, ordered by `displayOrder`.

**Response `200 OK`:**
```json
[
  {
    "imageId": 1,
    "locationId": 1,
    "imageUrl": "/images/pallet-town-overview.png",
    "displayOrder": 1,
    "caption": "Pallet Town overview"
  }
]
```

| **Status** | **Response** |
|---|---|
| `200 OK` | Array of image objects |
| `404 Not Found` | Location does not exist |

### 4.4 Buildings

#### `GET /locations/{locationId}/buildings`

Retrieve all buildings for a location.

**Response `200 OK`:**
```json
[
  {
    "buildingId": 4,
    "locationId": 2,
    "name": "Pewter City Gym",
    "buildingType": "gym",
    "description": "A small building featuring a Japanese rock garden...",
    "landmarkDescription": null
  }
]
```

| **Status** | **Response** |
|---|---|
| `200 OK` | Array of building objects |
| `404 Not Found` | Location does not exist |

#### `GET /locations/{locationId}/buildings/{buildingId}`

Retrieve a single building. If the building's `buildingType` is `"gym"`, the gym record is included in the `gym` field.

**Response `200 OK` (gym):**
```json
{
  "buildingId": 4,
  "locationId": 2,
  "name": "Pewter City Gym",
  "buildingType": "gym",
  "description": "A small building featuring a Japanese rock garden...",
  "landmarkDescription": null,
  "gym": {
    "gymId": 1,
    "buildingId": 4,
    "gymType": "Rock",
    "badgeName": "Boulder Badge",
    "gymLeader": "Brock",
    "gymOrder": 1
  }
}
```

| **Status** | **Response** |
|---|---|
| `200 OK` | Building object (with `gym` if applicable) |
| `404 Not Found` | Location or building does not exist |

### 4.5 Gyms

Convenience endpoints for viewing all gyms across locations, sorted by progression order.

#### `GET /gyms`

Retrieve all gyms in progression order (1–8). Returns `GymSummary` rows that include the parent location and building names so clients don't need a second roundtrip.

**Response `200 OK`:**
```json
[
  {
    "gymId": 1,
    "buildingId": 4,
    "locationId": 2,
    "locationName": "Pewter City",
    "buildingName": "Pewter City Gym",
    "gymType": "Rock",
    "badgeName": "Boulder Badge",
    "gymLeader": "Brock",
    "gymOrder": 1
  }
]
```

#### `GET /gyms/{gymId}`

Retrieve a single gym by gym ID.

**Response `200 OK`:** Same shape as a single element from the list above.

| **Status** | **Response** |
|---|---|
| `200 OK` | Gym object |
| `404 Not Found` | Gym does not exist |

## 5. Error Handling

**Status Codes:**

| **Code** | **Meaning** | **When Used** |
|---|---|---|
| `200` | OK | Successful read |
| `401` | Unauthorized | Missing or invalid Bearer token |
| `404` | Not Found | Resource does not exist |
| `405` | Method Not Allowed | Write verbs (POST/PUT/DELETE) against any path |
| `500` | Internal Server Error | Unexpected failure |

ASP.NET's default error response shape is used; there is no custom error envelope.

## 6. Schema-Level Validation

Because the API is read-only, all data validation lives at the migration / database level rather than in HTTP request handlers:

**Locations:**
- `name` is `NOT NULL` and `VARCHAR(255)`
- `video_url` is `VARCHAR(500)`

**Location Images:**
- `image_url` is `NOT NULL` and `VARCHAR(500)`
- `display_order` is `NOT NULL` and `DEFAULT 0`
- `caption` is `VARCHAR(255)`
- `location_id` is `NOT NULL` and references `locations.location_id`

**Buildings:**
- `name` is `NOT NULL` and `VARCHAR(255)`
- `building_type` must be a valid `building_type` enum value (DB-enforced)
- `location_id` is `NOT NULL` and references `locations.location_id`

**Gyms:**
- `gym_type`, `badge_name`, `gym_leader` are all `NOT NULL`
- `gym_order` is `NOT NULL` (canonically 1–8 for Kanto, but not constrained)
- `building_id` is `NOT NULL UNIQUE` and references `buildings.building_id` (one gym row per gym building)

Migrations that violate any of these constraints fail at apply-time, surfacing a startup error.

## 7. Future Considerations

- Potentially track Pokémon types per location (would require additional logic, tables, endpoints)
- API versioning when integrating with another team's API
- Integration with another team's API
