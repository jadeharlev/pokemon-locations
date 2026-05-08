# SE498-90 Random Theme Re-Randomizes on (Re)Load — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Persist `'random'` as a valid value for `user.theme`. When the user picks "Random", the server stores `'random'` (not the rolled theme), and every page (re)load resolves a fresh random theme from their unlocked set.

**Architecture:** Two-layer change. Backend: add `'random'` to the `user_theme` Postgres enum and to the `Themes` validation set; the existing `PUT /account/theme` and `GET /api/me` endpoints surface the new value without controller changes. Frontend: the theme-button click handler sends `selectedTheme` (which may be `'random'`) instead of the resolved name, and the `/api/me` response handler resolves a fresh random theme when `user.theme === 'random'`.

**Tech Stack:** ASP.NET 10, Dapper + Postgres, Vanilla JS frontend, xUnit + Testcontainers + WebApplicationFactory.

**Spec reference:** `docs/designs/specs/2026-05-08-random-theme-on-reload.md`

---

## File Structure

### New files

| Path | Purpose |
|---|---|
| `Backend/PokemonLocations.WebServer/Database/Migrations/0011_add_random_theme.sql` | Migration: adds `'random'` to the `user_theme` enum |

### Modified files

| Path | Change |
|---|---|
| `Backend/PokemonLocations.WebServer/Models/Themes.cs` | Add `Random` constant; add to the `All` set |
| `Backend/PokemonLocations.WebServer.Tests/Database/UserRepositoryTests.cs` | Add `[InlineData("random")]` to the `UpdateThemeAsyncPersistsTheme` theory |
| `Backend/PokemonLocations.WebServer.Tests/Controllers/AccountControllerTests.cs` | Add `[InlineData("random")]` to the `UpdateThemeReturns204AndPersists` theory |
| `Backend/PokemonLocations.WebServer/wwwroot/script.js` | Theme-button click handler: rename `theme` → `resolved`, send `selectedTheme` to server, label uses `selectedTheme`. `/api/me` response handler: resolve `'random'` to a concrete theme via `pickRandomTheme()` before applying. |

---

## Phase 1 — Backend

### Task 1: Migration 0011 — add `'random'` to the user_theme enum

**Files:**
- Create: `Backend/PokemonLocations.WebServer/Database/Migrations/0011_add_random_theme.sql`

- [ ] **Step 1: Write the migration**

```sql
ALTER TYPE user_theme ADD VALUE IF NOT EXISTS 'random';
```

- [ ] **Step 2: Build to confirm migration is embedded**

Run: `cd /Users/makspopov/Documents/SE498/src/pokemon-locations/Backend && dotnet build PokemonLocations.WebServer/PokemonLocations.WebServer.csproj -nologo 2>&1 | tail -3`

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
cd /Users/makspopov/Documents/SE498/src/pokemon-locations
git add Backend/PokemonLocations.WebServer/Database/Migrations/0011_add_random_theme.sql
git commit -m "feat(webserver): migration 0011 — add 'random' to user_theme enum"
```

---

### Task 2: `Themes.cs` — add `Random` constant + include in validation set

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/Models/Themes.cs`

- [ ] **Step 1: Add `Random` constant + include in `All`**

The current file declares constants for each theme and an `All` HashSet used by `IsValid`. After the existing `Dragonite` constant, add:

```csharp
public const string Random = "random";
```

In the `All` HashSet initializer, add `Random` to the list:

```csharp
private static readonly HashSet<string> All = new(StringComparer.Ordinal)
{
    Bulbasaur,
    Charmander,
    Squirtle,
    Pikachu,
    Rattata,
    Diglett,
    Geodude,
    Dratini,
    Mew,
    Dragonite,
    Random,
};
```

- [ ] **Step 2: Build**

Run: `cd /Users/makspopov/Documents/SE498/src/pokemon-locations/Backend && dotnet build PokemonLocations.WebServer/PokemonLocations.WebServer.csproj -nologo 2>&1 | tail -3`

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
cd /Users/makspopov/Documents/SE498/src/pokemon-locations
git add Backend/PokemonLocations.WebServer/Models/Themes.cs
git commit -m "feat(webserver): Themes.IsValid accepts 'random'"
```

---

### Task 3: `UserRepositoryTests` — round-trip `'random'`

**Files:**
- Modify: `Backend/PokemonLocations.WebServer.Tests/Database/UserRepositoryTests.cs`

The existing `UpdateThemeAsyncPersistsTheme` theory takes a theme name as `[InlineData]`, calls `UpdateThemeAsync`, then verifies `GetByIdAsync` returns it. Adding `[InlineData("random")]` is enough to cover persistence + retrieval round-trip.

- [ ] **Step 1: Add the theory case**

In the existing test, find this block (around line 89-93):

```csharp
[Theory]
[InlineData("bulbasaur")]
[InlineData("charmander")]
[InlineData("squirtle")]
[InlineData("pikachu")]
public async Task UpdateThemeAsyncPersistsTheme(string theme) {
```

Add a new line:

```csharp
[Theory]
[InlineData("bulbasaur")]
[InlineData("charmander")]
[InlineData("squirtle")]
[InlineData("pikachu")]
[InlineData("random")]
public async Task UpdateThemeAsyncPersistsTheme(string theme) {
```

- [ ] **Step 2: Run the test**

Run: `cd /Users/makspopov/Documents/SE498/src/pokemon-locations/Backend && dotnet test PokemonLocations.WebServer.Tests/PokemonLocations.WebServer.Tests.csproj --filter "FullyQualifiedName~UpdateThemeAsyncPersistsTheme" -nologo 2>&1 | tail -10`

Expected: 5 passed (4 existing + the new `random` case). Migration 0011 makes `'random'` a valid enum value at the DB layer; `UserRepository.UpdateThemeAsync` is just SQL parameter binding so no code change is needed.

- [ ] **Step 3: Commit**

```bash
cd /Users/makspopov/Documents/SE498/src/pokemon-locations
git add Backend/PokemonLocations.WebServer.Tests/Database/UserRepositoryTests.cs
git commit -m "test(webserver): round-trip 'random' theme through UserRepository"
```

---

### Task 4: `AccountControllerTests` — `PUT /account/theme` with `'random'`

**Files:**
- Modify: `Backend/PokemonLocations.WebServer.Tests/Controllers/AccountControllerTests.cs`

The existing `UpdateThemeReturns204AndPersists` theory takes a theme name, PUTs it, then GETs `/api/me` and asserts the theme came back. Adding `[InlineData("random")]` covers both the validator (`Themes.IsValid("random")` after Task 2) and the full request → DB → response round-trip.

- [ ] **Step 1: Add the theory case**

In the existing test, find this block (around line 265-273):

```csharp
[Theory]
[InlineData("bulbasaur")]
[InlineData("charmander")]
[InlineData("squirtle")]
[InlineData("pikachu")]
public async Task UpdateThemeReturns204AndPersists(string theme) {
```

Add a new line:

```csharp
[Theory]
[InlineData("bulbasaur")]
[InlineData("charmander")]
[InlineData("squirtle")]
[InlineData("pikachu")]
[InlineData("random")]
public async Task UpdateThemeReturns204AndPersists(string theme) {
```

- [ ] **Step 2: Run the test**

Run: `cd /Users/makspopov/Documents/SE498/src/pokemon-locations/Backend && dotnet test PokemonLocations.WebServer.Tests/PokemonLocations.WebServer.Tests.csproj --filter "FullyQualifiedName~UpdateThemeReturns204AndPersists" -nologo 2>&1 | tail -10`

Expected: 5 passed. The validator path now lets `'random'` through (Task 2), and the DB now accepts it (Task 1). `/api/me` returns the persisted value as a string.

- [ ] **Step 3: Commit**

```bash
cd /Users/makspopov/Documents/SE498/src/pokemon-locations
git add Backend/PokemonLocations.WebServer.Tests/Controllers/AccountControllerTests.cs
git commit -m "test(webserver): PUT /account/theme accepts 'random' end-to-end"
```

---

### Task 5: Run the full test suite — backend checkpoint

**Files:** none

- [ ] **Step 1: Run all tests**

Run: `cd /Users/makspopov/Documents/SE498/src/pokemon-locations/Backend && dotnet test -nologo 2>&1 | tail -8`

Expected: TokenIssuer + Api + WebServer all green. WebServer count should be the previous baseline + 2 new theory cases.

- [ ] **Step 2: No commit** (verification only).

---

## Phase 2 — Frontend

### Task 6: `script.js` — theme-button click handler

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/wwwroot/script.js`

Per spec §4.4 (a), three coordinated changes inside the existing click handler (around lines 955–987):

1. Rename the local `theme` to `resolved` — making the distinction between *preference* and *applied name* explicit.
2. Change the `body: JSON.stringify(...)` to send `selectedTheme` (the preference) instead of the resolved name.
3. Change the `themeLabel.textContent = formatThemeName(...)` line to use `selectedTheme` so the "Theme: ..." label reads "Random" after the user clicks Random.

- [ ] **Step 1: Apply the click-handler edit**

Find this existing block:

```javascript
document.querySelectorAll('.theme-option').forEach(btn => {
    btn.addEventListener('click', async () => {
        const selectedTheme = btn.dataset.theme;
        const theme = selectedTheme === 'random' ? pickRandomTheme() : selectedTheme;
        if (!isThemeUnlocked(theme)) {
            alert(`This theme is locked. ${THEME_UNLOCK_RULES[theme].label} to unlock it.`);
            return;
        }

        const previous = document.documentElement.getAttribute('data-theme') || 'bulbasaur';
        applyTheme(theme);

        try {
            const res = await PLAuth.authFetch('/account/theme', {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ theme })
            });
            if (!res.ok) {
                applyTheme(previous);
                alert('Failed to update theme.');
                return;
            }
            const themeLabel = document.querySelector('#user-info p:nth-child(2) strong');
            if (themeLabel) themeLabel.textContent = formatThemeName(theme);
            themeModal.hide();
        } catch (e) {
            applyTheme(previous);
            alert('Failed to update theme.');
            console.error('Theme update failed:', e.message);
        }
    });
});
```

Replace it with:

```javascript
document.querySelectorAll('.theme-option').forEach(btn => {
    btn.addEventListener('click', async () => {
        const selectedTheme = btn.dataset.theme;
        const resolved = selectedTheme === 'random' ? pickRandomTheme() : selectedTheme;
        if (!isThemeUnlocked(resolved)) {
            alert(`This theme is locked. ${THEME_UNLOCK_RULES[resolved].label} to unlock it.`);
            return;
        }

        const previous = document.documentElement.getAttribute('data-theme') || 'bulbasaur';
        applyTheme(resolved);

        try {
            const res = await PLAuth.authFetch('/account/theme', {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ theme: selectedTheme })
            });
            if (!res.ok) {
                applyTheme(previous);
                alert('Failed to update theme.');
                return;
            }
            const themeLabel = document.querySelector('#user-info p:nth-child(2) strong');
            if (themeLabel) themeLabel.textContent = formatThemeName(selectedTheme);
            themeModal.hide();
        } catch (e) {
            applyTheme(previous);
            alert('Failed to update theme.');
            console.error('Theme update failed:', e.message);
        }
    });
});
```

- [ ] **Step 2: Build to verify static assets pack cleanly**

Run: `cd /Users/makspopov/Documents/SE498/src/pokemon-locations/Backend && dotnet build PokemonLocations.WebServer/PokemonLocations.WebServer.csproj -nologo 2>&1 | tail -3`

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
cd /Users/makspopov/Documents/SE498/src/pokemon-locations
git add Backend/PokemonLocations.WebServer/wwwroot/script.js
git commit -m "feat(frontend): persist 'random' as theme preference; label reflects choice"
```

---

### Task 7: `script.js` — `/api/me` response resolves `'random'`

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/wwwroot/script.js`

Per spec §4.4 (b), when `/api/me` returns and `user.theme === 'random'`, the frontend should pick a fresh random theme rather than calling `applyTheme('random')` directly. The displayed user-info paragraph does NOT need to change — `formatThemeName('random')` already returns `"Random"`.

- [ ] **Step 1: Apply the load-handler edit**

Find this existing block (around lines 845–860):

```javascript
try {
    const res = await apiFetch('/me');
    if (!res.ok) throw new Error(`Status: ${res.status}`);
    const user = await res.json();

    applyTheme(user.theme);
    setHomePlanet(user.permanentPlanetName ?? null);

    container.innerHTML = `
        <p>Logged in as: <strong>${escapeHtml(user.displayName)}</strong></p>
        <p>Theme: <strong>${escapeHtml(formatThemeName(user.theme))}</strong></p>
    `;
} catch (e) {
    container.innerHTML = '<p class="loading-text">Not signed in</p>';
    console.error('Failed to load user info:', e.message);
}
```

Change ONE line: `applyTheme(user.theme)` resolves `'random'` first. Replace the block with:

```javascript
try {
    const res = await apiFetch('/me');
    if (!res.ok) throw new Error(`Status: ${res.status}`);
    const user = await res.json();

    const themeToApply = user.theme === 'random' ? pickRandomTheme() : user.theme;
    applyTheme(themeToApply);
    setHomePlanet(user.permanentPlanetName ?? null);

    container.innerHTML = `
        <p>Logged in as: <strong>${escapeHtml(user.displayName)}</strong></p>
        <p>Theme: <strong>${escapeHtml(formatThemeName(user.theme))}</strong></p>
    `;
} catch (e) {
    container.innerHTML = '<p class="loading-text">Not signed in</p>';
    console.error('Failed to load user info:', e.message);
}
```

The user-info paragraph still uses `user.theme` directly (which renders as `"Random"` for the random preference, exactly what we want).

- [ ] **Step 2: Build**

Run: `cd /Users/makspopov/Documents/SE498/src/pokemon-locations/Backend && dotnet build PokemonLocations.WebServer/PokemonLocations.WebServer.csproj -nologo 2>&1 | tail -3`

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
cd /Users/makspopov/Documents/SE498/src/pokemon-locations
git add Backend/PokemonLocations.WebServer/wwwroot/script.js
git commit -m "feat(frontend): resolve 'random' theme on /api/me load"
```

---

## Phase 3 — Verification

### Task 8: Manual end-to-end browser verification

**Files:** none

- [ ] **Step 1: Bring the stack up**

```bash
cd /Users/makspopov/Documents/SE498/src/pokemon-locations
podman compose -f docker-compose.debug.yml --profile frontend build webserver
podman rm -f PokemonLocations-WebServer
podman compose -f docker-compose.debug.yml --profile frontend up -d
sleep 5
podman ps --format "{{.Names}}: {{.Status}}"
```

Expected: 4 containers Up.

- [ ] **Step 2: Browser checklist (http://localhost:3001)**

Sign in, then open the theme picker. Run through each:

| Scenario | Expected |
|---|---|
| Click Random | Page applies a theme; the "Theme: …" line in the user-info area reads `Random` (not the rolled name). |
| Hard-refresh the page | Page applies a theme — possibly different from the previous one. Theme label still reads `Random`. |
| Hard-refresh several more times | Theme cycles through whichever themes are unlocked, never settling. Label stays `Random`. |
| Click a specific theme (e.g. Pikachu) | Page applies Pikachu; label reads `Pikachu`. |
| Hard-refresh after picking Pikachu | Theme stays Pikachu across reloads. Label stays `Pikachu`. |
| As a fresh user with only default themes unlocked, click Random | Each reload cycles among Bulbasaur / Charmander / Squirtle. |
| Inspect `/api/me` response in Network tab after picking Random | `theme: "random"` in the JSON body. |

- [ ] **Step 3: Take down the stack**

```bash
cd /Users/makspopov/Documents/SE498/src/pokemon-locations
podman compose -f docker-compose.debug.yml --profile frontend down
```

- [ ] **Step 4: No commit** (verification only).

---

## Phase 4 — Branch hand-off

### Task 9: Final test suite + push branch

**Files:** none

- [ ] **Step 1: Run all tests one more time**

```bash
cd /Users/makspopov/Documents/SE498/src/pokemon-locations/Backend && dotnet test -nologo 2>&1 | tail -8
```

Expected: all green.

- [ ] **Step 2: Push the branch**

```bash
cd /Users/makspopov/Documents/SE498/src/pokemon-locations
git push -u origin feat/se498-90-random-theme-on-reload
```

- [ ] **Step 3: Open PR via GitHub web UI** (or `gh pr create`).

---

## Self-Review Notes

**Spec coverage check** (against `docs/designs/specs/2026-05-08-random-theme-on-reload.md`):

| Spec section | Tasks |
|---|---|
| §4.1 Migration 0011 | Task 1 |
| §4.2 Themes.cs | Task 2 |
| §4.3 API (no controller change) | Implicitly verified by Task 4 |
| §4.4 (a) Click handler — rename, persist preference, label uses preference | Task 6 |
| §4.4 (b) /api/me load — resolve 'random' before applyTheme | Task 7 |
| §4.4 (c) Initial paint unchanged | No task — explicit no-change in spec |
| §4.4 (d) /api/me label unchanged | No task — explicit no-change in spec |
| §6.1 Backend tests | Tasks 3, 4 |
| §6.2 Frontend manual | Task 8 |

No spec gaps. No placeholders. No naming inconsistencies — `selectedTheme` and `resolved` used consistently in Task 6; `themeToApply` is the only local in Task 7 and matches the spec.
