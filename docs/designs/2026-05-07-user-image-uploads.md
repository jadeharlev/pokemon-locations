# SE498-68: Per-User Image Uploads — Design

**Status:** Draft, pending implementation
**Ticket:** SE498-68 (Jira)
**Author:** Maks Popov
**Date:** 2026-05-07

## 1. Goal

Let an authenticated user upload their own photos for any location and see those photos appear in that location's gallery alongside the canonical screenshots from SE498-14. Each user has a private gallery on each location — they only see their own uploads, never anyone else's.

## 2. User-Facing Requirements

| Aspect | Decision |
|---|---|
| Privacy | Per-user private. User A's uploads are visible only to user A. |
| Per-file size cap | 10 MB (covers any reasonable phone photo) |
| Per-location count cap | 20 images per user per location |
| Total per-user cap | None |
| Allowed file types | `image/png`, `image/jpeg`, `image/webp` (HEIC explicitly out of scope — no browser support without server-side conversion, deferred to a future ticket) |
| Upload UX | Themed "+" button as the primary affordance, drag-drop on the gallery box as a bonus path. Multi-select supported on both paths. |
| Delete UX | Hover-revealed "X" on user-uploaded slides only (canonical covers don't get the X). Native `confirm()` before delete. |
| At-cap behavior | Upload button disabled at 20; server still validates and returns 400 if a request slips through |
| Server-side processing | Auto-resize at upload via SkiaSharp: longest edge clamped to 2000 px, format preserved |

## 3. Architecture

### 3.1 The Two-Database Pattern (Unchanged)

User uploads are **per-user state**, so:
- File bytes live in the `webserver_uploads` named volume (already provisioned in `docker-compose.debug.yml`, mounted at `/app/uploads`).
- Metadata (one row per upload) lives in the **WebServer Postgres** database.
- The **API Postgres** database stays untouched. No new API-side migrations.

This preserves the project's invariant: API DB is read-only canonical content; WebServer DB is per-user state. No cross-DB joins.

### 3.2 On-Disk Layout

```
/app/uploads/{userId}/{imageUuid}.{ext}
```

Per-user subdirectories make ownership inspectable on disk and per-user cleanup trivial. Filenames use the same UUID as the DB primary key, so there's a one-to-one mapping. Extension matches the format SkiaSharp actually wrote (not the Content-Type the client claimed).

### 3.3 Image Serving (Decision: F2 — Auth-Streamed via Blob URLs)

User-uploaded images are served through an **authenticated streaming endpoint** (`GET /api/me/locations/{locationId}/images/{imageId}`). The endpoint verifies ownership on every request and streams the file bytes back.

The frontend cannot use `<img src="/api/me/...">` directly because the existing app suppresses the `WWW-Authenticate` header (`SuppressWWWAuthenticateHeader = true` in `Program.cs`), so the browser never caches Basic Auth credentials and `<img>` requests would arrive with no Authorization header.

Instead, the frontend pattern is:

1. `fetch(authStreamUrl, { headers: { Authorization } })` — adds Basic Auth manually via the existing `PLAuth.authFetch`.
2. `await res.blob()` — get image bytes.
3. `URL.createObjectURL(blob)` — create a per-document blob URL.
4. Set `<img src>` to the blob URL.
5. Track the blob URL; revoke it when the slide is removed (location change, delete, gallery teardown) to free memory.

**Trade-off accepted:** No native HTTP image cache for user images — each visit re-downloads the bytes. With max 20 images × ~1 MB resized = ~20 MB worst case per location view, this is negligible.

Canonical images (from SE498-14) stay on the static-files path (`/images/route-1.png`) — they're public, browser-cached, no auth required. Mixed strategy: hybrid serving as approved (A3 → F2 implementation).

## 4. Database Schema

### 4.1 New Migration

`Backend/PokemonLocations.WebServer/Database/Migrations/0010_create_user_images_table.sql`:

```sql
CREATE TABLE user_images (
    image_id          UUID PRIMARY KEY,
    user_id           INTEGER NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    location_id       INTEGER NOT NULL,
    file_path         VARCHAR(500) NOT NULL,
    original_filename VARCHAR(255) NOT NULL,
    content_type      VARCHAR(50)  NOT NULL,
    byte_size         INTEGER      NOT NULL,
    uploaded_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX ix_user_images_user_location
    ON user_images (user_id, location_id, uploaded_at DESC);
```

**Schema rationale:**

- **`image_id UUID`** — needed as the unguessable identifier for the auth-streaming URL. Using UUID for both PK and disk filename keeps the mapping one-to-one. Auto-incrementing INT would expose enumerable IDs.
- **`location_id` is *not* a foreign key.** The API DB is the source of truth for locations and we never cross DBs. Validation that the locationId exists happens at upload time via the existing `IPokemonLocationsApiClient.ExistsAsync`, the same pattern `VisitedController` uses.
- **`ON DELETE CASCADE` on `user_id`** — when a user deletes their account, their image rows go away automatically. Disk file cleanup is handled in the existing account-deletion flow (Section 8.4).
- **Composite index** — `(user_id, location_id, uploaded_at DESC)` makes the gallery query fast: "all of user X's images at location Y, newest first."
- **`content_type`** stores the format SkiaSharp actually encoded to, not what the client claimed. Authoritative.

### 4.2 Repository Contract

`Backend/PokemonLocations.WebServer/Database/Repositories/IUserImageRepository.cs`:

```csharp
public interface IUserImageRepository {
    // Inserts inside a SERIALIZABLE transaction with cap re-check.
    // Returns Success on insert. AtCap if user already has >= cap images at the location.
    // Throws PostgresException(SqlState=40001) on serialization conflict — controller catches and retries.
    Task<AddResult> AddAsync(UserImage image, int locationCap);

    // Newest-first by uploaded_at, scoped to (user, location).
    Task<IReadOnlyList<UserImage>> GetForUserAndLocationAsync(int userId, int locationId);

    // Returns row when (imageId, userId) matches; null otherwise.
    Task<UserImage?> GetByIdForUserAsync(int userId, Guid imageId);

    // Idempotent: 0-row delete is success.
    Task RemoveAsync(int userId, Guid imageId);

    Task<int> CountForUserAndLocationAsync(int userId, int locationId);
}

public enum AddResult { Success, AtCap }

public record UserImage(
    Guid ImageId,
    int UserId,
    int LocationId,
    string FilePath,
    string OriginalFilename,
    string ContentType,
    int ByteSize,
    DateTimeOffset UploadedAt);
```

The `AddAsync` race-handling pattern: cap-check + insert happen inside one SERIALIZABLE transaction; conflicts surface as `PostgresException` with SqlState `40001`, which the controller catches for retry (Section 5.1 step 6).

## 5. API Surface

All new endpoints live on the WebServer (BFF), require Basic Auth, and operate as the `currentUser`.

### 5.1 `POST /api/me/locations/{locationId}/images`

Upload a single file. Frontend batches sequential POSTs for multi-file uploads.

**Request:** `multipart/form-data` with one `file` field.

**Validation (in order):**
1. Location exists per `IPokemonLocationsApiClient.ExistsAsync($"/locations/{locationId}")` → 404 (`location_not_found`) if not.
2. MIME type ∈ {`image/png`, `image/jpeg`, `image/webp`} → 400 (`unsupported_media_type`) if not.
3. `file.Length > 10 * 1024 * 1024` → 400 (`file_too_large`). Framework-level cap (`MaxRequestBodySize`) is set to **12,582,912 bytes (12 MB exactly)** which rejects oversized request bodies with 413 before this check runs. Multipart-specific limits also need explicit configuration: `FormOptions.MultipartBodyLengthLimit` and `FormOptions.MultipartHeadersLengthLimit` set in `Program.cs` to match.
4. **Pre-decode count check** (fail-fast): user's current count for this location ≥ 20 → 400 (`cap_reached`). Not race-safe but provides fast rejection before SkiaSharp work.
5. **SkiaSharp pipeline** (Section 6) → may return 415 (`decode_failed`) or 400 (`decode_bomb`).
6. **Race-safe insert** via `IUserImageRepository.AddAsync`: opens SERIALIZABLE transaction, re-checks count, inserts row. Behavior:
   - Returns `AddResult.Success` → controller responds 201.
   - Returns `AddResult.AtCap` → controller deletes the file already written, responds 400 (`cap_reached`).
   - Throws `PostgresException(SqlState="40001")` on serialization conflict → controller retries the call **once**. If retry returns Success/AtCap, behave normally. If retry also throws conflict, controller deletes the file and responds 409 (`serialization_conflict`). The retry is **server-side only** — clients never observe a transient 409 absent a real second-conflict, which is vanishingly rare.

**Response 201:**

```json
{
  "imageId": "550e8400-e29b-41d4-a716-446655440000",
  "imageUrl": "/api/me/locations/1/images/550e8400-e29b-41d4-a716-446655440000",
  "originalFilename": "viridian-route.png",
  "uploadedAt": "2026-05-07T18:42:11Z"
}
```

**Error codes (full matrix in Section 8):** 400, 404, 409, 413, 415, 500.

### 5.2 `DELETE /api/me/locations/{locationId}/images/{imageId}`

**Validation:** row exists, `user_id` matches current user, `location_id` in row matches URL parameter.

**Operation:**
1. Delete DB row (inside a small transaction).
2. After commit, `File.Delete` from disk. If file delete fails, log it — DB row is already gone, file is now an orphan but never referenced. Don't fail the response.

**Response 204** (idempotent: a second DELETE on the same id returns 404 cleanly).

### 5.3 `GET /api/me/locations/{locationId}/images/{imageId}` — Auth-Streamed Image

**Validation:** row exists, owned by current user, location matches URL.

**Operation:** open file at `row.file_path`, stream bytes back with:
- `Content-Type: <row.content_type>`
- `Cache-Control: private, max-age=3600` (frontend won't reuse this anyway because of blob URL, but private caching is correct in case we change strategy later)

**Response 200** with image bytes. **404** if row doesn't exist, isn't owned, or location doesn't match. **404** also if the row exists but the file is missing on disk (orphan-on-read, must be tested explicitly).

### 5.4 Modified Existing Endpoint

`GET /api/locations/{id}` already adds `visited` and currently stubs `userImages: []`. The stub is replaced with a real query against the new `IUserImageRepository`. Response shape:

```json
{
  "locationId": 1,
  "name": "Pallet Town",
  "description": "...",
  "videoUrl": null,
  "images": [/* canonical, from API DB — unchanged */],
  "userImages": [
    {
      "imageId": "uuid",
      "imageUrl": "/api/me/locations/1/images/uuid",
      "originalFilename": "my-screenshot.png",
      "uploadedAt": "2026-05-07T18:42:11Z"
    }
  ],
  "visited": true
}
```

`userImages` is **ordered newest-first** (matches the `(user_id, location_id, uploaded_at DESC)` index).

## 6. Image Processing Pipeline (SkiaSharp)

### 6.1 Dependencies

- `SkiaSharp` (MIT-licensed, Microsoft-maintained)
- `SkiaSharp.NativeAssets.Linux` (Linux native binaries for the WebServer container)

### 6.2 Pipeline Steps

```csharp
// 1. Decode bomb prevention — read header before allocating pixels
using var codec = SKCodec.Create(stream);
if (codec is null) return 415;                              // not decodable
if (codec.EncodedFormat is not (Png|Jpeg|Webp)) return 415; // mislabeled MIME, real format unsupported
if (codec.Info.Width * codec.Info.Height > 50_000_000) return 400; // >50 MP rejected

// 2. Decode pixels (now safe)
using var bitmap = SKBitmap.Decode(codec);

// 3. Resize if needed
var longest = Math.Max(bitmap.Width, bitmap.Height);
SKBitmap final = bitmap;
if (longest > 2000) {
    var scale = 2000.0 / longest;
    var newW = (int)(bitmap.Width * scale);
    var newH = (int)(bitmap.Height * scale);
    final = bitmap.Resize(new SKImageInfo(newW, newH),
        new SKSamplingOptions(SKCubicResampler.Mitchell));
}

// 4. Re-encode in original format with quality 85 (PNG ignores quality, lossless)
using var image = SKImage.FromBitmap(final);
using var data = image.Encode(codec.EncodedFormat, 85);

// 5. Ensure user dir exists, atomic write
Directory.CreateDirectory(userDir);   // idempotent
var tempPath = Path.Combine(userDir, $"{uuid}.tmp");
var finalPath = Path.Combine(userDir, $"{uuid}.{ext}");
File.WriteAllBytes(tempPath, data.ToArray());
File.Move(tempPath, finalPath);       // atomic on POSIX same-volume

// 6. Open SERIALIZABLE transaction, recheck cap, insert row.
//    On serialization conflict: catch, delete finalPath, return 409.
//    On any other failure after file write: catch, delete finalPath, surface error.
```

### 6.3 Pipeline Notes

- **Sampling quality:** `SKCubicResampler.Mitchell` is good enough for photo content; default nearest-neighbor would alias badly.
- **EXIF stripping:** SkiaSharp's encoder strips metadata by default. Free privacy benefit (GPS coords from phone photos won't survive the resize).
- **Format preservation:** PNG → PNG, JPEG → JPEG, WebP → WebP. Quality 85 for lossy formats; PNG ignores the quality parameter (lossless).
- **Native binary verification:** `SkiaSharp.NativeAssets.Linux` must resolve correctly inside the WebServer Dockerfile. Tests must include an end-to-end smoke that exercises the encoder inside the testcontainer to catch packaging issues at CI time.

### 6.4 Service Placement

The pipeline lives behind an interface for testability:

- **`Backend/PokemonLocations.WebServer/Services/IImageProcessor.cs`** — interface with one method: `Task<ProcessedImage> ProcessAsync(Stream input, CancellationToken ct)`. Returns the encoded bytes + the format actually written + final dimensions, or throws specific exceptions (`UnsupportedFormatException`, `DecodeFailedException`, `DecodeBombException`).
- **`Backend/PokemonLocations.WebServer/Services/ImageProcessor.cs`** — concrete SkiaSharp implementation.
- **Controller depends on `IImageProcessor`** — pipeline tests exercise the implementation directly with byte fixtures; controller tests can substitute a mock to exercise validation/error paths without invoking SkiaSharp.

### 6.5 File Extension Mapping

When writing files to disk, the extension is derived from the SkiaSharp-detected `EncodedFormat`, not the client's MIME claim:

| `SKEncodedImageFormat` | File extension |
|---|---|
| `Png` | `.png` |
| `Jpeg` | `.jpg` (chosen over `.jpeg` for URL brevity) |
| `Webp` | `.webp` |

The `content_type` column stores the canonical MIME (`image/png`, `image/jpeg`, `image/webp`).

## 7. Frontend Integration

### 7.1 Existing State

- `script.js:renderGallery(galleryEl, images, locationName)` already concatenates `images + userImages` and renders them through the carousel.
- `index.html` has the existing gallery markup with prev/next arrows, modal, etc.
- `auth.js:PLAuth.authFetch(path, options)` already attaches Basic Auth from `sessionStorage`.

### 7.2 New DOM Elements

In `index.html`, inside `.image-gallery`:

- **`<button class="gallery-upload">`** — circular "+" in the bottom-right, themed like prev/next arrows. `disabled` when `userImages.length >= 20`. Tooltip when disabled: "20-image limit reached".
- **Hidden `<input type="file" multiple accept="image/png,image/jpeg,image/webp">`** — clicked programmatically when the upload button is clicked.
- **Drag overlay:** absolutely-positioned div, hidden by default, shown during a `dragover` with a dashed border and "Drop to upload" text.

Per-slide additions, **only when slide is user-uploaded**:

- **`<button class="slide-delete">`** — top-right "X", hover-revealed, `var(--theme-danger)` color.

The `renderGallery` function differentiates user-uploaded vs canonical by source array — items from `location.images` get no delete button; items from `location.userImages` get one.

### 7.3 Loading User Images (F2 Pattern)

```js
let activeBlobUrls = [];

async function loadUserImageBlob(imageUrl) {
    const res = await PLAuth.authFetch(imageUrl);
    if (!res.ok) throw new Error(`Image fetch failed: ${res.status}`);
    const blob = await res.blob();
    const blobUrl = URL.createObjectURL(blob);
    activeBlobUrls.push(blobUrl);
    return blobUrl;
}

function teardownGallery() {
    // ...existing teardown (carousel timer, etc.)
    activeBlobUrls.forEach(URL.revokeObjectURL);
    activeBlobUrls = [];
}
```

When a user-uploaded slide is created, `<img src>` is initially empty; `loadUserImageBlob(image.imageUrl)` runs; on resolve, the result is set as `src`. On rejection, slide stays empty (logged to console, doesn't break the rest of the gallery).

**Important:** `imageUrl` for a user image (`/api/me/locations/{locationId}/images/{imageId}`) is a **fetch URL, not a renderable URL**. Wiring it directly to `<img src>` won't work — the auth-streaming endpoint requires the `Authorization` header, which `<img>` doesn't attach. Always go through the `loadUserImageBlob` indirection. Canonical images (`/images/route-1.png`) ARE renderable URLs and bypass the indirection.

The expand modal reuses the slide's blob URL — no separate fetch on click.

### 7.4 Upload Flow

Triggered by either button click or drag-drop. Both paths converge:

1. **Get a `FileList`.**
2. **Client-side filter:** drop entries whose `type` isn't in the allowlist or whose `size > 10 MB`. Collect rejected ones for end-of-batch report.
3. **Cap check:** `currentCount + acceptedCount > 20` → trim batch to fit, mark overflow as rejected.
4. **Sequential POSTs** to `/api/me/locations/{locationId}/images`. One at a time. Mid-batch failure does not stop the batch; failures are collected.
5. **Per-success:** prepend the returned image record to `location.userImages`, call `renderGallery` again with the merged list, kick off `loadUserImageBlob` for the new slide.
6. **End-of-batch toast:** counts successes and failure categories. Auto-dismiss after 4 s on full success; persistent until user-dismissed if any failures.

Toast message format: `"3 uploaded · 1 skipped (too large) · 1 skipped (would exceed 20-image limit)"`

**Filename display sanitization:** when surfacing per-file errors in toast/`<img alt>`, truncate `originalFilename` to 80 chars and HTML-escape (DOM `textContent` does this automatically; if interpolating into innerHTML, use a textNode). Exotic Unicode (emoji, RTL) renders fine; control characters are stripped by the browser's text rendering.

### 7.5 Delete Flow

1. User clicks the slide's "X" → native `confirm("Delete this image?")`.
2. On confirm: `DELETE /api/me/locations/{locationId}/images/{imageId}` via `authFetch`.
3. On 204: revoke the slide's blob URL, remove image from `location.userImages`, call `renderGallery` again.
4. On 404 or other failure: surface a brief error toast, no in-memory mutation.

### 7.6 Drag-Drop Event Handling

On the `.image-gallery` element:

- `dragenter` / `dragover`: `e.preventDefault()` (required to allow drop), add `.drag-active` class to show the overlay.
- `dragleave`: remove the class with a slight delay (handles flicker between child elements).
- `drop`: `e.preventDefault()`, read `e.dataTransfer.files`, run the same upload flow as the button.

## 8. Error Handling

### 8.1 Server Error Codes

Error response body matches the existing project convention: `{ "error": "snake_case_code" }` (consistent with `email_taken`, `unknown_planet`, `invalid_badge` already in use elsewhere).

| Code | Cause | Response body |
|---|---|---|
| `400` | `file_too_large` (post-multipart-parse, > 10 MB) · `unsupported_media_type` (MIME not in allowlist) · `decode_bomb` (decoded dimensions > 50 MP) · `cap_reached` (pre-decode or in-transaction count cap) | `{ "error": "<code>" }` |
| `404` | `location_not_found` (locationId not in API DB) · `not_found` (image doesn't exist OR isn't owned OR location-id mismatch on DELETE/GET · orphan-on-read where DB row points to missing file) | `{ "error": "<code>" }` |
| `409` | `serialization_conflict` — SERIALIZABLE conflict still occurred after the controller's one internal retry (vanishingly rare in practice) | `{ "error": "serialization_conflict" }` |
| `413` | Request body exceeds `MaxRequestBodySize` (12,582,912 bytes / 12 MB) — rejected by ASP.NET pipeline before the controller runs | empty (framework default) |
| `415` | `decode_failed` — SkiaSharp can't decode bytes, OR decoded format outside the allowlist (e.g., TIFF mislabeled as PNG) | `{ "error": "decode_failed" }` |
| `500` | Disk write failure, DB exception, unexpected error | `{ "error": "server_error" }` (full detail logged server-side, never returned) |

### 8.2 Frontend per-File Behavior

- **Success (201):** prepend to `userImages`, re-render.
- **400 / 413 / 415:** collect for end-of-batch toast, mapping the snake_case code to a human message (`file_too_large` → `"X.png — too large"`, `decode_failed` → `"X.png — couldn't read image"`, etc.).
- **404 (`location_not_found`):** rare in practice; toast says "location not found, refresh the page" and stops further uploads in this batch.
- **409 (`serialization_conflict`):** server has already retried once internally. Treat as a transient failure surfaced in the toast (`"X.png — temporary conflict, try again"`). Do not retry on the client.
- **500 (`server_error`):** log to console, surface generic "server error" message in toast. Continue the batch.

### 8.3 Auth-Stream Fetch Failures (Image Loads)

Image load fetches use `PLAuth.authFetch`, which already handles session-expiry redirect (delegate to existing path). On other errors the image stays empty and a console error is logged — doesn't break the rest of the gallery.

### 8.4 Account Deletion

The existing `DELETE /account` endpoint (`AccountController.Delete` → `userRepository.DeleteAsync(userId)`) handles row cleanup via the `user_id` FK cascade — `user_images` rows go away automatically.

What we need to add: extend `UserRepository.DeleteAsync` (or, if cleaner, the controller action itself) to also remove the user's upload directory after the DB transaction commits:

```csharp
var userDir = Path.Combine(uploadRoot, userId.ToString());
if (Directory.Exists(userDir)) {
    try { Directory.Delete(userDir, recursive: true); }
    catch (IOException ex) { logger.LogWarning(ex, "Failed to delete upload dir for user {UserId}", userId); }
}
```

If file deletion fails, log and continue — DB rows are gone, the orphan files are never referenced and harmless. This addition needs its own test in `UserRepositoryTests` (or `AccountControllerTests`) verifying the directory is removed on account deletion.

## 9. Testing Strategy

TDD throughout the backend. No JS tests (consistent with project pattern; manual verification in browser).

### 9.1 Implementation Order

Each phase is independently green-able:

1. **`UserImageRepository`** — write repo tests, implement.
2. **SkiaSharp pipeline** (pure unit tests, no DB) — write pipeline tests, implement.
3. **Endpoint happy path** (POST/DELETE/GET) — endpoint tests, implement controller stubs.
4. **Endpoint validation cases** — add tests, add validation.
5. **Modified `LocationsController.GetById`** — update existing test, wire repo into proxy logic.
6. **Frontend** (manual, no TDD).

### 9.2 Test Inventory

**`UserImageRepositoryTests`** (`Backend/PokemonLocations.WebServer.Tests/Database/`):

- `AddAsync` persists row with all fields populated.
- `GetForUserAndLocationAsync` returns user's images for a location, newest-first.
- `GetForUserAndLocationAsync` does not return another user's images for the same location.
- `GetByIdForUserAsync` returns row when owned, `null` otherwise.
- `RemoveAsync` deletes when owned, no-op when not.
- `CountForUserAndLocationAsync` returns correct count, scoped to user + location.
- Cascade: deleting a user removes their image rows.

**Image processing pipeline tests** (`Backend/PokemonLocations.WebServer.Tests/Imaging/`):

- Resize: 4000×3000 input → output longest edge is exactly 2000 (boundary).
- Resize: 1500×1000 input → not resized, dimensions unchanged (under threshold).
- Resize: 2000×1500 input → not resized (boundary, equality is *not* > threshold).
- Resize: 3000×4000 input (portrait) → 1500×2000 output (height-driven scale).
- Format preservation per format: PNG in → PNG out; JPEG in → JPEG out; WebP in → WebP out.
- Format detection: TIFF bytes saved with `.png` extension and `image/png` MIME → rejected.
- Decode-bomb cap: 8000×8000 (64 MP) image rejected before full decode.

**`UserImagesControllerTests`** (`Backend/PokemonLocations.WebServer.Tests/Controllers/`):

- All endpoints return 401 without Basic Auth.
- `POST` with valid PNG → 201, response shape correct, file written at expected path on disk, DB row present.
- `POST` with valid JPEG → 201 (per-format coverage).
- `POST` with valid WebP → 201 (per-format coverage).
- `POST` with file >10 MB (post-parse) → 400.
- `POST` with body >12 MB (pre-parse) → 413 (separate codepath).
- `POST` with `image/tiff` MIME → 400.
- `POST` for non-existent location → 404.
- `POST` when user already at 20 cap → 400 (pre-decode fail-fast).
- `POST` with corrupt bytes despite valid MIME → 415.
- `POST` with image dimensions exceeding 50 MP → 400.
- `POST` 409-retry-success: mocked repository throws `PostgresException(SqlState="40001")` on first call, returns Success on second → endpoint returns **201** (verifies the controller's internal retry handler).
- `POST` 409-retry-still-conflict: mocked repository throws on both calls → endpoint returns **409** + cleanup of file already on disk (verifies retry budget is bounded to one).
- `DELETE` on owned image → 204; row + file gone afterward.
- `DELETE` on another user's image → 404.
- `DELETE` is idempotent (second 404).
- `GET` on owned image → 200 with correct Content-Type and bytes.
- `GET` on another user's image → 404.
- `GET` on owned image whose file has been deleted from disk → 404 (orphan-on-read).

**Modified `LocationsControllerTests`:**

- `userImages` array now contains the user's actual uploads (replace the existing "always empty" assertion).
- Other users' uploads do not appear.
- Newest-first ordering preserved through the proxy.

### 9.3 Test Infrastructure Additions

- **Configurable upload root.** `UserImages:UploadRoot` setting in `appsettings.json` defaults to `/app/uploads`. `WebApplicationFactory` overrides to a per-test temp directory in test setup; teardown cleans it up.
- **Test fixtures for sample image bytes.** Small valid PNG, JPEG, WebP byte arrays embedded as test resources. Plus crafted "TIFF as PNG", "decode bomb", "corrupt bytes" fixtures.
- **SkiaSharp container smoke test.** End-to-end POST inside the testcontainer setup catches `SkiaSharp.NativeAssets.Linux` packaging issues at CI time.

## 10. Wiring & Configuration

### 10.1 New DI Registrations (`Program.cs`)

```csharp
builder.Services.AddSingleton<IUserImageRepository, UserImageRepository>();
builder.Services.AddSingleton<IImageProcessor, ImageProcessor>();

builder.Services.Configure<UserImagesOptions>(
    builder.Configuration.GetSection("UserImages"));

builder.Services.Configure<FormOptions>(opts => {
    opts.MultipartBodyLengthLimit = 12_582_912;
    opts.MultipartHeadersLengthLimit = 32_768;
});
builder.WebHost.ConfigureKestrel(opts => {
    opts.Limits.MaxRequestBodySize = 12_582_912;
});
```

`Singleton` lifetime matches the project's existing pattern for repositories.

### 10.2 New Configuration Keys (`appsettings.json`)

```json
"UserImages": {
  "UploadRoot": "/app/uploads",
  "MaxFilesPerLocation": 20,
  "MaxBytesPerFile": 10485760,
  "MaxPixelsPerImage": 50000000,
  "ResizeLongestEdge": 2000
}
```

`UserImagesOptions` POCO binds these. Tests override `UploadRoot` per test to a temp dir; the other values stay as defaults to keep tests aligned with production behavior.

### 10.3 New Files Created

```
Backend/PokemonLocations.WebServer/
├── Controllers/UserImagesController.cs            (new)
├── Database/Migrations/0010_create_user_images_table.sql  (new)
├── Database/Repositories/IUserImageRepository.cs  (new)
├── Database/Repositories/UserImageRepository.cs   (new)
├── Models/UserImage.cs                            (new)
├── Models/Responses/UploadedImageResponse.cs      (new)
├── Models/UserImagesOptions.cs                    (new)
├── Services/IImageProcessor.cs                    (new)
└── Services/ImageProcessor.cs                     (new)
```

### 10.4 Files Modified

- `Backend/PokemonLocations.WebServer/Program.cs` — DI + Form/Kestrel limits
- `Backend/PokemonLocations.WebServer/appsettings.json` — `UserImages` config block
- `Backend/PokemonLocations.WebServer/Controllers/LocationsController.cs` — populate `userImages` from new repo
- `Backend/PokemonLocations.WebServer/Database/Repositories/UserRepository.cs` (or `AccountController.Delete`) — disk cleanup on account deletion
- `Backend/PokemonLocations.WebServer/wwwroot/index.html` — upload button, hidden file input, drag overlay, slide delete button styling
- `Backend/PokemonLocations.WebServer/wwwroot/script.js` — `renderGallery` upgrades, `loadUserImageBlob`, upload flow, delete flow, drag-drop handlers, blob URL tracking & cleanup

### 10.5 New Tests Created

```
Backend/PokemonLocations.WebServer.Tests/
├── Controllers/UserImagesControllerTests.cs       (new)
├── Database/UserImageRepositoryTests.cs           (new)
└── Imaging/ImageProcessorTests.cs                 (new)
```

Plus: existing `LocationsControllerTests.cs` + `UserRepositoryTests.cs` get new test methods (modified, not replaced).

## 11. Out of Scope

- HEIC support (deferred — no native browser support without server-side conversion).
- Image thumbnails (the carousel renders the resized 2000-px-max image; modal also uses it. Single artifact per upload).
- Per-user total-bytes cap.
- Public sharing (uploads stay private to the uploading user forever).
- Caption editing on user uploads.
- Reordering user images (always newest-first).
- Bulk delete UI.
- Server-side periodic orphan-file reaper (acceptable at our scale).
- Real-world concurrent SERIALIZABLE conflict tests (non-deterministic; trust Postgres + unit-test the retry handler).
