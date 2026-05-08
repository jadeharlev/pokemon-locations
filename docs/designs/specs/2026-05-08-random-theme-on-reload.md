# SE498-90: Random theme re-randomizes on page (re)load — Design

**Status:** Draft, pending implementation
**Ticket:** SE498-90 (Jira)
**Author:** Maks Popov
**Date:** 2026-05-08

## 1. Goal

When a user picks "Random" as their theme, the app should persist that preference itself (rather than the specific theme that was randomly resolved at click time). On every subsequent page load, the app should pick a fresh random theme from the user's unlocked set and apply it.

## 2. Current Behavior (the bug)

Today, clicking the **Random** theme button:

1. Calls `pickRandomTheme()` client-side, resolving to a specific theme (e.g. `pikachu`).
2. Applies that theme locally.
3. **Sends the resolved theme name to `PUT /account/theme`**, persisting `pikachu` in the DB.

Result: every subsequent page load shows `pikachu` until the user clicks **Random** again. The "random" choice collapses into a one-shot roll, not an ongoing preference.

## 3. Desired Behavior

- "Random" is itself a stored preference value (`user.theme = 'random'`).
- The theme button click flow sends `'random'` to the server (not the resolved theme).
- The frontend continues to apply a *resolved* concrete theme to the UI for immediate visual feedback — `'random'` is never used as a CSS file name.
- On every subsequent page load, when `/api/me` returns `theme = 'random'`, the frontend resolves a fresh random theme from the user's unlocked set and applies it.

## 4. Architecture & Changes

### 4.1 Database

New migration `Backend/PokemonLocations.WebServer/Database/Migrations/0011_add_random_theme.sql`:

```sql
ALTER TYPE user_theme ADD VALUE IF NOT EXISTS 'random';
```

### 4.2 Models

`Backend/PokemonLocations.WebServer/Models/Themes.cs`: add `'random'` as a recognized theme value:

```csharp
public const string Random = "random";

private static readonly HashSet<string> All = new(StringComparer.Ordinal) {
    Bulbasaur, Charmander, Squirtle, Pikachu,
    Rattata, Diglett, Geodude, Dratini, Mew, Dragonite,
    Random,
};
```

`Themes.IsValid("random")` then returns `true`, so `UpdateThemeRequest.Validate` accepts the new value without controller changes.

### 4.3 API

**No controller changes.** `AccountController.UpdateTheme` already routes through `UpdateThemeRequest.Validate` (which calls `Themes.IsValid`). Once `Themes.IsValid("random")` is true, the existing endpoint accepts and persists `'random'` automatically.

`GET /api/me` already returns `user.theme` as a string. Once `'random'` is a valid stored value, the endpoint surfaces it without modification.

### 4.4 Frontend (`script.js`)

Three changes, all isolated to the existing theme code (lines ~770–990):

**(a) Theme-button click handler** — currently sends the *resolved* theme to the server:

```js
// before
const selectedTheme = btn.dataset.theme;
const theme = selectedTheme === 'random' ? pickRandomTheme() : selectedTheme;
// ...
applyTheme(theme);
await PLAuth.authFetch('/account/theme', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ theme })  // sends resolved name
});
```

becomes:

```js
const selectedTheme = btn.dataset.theme;            // may be 'random'
const resolved = selectedTheme === 'random' ? pickRandomTheme() : selectedTheme;
// ...
applyTheme(resolved);                               // local UI uses resolved theme
await PLAuth.authFetch('/account/theme', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ theme: selectedTheme })  // persist 'random' as preference
});
```

**(b) `/api/me` response handler** — currently calls `applyTheme(user.theme)` directly. Change to resolve `'random'` to a concrete theme before applying:

```js
// before
applyTheme(user.theme);

// after
const themeToApply = user.theme === 'random' ? pickRandomTheme() : user.theme;
applyTheme(themeToApply);
```

**(c) Initial paint** — unchanged. The initial `applyTheme(sessionStorage.getItem(THEME_CACHE_KEY) || 'bulbasaur')` runs against the *resolved* theme name from the previous load, so the first paint is instant and visually consistent. The re-randomization in (b) happens shortly after when `/api/me` resolves, accepting a brief crossfade on reload.

### 4.5 Why no `applyThemePreference` wrapper?

We considered factoring out a `applyThemePreference(pref)` helper that handles the `'random' → resolve → apply` translation in one place. Rejected for two reasons:

1. The two call sites (button click, /api/me load) need to do *different* things on the resolved value — the button click also persists, the /api/me load only displays. Wrapping them in a single helper hides that asymmetry.
2. The translation is exactly two lines (`const themeToApply = user.theme === 'random' ? pickRandomTheme() : user.theme; applyTheme(themeToApply);`). Inlining is clearer than abstracting at this size.

## 5. Edge Cases

- **No themes unlocked beyond the current one** — `pickRandomTheme()` filters the current theme out of the candidate list; if the filter empties the list, it falls back to the current theme. The user effectively keeps the same theme. Acceptable.
- **`currentStats` not yet loaded** — `currentStats` is initialized to `{ gymsComplete: 0, locationsVisited: 0, buildingsVisited: 0 }` at module load. `isThemeUnlocked` calls into `THEME_UNLOCK_RULES[theme].isUnlocked(currentStats)`, which compares those zeros to thresholds and returns `false` for stat-gated themes (correct behavior — nothing earned yet). Default themes (no rule) always come back as unlocked. So `pickRandomTheme()` is safe to call at any point in the page lifecycle.
- **Confetti / unlock animations** — `pickRandomTheme()` does NOT trigger `showUnlockMessage` / `showConfetti`. Those animations are gated by the unlock detection flow (`checkForNewThemeUnlocks`), which is independent of theme resolution.
- **Brief crossfade on reload** — when sessionStorage's cached resolved theme differs from the new random pick, the user sees the cached theme for ~100–300 ms before /api/me resolves and the re-randomization applies. Acceptable; this is the explicit signal that re-randomization happened.
- **Signin / signup pages** — no theme picker there, no /api/me call (unauthenticated). Initial paint uses sessionStorage or the default. No special handling needed.
- **`applyTheme('random')` called by accident** — would write `random` to `data-theme` attribute and try to load `/css/themes/random.css` (404). Both new call sites (button click, /api/me handler) resolve `'random'` *before* calling `applyTheme`, so this can't happen via these paths. Documented as an invariant; no defensive coding added.

## 6. Testing

### 6.1 Backend

`Backend/PokemonLocations.WebServer.Tests/Models/Requests/UpdateThemeRequestTests.cs` (or whichever existing file holds these tests — extend if present, create if not):
- `UpdateThemeRequest` with `Theme = "random"` validates successfully (no `ValidationResult` errors).
- Existing tests that round-trip a specific theme continue to pass.

`Backend/PokemonLocations.WebServer.Tests/Database/UserRepositoryTests.cs`:
- `UpdateThemeAsync(userId, "random")` persists; subsequent `GetByIdAsync(userId)` returns `theme = "random"`.

`Backend/PokemonLocations.WebServer.Tests/Controllers/AccountControllerTests.cs`:
- `PUT /account/theme` with body `{"theme": "random"}` returns 204.
- `GET /api/me` after that PUT returns body containing `theme: "random"`.

### 6.2 Frontend

Manual verification (no JS test framework in this project):

- Click Random → page applies a theme. `/api/me` shows `theme: "random"`. Hard-refresh page → another (often different) theme applies. Hard-refresh again → another. Confirms re-randomization.
- Click a specific theme (e.g. Pikachu) → `/api/me` shows `theme: "pikachu"`. Refresh → still Pikachu. Confirms specific themes still pin.
- New user (only default themes unlocked) → click Random repeatedly → cycles through Bulbasaur / Charmander / Squirtle. Confirms unlocked-set filtering works.

## 7. Out of Scope

- Animating the theme transition (currently just an instant CSS `<link>` swap).
- Showing the user *which* random theme got picked (no UX for "the random pick was X" — they see the visual change).
- A "shuffle now" button that re-rolls without a page reload.
