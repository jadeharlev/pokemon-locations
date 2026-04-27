# API Spec - pokemon-locations

## 1. Overview

The Pokémon Locations API is a RESTful service that provides CRUD operations for locations, buildings, gyms, and location images from the Pokémon Red world.

The **primary consumer** is the web server (backend). The frontend doesn't call the API directly, with requests routed through the web server.

| **Function** | **Choice** | 
| --- | --- | 
| Database | *Postgres* | 
| Data Access | *Dapper* |
| API Documentation/Spec | Swagger/OpenAPI (`/swagger`) |

In line with the rest of the tech stack, everything runs on *ASP.NET 10* with containerization being handled by *Docker/Podman*.

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

## 3. Schema

The API maintains a PostgreSQL database containing content/domain data only. User-specific data (accounts, favorites) is managed by the backend's separate database.

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
| `location_id` | `INT` | `NOT NULL` | Foreign Key |
| `image_url` | `VARCHAR(500)` | `NOT NULL` | |
| `display_order` | `INT` | `NOT NULL` `DEFAULT 0` | 
| `caption` | `VARCHAR(255)` | | | 

### 3.4 `Building`

| **Column** | **Type** | **Constraints** | **Notes** |
| --- | --- | --- | --- |
| `building_id` | `INT` | `PRIMARY KEY` | | 
| `location_id` | `INT` | `NOT NULL` | Foreign Key |
| `name` | `VARCHAR(255)` | `NOT NULL` | |
| `building_type` | `ENUM` | `NOT NULL` | |
| `description` | `TEXT` | | | 
| `landmark_description` | `TEXT` | | Only used when building type is landmark |

### 3.5 `Gym` (building with `type = gym`)

| **Column** | **Type** | **Constraints** | **Notes** |
| --- | --- | --- | --- |
| `gym_id` | `INT` | `PRIMARY KEY` | | 
| `building_id` | `INT` | `NOT NULL` `UNIQUE` | Foreign Key |
| `gym_type` | `VARCHAR(50)` | `NOT NULL` | e.g. Fire |
| `badge_name` | `VARCHAR(100)` | `NOT NULL` | |
| `gym_leader` | `VARCHAR(100)` | `NOT NULL` | | 
| `gym_order` | `INT` | `NOT NULL` | 1-8 gym progression order |

## 4. API Endpoints

**Base URL:** `https://localhost:8081` (dev), `TBD` (prod)

All requests and response bodies use `application/json`.

### 4.1 Health

No auth required.

#### `GET /health/db`

Check database connection.

| **Status** | **Response** |
| --- | --- | 
| `200 OK` | "Database connected" |
| `500 Internal Server Error` | Database unreachable |

### 4.2 Locations
 
#### `GET /locations`
 
Retrieve all locations.
 
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
 
Retrieve a single location with its images and buildings.
 
**Response `200 OK`:**
```json
{
  "locationId": 1,
  "name": "Pallet Town",
  "description": "The player's small hometown...",
  "videoUrl": "/videos/pallet-town.mp4",
  "images": [
    {
      "imageId": 1,
      "imageUrl": "/images/pallet-town-overview.png",
      "displayOrder": 1,
      "caption": "Pallet Town overview"
    }
  ],
  "buildings": [
    {
      "buildingId": 1,
      "name": "Oak Pokémon Research Lab",
      "buildingType": "lab",
      "description": "The Pokémon research lab of Professor Oak."
    }
  ]
}
```
 
| **Status** | **Response** |
|---|---|
| `200 OK` | Location object with images and buildings |
| `404 Not Found` | `{ "error": "Location not found" }` |
 
#### `POST /locations`
 
Create a new location.
 
**Request:**
```json
{
  "name": "Pallet Town",
  "description": "The player's small hometown...",
  "videoUrl": "/videos/pallet-town.mp4"
}
```
 
| **Status** | **Response** |
|---|---|
| `201 Created` | Created location object (with `locationId`) |
| `400 Bad Request` | Validation errors |
 
#### `PUT /locations/{locationId}`
 
Update a location. Request body is the same shape as `POST`.
 
| **Status** | **Response** |
|---|---|
| `200 OK` | Updated location object |
| `400 Bad Request` | Validation errors |
| `404 Not Found` | Location does not exist |
 
#### `DELETE /locations/{locationId}`
 
Delete a location and all associated images, buildings, and gyms.
 
| **Status** | **Response** |
|---|---|
| `204 No Content` | Deleted |
| `404 Not Found` | Location does not exist |

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
 
#### `POST /locations/{locationId}/images`
 
Add an image to a location.
 
**Request:**
```json
{
  "imageUrl": "/images/pallet-town-overview.png",
  "displayOrder": 1,
  "caption": "Pallet Town overview"
}
```
 
| **Status** | **Response** |
|---|---|
| `201 Created` | Created image object (with `imageId`) |
| `400 Bad Request` | Validation errors |
| `404 Not Found` | Location does not exist |
 
#### `PUT /locations/{locationId}/images/{imageId}`
 
Update an image's URL, order, or caption. Request body same shape as `POST`.
 
| **Status** | **Response** |
|---|---|
| `200 OK` | Updated image object |
| `400 Bad Request` | Validation errors |
| `404 Not Found` | Location or image does not exist |
 
#### `DELETE /locations/{locationId}/images/{imageId}`
 
Remove an image.
 
| **Status** | **Response** |
|---|---|
| `204 No Content` | Deleted |
| `404 Not Found` | Location or image does not exist |

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
    "description": "The official Pewter City Pokémon Gym.",
    "landmarkDescription": null
  }
]
```
 
| **Status** | **Response** |
|---|---|
| `200 OK` | Array of building objects |
| `404 Not Found` | Location does not exist |
 
#### `GET /locations/{locationId}/buildings/{buildingId}`
 
Retrieve a single building. If the building is a gym, gym details are included.
 
**Response `200 OK` (gym):**
```json
{
  "buildingId": 4,
  "locationId": 2,
  "name": "Pewter City Gym",
  "buildingType": "gym",
  "description": "The official Pewter City Pokémon Gym.",
  "landmarkDescription": null,
  "gym": {
    "gymId": 1,
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
 
#### `POST /locations/{locationId}/buildings`
 
Create a building. If `buildingType` is `"gym"`, the `gym` object is required.
 
**Request (gym):**
```json
{
  "name": "Pewter City Gym",
  "buildingType": "gym",
  "description": "The official Pewter City Pokémon Gym.",
  "gym": {
    "gymType": "Rock",
    "badgeName": "Boulder Badge",
    "gymLeader": "Brock",
    "gymOrder": 1
  }
}
```
 
**Request (landmark):**
```json
{
  "name": "Pewter Museum of Science",
  "buildingType": "landmark",
  "description": "A museum showcasing rare fossils and space exhibits.",
  "landmarkDescription": "Features a Kabutops fossil and a Space Shuttle exhibit."
}
```
 
| **Status** | **Response** |
|---|---|
| `201 Created` | Created building object |
| `400 Bad Request` | Validation errors |
| `404 Not Found` | Location does not exist |
 
#### `PUT /locations/{locationId}/buildings/{buildingId}`
 
Update a building. Request body same as `POST`.
 
| **Status** | **Response** |
|---|---|
| `200 OK` | Updated building object |
| `400 Bad Request` | Validation errors |
| `404 Not Found` | Location or building does not exist |
 
#### `DELETE /locations/{locationId}/buildings/{buildingId}`
 
Delete a building and its gym record (if applicable).
 
| **Status** | **Response** |
|---|---|
| `204 No Content` | Deleted |
| `404 Not Found` | Location or building does not exist |

### 4.5 Gyms
 
Read-only convenience endpoints for viewing all gyms across locations, sorted by progression order.
 
#### `GET /gyms`
 
Retrieve all gyms in progression order (1–8).
 
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
 
All error responses use the following format:
 
```json
{
  "error": "A human readable error message"
}
```
 
**Status Codes:**
 
| **Code** | **Meaning** | **When Used** |
|---|---|---|
| `200` | OK | Successful read or update |
| `201` | Created | Successful resource creation |
| `204` | No Content | Successful deletion |
| `400` | Bad Request | Validation failure or bad body |
| `401` | Unauthorized | Missing or invalid Bearer token |
| `404` | Not Found | Resource does not exist |
| `500` | Internal Server Error | Unexpected failure |

## 6. Data Validation Rules
 
These rules are enforced by the API and covered by unit tests.
 
**Locations:**
- `name` — required, max 255 characters
- `description` — optional
- `videoUrl` — optional, max 500 characters
 
**Location Images:**
- `imageUrl` — required, max 500 characters
- `displayOrder` — required, integer >= 0
- `caption` — optional, max 255 characters
- Parent `locationId` must reference an existing location
 
**Buildings:**
- `name` — required, max 255 characters
- `buildingType` — required, must be a valid `building_type` enum value
- `description` — optional
- `landmarkDescription` — optional (only meaningful when type is `landmark`)
- Parent `locationId` must reference an existing location
 
**Gyms** (required when `buildingType` is `"gym"`):
- `gymType` — required, max 50 characters
- `badgeName` — required, max 100 characters
- `gymLeader` — required, max 100 characters
- `gymOrder` — required, integer 1–8

## 7. Future Considerations

- Potentially have Pokémon types for locations (would require additional logic, tables, endpoints, and minor restructuring of responses)
- API versioning when integrating with another team's API
- Integration with other team's API
