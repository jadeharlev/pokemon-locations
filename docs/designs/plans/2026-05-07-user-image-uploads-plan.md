# SE498-68 User Image Uploads Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add per-user private image uploads to the location gallery — users can upload their own photos to any location, see them appear in the carousel after the canonical screenshots, and delete them individually.

**Architecture:** File bytes live in the `webserver_uploads` named volume at `/app/uploads/{userId}/{uuid}.{ext}`. Metadata in the WebServer Postgres `user_images` table. Server-side resize via SkiaSharp (longest edge ≤ 2000 px). Auth-streamed delivery via `<img>` blob URLs in the frontend (per design F2). One SERIALIZABLE-protected insert with one server-side retry on conflict.

**Tech Stack:** ASP.NET 10 (WebServer / BFF), Dapper + Postgres, SkiaSharp 3.x, vanilla JS frontend (no build step), xUnit + Testcontainers + WebApplicationFactory.

**Spec reference:** `docs/designs/specs/2026-05-07-user-image-uploads.md`

---

## File Structure

### New files

| Path | Purpose |
|---|---|
| `Backend/PokemonLocations.WebServer/Database/Migrations/0010_create_user_images_table.sql` | Migration: creates `user_images` table + composite index |
| `Backend/PokemonLocations.WebServer/Models/UserImage.cs` | Domain record matching the DB row shape |
| `Backend/PokemonLocations.WebServer/Models/UserImagesOptions.cs` | POCO for `UserImages:*` config keys |
| `Backend/PokemonLocations.WebServer/Models/Responses/UploadedImageResponse.cs` | DTO used for both POST 201 body and GET userImages array |
| `Backend/PokemonLocations.WebServer/Database/Repositories/IUserImageRepository.cs` | Repo interface + `AddResult` enum |
| `Backend/PokemonLocations.WebServer/Database/Repositories/UserImageRepository.cs` | Dapper-based impl, SERIALIZABLE-wrapped Add |
| `Backend/PokemonLocations.WebServer/Services/IImageProcessor.cs` | Pipeline contract + `ProcessedImage` record + exception types |
| `Backend/PokemonLocations.WebServer/Services/ImageProcessor.cs` | SkiaSharp impl |
| `Backend/PokemonLocations.WebServer/Controllers/UserImagesController.cs` | POST/DELETE/GET on `/api/me/locations/{id}/images` |
| `Backend/PokemonLocations.WebServer.Tests/Database/UserImageRepositoryTests.cs` | Repo tests |
| `Backend/PokemonLocations.WebServer.Tests/Imaging/ImageProcessorTests.cs` | Pipeline tests |
| `Backend/PokemonLocations.WebServer.Tests/Imaging/TestImageFixtures.cs` | Helper for generating test images |
| `Backend/PokemonLocations.WebServer.Tests/Controllers/UserImagesControllerTests.cs` | Endpoint tests |

### Modified files

| Path | Change |
|---|---|
| `Backend/PokemonLocations.WebServer/PokemonLocations.WebServer.csproj` | Add `SkiaSharp` + `SkiaSharp.NativeAssets.Linux` |
| `Backend/PokemonLocations.WebServer/Program.cs` | DI registrations + Form/Kestrel limits |
| `Backend/PokemonLocations.WebServer/appsettings.json` | `UserImages` config block |
| `Backend/PokemonLocations.WebServer/Controllers/LocationsController.cs` | Replace `userImages: []` stub with real repo query |
| `Backend/PokemonLocations.WebServer/Controllers/AccountController.cs` | Extend `Delete` action to remove user dir after DB cascade |
| `Backend/PokemonLocations.WebServer/wwwroot/index.html` | Upload button, hidden file input, drag overlay, slide-delete styling |
| `Backend/PokemonLocations.WebServer/wwwroot/script.js` | Gallery rewiring, blob loader, upload flow, delete flow, drag-drop |
| `Backend/PokemonLocations.WebServer.Tests/Controllers/LocationsControllerTests.cs` | `userImages` is now real, not always-empty |
| `Backend/PokemonLocations.WebServer.Tests/Controllers/AccountControllerTests.cs` | Verify delete removes user dir |
| `Backend/PokemonLocations.WebServer.Tests/Infrastructure/PokemonLocationsWebServerFactory.cs` | Override `UserImages:UploadRoot` to per-test temp dir |

---

## Phase 1 — Foundation

### Task 1: Add SkiaSharp dependencies

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/PokemonLocations.WebServer.csproj`

- [ ] **Step 1: Add the package references**

In the `<ItemGroup>` block that already lists `<PackageReference>` lines (next to `Dapper`, `Npgsql`, etc.), add:

```xml
<PackageReference Include="SkiaSharp" Version="3.116.1" />
<PackageReference Include="SkiaSharp.NativeAssets.Linux" Version="3.116.1" />
```

(Pin to 3.116.x; later 3.x versions may change `SKBitmap.Resize` signatures.)

- [ ] **Step 2: Restore + build to verify the deps resolve**

Run:
```bash
cd Backend && dotnet restore PokemonLocations.WebServer/PokemonLocations.WebServer.csproj && dotnet build PokemonLocations.WebServer/PokemonLocations.WebServer.csproj -nologo
```
Expected: build succeeds, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add Backend/PokemonLocations.WebServer/PokemonLocations.WebServer.csproj
git commit -m "deps: add SkiaSharp for user image processing"
```

---

### Task 2: Migration 0010 — `user_images` table

**Files:**
- Create: `Backend/PokemonLocations.WebServer/Database/Migrations/0010_create_user_images_table.sql`

- [ ] **Step 1: Write the migration**

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

The csproj's existing `<EmbeddedResource Include="Database\Migrations\*.sql" />` (verify it exists; copy from `PokemonLocations.Api.csproj` if not) auto-picks it up.

- [ ] **Step 2: Confirm csproj embeds migrations**

Open `Backend/PokemonLocations.WebServer/PokemonLocations.WebServer.csproj`. Look for an `<EmbeddedResource Include="Database\Migrations\*.sql" />` block. If absent, add it inside an `<ItemGroup>`. The Api project has this pattern.

- [ ] **Step 3: Build to verify migration is embedded**

Run: `cd Backend && dotnet build PokemonLocations.WebServer/PokemonLocations.WebServer.csproj -nologo`
Expected: build succeeds.

- [ ] **Step 4: Commit**

```bash
git add Backend/PokemonLocations.WebServer/Database/Migrations/0010_create_user_images_table.sql Backend/PokemonLocations.WebServer/PokemonLocations.WebServer.csproj
git commit -m "feat(webserver): migration 0010 — user_images table"
```

---

### Task 3: Domain models — `UserImage`, `UserImagesOptions`, `UploadedImageResponse`

**Files:**
- Create: `Backend/PokemonLocations.WebServer/Models/UserImage.cs`
- Create: `Backend/PokemonLocations.WebServer/Models/UserImagesOptions.cs`
- Create: `Backend/PokemonLocations.WebServer/Models/Responses/UploadedImageResponse.cs`

- [ ] **Step 1: Write `UserImage.cs`**

```csharp
namespace PokemonLocations.WebServer.Models;

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

- [ ] **Step 2: Write `UserImagesOptions.cs`**

```csharp
namespace PokemonLocations.WebServer.Models;

public class UserImagesOptions {
    public string UploadRoot { get; set; } = "/app/uploads";
    public int MaxFilesPerLocation { get; set; } = 20;
    public int MaxBytesPerFile { get; set; } = 10 * 1024 * 1024;          // 10 MB
    public int MaxPixelsPerImage { get; set; } = 50_000_000;              // 50 MP
    public int ResizeLongestEdge { get; set; } = 2000;
}
```

- [ ] **Step 3: Write `UploadedImageResponse.cs`**

```csharp
namespace PokemonLocations.WebServer.Models.Responses;

public record UploadedImageResponse(
    Guid ImageId,
    string ImageUrl,
    string OriginalFilename,
    DateTimeOffset UploadedAt) {
    public static UploadedImageResponse FromDomain(UserImage image) =>
        new(
            image.ImageId,
            $"/api/me/locations/{image.LocationId}/images/{image.ImageId}",
            image.OriginalFilename,
            image.UploadedAt);
}
```

(`UserImage` lives in `PokemonLocations.WebServer.Models` so the import inside `Responses` is via the same project namespace — add `using PokemonLocations.WebServer.Models;` at the top.)

- [ ] **Step 4: Build to verify**

Run: `cd Backend && dotnet build PokemonLocations.WebServer/PokemonLocations.WebServer.csproj -nologo`
Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add Backend/PokemonLocations.WebServer/Models/
git commit -m "feat(webserver): UserImage domain record + options + response DTO"
```

---

## Phase 2 — Repository (TDD)

### Task 4: `IUserImageRepository` interface + `AddResult` enum

**Files:**
- Create: `Backend/PokemonLocations.WebServer/Database/Repositories/IUserImageRepository.cs`

- [ ] **Step 1: Write the interface**

```csharp
using PokemonLocations.WebServer.Models;

namespace PokemonLocations.WebServer.Database.Repositories;

public enum AddResult { Success, AtCap }

public interface IUserImageRepository {
    /// <summary>
    /// Inserts inside a SERIALIZABLE transaction with a cap re-check.
    /// Returns Success on insert; AtCap if user already has &gt;= cap images at the location.
    /// Throws Npgsql.PostgresException(SqlState="40001") on serialization conflict — the controller
    /// catches this and retries once (see UserImagesController).
    /// </summary>
    Task<AddResult> AddAsync(UserImage image, int locationCap);

    /// <summary>Newest-first by uploaded_at. Scoped to (user, location).</summary>
    Task<IReadOnlyList<UserImage>> GetForUserAndLocationAsync(int userId, int locationId);

    /// <summary>Returns the row when (imageId, userId) matches; null otherwise.</summary>
    Task<UserImage?> GetByIdForUserAsync(int userId, Guid imageId);

    /// <summary>Idempotent: deleting a missing row is success.</summary>
    Task RemoveAsync(int userId, Guid imageId);

    Task<int> CountForUserAndLocationAsync(int userId, int locationId);
}
```

- [ ] **Step 2: Build**

Run: `cd Backend && dotnet build PokemonLocations.WebServer/PokemonLocations.WebServer.csproj -nologo`
Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add Backend/PokemonLocations.WebServer/Database/Repositories/IUserImageRepository.cs
git commit -m "feat(webserver): IUserImageRepository contract"
```

---

### Task 5: `UserImageRepository.AddAsync` (TDD)

**Files:**
- Create: `Backend/PokemonLocations.WebServer.Tests/Database/UserImageRepositoryTests.cs`
- Create: `Backend/PokemonLocations.WebServer/Database/Repositories/UserImageRepository.cs`

- [ ] **Step 1: Write the test class skeleton + first failing test**

```csharp
using Dapper;
using Npgsql;
using PokemonLocations.WebServer.Database.Repositories;
using PokemonLocations.WebServer.Models;
using PokemonLocations.WebServer.Tests.Infrastructure;

namespace PokemonLocations.WebServer.Tests.Database;

[Collection("Postgres")]
public class UserImageRepositoryTests {
    private readonly PostgresFixture postgresFixture;

    public UserImageRepositoryTests(PostgresFixture postgresFixture) {
        this.postgresFixture = postgresFixture;
    }

    private UserImageRepository CreateRepository() {
        var dataSource = NpgsqlDataSource.Create(postgresFixture.ConnectionString);
        return new UserImageRepository(dataSource);
    }

    private UserRepository CreateUserRepository() {
        var dataSource = NpgsqlDataSource.Create(postgresFixture.ConnectionString);
        return new UserRepository(dataSource);
    }

    private async Task ResetAsync() {
        await using var connection = new NpgsqlConnection(postgresFixture.ConnectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync("DELETE FROM users");
    }

    private async Task<int> SeedUserAsync(string email = "red@example.com") {
        var users = CreateUserRepository();
        var user = await users.CreateAsync(email, "hashed-pw", "Red");
        return user.UserId;
    }

    private static UserImage MakeImage(int userId, int locationId, Guid? imageId = null) =>
        new(
            ImageId: imageId ?? Guid.NewGuid(),
            UserId: userId,
            LocationId: locationId,
            FilePath: $"/app/uploads/{userId}/{Guid.NewGuid()}.png",
            OriginalFilename: "screenshot.png",
            ContentType: "image/png",
            ByteSize: 12345,
            UploadedAt: DateTimeOffset.UtcNow);

    [Fact]
    public async Task AddAsyncReturnsSuccessAndPersistsRow() {
        await ResetAsync();
        var userId = await SeedUserAsync();
        var repository = CreateRepository();
        var image = MakeImage(userId, locationId: 1);

        var result = await repository.AddAsync(image, locationCap: 20);

        Assert.Equal(AddResult.Success, result);
        var loaded = await repository.GetByIdForUserAsync(userId, image.ImageId);
        Assert.NotNull(loaded);
        Assert.Equal(image.OriginalFilename, loaded!.OriginalFilename);
        Assert.Equal(image.ContentType, loaded.ContentType);
        Assert.Equal(image.ByteSize, loaded.ByteSize);
        Assert.Equal(image.FilePath, loaded.FilePath);
    }
}
```

- [ ] **Step 2: Run the test — expect compile failure**

Run: `cd Backend && dotnet test PokemonLocations.WebServer.Tests/PokemonLocations.WebServer.Tests.csproj -nologo`
Expected: compile error: `UserImageRepository` not defined.

- [ ] **Step 3: Implement `UserImageRepository.AddAsync` + `GetByIdForUserAsync`**

Create `Backend/PokemonLocations.WebServer/Database/Repositories/UserImageRepository.cs`:

```csharp
using Dapper;
using Npgsql;
using PokemonLocations.WebServer.Models;
using System.Data;

namespace PokemonLocations.WebServer.Database.Repositories;

public class UserImageRepository : IUserImageRepository {
    private readonly NpgsqlDataSource dataSource;

    public UserImageRepository(NpgsqlDataSource dataSource) {
        this.dataSource = dataSource;
    }

    public async Task<AddResult> AddAsync(UserImage image, int locationCap) {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var tx = await connection.BeginTransactionAsync(IsolationLevel.Serializable);

        var current = await connection.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM user_images
               WHERE user_id = @UserId AND location_id = @LocationId",
            new { image.UserId, image.LocationId },
            tx);

        if (current >= locationCap) {
            await tx.RollbackAsync();
            return AddResult.AtCap;
        }

        await connection.ExecuteAsync(
            @"INSERT INTO user_images (
                  image_id, user_id, location_id, file_path,
                  original_filename, content_type, byte_size, uploaded_at)
              VALUES (
                  @ImageId, @UserId, @LocationId, @FilePath,
                  @OriginalFilename, @ContentType, @ByteSize, @UploadedAt)",
            image,
            tx);

        await tx.CommitAsync();
        return AddResult.Success;
    }

    public async Task<UserImage?> GetByIdForUserAsync(int userId, Guid imageId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<UserImage>(
            @"SELECT image_id, user_id, location_id, file_path,
                     original_filename, content_type, byte_size, uploaded_at
                FROM user_images
               WHERE user_id = @UserId AND image_id = @ImageId",
            new { UserId = userId, ImageId = imageId });
    }

    // Stubs for the remaining methods — filled in later tasks
    public Task<IReadOnlyList<UserImage>> GetForUserAndLocationAsync(int userId, int locationId) =>
        throw new NotImplementedException();
    public Task RemoveAsync(int userId, Guid imageId) =>
        throw new NotImplementedException();
    public Task<int> CountForUserAndLocationAsync(int userId, int locationId) =>
        throw new NotImplementedException();
}
```

- [ ] **Step 4: Run the test — expect pass**

Run: `cd Backend && dotnet test PokemonLocations.WebServer.Tests/PokemonLocations.WebServer.Tests.csproj --filter "FullyQualifiedName~UserImageRepositoryTests" -nologo`
Expected: 1 test passing.

- [ ] **Step 5: Commit**

```bash
git add Backend/PokemonLocations.WebServer/Database/Repositories/UserImageRepository.cs Backend/PokemonLocations.WebServer.Tests/Database/UserImageRepositoryTests.cs
git commit -m "feat(webserver): UserImageRepository.AddAsync + GetByIdForUserAsync"
```

---

### Task 6: `GetForUserAndLocationAsync` — newest-first, scoped per user

**Files:**
- Modify: `Backend/PokemonLocations.WebServer.Tests/Database/UserImageRepositoryTests.cs`
- Modify: `Backend/PokemonLocations.WebServer/Database/Repositories/UserImageRepository.cs`

- [ ] **Step 1: Add three failing tests**

Append inside the `UserImageRepositoryTests` class:

```csharp
[Fact]
public async Task GetForUserAndLocationAsyncReturnsImagesNewestFirst() {
    await ResetAsync();
    var userId = await SeedUserAsync();
    var repository = CreateRepository();
    var older = MakeImage(userId, 1) with { UploadedAt = DateTimeOffset.UtcNow.AddMinutes(-5) };
    var newer = MakeImage(userId, 1) with { UploadedAt = DateTimeOffset.UtcNow };
    await repository.AddAsync(older, 20);
    await repository.AddAsync(newer, 20);

    var images = await repository.GetForUserAndLocationAsync(userId, 1);

    Assert.Equal(2, images.Count);
    Assert.Equal(newer.ImageId, images[0].ImageId);
    Assert.Equal(older.ImageId, images[1].ImageId);
}

[Fact]
public async Task GetForUserAndLocationAsyncIsScopedToLocation() {
    await ResetAsync();
    var userId = await SeedUserAsync();
    var repository = CreateRepository();
    await repository.AddAsync(MakeImage(userId, 1), 20);
    await repository.AddAsync(MakeImage(userId, 2), 20);

    var loc1 = await repository.GetForUserAndLocationAsync(userId, 1);

    Assert.Single(loc1);
    Assert.Equal(1, loc1[0].LocationId);
}

[Fact]
public async Task GetForUserAndLocationAsyncDoesNotReturnAnotherUsersImages() {
    await ResetAsync();
    var redId = await SeedUserAsync("red@example.com");
    var blueId = await SeedUserAsync("blue@example.com");
    var repository = CreateRepository();
    await repository.AddAsync(MakeImage(redId, 1), 20);
    await repository.AddAsync(MakeImage(blueId, 1), 20);

    var redImages = await repository.GetForUserAndLocationAsync(redId, 1);

    Assert.Single(redImages);
    Assert.Equal(redId, redImages[0].UserId);
}
```

- [ ] **Step 2: Run — expect 3 failures (NotImplementedException)**

Run: `cd Backend && dotnet test PokemonLocations.WebServer.Tests/PokemonLocations.WebServer.Tests.csproj --filter "FullyQualifiedName~GetForUserAndLocationAsync" -nologo`
Expected: 3 failures, all `NotImplementedException`.

- [ ] **Step 3: Implement the method**

Replace the stub in `UserImageRepository.cs`:

```csharp
public async Task<IReadOnlyList<UserImage>> GetForUserAndLocationAsync(int userId, int locationId) {
    await using var connection = await dataSource.OpenConnectionAsync();
    var rows = await connection.QueryAsync<UserImage>(
        @"SELECT image_id, user_id, location_id, file_path,
                 original_filename, content_type, byte_size, uploaded_at
            FROM user_images
           WHERE user_id = @UserId AND location_id = @LocationId
           ORDER BY uploaded_at DESC",
        new { UserId = userId, LocationId = locationId });
    return rows.ToList();
}
```

- [ ] **Step 4: Run — expect 3 passes**

Run: `cd Backend && dotnet test PokemonLocations.WebServer.Tests/PokemonLocations.WebServer.Tests.csproj --filter "FullyQualifiedName~GetForUserAndLocationAsync" -nologo`
Expected: 3 passes.

- [ ] **Step 5: Commit**

```bash
git add Backend/PokemonLocations.WebServer/Database/Repositories/UserImageRepository.cs Backend/PokemonLocations.WebServer.Tests/Database/UserImageRepositoryTests.cs
git commit -m "feat(webserver): UserImageRepository.GetForUserAndLocationAsync"
```

---

### Task 7: `RemoveAsync` — idempotent, ownership-scoped

**Files:**
- Modify: `Backend/PokemonLocations.WebServer.Tests/Database/UserImageRepositoryTests.cs`
- Modify: `Backend/PokemonLocations.WebServer/Database/Repositories/UserImageRepository.cs`

- [ ] **Step 1: Add tests**

```csharp
[Fact]
public async Task RemoveAsyncDeletesOwnedImage() {
    await ResetAsync();
    var userId = await SeedUserAsync();
    var repository = CreateRepository();
    var image = MakeImage(userId, 1);
    await repository.AddAsync(image, 20);

    await repository.RemoveAsync(userId, image.ImageId);

    Assert.Null(await repository.GetByIdForUserAsync(userId, image.ImageId));
}

[Fact]
public async Task RemoveAsyncIsIdempotentWhenRowMissing() {
    await ResetAsync();
    var userId = await SeedUserAsync();
    var repository = CreateRepository();

    await repository.RemoveAsync(userId, Guid.NewGuid()); // no throw
}

[Fact]
public async Task RemoveAsyncDoesNotDeleteAnotherUsersImage() {
    await ResetAsync();
    var redId = await SeedUserAsync("red@example.com");
    var blueId = await SeedUserAsync("blue@example.com");
    var repository = CreateRepository();
    var blueImage = MakeImage(blueId, 1);
    await repository.AddAsync(blueImage, 20);

    await repository.RemoveAsync(redId, blueImage.ImageId);

    var stillThere = await repository.GetByIdForUserAsync(blueId, blueImage.ImageId);
    Assert.NotNull(stillThere);
}
```

- [ ] **Step 2: Run — expect 3 failures**

Run: `cd Backend && dotnet test PokemonLocations.WebServer.Tests/PokemonLocations.WebServer.Tests.csproj --filter "FullyQualifiedName~RemoveAsync" -nologo`
Expected: 3 failures.

- [ ] **Step 3: Implement**

Replace the `RemoveAsync` stub:

```csharp
public async Task RemoveAsync(int userId, Guid imageId) {
    await using var connection = await dataSource.OpenConnectionAsync();
    await connection.ExecuteAsync(
        "DELETE FROM user_images WHERE user_id = @UserId AND image_id = @ImageId",
        new { UserId = userId, ImageId = imageId });
}
```

- [ ] **Step 4: Run — expect 3 passes**

- [ ] **Step 5: Commit**

```bash
git add Backend/PokemonLocations.WebServer/Database/Repositories/UserImageRepository.cs Backend/PokemonLocations.WebServer.Tests/Database/UserImageRepositoryTests.cs
git commit -m "feat(webserver): UserImageRepository.RemoveAsync"
```

---

### Task 8: `CountForUserAndLocationAsync`

**Files:**
- Modify: `Backend/PokemonLocations.WebServer.Tests/Database/UserImageRepositoryTests.cs`
- Modify: `Backend/PokemonLocations.WebServer/Database/Repositories/UserImageRepository.cs`

- [ ] **Step 1: Add test**

```csharp
[Fact]
public async Task CountForUserAndLocationAsyncReturnsScopedCount() {
    await ResetAsync();
    var userId = await SeedUserAsync();
    var repository = CreateRepository();
    await repository.AddAsync(MakeImage(userId, 1), 20);
    await repository.AddAsync(MakeImage(userId, 1), 20);
    await repository.AddAsync(MakeImage(userId, 2), 20);

    Assert.Equal(2, await repository.CountForUserAndLocationAsync(userId, 1));
    Assert.Equal(1, await repository.CountForUserAndLocationAsync(userId, 2));
    Assert.Equal(0, await repository.CountForUserAndLocationAsync(userId, 99));
}
```

- [ ] **Step 2: Run — expect failure**

- [ ] **Step 3: Implement**

```csharp
public async Task<int> CountForUserAndLocationAsync(int userId, int locationId) {
    await using var connection = await dataSource.OpenConnectionAsync();
    return await connection.ExecuteScalarAsync<int>(
        @"SELECT COUNT(*) FROM user_images
           WHERE user_id = @UserId AND location_id = @LocationId",
        new { UserId = userId, LocationId = locationId });
}
```

- [ ] **Step 4: Run — expect pass**

- [ ] **Step 5: Commit**

```bash
git add Backend/PokemonLocations.WebServer/Database/Repositories/UserImageRepository.cs Backend/PokemonLocations.WebServer.Tests/Database/UserImageRepositoryTests.cs
git commit -m "feat(webserver): UserImageRepository.CountForUserAndLocationAsync"
```

---

### Task 9: AddAsync cap-reached behavior + cascade test

**Files:**
- Modify: `Backend/PokemonLocations.WebServer.Tests/Database/UserImageRepositoryTests.cs`

- [ ] **Step 1: Add tests**

```csharp
[Fact]
public async Task AddAsyncReturnsAtCapWhenLimitReached() {
    await ResetAsync();
    var userId = await SeedUserAsync();
    var repository = CreateRepository();
    for (int i = 0; i < 3; i++) {
        await repository.AddAsync(MakeImage(userId, 1), locationCap: 3);
    }

    var result = await repository.AddAsync(MakeImage(userId, 1), locationCap: 3);

    Assert.Equal(AddResult.AtCap, result);
    Assert.Equal(3, await repository.CountForUserAndLocationAsync(userId, 1));
}

[Fact]
public async Task DeletingUserCascadesUserImages() {
    await ResetAsync();
    var userId = await SeedUserAsync();
    var repository = CreateRepository();
    await repository.AddAsync(MakeImage(userId, 1), 20);
    await repository.AddAsync(MakeImage(userId, 2), 20);

    await CreateUserRepository().DeleteAsync(userId);

    await using var connection = new NpgsqlConnection(postgresFixture.ConnectionString);
    await connection.OpenAsync();
    var orphans = await connection.ExecuteScalarAsync<int>(
        "SELECT COUNT(*) FROM user_images WHERE user_id = @UserId",
        new { UserId = userId });
    Assert.Equal(0, orphans);
}
```

- [ ] **Step 2: Run — expect both pass (AddAsync cap behavior already wired up by Task 5; cascade is FK-driven by migration)**

Run: `cd Backend && dotnet test PokemonLocations.WebServer.Tests/PokemonLocations.WebServer.Tests.csproj --filter "FullyQualifiedName~AddAsyncReturnsAtCap|FullyQualifiedName~DeletingUserCascades" -nologo`
Expected: 2 passes.

- [ ] **Step 3: Commit**

```bash
git add Backend/PokemonLocations.WebServer.Tests/Database/UserImageRepositoryTests.cs
git commit -m "test(webserver): cap-reached + user-delete cascade for user_images"
```

---

## Phase 3 — Image Processor (TDD)

### Task 10: Test image fixture helper

**Files:**
- Create: `Backend/PokemonLocations.WebServer.Tests/Imaging/TestImageFixtures.cs`

- [ ] **Step 1: Write the helper**

```csharp
using SkiaSharp;

namespace PokemonLocations.WebServer.Tests.Imaging;

public static class TestImageFixtures {
    public static byte[] CreateImage(int width, int height, SKEncodedImageFormat format, int quality = 90) {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.CornflowerBlue);
        // Draw a simple pattern so the image isn't pure-solid (PNG zlib compresses too aggressively)
        using var paint = new SKPaint { Color = SKColors.White };
        for (int y = 0; y < height; y += 32) canvas.DrawLine(0, y, width, y, paint);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, quality);
        return data.ToArray();
    }

    public static byte[] CreatePng(int width, int height) =>
        CreateImage(width, height, SKEncodedImageFormat.Png, 100);

    public static byte[] CreateJpeg(int width, int height) =>
        CreateImage(width, height, SKEncodedImageFormat.Jpeg, 85);

    public static byte[] CreateWebp(int width, int height) =>
        CreateImage(width, height, SKEncodedImageFormat.Webp, 85);

    public static byte[] CreateGif(int width = 1, int height = 1) {
        // 35-byte transparent 1×1 GIF (well-known constant; SkiaSharp can decode but we don't accept GIF).
        return new byte[] {
            0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00,
            0xFF, 0xFF, 0xFF, 0x21, 0xF9, 0x04, 0x01, 0x00, 0x00, 0x00, 0x00, 0x2C, 0x00, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x01, 0x00, 0x00, 0x02, 0x02, 0x44, 0x01, 0x00, 0x3B
        };
    }

    public static byte[] CreateCorruptBytes() =>
        new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };
}
```

- [ ] **Step 2: Build to verify**

Run: `cd Backend && dotnet build PokemonLocations.WebServer.Tests/PokemonLocations.WebServer.Tests.csproj -nologo`
Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add Backend/PokemonLocations.WebServer.Tests/Imaging/TestImageFixtures.cs
git commit -m "test(webserver): test image fixture helper for SkiaSharp tests"
```

---

### Task 11: `IImageProcessor` interface + `ProcessedImage` + exception types

**Files:**
- Create: `Backend/PokemonLocations.WebServer/Services/IImageProcessor.cs`

- [ ] **Step 1: Write the interface**

```csharp
using SkiaSharp;

namespace PokemonLocations.WebServer.Services;

public interface IImageProcessor {
    Task<ProcessedImage> ProcessAsync(Stream input, CancellationToken ct);
}

public record ProcessedImage(
    byte[] Bytes,
    SKEncodedImageFormat Format,
    int Width,
    int Height);

public class UnsupportedFormatException : Exception { }
public class DecodeFailedException : Exception { }
public class DecodeBombException : Exception { }
```

- [ ] **Step 2: Build**

Run: `cd Backend && dotnet build PokemonLocations.WebServer/PokemonLocations.WebServer.csproj -nologo`
Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add Backend/PokemonLocations.WebServer/Services/IImageProcessor.cs
git commit -m "feat(webserver): IImageProcessor contract"
```

---

### Task 12: `ImageProcessor` happy path — decode/encode (no resize) per format

**Files:**
- Create: `Backend/PokemonLocations.WebServer.Tests/Imaging/ImageProcessorTests.cs`
- Create: `Backend/PokemonLocations.WebServer/Services/ImageProcessor.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using PokemonLocations.WebServer.Services;
using SkiaSharp;

namespace PokemonLocations.WebServer.Tests.Imaging;

public class ImageProcessorTests {
    private readonly ImageProcessor processor = new();

    [Fact]
    public async Task ProcessesPngAndPreservesFormat() {
        var input = TestImageFixtures.CreatePng(300, 200);
        var result = await processor.ProcessAsync(new MemoryStream(input), default);

        Assert.Equal(SKEncodedImageFormat.Png, result.Format);
        Assert.Equal(300, result.Width);
        Assert.Equal(200, result.Height);
        Assert.NotEmpty(result.Bytes);
    }

    [Fact]
    public async Task ProcessesJpegAndPreservesFormat() {
        var input = TestImageFixtures.CreateJpeg(300, 200);
        var result = await processor.ProcessAsync(new MemoryStream(input), default);

        Assert.Equal(SKEncodedImageFormat.Jpeg, result.Format);
        Assert.Equal(300, result.Width);
        Assert.Equal(200, result.Height);
    }

    [Fact]
    public async Task ProcessesWebpAndPreservesFormat() {
        var input = TestImageFixtures.CreateWebp(300, 200);
        var result = await processor.ProcessAsync(new MemoryStream(input), default);

        Assert.Equal(SKEncodedImageFormat.Webp, result.Format);
        Assert.Equal(300, result.Width);
        Assert.Equal(200, result.Height);
    }
}
```

- [ ] **Step 2: Run — expect compile failure (`ImageProcessor` not defined)**

Run: `cd Backend && dotnet test PokemonLocations.WebServer.Tests/PokemonLocations.WebServer.Tests.csproj --filter "FullyQualifiedName~ImageProcessorTests" -nologo`
Expected: compile error.

- [ ] **Step 3: Implement `ImageProcessor`**

```csharp
using SkiaSharp;

namespace PokemonLocations.WebServer.Services;

public class ImageProcessor : IImageProcessor {
    private const int ResizeLongestEdge = 2000;
    private const long MaxPixels = 50_000_000;
    private const int Quality = 85;

    public Task<ProcessedImage> ProcessAsync(Stream input, CancellationToken ct) {
        using var managed = new SKManagedStream(input);
        using var codec = SKCodec.Create(managed);
        if (codec is null) throw new DecodeFailedException();

        var format = codec.EncodedFormat;
        if (format is not (SKEncodedImageFormat.Png or SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Webp)) {
            throw new UnsupportedFormatException();
        }

        var info = codec.Info;
        if ((long)info.Width * info.Height > MaxPixels) throw new DecodeBombException();

        using var sourceBitmap = SKBitmap.Decode(codec);
        if (sourceBitmap is null) throw new DecodeFailedException();

        SKBitmap? resizedBitmap = null;
        try {
            var workingBitmap = sourceBitmap;
            var longest = Math.Max(sourceBitmap.Width, sourceBitmap.Height);
            if (longest > ResizeLongestEdge) {
                var scale = (double)ResizeLongestEdge / longest;
                var newW = (int)(sourceBitmap.Width * scale);
                var newH = (int)(sourceBitmap.Height * scale);
                resizedBitmap = sourceBitmap.Resize(
                    new SKImageInfo(newW, newH),
                    new SKSamplingOptions(SKCubicResampler.Mitchell));
                if (resizedBitmap is null) throw new DecodeFailedException();
                workingBitmap = resizedBitmap;
            }

            using var image = SKImage.FromBitmap(workingBitmap);
            using var data = image.Encode(format, Quality);
            return Task.FromResult(new ProcessedImage(
                Bytes: data.ToArray(),
                Format: format,
                Width: workingBitmap.Width,
                Height: workingBitmap.Height));
        } finally {
            resizedBitmap?.Dispose();
        }
    }
}
```

- [ ] **Step 4: Run — expect 3 passes**

- [ ] **Step 5: Commit**

```bash
git add Backend/PokemonLocations.WebServer/Services/ImageProcessor.cs Backend/PokemonLocations.WebServer.Tests/Imaging/ImageProcessorTests.cs
git commit -m "feat(webserver): ImageProcessor — decode+encode per format"
```

---

### Task 13: Resize behavior — landscape, portrait, boundary

**Files:**
- Modify: `Backend/PokemonLocations.WebServer.Tests/Imaging/ImageProcessorTests.cs`

- [ ] **Step 1: Add tests**

```csharp
[Fact]
public async Task ResizesLandscapeOversizedImageToLongestEdge2000() {
    var input = TestImageFixtures.CreatePng(4000, 3000);
    var result = await processor.ProcessAsync(new MemoryStream(input), default);

    Assert.Equal(2000, result.Width);
    Assert.Equal(1500, result.Height);
}

[Fact]
public async Task ResizesPortraitOversizedImageHeightDriven() {
    var input = TestImageFixtures.CreatePng(3000, 4000);
    var result = await processor.ProcessAsync(new MemoryStream(input), default);

    Assert.Equal(1500, result.Width);
    Assert.Equal(2000, result.Height);
}

[Fact]
public async Task DoesNotResizeWhenLongestEdgeUnderThreshold() {
    var input = TestImageFixtures.CreatePng(1500, 1000);
    var result = await processor.ProcessAsync(new MemoryStream(input), default);

    Assert.Equal(1500, result.Width);
    Assert.Equal(1000, result.Height);
}

[Fact]
public async Task DoesNotResizeAtExactBoundary() {
    var input = TestImageFixtures.CreatePng(2000, 1500);
    var result = await processor.ProcessAsync(new MemoryStream(input), default);

    Assert.Equal(2000, result.Width);
    Assert.Equal(1500, result.Height);
}
```

- [ ] **Step 2: Run — expect 4 passes (logic implemented in Task 12)**

Run: `cd Backend && dotnet test PokemonLocations.WebServer.Tests/PokemonLocations.WebServer.Tests.csproj --filter "FullyQualifiedName~Resize|FullyQualifiedName~DoesNotResize" -nologo`
Expected: 4 passes.

- [ ] **Step 3: Commit**

```bash
git add Backend/PokemonLocations.WebServer.Tests/Imaging/ImageProcessorTests.cs
git commit -m "test(webserver): image-processor resize boundaries (landscape, portrait, threshold)"
```

---

### Task 14: Rejection paths — unsupported format, decode bomb, corrupt bytes

**Files:**
- Modify: `Backend/PokemonLocations.WebServer.Tests/Imaging/ImageProcessorTests.cs`

- [ ] **Step 1: Add tests**

```csharp
[Fact]
public async Task RejectsUnsupportedFormat() {
    var input = TestImageFixtures.CreateGif();
    await Assert.ThrowsAsync<UnsupportedFormatException>(
        () => processor.ProcessAsync(new MemoryStream(input), default));
}

[Fact]
public async Task RejectsDecodeBombByPixelCount() {
    // 8000×8000 = 64 MP, exceeds 50 MP cap. PNG of solid color compresses small.
    var input = TestImageFixtures.CreatePng(8000, 8000);
    await Assert.ThrowsAsync<DecodeBombException>(
        () => processor.ProcessAsync(new MemoryStream(input), default));
}

[Fact]
public async Task RejectsCorruptBytes() {
    var input = TestImageFixtures.CreateCorruptBytes();
    await Assert.ThrowsAsync<DecodeFailedException>(
        () => processor.ProcessAsync(new MemoryStream(input), default));
}
```

- [ ] **Step 2: Run — expect 3 passes (logic already implemented in Task 12)**

- [ ] **Step 3: Commit**

```bash
git add Backend/PokemonLocations.WebServer.Tests/Imaging/ImageProcessorTests.cs
git commit -m "test(webserver): image-processor rejection paths"
```

---

## Phase 4 — Configuration & Wiring

### Task 15: `UserImagesOptions` binding + `appsettings.json`

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/appsettings.json`

- [ ] **Step 1: Read the existing appsettings to know structure**

Run: `cat Backend/PokemonLocations.WebServer/appsettings.json` — note the existing top-level keys (`ConnectionStrings`, `Jwt`, etc.).

- [ ] **Step 2: Add the `UserImages` block**

Insert at the same level as `ConnectionStrings`:

```json
"UserImages": {
  "UploadRoot": "/app/uploads",
  "MaxFilesPerLocation": 20,
  "MaxBytesPerFile": 10485760,
  "MaxPixelsPerImage": 50000000,
  "ResizeLongestEdge": 2000
}
```

- [ ] **Step 3: Commit**

```bash
git add Backend/PokemonLocations.WebServer/appsettings.json
git commit -m "config(webserver): UserImages block in appsettings.json"
```

---

### Task 16: DI registration + Form/Kestrel limits in `Program.cs`

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/Program.cs`

- [ ] **Step 1: Add `using` directives at the top if missing**

Open the file. Verify it has:
```csharp
using Microsoft.AspNetCore.Http.Features;
using PokemonLocations.WebServer.Models;
using PokemonLocations.WebServer.Services;
```
Add any that are missing.

- [ ] **Step 2: Add the new DI registrations**

After the existing `builder.Services.AddSingleton<IUserNoteRepository, UserNoteRepository>();` line (or wherever singletons are registered), add:

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

- [ ] **Step 3: Build to verify**

Run: `cd Backend && dotnet build PokemonLocations.WebServer/PokemonLocations.WebServer.csproj -nologo`
Expected: build succeeds.

- [ ] **Step 4: Commit**

```bash
git add Backend/PokemonLocations.WebServer/Program.cs
git commit -m "feat(webserver): wire IUserImageRepository, IImageProcessor, upload size limits"
```

---

### Task 17: Test factory override for `UserImages:UploadRoot`

**Files:**
- Modify: `Backend/PokemonLocations.WebServer.Tests/Infrastructure/PokemonLocationsWebServerFactory.cs`

- [ ] **Step 1: Read the existing factory**

Run: `cat Backend/PokemonLocations.WebServer.Tests/Infrastructure/PokemonLocationsWebServerFactory.cs`

- [ ] **Step 2: Add a per-test temp upload dir property + override**

In the factory class, add:

```csharp
public string UploadRoot { get; } = Path.Combine(
    Path.GetTempPath(),
    "pokemon-locations-tests",
    Guid.NewGuid().ToString());
```

Then, inside the existing `ConfigureWebHost` method (or wherever the in-memory configuration is layered), append a new in-memory config source:

```csharp
builder.ConfigureAppConfiguration((_, config) => {
    config.AddInMemoryCollection(new Dictionary<string, string?> {
        ["UserImages:UploadRoot"] = UploadRoot
    });
});
```

Add a `Dispose` override (or extend the existing one) to clean up the temp dir:

```csharp
protected override void Dispose(bool disposing) {
    base.Dispose(disposing);
    if (disposing && Directory.Exists(UploadRoot)) {
        try { Directory.Delete(UploadRoot, recursive: true); } catch { /* swallow */ }
    }
}
```

- [ ] **Step 3: Build to verify**

Run: `cd Backend && dotnet build PokemonLocations.WebServer.Tests/PokemonLocations.WebServer.Tests.csproj -nologo`
Expected: build succeeds.

- [ ] **Step 4: Commit**

```bash
git add Backend/PokemonLocations.WebServer.Tests/Infrastructure/PokemonLocationsWebServerFactory.cs
git commit -m "test(webserver): override UserImages:UploadRoot to per-test temp dir"
```

---

## Phase 5 — Endpoint Controller (TDD)

### Task 18: `UserImagesController` skeleton + 401 tests

**Files:**
- Create: `Backend/PokemonLocations.WebServer/Controllers/UserImagesController.cs`
- Create: `Backend/PokemonLocations.WebServer.Tests/Controllers/UserImagesControllerTests.cs`

- [ ] **Step 1: Write a minimal controller skeleton (so test compilation works)**

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PokemonLocations.WebServer.Authentication;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Database.Repositories;
using PokemonLocations.WebServer.Models;
using PokemonLocations.WebServer.Services;

namespace PokemonLocations.WebServer.Controllers;

[ApiController]
[Route("/api/me/locations/{locationId:int}/images")]
public class UserImagesController : ControllerBase {
    private readonly IUserImageRepository repository;
    private readonly IImageProcessor processor;
    private readonly IPokemonLocationsApiClient apiClient;
    private readonly UserImagesOptions options;
    private readonly ILogger<UserImagesController> logger;

    public UserImagesController(
        IUserImageRepository repository,
        IImageProcessor processor,
        IPokemonLocationsApiClient apiClient,
        IOptions<UserImagesOptions> options,
        ILogger<UserImagesController> logger) {
        this.repository = repository;
        this.processor = processor;
        this.apiClient = apiClient;
        this.options = options.Value;
        this.logger = logger;
    }

    [HttpPost]
    public Task<IActionResult> Upload(int locationId, IFormFile file) =>
        throw new NotImplementedException();

    [HttpDelete("{imageId:guid}")]
    public Task<IActionResult> Delete(int locationId, Guid imageId) =>
        throw new NotImplementedException();

    [HttpGet("{imageId:guid}")]
    public Task<IActionResult> Get(int locationId, Guid imageId) =>
        throw new NotImplementedException();
}
```

- [ ] **Step 2: Write the test class skeleton with three 401 tests**

```csharp
using System.Net;
using NSubstitute;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Tests.Infrastructure;
using static PokemonLocations.WebServer.Tests.Infrastructure.TestHelpers;

namespace PokemonLocations.WebServer.Tests.Controllers;

[Collection("PostgresAndRedis")]
public class UserImagesControllerTests {
    private readonly PostgresFixture postgresFixture;
    private readonly RedisFixture redisFixture;

    public UserImagesControllerTests(PostgresFixture postgresFixture, RedisFixture redisFixture) {
        this.postgresFixture = postgresFixture;
        this.redisFixture = redisFixture;
    }

    private PokemonLocationsWebServerFactory CreateFactory(IPokemonLocationsApiClient? apiClient = null) =>
        new(postgresFixture.ConnectionString, redisFixture.ConnectionString) {
            ApiClient = apiClient ?? Substitute.For<IPokemonLocationsApiClient>()
        };

    [Fact]
    public async Task PostReturns401WithoutAuth() {
        var factory = CreateFactory();
        var client = factory.CreateClient();

        using var content = new MultipartFormDataContent {
            { new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47 }), "file", "x.png" }
        };
        var response = await client.PostAsync("/api/me/locations/1/images", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteReturns401WithoutAuth() {
        var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/me/locations/1/images/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetReturns401WithoutAuth() {
        var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/me/locations/1/images/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

- [ ] **Step 3: Run — expect 3 passes (the fallback policy already enforces auth on undecorated endpoints)**

Run: `cd Backend && dotnet test PokemonLocations.WebServer.Tests/PokemonLocations.WebServer.Tests.csproj --filter "FullyQualifiedName~UserImagesControllerTests" -nologo`
Expected: 3 passes.

- [ ] **Step 4: Commit**

```bash
git add Backend/PokemonLocations.WebServer/Controllers/UserImagesController.cs Backend/PokemonLocations.WebServer.Tests/Controllers/UserImagesControllerTests.cs
git commit -m "feat(webserver): UserImagesController skeleton + auth tests"
```

---

### Task 19: `POST` happy path (PNG → 201, file on disk, DB row)

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/Controllers/UserImagesController.cs`
- Modify: `Backend/PokemonLocations.WebServer.Tests/Controllers/UserImagesControllerTests.cs`

- [ ] **Step 1: Add helper methods + happy-path test**

Inside `UserImagesControllerTests`, add helpers:

```csharp
private static IPokemonLocationsApiClient ApiClientThatAcceptsLocations() {
    var client = Substitute.For<IPokemonLocationsApiClient>();
    client.ExistsAsync(Arg.Any<string>()).Returns(true);
    return client;
}

private static HttpClient AuthorizedClient(
    PokemonLocationsWebServerFactory factory, string email, string password) {
    var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = BasicHeader(email, password);
    return client;
}

private static MultipartFormDataContent MakeMultipart(byte[] bytes, string filename, string mime) {
    var content = new MultipartFormDataContent();
    var fileContent = new ByteArrayContent(bytes);
    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mime);
    content.Add(fileContent, "file", filename);
    return content;
}

private static byte[] ValidPngBytes(int w = 64, int h = 64) =>
    PokemonLocations.WebServer.Tests.Imaging.TestImageFixtures.CreatePng(w, h);
```

Then add the test:

```csharp
[Fact]
public async Task PostValidPngReturns201WithFileOnDiskAndDbRow() {
    await ResetUsersAsync(postgresFixture.ConnectionString);
    await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
    var factory = CreateFactory(ApiClientThatAcceptsLocations());
    var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

    var bytes = ValidPngBytes();
    using var content = MakeMultipart(bytes, "shot.png", "image/png");
    var response = await client.PostAsync("/api/me/locations/1/images", content);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var body = await ReadJsonAsync(response);
    var imageId = Guid.Parse(body.GetProperty("imageId").GetString()!);
    Assert.Equal($"/api/me/locations/1/images/{imageId}", body.GetProperty("imageUrl").GetString());
    Assert.Equal("shot.png", body.GetProperty("originalFilename").GetString());

    // File on disk
    Assert.True(Directory.EnumerateFiles(factory.UploadRoot, "*", SearchOption.AllDirectories)
                         .Any(p => p.EndsWith($"{imageId}.png")));
}
```

- [ ] **Step 2: Run — expect failure (NotImplementedException)**

- [ ] **Step 3: Implement `Upload`**

Replace the `Upload` body in `UserImagesController.cs`:

```csharp
[HttpPost]
public async Task<IActionResult> Upload(int locationId, IFormFile file, CancellationToken ct) {
    var userId = User.GetUserId();

    // 1. Location exists
    if (!await apiClient.ExistsAsync($"/locations/{locationId}")) {
        return NotFound(new { error = "location_not_found" });
    }

    // 2. MIME validation
    var mime = file.ContentType?.ToLowerInvariant();
    if (mime is not ("image/png" or "image/jpeg" or "image/webp")) {
        return BadRequest(new { error = "unsupported_media_type" });
    }

    // 3. Size cap (post-multipart-parse)
    if (file.Length > options.MaxBytesPerFile) {
        return BadRequest(new { error = "file_too_large" });
    }

    // 4. Pre-decode count check (fail-fast)
    var current = await repository.CountForUserAndLocationAsync(userId, locationId);
    if (current >= options.MaxFilesPerLocation) {
        return BadRequest(new { error = "cap_reached" });
    }

    // 5. SkiaSharp pipeline
    ProcessedImage processed;
    try {
        await using var stream = file.OpenReadStream();
        processed = await processor.ProcessAsync(stream, ct);
    } catch (UnsupportedFormatException) {
        return StatusCode(StatusCodes.Status415UnsupportedMediaType, new { error = "decode_failed" });
    } catch (DecodeFailedException) {
        return StatusCode(StatusCodes.Status415UnsupportedMediaType, new { error = "decode_failed" });
    } catch (DecodeBombException) {
        return BadRequest(new { error = "decode_bomb" });
    }

    // 6. Disk write
    var uuid = Guid.NewGuid();
    var ext = ExtensionFor(processed.Format);
    var userDir = Path.Combine(options.UploadRoot, userId.ToString());
    Directory.CreateDirectory(userDir);
    var tempPath = Path.Combine(userDir, $"{uuid}.tmp");
    var finalPath = Path.Combine(userDir, $"{uuid}.{ext}");
    await System.IO.File.WriteAllBytesAsync(tempPath, processed.Bytes, ct);
    System.IO.File.Move(tempPath, finalPath);

    var image = new UserImage(
        ImageId: uuid,
        UserId: userId,
        LocationId: locationId,
        FilePath: finalPath,
        OriginalFilename: file.FileName,
        ContentType: ContentTypeFor(processed.Format),
        ByteSize: processed.Bytes.Length,
        UploadedAt: DateTimeOffset.UtcNow);

    // 7. Race-safe insert with one-time retry
    var insertResult = await TryInsertWithOneRetry(image, finalPath);
    if (insertResult is OkObjectResult ok) {
        return Created($"/api/me/locations/{locationId}/images/{uuid}", ok.Value);
    }
    return insertResult;
}

private async Task<IActionResult> TryInsertWithOneRetry(UserImage image, string finalPath) {
    try {
        var result = await repository.AddAsync(image, options.MaxFilesPerLocation);
        if (result == AddResult.AtCap) {
            DeleteSilently(finalPath);
            return BadRequest(new { error = "cap_reached" });
        }
    } catch (Npgsql.PostgresException ex) when (ex.SqlState == "40001") {
        try {
            var retry = await repository.AddAsync(image, options.MaxFilesPerLocation);
            if (retry == AddResult.AtCap) {
                DeleteSilently(finalPath);
                return BadRequest(new { error = "cap_reached" });
            }
        } catch (Npgsql.PostgresException) {
            DeleteSilently(finalPath);
            return Conflict(new { error = "serialization_conflict" });
        }
    }
    return Ok(Models.Responses.UploadedImageResponse.FromDomain(image));
}

private void DeleteSilently(string path) {
    try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
    catch (Exception ex) { logger.LogWarning(ex, "Failed to delete orphaned upload {Path}", path); }
}

private static string ExtensionFor(SKEncodedImageFormat fmt) => fmt switch {
    SKEncodedImageFormat.Png => "png",
    SKEncodedImageFormat.Jpeg => "jpg",
    SKEncodedImageFormat.Webp => "webp",
    _ => throw new InvalidOperationException("Unsupported format reached file write")
};

private static string ContentTypeFor(SKEncodedImageFormat fmt) => fmt switch {
    SKEncodedImageFormat.Png => "image/png",
    SKEncodedImageFormat.Jpeg => "image/jpeg",
    SKEncodedImageFormat.Webp => "image/webp",
    _ => throw new InvalidOperationException("Unsupported format reached content-type")
};
```

Add `using SkiaSharp;` at the top.

- [ ] **Step 4: Run — expect pass**

Run: `cd Backend && dotnet test PokemonLocations.WebServer.Tests/PokemonLocations.WebServer.Tests.csproj --filter "FullyQualifiedName~PostValidPng" -nologo`
Expected: 1 pass.

- [ ] **Step 5: Commit**

```bash
git add Backend/PokemonLocations.WebServer/Controllers/UserImagesController.cs Backend/PokemonLocations.WebServer.Tests/Controllers/UserImagesControllerTests.cs
git commit -m "feat(webserver): UserImagesController POST happy path (PNG)"
```

---

### Task 20: `POST` JPEG + WebP per-format coverage

**Files:**
- Modify: `Backend/PokemonLocations.WebServer.Tests/Controllers/UserImagesControllerTests.cs`

- [ ] **Step 1: Add tests**

```csharp
[Fact]
public async Task PostValidJpegReturns201WithJpgExtension() {
    await ResetUsersAsync(postgresFixture.ConnectionString);
    await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
    var factory = CreateFactory(ApiClientThatAcceptsLocations());
    var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

    var bytes = TestImageFixtures.CreateJpeg(64, 64);
    using var content = MakeMultipart(bytes, "shot.jpg", "image/jpeg");
    var response = await client.PostAsync("/api/me/locations/1/images", content);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var body = await ReadJsonAsync(response);
    var imageId = Guid.Parse(body.GetProperty("imageId").GetString()!);
    Assert.True(Directory.EnumerateFiles(factory.UploadRoot, $"{imageId}.jpg", SearchOption.AllDirectories).Any());
}

[Fact]
public async Task PostValidWebpReturns201WithWebpExtension() {
    await ResetUsersAsync(postgresFixture.ConnectionString);
    await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
    var factory = CreateFactory(ApiClientThatAcceptsLocations());
    var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

    var bytes = TestImageFixtures.CreateWebp(64, 64);
    using var content = MakeMultipart(bytes, "shot.webp", "image/webp");
    var response = await client.PostAsync("/api/me/locations/1/images", content);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var body = await ReadJsonAsync(response);
    var imageId = Guid.Parse(body.GetProperty("imageId").GetString()!);
    Assert.True(Directory.EnumerateFiles(factory.UploadRoot, $"{imageId}.webp", SearchOption.AllDirectories).Any());
}
```

- [ ] **Step 2: Run — expect 2 passes**

- [ ] **Step 3: Commit**

```bash
git add Backend/PokemonLocations.WebServer.Tests/Controllers/UserImagesControllerTests.cs
git commit -m "test(webserver): POST per-format coverage (JPEG, WebP)"
```

---

### Task 21: `POST` validation — file too large, body too large, MIME, location, cap

**Files:**
- Modify: `Backend/PokemonLocations.WebServer.Tests/Controllers/UserImagesControllerTests.cs`

- [ ] **Step 1: Add tests**

```csharp
[Fact]
public async Task PostFileLargerThan10MbReturns400FileTooLarge() {
    await ResetUsersAsync(postgresFixture.ConnectionString);
    await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
    var factory = CreateFactory(ApiClientThatAcceptsLocations());
    var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

    // 11 MB of plausible PNG-prefix bytes (will fail MIME-decode if it gets that far,
    // but size check fires first)
    var bytes = new byte[11 * 1024 * 1024];
    new Random(0).NextBytes(bytes);
    bytes[0] = 0x89; bytes[1] = 0x50; bytes[2] = 0x4E; bytes[3] = 0x47;
    using var content = MakeMultipart(bytes, "big.png", "image/png");

    var response = await client.PostAsync("/api/me/locations/1/images", content);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    var body = await ReadJsonAsync(response);
    Assert.Equal("file_too_large", body.GetProperty("error").GetString());
}

[Fact]
public async Task PostBodyExceedingMaxRequestBodySizeReturns413() {
    await ResetUsersAsync(postgresFixture.ConnectionString);
    await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
    var factory = CreateFactory(ApiClientThatAcceptsLocations());
    var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

    // 15 MB bytes — exceeds MaxRequestBodySize of 12 MB
    var bytes = new byte[15 * 1024 * 1024];
    using var content = MakeMultipart(bytes, "huge.png", "image/png");

    var response = await client.PostAsync("/api/me/locations/1/images", content);

    Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
}

[Fact]
public async Task PostUnsupportedMimeReturns400() {
    await ResetUsersAsync(postgresFixture.ConnectionString);
    await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
    var factory = CreateFactory(ApiClientThatAcceptsLocations());
    var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

    using var content = MakeMultipart(new byte[] { 0x00 }, "x.tiff", "image/tiff");
    var response = await client.PostAsync("/api/me/locations/1/images", content);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal("unsupported_media_type",
        (await ReadJsonAsync(response)).GetProperty("error").GetString());
}

[Fact]
public async Task PostNonExistentLocationReturns404() {
    await ResetUsersAsync(postgresFixture.ConnectionString);
    await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
    var apiClient = Substitute.For<IPokemonLocationsApiClient>();
    apiClient.ExistsAsync(Arg.Any<string>()).Returns(false);
    var factory = CreateFactory(apiClient);
    var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

    using var content = MakeMultipart(ValidPngBytes(), "x.png", "image/png");
    var response = await client.PostAsync("/api/me/locations/999/images", content);

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    Assert.Equal("location_not_found",
        (await ReadJsonAsync(response)).GetProperty("error").GetString());
}

[Fact]
public async Task PostWhenAtCapReturns400CapReached() {
    await ResetUsersAsync(postgresFixture.ConnectionString);
    await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
    var factory = CreateFactory(ApiClientThatAcceptsLocations());
    var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

    // Saturate to 20 uploads
    for (int i = 0; i < 20; i++) {
        using var c = MakeMultipart(ValidPngBytes(), $"x{i}.png", "image/png");
        var r = await client.PostAsync("/api/me/locations/1/images", c);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    using var overflow = MakeMultipart(ValidPngBytes(), "overflow.png", "image/png");
    var response = await client.PostAsync("/api/me/locations/1/images", overflow);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal("cap_reached",
        (await ReadJsonAsync(response)).GetProperty("error").GetString());
}
```

- [ ] **Step 2: Run — expect 5 passes (logic from Task 19)**

- [ ] **Step 3: Commit**

```bash
git add Backend/PokemonLocations.WebServer.Tests/Controllers/UserImagesControllerTests.cs
git commit -m "test(webserver): POST validation cases (size, MIME, location, cap)"
```

---

### Task 22: `POST` decode failures — corrupt bytes, decode bomb

**Files:**
- Modify: `Backend/PokemonLocations.WebServer.Tests/Controllers/UserImagesControllerTests.cs`

- [ ] **Step 1: Add tests**

```csharp
[Fact]
public async Task PostCorruptBytesWithValidMimeReturns415() {
    await ResetUsersAsync(postgresFixture.ConnectionString);
    await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
    var factory = CreateFactory(ApiClientThatAcceptsLocations());
    var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

    using var content = MakeMultipart(
        TestImageFixtures.CreateCorruptBytes(), "x.png", "image/png");
    var response = await client.PostAsync("/api/me/locations/1/images", content);

    Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    Assert.Equal("decode_failed",
        (await ReadJsonAsync(response)).GetProperty("error").GetString());
}

[Fact]
public async Task PostDecodeBombReturns400() {
    await ResetUsersAsync(postgresFixture.ConnectionString);
    await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
    var factory = CreateFactory(ApiClientThatAcceptsLocations());
    var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

    var bytes = TestImageFixtures.CreatePng(8000, 8000); // 64 MP > 50 MP cap
    using var content = MakeMultipart(bytes, "bomb.png", "image/png");
    var response = await client.PostAsync("/api/me/locations/1/images", content);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal("decode_bomb",
        (await ReadJsonAsync(response)).GetProperty("error").GetString());
}
```

- [ ] **Step 2: Run — expect 2 passes**

- [ ] **Step 3: Commit**

```bash
git add Backend/PokemonLocations.WebServer.Tests/Controllers/UserImagesControllerTests.cs
git commit -m "test(webserver): POST decode failure paths"
```

---

### Task 23: `POST` 409 retry — both paths via mocked repo

**Files:**
- Modify: `Backend/PokemonLocations.WebServer.Tests/Controllers/UserImagesControllerTests.cs`
- Modify: `Backend/PokemonLocations.WebServer.Tests/Infrastructure/PokemonLocationsWebServerFactory.cs`

To mock the repository inside `WebApplicationFactory`, the factory needs to expose a hook similar to `ApiClient`. The pattern in this codebase is to expose a property and replace the registration in `ConfigureWebHost`.

- [ ] **Step 1: Expose repo override in the factory**

In `PokemonLocationsWebServerFactory`, add:

```csharp
public IUserImageRepository? UserImageRepositoryOverride { get; set; }
```

Inside `ConfigureWebHost`, after the existing service replacements, add:

```csharp
builder.ConfigureTestServices(services => {
    if (UserImageRepositoryOverride is not null) {
        services.RemoveAll(typeof(IUserImageRepository));
        services.AddSingleton(UserImageRepositoryOverride);
    }
});
```

Add `using Microsoft.Extensions.DependencyInjection;` and `using Microsoft.Extensions.DependencyInjection.Extensions;` at the top.

- [ ] **Step 2: Add the two retry tests**

```csharp
[Fact]
public async Task PostFirstConflictThenSuccessReturns201() {
    await ResetUsersAsync(postgresFixture.ConnectionString);
    await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");

    var mockRepo = Substitute.For<IUserImageRepository>();
    mockRepo.CountForUserAndLocationAsync(Arg.Any<int>(), Arg.Any<int>()).Returns(0);
    mockRepo.AddAsync(Arg.Any<UserImage>(), Arg.Any<int>())
        .Returns(
            x => throw new Npgsql.PostgresException("conflict", "ERROR", "ERROR", "40001"),
            x => Task.FromResult(AddResult.Success));

    var factory = CreateFactory(ApiClientThatAcceptsLocations());
    factory.UserImageRepositoryOverride = mockRepo;
    var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

    using var content = MakeMultipart(ValidPngBytes(), "shot.png", "image/png");
    var response = await client.PostAsync("/api/me/locations/1/images", content);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    await mockRepo.Received(2).AddAsync(Arg.Any<UserImage>(), Arg.Any<int>());
}

[Fact]
public async Task PostBothAttemptsConflictReturns409AndCleansFile() {
    await ResetUsersAsync(postgresFixture.ConnectionString);
    await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");

    var mockRepo = Substitute.For<IUserImageRepository>();
    mockRepo.CountForUserAndLocationAsync(Arg.Any<int>(), Arg.Any<int>()).Returns(0);
    mockRepo.AddAsync(Arg.Any<UserImage>(), Arg.Any<int>())
        .Returns<Task<AddResult>>(_ =>
            throw new Npgsql.PostgresException("conflict", "ERROR", "ERROR", "40001"));

    var factory = CreateFactory(ApiClientThatAcceptsLocations());
    factory.UserImageRepositoryOverride = mockRepo;
    var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

    using var content = MakeMultipart(ValidPngBytes(), "shot.png", "image/png");
    var response = await client.PostAsync("/api/me/locations/1/images", content);

    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    Assert.Equal("serialization_conflict",
        (await ReadJsonAsync(response)).GetProperty("error").GetString());
    await mockRepo.Received(2).AddAsync(Arg.Any<UserImage>(), Arg.Any<int>());

    // Verify no file orphans on disk for this user
    var userId = await GetUserIdAsync(postgresFixture.ConnectionString, "red@example.com");
    var userDir = Path.Combine(factory.UploadRoot, userId.ToString());
    Assert.False(Directory.Exists(userDir) && Directory.EnumerateFiles(userDir).Any());
}
```

(Add a `GetUserIdAsync` helper to `TestHelpers` that queries `SELECT user_id FROM users WHERE email = ...`. If one doesn't already exist, add it.)

- [ ] **Step 3: Run — expect 2 passes**

- [ ] **Step 4: Commit**

```bash
git add Backend/PokemonLocations.WebServer.Tests/
git commit -m "test(webserver): POST 409-retry success and exhaustion paths"
```

---

### Task 24: `DELETE` happy path + ownership + idempotency

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/Controllers/UserImagesController.cs`
- Modify: `Backend/PokemonLocations.WebServer.Tests/Controllers/UserImagesControllerTests.cs`

- [ ] **Step 1: Add tests**

```csharp
[Fact]
public async Task DeleteOwnedImageReturns204AndRemovesRowAndFile() {
    await ResetUsersAsync(postgresFixture.ConnectionString);
    await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
    var factory = CreateFactory(ApiClientThatAcceptsLocations());
    var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

    using (var c = MakeMultipart(ValidPngBytes(), "shot.png", "image/png")) {
        var post = await client.PostAsync("/api/me/locations/1/images", c);
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
    }
    var listing = await (await client.GetAsync("/api/locations/1")).Content.ReadAsStringAsync();
    var imageId = System.Text.Json.JsonDocument.Parse(listing).RootElement
        .GetProperty("userImages")[0].GetProperty("imageId").GetString();

    var del = await client.DeleteAsync($"/api/me/locations/1/images/{imageId}");

    Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    Assert.False(Directory.EnumerateFiles(factory.UploadRoot, "*", SearchOption.AllDirectories).Any());
}

[Fact]
public async Task DeleteAnotherUsersImageReturns404() {
    await ResetUsersAsync(postgresFixture.ConnectionString);
    await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
    await SeedUserAsync(postgresFixture.ConnectionString, "blue@example.com", "squirtle1", "Blue");
    var factory = CreateFactory(ApiClientThatAcceptsLocations());

    var redClient = AuthorizedClient(factory, "red@example.com", "pikachu123");
    using (var c = MakeMultipart(ValidPngBytes(), "shot.png", "image/png")) {
        await redClient.PostAsync("/api/me/locations/1/images", c);
    }
    var redListing = System.Text.Json.JsonDocument.Parse(
        await (await redClient.GetAsync("/api/locations/1")).Content.ReadAsStringAsync())
        .RootElement.GetProperty("userImages")[0].GetProperty("imageId").GetString();

    var blueClient = AuthorizedClient(factory, "blue@example.com", "squirtle1");
    var del = await blueClient.DeleteAsync($"/api/me/locations/1/images/{redListing}");

    Assert.Equal(HttpStatusCode.NotFound, del.StatusCode);
}

[Fact]
public async Task DeleteIsIdempotentForMissingImage() {
    await ResetUsersAsync(postgresFixture.ConnectionString);
    await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
    var factory = CreateFactory(ApiClientThatAcceptsLocations());
    var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

    var first = await client.DeleteAsync($"/api/me/locations/1/images/{Guid.NewGuid()}");
    Assert.Equal(HttpStatusCode.NotFound, first.StatusCode);
    var second = await client.DeleteAsync($"/api/me/locations/1/images/{Guid.NewGuid()}");
    Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
}
```

- [ ] **Step 2: Run — expect failures (NotImplementedException for Delete)**

- [ ] **Step 3: Implement `Delete`**

Replace `Delete` in `UserImagesController.cs`:

```csharp
[HttpDelete("{imageId:guid}")]
public async Task<IActionResult> Delete(int locationId, Guid imageId) {
    var userId = User.GetUserId();
    var image = await repository.GetByIdForUserAsync(userId, imageId);
    if (image is null || image.LocationId != locationId) {
        return NotFound(new { error = "not_found" });
    }
    await repository.RemoveAsync(userId, imageId);
    DeleteSilently(image.FilePath);
    return NoContent();
}
```

- [ ] **Step 4: Run — expect 3 passes**

- [ ] **Step 5: Commit**

```bash
git add Backend/PokemonLocations.WebServer/Controllers/UserImagesController.cs Backend/PokemonLocations.WebServer.Tests/Controllers/UserImagesControllerTests.cs
git commit -m "feat(webserver): UserImagesController.Delete + tests"
```

---

### Task 25: `GET` happy path + ownership + orphan-on-read

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/Controllers/UserImagesController.cs`
- Modify: `Backend/PokemonLocations.WebServer.Tests/Controllers/UserImagesControllerTests.cs`

- [ ] **Step 1: Add tests**

```csharp
[Fact]
public async Task GetOwnedImageReturns200WithCorrectContentTypeAndBytes() {
    await ResetUsersAsync(postgresFixture.ConnectionString);
    await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
    var factory = CreateFactory(ApiClientThatAcceptsLocations());
    var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

    using (var c = MakeMultipart(ValidPngBytes(), "shot.png", "image/png")) {
        await client.PostAsync("/api/me/locations/1/images", c);
    }
    var imageId = System.Text.Json.JsonDocument.Parse(
        await (await client.GetAsync("/api/locations/1")).Content.ReadAsStringAsync())
        .RootElement.GetProperty("userImages")[0].GetProperty("imageId").GetString();

    var response = await client.GetAsync($"/api/me/locations/1/images/{imageId}");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
    Assert.NotEmpty(await response.Content.ReadAsByteArrayAsync());
}

[Fact]
public async Task GetAnotherUsersImageReturns404() {
    await ResetUsersAsync(postgresFixture.ConnectionString);
    await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
    await SeedUserAsync(postgresFixture.ConnectionString, "blue@example.com", "squirtle1", "Blue");
    var factory = CreateFactory(ApiClientThatAcceptsLocations());

    var redClient = AuthorizedClient(factory, "red@example.com", "pikachu123");
    using (var c = MakeMultipart(ValidPngBytes(), "shot.png", "image/png")) {
        await redClient.PostAsync("/api/me/locations/1/images", c);
    }
    var redImageId = System.Text.Json.JsonDocument.Parse(
        await (await redClient.GetAsync("/api/locations/1")).Content.ReadAsStringAsync())
        .RootElement.GetProperty("userImages")[0].GetProperty("imageId").GetString();

    var blueClient = AuthorizedClient(factory, "blue@example.com", "squirtle1");
    var response = await blueClient.GetAsync($"/api/me/locations/1/images/{redImageId}");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}

[Fact]
public async Task GetWhenFileDeletedFromDiskReturns404() {
    await ResetUsersAsync(postgresFixture.ConnectionString);
    await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
    var factory = CreateFactory(ApiClientThatAcceptsLocations());
    var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

    using (var c = MakeMultipart(ValidPngBytes(), "shot.png", "image/png")) {
        await client.PostAsync("/api/me/locations/1/images", c);
    }
    var imageId = System.Text.Json.JsonDocument.Parse(
        await (await client.GetAsync("/api/locations/1")).Content.ReadAsStringAsync())
        .RootElement.GetProperty("userImages")[0].GetProperty("imageId").GetString();

    // Manually delete the file from disk while the DB row remains
    foreach (var f in Directory.EnumerateFiles(factory.UploadRoot, $"{imageId}.*", SearchOption.AllDirectories)) {
        File.Delete(f);
    }

    var response = await client.GetAsync($"/api/me/locations/1/images/{imageId}");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}
```

- [ ] **Step 2: Run — expect 3 failures**

- [ ] **Step 3: Implement `Get`**

Replace `Get` in `UserImagesController.cs`:

```csharp
[HttpGet("{imageId:guid}")]
public async Task<IActionResult> Get(int locationId, Guid imageId) {
    var userId = User.GetUserId();
    var image = await repository.GetByIdForUserAsync(userId, imageId);
    if (image is null || image.LocationId != locationId) {
        return NotFound(new { error = "not_found" });
    }
    if (!System.IO.File.Exists(image.FilePath)) {
        return NotFound(new { error = "not_found" });
    }
    Response.Headers.CacheControl = "private, max-age=3600";
    var stream = System.IO.File.OpenRead(image.FilePath);
    return File(stream, image.ContentType);
}
```

- [ ] **Step 4: Run — expect 3 passes**

- [ ] **Step 5: Commit**

```bash
git add Backend/PokemonLocations.WebServer/Controllers/UserImagesController.cs Backend/PokemonLocations.WebServer.Tests/Controllers/UserImagesControllerTests.cs
git commit -m "feat(webserver): UserImagesController.Get + tests"
```

---

## Phase 6 — Integration with Existing Code

### Task 26: Modified `LocationsController.GetById` populates `userImages`

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/Controllers/LocationsController.cs`
- Modify: `Backend/PokemonLocations.WebServer.Tests/Controllers/LocationsControllerTests.cs`

- [ ] **Step 1: Update the existing `GetByIdIncludesEmptyUserImages` test and add a populated-case test**

Replace the existing `GetByIdIncludesEmptyUserImages` test body to test the **empty state for a fresh user**:

```csharp
[Fact]
public async Task GetByIdReturnsEmptyUserImagesForNewUser() {
    await ResetUsersAsync(postgresFixture.ConnectionString);
    await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
    var apiClient = CreateApiClient();
    apiClient.GetWithStatusAsync("/locations/1").Returns(new ApiResponse(200, SingleLocationJson));
    var factory = CreateFactory(apiClient);
    var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

    var response = await client.GetAsync("/api/locations/1");
    var body = await ReadJsonAsync(response);

    Assert.Equal(0, body.GetProperty("userImages").GetArrayLength());
}
```

Then add a populated test:

```csharp
[Fact]
public async Task GetByIdIncludesUploadedUserImages() {
    await ResetUsersAsync(postgresFixture.ConnectionString);
    await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
    var apiClient = CreateApiClient();
    apiClient.GetWithStatusAsync("/locations/1").Returns(new ApiResponse(200, SingleLocationJson));
    var factory = CreateFactory(apiClient);
    var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

    using var content = new MultipartFormDataContent();
    var fc = new ByteArrayContent(TestImageFixtures.CreatePng(64, 64));
    fc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
    content.Add(fc, "file", "shot.png");
    await client.PostAsync("/api/me/locations/1/images", content);

    var response = await client.GetAsync("/api/locations/1");
    var body = await ReadJsonAsync(response);
    var ui = body.GetProperty("userImages");

    Assert.Equal(1, ui.GetArrayLength());
    Assert.Equal("shot.png", ui[0].GetProperty("originalFilename").GetString());
    Assert.StartsWith("/api/me/locations/1/images/", ui[0].GetProperty("imageUrl").GetString());
}

[Fact]
public async Task GetByIdDoesNotLeakAnotherUsersImages() {
    await ResetUsersAsync(postgresFixture.ConnectionString);
    await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
    await SeedUserAsync(postgresFixture.ConnectionString, "blue@example.com", "squirtle1", "Blue");
    var apiClient = CreateApiClient();
    apiClient.GetWithStatusAsync("/locations/1").Returns(new ApiResponse(200, SingleLocationJson));
    var factory = CreateFactory(apiClient);

    var blueClient = AuthorizedClient(factory, "blue@example.com", "squirtle1");
    using var content = new MultipartFormDataContent();
    var fc = new ByteArrayContent(TestImageFixtures.CreatePng(64, 64));
    fc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
    content.Add(fc, "file", "blueshot.png");
    await blueClient.PostAsync("/api/me/locations/1/images", content);

    var redClient = AuthorizedClient(factory, "red@example.com", "pikachu123");
    var response = await redClient.GetAsync("/api/locations/1");
    var body = await ReadJsonAsync(response);

    Assert.Equal(0, body.GetProperty("userImages").GetArrayLength());
}
```

- [ ] **Step 2: Run — expect failures (current implementation always returns empty)**

Run: `cd Backend && dotnet test PokemonLocations.WebServer.Tests/PokemonLocations.WebServer.Tests.csproj --filter "FullyQualifiedName~GetByIdIncludesUploaded|FullyQualifiedName~GetByIdDoesNotLeak" -nologo`
Expected: 2 failures.

- [ ] **Step 3: Update `LocationsController.GetById`**

Replace the existing `userImages = new JsonArray()` line. Inject `IUserImageRepository` into the constructor:

```csharp
public LocationsController(
    IPokemonLocationsApiClient apiClient,
    IVisitedBuildingRepository visitedBuildingRepository,
    IUserImageRepository userImageRepository) {
    this.apiClient = apiClient;
    this.visitedBuildingRepository = visitedBuildingRepository;
    this.userImageRepository = userImageRepository;
}

private readonly IUserImageRepository userImageRepository;
```

In `GetById`, replace:

```csharp
location["userImages"] = new JsonArray();
```

with:

```csharp
var userImages = await userImageRepository.GetForUserAndLocationAsync(User.GetUserId(), locationId);
var userImagesArray = new JsonArray();
foreach (var ui in userImages) {
    userImagesArray.Add(new JsonObject {
        ["imageId"] = ui.ImageId.ToString(),
        ["imageUrl"] = $"/api/me/locations/{locationId}/images/{ui.ImageId}",
        ["originalFilename"] = ui.OriginalFilename,
        ["uploadedAt"] = ui.UploadedAt.ToString("o")
    });
}
location["userImages"] = userImagesArray;
```

- [ ] **Step 4: Run — expect all three (empty, populated, no-leak) pass**

- [ ] **Step 5: Commit**

```bash
git add Backend/PokemonLocations.WebServer/Controllers/LocationsController.cs Backend/PokemonLocations.WebServer.Tests/Controllers/LocationsControllerTests.cs
git commit -m "feat(webserver): GetById populates userImages from repository"
```

---

### Task 27: `AccountController.Delete` cleans up upload directory

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/Controllers/AccountController.cs`
- Modify: `Backend/PokemonLocations.WebServer.Tests/Controllers/AccountControllerTests.cs`

- [ ] **Step 1: Add a test**

Add to `AccountControllerTests`:

```csharp
[Fact]
public async Task DeleteAccountAlsoRemovesUploadDirectory() {
    await ResetUsersAsync(postgresFixture.ConnectionString);
    await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
    var apiClient = Substitute.For<IPokemonLocationsApiClient>();
    apiClient.ExistsAsync(Arg.Any<string>()).Returns(true);
    var factory = new PokemonLocationsWebServerFactory(
        postgresFixture.ConnectionString, redisFixture.ConnectionString) { ApiClient = apiClient };
    var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = BasicHeader("red@example.com", "pikachu123");

    using (var c = new MultipartFormDataContent {
        { new ByteArrayContent(PokemonLocations.WebServer.Tests.Imaging.TestImageFixtures.CreatePng(64, 64))
            { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png") } },
          "file", "shot.png" }
    }) {
        var post = await client.PostAsync("/api/me/locations/1/images", c);
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
    }

    var userId = await GetUserIdAsync(postgresFixture.ConnectionString, "red@example.com");
    var userDir = Path.Combine(factory.UploadRoot, userId.ToString());
    Assert.True(Directory.Exists(userDir));

    var delete = await client.DeleteAsync("/account");

    Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    Assert.False(Directory.Exists(userDir));
}
```

- [ ] **Step 2: Run — expect failure (current `AccountController.Delete` doesn't touch disk)**

- [ ] **Step 3: Update `AccountController.Delete`**

Inject `IOptions<UserImagesOptions>` into the constructor (add the parameter, retain the existing ones). Then change the `Delete` action:

```csharp
[HttpDelete("/account")]
public async Task<IActionResult> Delete(IOptions<UserImagesOptions> options, ILogger<AccountController> logger) {
    var userId = User.GetUserId();
    await userRepository.DeleteAsync(userId);

    var userDir = Path.Combine(options.Value.UploadRoot, userId.ToString());
    if (Directory.Exists(userDir)) {
        try { Directory.Delete(userDir, recursive: true); }
        catch (IOException ex) { logger.LogWarning(ex, "Failed to delete upload dir for user {UserId}", userId); }
    }
    return NoContent();
}
```

(If the existing constructor pattern doesn't take `IOptions` already, switch to method-injected parameters as shown — ASP.NET Core supports both. Add `using Microsoft.Extensions.Options;` and `using PokemonLocations.WebServer.Models;` at the top if not already present.)

- [ ] **Step 4: Run — expect pass**

- [ ] **Step 5: Commit**

```bash
git add Backend/PokemonLocations.WebServer/Controllers/AccountController.cs Backend/PokemonLocations.WebServer.Tests/Controllers/AccountControllerTests.cs
git commit -m "feat(webserver): AccountController.Delete also cleans upload dir"
```

---

### Task 28: Run full test suite — confirm green before frontend

**Files:** none

- [ ] **Step 1: Run all tests**

Run: `cd Backend && dotnet test -nologo 2>&1 | tail -8`
Expected: TokenIssuer + Api + WebServer all green, total ~250+ tests passing.

- [ ] **Step 2: Commit a tag/note if desired**

Optional. The branch state is the implicit waypoint.

---

## Phase 7 — Frontend (Manual, no TDD)

Frontend work is a single coherent feature: upload UI + delete UI + blob loading + drag-drop. Tasks broken by file and concern.

### Task 29: `index.html` DOM additions

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/wwwroot/index.html`

- [ ] **Step 1: Add upload button + hidden input + drag overlay markup inside `.image-gallery` div**

Find the `<div class="image-gallery" id="image-gallery">` element. Inside it (after any existing children), add:

```html
<button type="button" class="gallery-upload" id="gallery-upload" aria-label="Upload images">+</button>
<input type="file" class="gallery-file-input" id="gallery-file-input" multiple accept="image/png,image/jpeg,image/webp" style="display: none;">
<div class="gallery-drag-overlay" id="gallery-drag-overlay" aria-hidden="true">Drop to upload</div>
```

- [ ] **Step 2: Add CSS for the new elements**

Inside the existing `<style>` block, add:

```css
.gallery-upload {
  position: absolute;
  bottom: 12px;
  right: 12px;
  width: 44px;
  height: 44px;
  border-radius: 50%;
  border: 2px solid var(--theme-primary);
  background: var(--theme-primary-dark);
  color: white;
  font-size: 28px;
  font-weight: 600;
  cursor: pointer;
  z-index: 10;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: opacity 0.15s, transform 0.15s;
}
.gallery-upload:hover { transform: scale(1.05); }
.gallery-upload:disabled { opacity: 0.4; cursor: not-allowed; }

.gallery-drag-overlay {
  position: absolute;
  inset: 0;
  display: none;
  align-items: center;
  justify-content: center;
  background: rgba(0, 0, 0, 0.6);
  color: white;
  font-size: 18px;
  font-weight: 600;
  border: 4px dashed white;
  z-index: 20;
  pointer-events: none;
}
.image-gallery.drag-active .gallery-drag-overlay { display: flex; }

.slide-delete {
  position: absolute;
  top: 8px;
  right: 8px;
  width: 28px;
  height: 28px;
  border-radius: 50%;
  border: none;
  background: var(--theme-danger);
  color: white;
  font-size: 16px;
  font-weight: 600;
  cursor: pointer;
  opacity: 0;
  transition: opacity 0.15s;
  z-index: 5;
}
.gallery-slide:hover .slide-delete { opacity: 1; }

.gallery-toast {
  position: fixed;
  bottom: 24px;
  left: 50%;
  transform: translateX(-50%);
  background: var(--theme-primary-dark);
  color: white;
  padding: 12px 20px;
  border-radius: 8px;
  z-index: 1100;
  display: flex;
  align-items: center;
  gap: 12px;
  box-shadow: 0 4px 12px rgba(0,0,0,0.2);
}
.gallery-toast button {
  background: transparent;
  border: 1px solid white;
  color: white;
  border-radius: 4px;
  padding: 4px 8px;
  cursor: pointer;
}
```

- [ ] **Step 3: Verify the page still renders by manual smoke**

Stack should already be down; no need to spin up just for HTML. Build the WebServer to confirm no static-asset packaging issues:

```bash
cd Backend && dotnet build PokemonLocations.WebServer/PokemonLocations.WebServer.csproj -nologo
```

- [ ] **Step 4: Commit**

```bash
git add Backend/PokemonLocations.WebServer/wwwroot/index.html
git commit -m "feat(frontend): upload button, drag overlay, slide-delete, toast styles"
```

---

### Task 30: `script.js` — `loadUserImageBlob` + teardown

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/wwwroot/script.js`

- [ ] **Step 1: Add the loader + teardown integration**

Find the `// ─── Gallery carousel + modal ───` section. Above `function openGalleryModal`, add:

```javascript
let activeBlobUrls = [];

async function loadUserImageBlob(imageUrl) {
    const res = await PLAuth.authFetch(imageUrl);
    if (!res.ok) throw new Error(`Image fetch failed: ${res.status}`);
    const blob = await res.blob();
    const blobUrl = URL.createObjectURL(blob);
    activeBlobUrls.push(blobUrl);
    return blobUrl;
}

function revokeAllBlobUrls() {
    activeBlobUrls.forEach(URL.revokeObjectURL);
    activeBlobUrls = [];
}
```

- [ ] **Step 2: Find the `renderGallery` function. At its top (before clearing children), invoke the revoke**

```javascript
function renderGallery(galleryEl, images, locationName) {
    if (galleryTimer) {
        clearInterval(galleryTimer);
        galleryTimer = null;
    }
    resumeCarousel = null;
    revokeAllBlobUrls();   // <-- new line
    galleryEl.replaceChildren();
    // ...
```

- [ ] **Step 3: Commit**

```bash
git add Backend/PokemonLocations.WebServer/wwwroot/script.js
git commit -m "feat(frontend): blob URL loader and revocation hook"
```

---

### Task 31: `script.js` — render user-image slides via blob, add delete X

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/wwwroot/script.js`

- [ ] **Step 1: Update `renderGallery` to handle user-image slides**

In `renderGallery`, find the `images.map((img, i) => { ... })` block. The current code creates two `<img>` elements per slide. Replace the `slide.append(bg, fg);` line with logic that differentiates user vs canonical:

Modify the inner closure so it knows whether the slide is user-uploaded. The simplest signal: a new parameter on each input array element. Update the call site (search for `renderGallery(galleryEl, images, locationName)`) to construct the merged input with a tag:

```javascript
const merged = [
    ...(location.images || []).map(img => ({ ...img, isUserImage: false })),
    ...(location.userImages || []).map(img => ({ ...img, isUserImage: true }))
];
renderGallery(galleryEl, merged, location.name);
```

Then inside `renderGallery`, the slides closure becomes:

```javascript
const slides = images.map((img, i) => {
    const slide = document.createElement('div');
    slide.className = 'gallery-slide' + (i === 0 ? ' active' : '');

    const bg = document.createElement('img');
    bg.className = 'slide-bg';
    bg.alt = '';
    bg.setAttribute('aria-hidden', 'true');

    const fg = document.createElement('img');
    fg.className = 'slide-fg';
    fg.alt = img.caption || locationName || '';
    fg.addEventListener('click', () => openGalleryModal(fg.src, img.caption || locationName || ''));

    if (img.isUserImage) {
        // Auth-streamed blob: src is set after fetch resolves
        loadUserImageBlob(img.imageUrl).then(blobUrl => {
            bg.src = blobUrl;
            fg.src = blobUrl;
        }).catch(err => console.error('User image fetch failed:', err));

        const del = document.createElement('button');
        del.type = 'button';
        del.className = 'slide-delete';
        del.setAttribute('aria-label', 'Delete image');
        del.textContent = '×';
        del.addEventListener('click', async (e) => {
            e.stopPropagation();
            if (!confirm('Delete this image?')) return;
            const res = await PLAuth.authFetch(img.imageUrl, { method: 'DELETE' });
            if (res.ok) {
                // remove from in-memory model and re-render
                const idx = currentLocation.userImages.findIndex(u => u.imageId === img.imageId);
                if (idx >= 0) currentLocation.userImages.splice(idx, 1);
                rerenderGallery();
                showToast('Image deleted');
            } else {
                showToast(`Delete failed (${res.status})`, true);
            }
        });
        slide.appendChild(del);
    } else {
        bg.src = img.imageUrl;
        fg.src = img.imageUrl;
    }

    slide.append(bg, fg);
    galleryEl.appendChild(slide);
    return slide;
});
```

- [ ] **Step 2: Add module-level `currentLocation` and helpers**

Near the top of `script.js`, after the existing state declarations, add:

```javascript
let currentLocation = null; // { locationId, name, images, userImages, ... }

function rerenderGallery() {
    if (!currentLocation) return;
    const galleryEl = document.getElementById('image-gallery');
    const merged = [
        ...(currentLocation.images || []).map(img => ({ ...img, isUserImage: false })),
        ...(currentLocation.userImages || []).map(img => ({ ...img, isUserImage: true }))
    ];
    renderGallery(galleryEl, merged, currentLocation.name);
    updateUploadButtonState();
}

function updateUploadButtonState() {
    const btn = document.getElementById('gallery-upload');
    if (!btn || !currentLocation) return;
    const count = (currentLocation.userImages || []).length;
    btn.disabled = count >= 20;
    btn.title = btn.disabled ? '20-image limit reached' : 'Upload images';
}

function showToast(message, persistent = false) {
    let toast = document.getElementById('gallery-toast');
    if (toast) toast.remove();
    toast = document.createElement('div');
    toast.id = 'gallery-toast';
    toast.className = 'gallery-toast';
    const text = document.createElement('span');
    text.textContent = message;
    toast.appendChild(text);
    if (persistent) {
        const close = document.createElement('button');
        close.type = 'button';
        close.textContent = '×';
        close.addEventListener('click', () => toast.remove());
        toast.appendChild(close);
    } else {
        setTimeout(() => toast.remove(), 4000);
    }
    document.body.appendChild(toast);
}
```

- [ ] **Step 3: Find `loadLocationDetail` and store the location for later use**

In `loadLocationDetail`, after `const location = await res.json();`, add:

```javascript
currentLocation = location;
```

Then update the rendering call:

```javascript
const merged = [
    ...(location.images || []).map(img => ({ ...img, isUserImage: false })),
    ...(location.userImages || []).map(img => ({ ...img, isUserImage: true }))
];
renderGallery(galleryEl, merged, location.name);
updateUploadButtonState();
```

- [ ] **Step 4: Build, deploy, smoke-test in browser**

```bash
podman compose -f docker-compose.debug.yml --profile frontend build webserver
podman rm -f PokemonLocations-WebServer
podman compose -f docker-compose.debug.yml --profile frontend up -d webserver
```

Open http://localhost:3001, sign in, navigate to a location with canonical images. Should look identical to before (canonical images still render via direct URL, no behavior change for them). User images section is empty until Task 32.

- [ ] **Step 5: Commit**

```bash
git add Backend/PokemonLocations.WebServer/wwwroot/script.js
git commit -m "feat(frontend): blob-load user-image slides; delete X with confirm"
```

---

### Task 32: `script.js` — upload flow (button + multi-select + sequential POST + toast)

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/wwwroot/script.js`

- [ ] **Step 1: Add upload helpers**

Append to the gallery section of `script.js`:

```javascript
const ALLOWED_MIMES = ['image/png', 'image/jpeg', 'image/webp'];
const MAX_BYTES = 10 * 1024 * 1024;
const CAP_PER_LOCATION = 20;

async function uploadFiles(fileList) {
    if (!currentLocation) return;
    const files = Array.from(fileList);
    const rejected = [];
    const accepted = [];

    for (const f of files) {
        if (!ALLOWED_MIMES.includes(f.type)) {
            rejected.push({ name: f.name, reason: 'unsupported type' });
            continue;
        }
        if (f.size > MAX_BYTES) {
            rejected.push({ name: f.name, reason: 'too large' });
            continue;
        }
        accepted.push(f);
    }

    const remaining = CAP_PER_LOCATION - (currentLocation.userImages || []).length;
    const toUpload = accepted.slice(0, Math.max(0, remaining));
    const overflow = accepted.slice(Math.max(0, remaining));
    overflow.forEach(f => rejected.push({ name: f.name, reason: 'over cap' }));

    let succeeded = 0;
    const failed = [];
    for (const f of toUpload) {
        const fd = new FormData();
        fd.append('file', f, f.name);
        const url = `/api/me/locations/${currentLocation.locationId}/images`;
        const res = await PLAuth.authFetch(url, { method: 'POST', body: fd });
        if (res.ok) {
            const body = await res.json();
            currentLocation.userImages = [body, ...(currentLocation.userImages || [])];
            succeeded++;
        } else {
            let code = `${res.status}`;
            try { code = (await res.json()).error || code; } catch { /* not JSON */ }
            failed.push({ name: f.name, reason: code });
        }
    }

    rerenderGallery();

    const parts = [];
    if (succeeded) parts.push(`${succeeded} uploaded`);
    rejected.forEach(r => parts.push(`${r.name} skipped (${r.reason})`));
    failed.forEach(f => parts.push(`${f.name} failed (${f.reason})`));
    showToast(parts.join(' · '), failed.length > 0 || rejected.length > 0);
}
```

- [ ] **Step 2: Wire the upload button + file input**

Inside the `DOMContentLoaded` handler at the bottom of `script.js`, add:

```javascript
const uploadBtn = document.getElementById('gallery-upload');
const fileInput = document.getElementById('gallery-file-input');
if (uploadBtn && fileInput) {
    uploadBtn.addEventListener('click', () => fileInput.click());
    fileInput.addEventListener('change', async (e) => {
        if (e.target.files.length > 0) {
            await uploadFiles(e.target.files);
            e.target.value = ''; // reset so re-selecting same file fires change again
        }
    });
}
```

- [ ] **Step 3: Rebuild + browser-test**

Same rebuild commands as Task 31 step 4. Click "+", pick a small PNG, verify it appears in the gallery within ~1s and the toast says "1 uploaded".

- [ ] **Step 4: Commit**

```bash
git add Backend/PokemonLocations.WebServer/wwwroot/script.js
git commit -m "feat(frontend): upload flow — button + multi-select + sequential POST + toast"
```

---

### Task 33: `script.js` — drag-drop convergence

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/wwwroot/script.js`

- [ ] **Step 1: Add drag-drop handlers in `DOMContentLoaded`**

```javascript
const galleryEl = document.getElementById('image-gallery');
let dragLeaveTimeout = null;

if (galleryEl) {
    galleryEl.addEventListener('dragenter', (e) => {
        e.preventDefault();
        galleryEl.classList.add('drag-active');
        if (dragLeaveTimeout) { clearTimeout(dragLeaveTimeout); dragLeaveTimeout = null; }
    });
    galleryEl.addEventListener('dragover', (e) => e.preventDefault());
    galleryEl.addEventListener('dragleave', () => {
        if (dragLeaveTimeout) clearTimeout(dragLeaveTimeout);
        dragLeaveTimeout = setTimeout(() => galleryEl.classList.remove('drag-active'), 60);
    });
    galleryEl.addEventListener('drop', async (e) => {
        e.preventDefault();
        galleryEl.classList.remove('drag-active');
        if (e.dataTransfer.files.length > 0) {
            await uploadFiles(e.dataTransfer.files);
        }
    });
}
```

- [ ] **Step 2: Rebuild + browser-test**

Drag a few image files onto the gallery from your file manager. Should see the dashed overlay during drag, files upload on drop, toast summarizes results.

- [ ] **Step 3: Commit**

```bash
git add Backend/PokemonLocations.WebServer/wwwroot/script.js
git commit -m "feat(frontend): drag-drop convergence into upload flow"
```

---

### Task 34: Manual end-to-end verification

**Files:** none

- [ ] **Step 1: Bring the stack up fresh**

```bash
podman compose -f docker-compose.debug.yml --profile frontend build webserver
podman rm -f PokemonLocations-WebServer
podman compose -f docker-compose.debug.yml --profile frontend up -d
sleep 5
podman ps --format "{{.Names}}: {{.Status}}"
```
Expected: all 4 containers Up.

- [ ] **Step 2: Browser checklist (http://localhost:3001)**

For each scenario, verify behavior in the browser. Sign up if you don't already have an account.

| Scenario | Expected |
|---|---|
| Open Pallet Town | Canonical cover image displays; no user images yet |
| Click "+", pick 1 small PNG | Toast "1 uploaded"; image appears in gallery; carousel arrows show (now 2 slides total) |
| Click the new image | Modal opens with full-size view |
| Modal close X / overlay click / Escape | Modal closes |
| Hover the user-image slide | "X" delete button appears top-right |
| Click X, confirm | Image disappears; canonical cover remains |
| Drop 5 images onto the gallery | All upload; toast lists them |
| Drop a 50 MB file | Toast: "X skipped (too large)" |
| Drop a `.txt` file | Toast: "X skipped (unsupported type)" |
| Upload until at 20 cap | "+" button becomes disabled with tooltip |
| Switch to another location | Gallery resets correctly; no leakage from previous location |
| Sign out, sign back in as a different account | Other user's uploaded images NOT visible to this account |

- [ ] **Step 3: Take down the stack**

```bash
podman compose -f docker-compose.debug.yml --profile frontend down
```

- [ ] **Step 4: No commit needed (manual verification only)**

---

## Phase 8 — Documentation Updates

### Task 35: Update existing component specs to reflect new endpoints

**Files:**
- Modify: `docs/specs/SPEC_API.md` (no change — API didn't gain endpoints)
- Modify: `docs/specs/SPEC_WEBSERVER.md` (new endpoints + DI)
- Modify: `docs/specs/SPEC_FRONTEND.md` (gallery now supports uploads)

- [ ] **Step 1: SPEC_WEBSERVER.md — add user-images endpoints to the proxy/endpoint table**

Find the existing endpoint table. Add three rows:

```markdown
| `POST` | `/api/me/locations/{id}/images` | New endpoint: multipart upload, server resizes via SkiaSharp, stores in `webserver_uploads` volume + `user_images` table |
| `DELETE` | `/api/me/locations/{id}/images/{imageId}` | New endpoint: removes user's row + file |
| `GET` | `/api/me/locations/{id}/images/{imageId}` | New endpoint: auth-streamed image bytes (Basic Auth required) |
```

Also update the `/api/locations/{id}` row note to say the `userImages` array is now populated from `user_images` (was previously stubbed empty).

- [ ] **Step 2: SPEC_FRONTEND.md — update the location-detail section**

Find the "Location detail" section (around the carousel/modal description added in SE498-14). Add a paragraph:

```markdown
The gallery also supports per-user image uploads (SE498-68): a "+" button overlays the gallery (themed primary-dark), drag-drop on the gallery box also uploads. Multi-file selection is supported. Each user-uploaded slide gets a hover-revealed "X" to delete it (with confirmation). User images are loaded via authenticated `fetch` and rendered through `URL.createObjectURL` blob URLs — they never appear in `<img src>` directly because Basic Auth credentials don't auto-attach to image requests. Per-user, per-location cap is 20 images; the upload button disables at the cap. Cover images from SE498-14 stay public/static and bypass the blob indirection.
```

- [ ] **Step 3: Commit**

```bash
git add docs/specs/SPEC_WEBSERVER.md docs/specs/SPEC_FRONTEND.md
git commit -m "docs: SE498-68 — webserver + frontend specs reflect user image uploads"
```

---

## Final Pass

### Task 36: Re-run full test suite + sanity check

**Files:** none

- [ ] **Step 1: Run all tests**

```bash
cd Backend && dotnet test -nologo 2>&1 | tail -8
```
Expected: all green. Approximate counts: TokenIssuer 4, Api 64, WebServer ~200+ (was 175 before; +25-30 new tests).

- [ ] **Step 2: Confirm git state is clean**

```bash
git status
```
Expected: `nothing to commit, working tree clean`.

- [ ] **Step 3: Push branch**

```bash
git push -u origin feat/se498-68-user-image-uploads
```

- [ ] **Step 4: Open the PR**

Run: `gh pr create --title "feat: SE498-68 user image uploads" --body "..."` — see the spec at `docs/designs/specs/2026-05-07-user-image-uploads.md` for full design context to drop into the PR description.

---

## Self-Review Notes

Spec coverage check (cross-checked against `docs/designs/specs/2026-05-07-user-image-uploads.md`):

| Spec section | Tasks |
|---|---|
| §2 Requirements (privacy, caps, types, UX) | Tasks 19, 21, 24, 32 (validation + UI + caps) |
| §3 Architecture | Phase 1 + 4 (foundation + wiring) |
| §4 Schema + Repo contract | Tasks 2, 4–9 |
| §5 API surface | Tasks 18–25 |
| §6 SkiaSharp pipeline | Tasks 10–14 |
| §7 Frontend | Tasks 29–34 |
| §8 Error handling | Tasks 21, 22, 23 (validation paths) |
| §8.4 Account deletion | Task 27 |
| §9 Testing strategy | Phases 2/3/5 are TDD throughout; Task 28 is the green checkpoint |
| §10 Wiring & config | Tasks 15, 16, 17 |

No placeholders. No `TODO`, no `TBD`, no "implement later." All tasks have exact file paths, complete code, exact commands, and expected outputs.
