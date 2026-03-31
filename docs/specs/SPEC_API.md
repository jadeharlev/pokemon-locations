# API Spec - pokemon-locations

## 1. Overview

The Pokémon Locations API is a RESTful service that provides CRUD operations for locations, buildings, gyms, location images, and user favorites from the Pokémon Red world.

The **primary consumer** is the web server, the backend. The frontend doesn't call the API directly, with requests routed through the web server.

| **Function** | **Choice** | 
| --- | --- | 
| Database | *Postgres* | 
| Data Access | *Dapper* |
| Password Hashing | pgcrypto `crypt()` | 
| API Documentation/Spec | Swagger/OpenAPI (`/swagger`) |

In line with the rest of the tech stack, everything runs on *ASP.NET 10* with containerization being handled by *Docker/Podman*.

## 2. Authentication 

All endpoints use **Bearer Token** auth. There are 3 endpoints that **do not** require the bearer token in the `Authorization` header:

```
Authorization: Bearer <token>
```
`
- `GET /health/db`
- `POST /auth/register`
- `POST /auth/login`

| **Scenario** | **Response**
| --- | --- |
| Missing token | `401 Unauthorized` | 
| Invalid or Expired token | `401 Unauthorized` |

Token issuance: TBD 

## 3. Schema 

> [!TIP]
> Currently, everything is in one database. According to the architecture diagrams in the slides, there need to be **two** databases: one for the API and one for the web server. To be reviewed. 

The API keeps track of a PostgreSQL database (`pokemonlocations`). Passwords for basic auth are hashed using pgcrypto's `crypt()` function. 

### 3.1 **Enum:** `building_type`

``` 
gym 
pokeom_center
poke_mart
residential
landmark
lab
```

### 3.2 `User`

| **Column** | **Type** | **Constraints** | **Notes** |
| --- | --- | --- | --- |
| `user_id` | `INT` | `PRIMARY KEY` | | 
| `email` | `VARCHAR(255)` | `NOT NULL` `UNIQUE` | |
| `password` | `VARCHAR(255)` | `NOT NULL` | |

### 3.3 `Location`

| **Column** | **Type** | **Constraints** | **Notes** |
| --- | --- | --- | --- |
| `location_id` | `INT` | `PRIMARY KEY` | | 
| `name` | `VARCHAR(255)` | `NOT NULL` | |
| `description` | `TEXT` | | |
| `video_url` | `VARCHAR(500)` | | |

### 3.4 `LocationImage`

| **Column** | **Type** | **Constraints** | **Notes** |
| --- | --- | --- | --- |
| `image_id` | `INT` | `PRIMARY KEY` | | 
| `location_id` | `INT` | `NOT NULL` | Foreign Key |
| `image_url` | `VARCHAR(500)` | `NOT NULL` | |
| `display_order` | `INT` | `NOT NULL` `DEFAULT 0` | 
| `caption` | `VARCHAR(255)` | | | 

### 3.5 `Building`

| **Column** | **Type** | **Constraints** | **Notes** |
| --- | --- | --- | --- |
| `building_id` | `INT` | `PRIMARY KEY` | | 
| `location_id` | `INT` | `NOT NULL` | Foreign Key |
| `name` | `VARCHAR(255)` | `NOT NULL` | |
| `building_type` | `ENUM` | `NOT NULL` | |
| `description` | `TEXT` | | | 
| `landmark_description` | `TEXT` | | Only used when building type is landmark |

### 3.6 `Gym` (building with `type = gym`)

| **Column** | **Type** | **Constraints** | **Notes** |
| --- | --- | --- | --- |
| `gym_id` | `INT` | `PRIMARY KEY` | | 
| `building_id` | `INT` | `NOT NULL` `UNIQUE` | Foreign Key |
| `gym_type` | `VARCHAR(50)` | `NOT NULL` | e.g. Fire |
| `badge_name` | `VARCHAR(100)` | `NOT NULL` | |
| `gym_leader` | `VARCHAR(100)` | `NOT NULL` | | 
| `gym_order` | `INT` | `NOT NULL` | 1-8 gym progression order |

### 3.7 `FavoriteLocation`

| **Column** | **Type** | **Constraints** | **Notes** |
| --- | --- | --- | --- |
| `favorite_id` | `INT` | `PRIMARY KEY` | | 
| `user_id` | `INT` | `NOT NULL` | Foreign Key |
| `location_id` | `INT` | `NOT NULL` | Foreign Key |
| `created_at` | `TIMETSTAMP` | `NOT NULL` `DEFAULT NOW` | |
| | | `UNIQUE (user_id, location_id)`| Prevents duplicates |

### 3.8 `FavoriteBuilding`

| **Column** | **Type** | **Constraints** | **Notes** |
| --- | --- | --- | --- |
| `favorite_id` | `INT` | `PRIMARY KEY` | | 
| `user_id` | `INT` | `NOT NULL` | Foreign Key |
| `building_id` | `INT` | `NOT NULL` | Foreign Key |
| `created_at` | `TIMETSTAMP` | `NOT NULL` `DEFAULT NOW` | |
| | | `UNIQUE (user_id, building_id)`| Prevents duplicates |

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
| `500 Internal Serve Error` | Database unreachable |

### 4.2 Auth
 
No authentication required.
 
#### `POST /auth/register`
 
Register a new user account.
 
**Request:**
```json
{
  "email": "ash@pokemon.com",
  "password": "pikachu123"
}
```
 
| **Status** | **Response** |
|---|---|
| `201 Created` | `{ "userId": 1, "email": "ash@pokemon.com" }` |
| `400 Bad Request` | Validation errors |
| `409 Conflict` | Email already registered |
 
#### `POST /auth/login` (NOTE: TBD ON TOKEN ISSUING, THIS IS A POSSIBILITY)
 
Authenticate and receive a token.
 
**Request:**
```json
{
  "email": "ash@pokemon.com",
  "password": "pikachu123"
}
```
 
| **Status** | **Response** |
|---|---|
| `200 OK` | `{ "token": "<bearer_token>" }` |
| `401 Unauthorized` | Invalid email or password |

### 4.3 Locations
 
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
 
Delete a location and all associated images, buildings, gyms, and favorites.
 
| **Status** | **Response** |
|---|---|
| `204 No Content` | Deleted |
| `404 Not Found` | Location does not exist |

### 4.4 Location Images
 
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

### 4.5 Buildings
 
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
 
Delete a building and its gym record (if applicable) and favorites.
 
| **Status** | **Response** |
|---|---|
| `204 No Content` | Deleted |
| `404 Not Found` | Location or building does not exist |

### 4.6 Gyms
 
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

### 4.7 Favorite Locations
 
All favorite endpoints the authenticated user's (determined by Bearer token).
 
#### `GET /favorites/locations`
 
Retrieve the current user's favorited locations.
 
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
|---|---|
| `201 Created` | `{ "favoriteId": 1, "locationId": 1, "createdAt": "..." }` |
| `404 Not Found` | Location does not exist |
| `409 Conflict` | Already favorited |
 
#### `DELETE /favorites/locations/{locationId}`
 
Remove a location from the current user's favorites.
 
| **Status** | **Response** |
|---|---|
| `204 No Content` | Removed |
| `404 Not Found` | Location not in favorites |

### 4.8 Favorite Buildings
 
#### `GET /favorites/buildings`
 
Retrieve the current user's favorited buildings.
 
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
|---|---|
| `201 Created` | `{ "favoriteId": 1, "buildingId": 4, "createdAt": "..." }` |
| `404 Not Found` | Building does not exist |
| `409 Conflict` | Already favorited |
 
#### `DELETE /favorites/buildings/{buildingId}`
 
Remove a building from the current user's favorites.
 
| **Status** | **Response** |
|---|---|
| `204 No Content` | Removed |
| `404 Not Found` | Building not in favorites |

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
| `409` | Conflict | Duplicate (e.g., email, favorite) |
| `500` | Internal Server Error | Unexpected failure |

## 6. Data Validation Rules
 
These rules are enforced by the API and covered by unit tests.
 
**Users:**
- `email` — required, valid email format, max 255 characters, must be unique
- `password` — required, minimum 8 characters
 
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
 
**Favorites:**
- Target resource (`locationId` or `buildingId`) must exist
- A user cannot favorite the same resource twice

## 7. Future Consideration (brief)
- Potentially have Pokémon types for locations
    - Would require additional logic, tables, endpoints, and minor restructuring of responses
- Revisit database architecture (2 DBs required?), as mentioned above
- API versioning when integrating with another team's API
- Integration with other teams API