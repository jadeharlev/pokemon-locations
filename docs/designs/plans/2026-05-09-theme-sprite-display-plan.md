# SE498-92 Theme Sprite Display Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Display the active theme's Pokémon official artwork in the user-info area on the main page, refreshing automatically whenever the theme changes (picker selection, Random resolution, or Konami easter egg).

**Architecture:** Add a fixed-height sprite container in `index.html` between `#user-info` and `.stats-section`. Add a hardcoded theme→sprite-URL map and a `updateThemeSprite(name)` function in `script.js`. Hook the function into the existing `applyTheme(name)` chokepoint so every theme-change path triggers a sprite refresh automatically.

**Tech Stack:** Vanilla HTML/CSS/JS. No new dependencies. PokeAPI sprites served directly from `raw.githubusercontent.com` — no JSON fetch wrapper needed; the browser handles loading, caching, and errors via standard `<img>` semantics.

---

## Notes for the Engineer

- **All paths** are relative to repo root: `/Users/makspopov/Documents/SE498/src/pokemon-locations`.
- **No JS test framework exists.** Verification is via manual UX walkthrough at the end. Don't add Jest/Vitest — out of scope.
- **Run the stack with podman, not docker.** Start command:
  ```bash
  podman-compose -f docker-compose.debug.yml -f docker-compose.capstone.yml -f docker-compose.integrated.yml --profile frontend up --build
  ```
  WebServer is at **http://localhost:3001** (not 5000). Sign in with any test account.
- **Spec reference:** `docs/designs/specs/2026-05-09-theme-sprite-display.md`. Re-read it if anything below is ambiguous.
- **Line numbers in this plan reflect branch HEAD `235e84d`.** They will shift as tasks land — re-grep before each edit if uncertain.
- **Each task ends with one explicit commit step.** Don't bundle commits across tasks.

---

## File Map

| File | What changes |
|---|---|
| `Backend/PokemonLocations.WebServer/wwwroot/index.html` | Add CSS for `.theme-sprite-container` and `.theme-sprite`. Add the `<div class="theme-sprite-container"><img id="theme-sprite" …></div>` markup between `#user-info` and `.stats-section`. Bump `script.js` cache-buster. |
| `Backend/PokemonLocations.WebServer/wwwroot/script.js` | Add `THEME_TO_SPRITE_URL` constant with helper functions. Add `updateThemeSprite(name)` function. Add a single call to `updateThemeSprite(name)` as the last statement of `applyTheme(name)`. |

No new files. No deleted files. No backend changes.

---

## Task 1: Add the sprite container markup and CSS

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/wwwroot/index.html`

- [ ] **Step 1: Add the CSS rules**

Find the closing `</style>` of the main style block in `Backend/PokemonLocations.WebServer/wwwroot/index.html`. The block ends with the `.loading-text` rule (currently around line 770-ish; re-grep with `grep -n '\.loading-text' Backend/PokemonLocations.WebServer/wwwroot/index.html` to confirm). Insert the new CSS rules immediately *after* the `.loading-text` rule and *before* the closing `</style>` tag:

```css
    /* ── Theme sprite display ── */
    .theme-sprite-container {
      display: flex;
      justify-content: center;
      align-items: center;
      height: 180px;
      margin: 16px 0;
    }

    .theme-sprite {
      max-height: 180px;
      max-width: 100%;
      object-fit: contain;
      opacity: 0;
      transition: opacity 0.15s ease;
    }

    .theme-sprite.loaded {
      opacity: 1;
    }
```

- [ ] **Step 2: Add the container markup**

Find the `#status-panel` block in `Backend/PokemonLocations.WebServer/wwwroot/index.html` (currently around lines 856-878). The relevant region looks like this:

```html
        <div class="user-info" id="user-info">
          <p class="loading-text">Loading…</p>
        </div>
      </div>

      <div class="stats-section">
```

Insert the sprite container between the `</div>` that closes `.status-block` and the `<div class="stats-section">`. After the edit, the region should read:

```html
        <div class="user-info" id="user-info">
          <p class="loading-text">Loading…</p>
        </div>
      </div>

      <div class="theme-sprite-container">
        <img id="theme-sprite" class="theme-sprite" alt="" />
      </div>

      <div class="stats-section">
```

(The `alt` is empty for now — `updateThemeSprite` will set a meaningful alt at theme-change time.)

- [ ] **Step 3: Bump the cache-buster on script.js**

Find the `<script src="script.js?v=…"` line in `Backend/PokemonLocations.WebServer/wwwroot/index.html` (currently around line 875). Increment the version suffix so browsers re-fetch after our JS changes land in later tasks. For example, if the current line is:

```html
  <script src="script.js?v=dialogs4" defer></script>
```

Change it to:

```html
  <script src="script.js?v=sprite1" defer></script>
```

- [ ] **Step 4: Static verification**

Run from the repo root:

```bash
grep -n 'theme-sprite-container' Backend/PokemonLocations.WebServer/wwwroot/index.html
grep -n 'id="theme-sprite"' Backend/PokemonLocations.WebServer/wwwroot/index.html
grep -n '\.theme-sprite\b' Backend/PokemonLocations.WebServer/wwwroot/index.html
grep -n 'v=sprite1' Backend/PokemonLocations.WebServer/wwwroot/index.html
```

Expected: each grep returns at least one match. Specifically:
- `theme-sprite-container` should appear in BOTH the CSS rule and the markup (2 matches)
- `id="theme-sprite"` should appear once (the `<img>`)
- `.theme-sprite` should appear in the CSS (multiple matches)
- `v=sprite1` should appear once

- [ ] **Step 5: Commit**

```bash
git add Backend/PokemonLocations.WebServer/wwwroot/index.html
git commit -m "feat(frontend): add theme sprite container markup and CSS"
```

---

## Task 2: Add the theme→sprite-URL map and `updateThemeSprite` function

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/wwwroot/script.js`

- [ ] **Step 1: Locate the insertion point**

Find the `applyTheme` function in `Backend/PokemonLocations.WebServer/wwwroot/script.js` (currently at line 768). The new constant and function go *immediately before* `applyTheme`. Re-grep with:

```bash
grep -n 'function applyTheme' Backend/PokemonLocations.WebServer/wwwroot/script.js
```

Note the line number returned — you'll insert above it.

- [ ] **Step 2: Insert the constant and helper function**

Insert this block immediately *before* the `function applyTheme(name)` line, with one blank line separating it from the preceding code:

```js
// ─── Theme sprite display (PokeAPI official artwork) ───
const ART_BASE = 'https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/other/official-artwork';
const art = (id) => `${ART_BASE}/${id}.png`;
const shinyArt = (id) => `${ART_BASE}/shiny/${id}.png`;

const THEME_TO_SPRITE_URL = {
    bulbasaur:    art(1),
    charmander:   art(4),
    squirtle:     art(7),
    pikachu:      art(25),
    rattata:      art(19),
    diglett:      art(50),
    geodude:      art(74),
    dratini:      art(147),
    dragonite:    art(149),
    mew:          art(151),
    'shiny-eevee': shinyArt(133),
};

function updateThemeSprite(name) {
    const img = document.getElementById('theme-sprite');
    if (!img) return;

    const url = THEME_TO_SPRITE_URL[name];
    if (!url) {
        img.classList.remove('loaded');
        img.removeAttribute('src');
        return;
    }

    img.classList.remove('loaded');
    img.alt = `${formatThemeName(name)} sprite`;
    img.onload = () => img.classList.add('loaded');
    img.onerror = () => {
        console.warn(`Theme sprite failed to load for ${name}: ${url}`);
        img.classList.remove('loaded');
    };
    img.src = url;
}

```

(Trailing blank line is intentional — it separates the new section from `function applyTheme`.)

- [ ] **Step 3: Static verification**

Run from the repo root:

```bash
grep -n 'THEME_TO_SPRITE_URL' Backend/PokemonLocations.WebServer/wwwroot/script.js
grep -n 'function updateThemeSprite' Backend/PokemonLocations.WebServer/wwwroot/script.js
node --check Backend/PokemonLocations.WebServer/wwwroot/script.js && echo "JS parses OK"
```

Expected:
- `THEME_TO_SPRITE_URL` returns at least 2 matches (the const declaration + at least one read in `updateThemeSprite`).
- `function updateThemeSprite` returns 1 match.
- `node --check` prints `JS parses OK`.

- [ ] **Step 4: Commit**

```bash
git add Backend/PokemonLocations.WebServer/wwwroot/script.js
git commit -m "feat(frontend): add THEME_TO_SPRITE_URL map and updateThemeSprite helper"
```

---

## Task 3: Hook `updateThemeSprite` into `applyTheme`

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/wwwroot/script.js`

- [ ] **Step 1: Add the call as the last line of `applyTheme`**

Find the existing `applyTheme(name)` function in `Backend/PokemonLocations.WebServer/wwwroot/script.js`. The current body (around line 768) reads:

```js
function applyTheme(name) {
    if (!VALID_THEMES.includes(name)) name = 'bulbasaur';
    document.documentElement.setAttribute('data-theme', name);
    const link = document.getElementById('theme-stylesheet');
    if (link) link.href = `/css/themes/${name}.css`;
    sessionStorage.setItem(THEME_CACHE_KEY, name);
}
```

Add a single line — `updateThemeSprite(name);` — as the last statement of the function. The new body should read:

```js
function applyTheme(name) {
    if (!VALID_THEMES.includes(name)) name = 'bulbasaur';
    document.documentElement.setAttribute('data-theme', name);
    const link = document.getElementById('theme-stylesheet');
    if (link) link.href = `/css/themes/${name}.css`;
    sessionStorage.setItem(THEME_CACHE_KEY, name);
    updateThemeSprite(name);
}
```

- [ ] **Step 2: Static verification**

Run from the repo root:

```bash
grep -n 'updateThemeSprite' Backend/PokemonLocations.WebServer/wwwroot/script.js
node --check Backend/PokemonLocations.WebServer/wwwroot/script.js && echo "JS parses OK"
```

Expected:
- `updateThemeSprite` returns at least 3 matches: the function definition, the new call inside `applyTheme`, and the assignment site (`img.onload = …`) inside `updateThemeSprite`. (Three or more is fine — re-grep counts the textual occurrences.)
- `node --check` prints `JS parses OK`.

- [ ] **Step 3: Commit**

```bash
git add Backend/PokemonLocations.WebServer/wwwroot/script.js
git commit -m "feat(frontend): refresh theme sprite on every applyTheme call"
```

---

## Task 4: Manual UX verification + spec checklist

**Files:** None modified.

- [ ] **Step 1: Bring up the stack (if not already running)**

If the stack isn't already up, run:

```bash
podman machine list   # confirm "Currently running"
podman-compose -f docker-compose.debug.yml -f docker-compose.capstone.yml -f docker-compose.integrated.yml --profile frontend up --build
```

If the stack is already up, just rebuild and recreate the WebServer container so the new files are served:

```bash
podman-compose -f docker-compose.debug.yml -f docker-compose.capstone.yml -f docker-compose.integrated.yml --profile frontend up -d --build --force-recreate --no-deps webserver
```

- [ ] **Step 2: Hard-refresh the browser**

Open http://localhost:3001 and do a true hard refresh:
- Chrome/Brave: DevTools open (Cmd+Opt+I) → right-click reload button → "Empty Cache and Hard Reload"
- Safari: Cmd+Opt+E (clear cache), then Cmd+R

This is necessary because the WebServer bakes wwwroot into the image, and the browser may have cached the previous `index.html`.

- [ ] **Step 3: Walk through the spec's manual checklist**

Per `docs/designs/specs/2026-05-09-theme-sprite-display.md` §5.2:

- [ ] Sign in fresh. Bulbasaur (or whichever theme is persisted) sprite appears in the new container. ~180px tall, centered horizontally between the user info text and the stats.
- [ ] Open the theme picker. Click each unlocked theme one at a time. Sprite updates to match each Pokémon. First load briefly empty (fade-in), subsequent loads instant (browser cache).
- [ ] Set theme to Random, refresh the page, observe the user-info area: sprite shows whichever Pokémon `pickRandomTheme` resolved to this session.
- [ ] Activate Shiny Eevee via the Konami code (`↑ ↑ ↓ ↓ ← → ← → B A` or `↑ ↓ ↑ ↓ ← → ← → A B Enter`). Sprite updates to the **shiny** Eevee artwork (alternate color palette — verify it's not the standard brown Eevee).
- [ ] Open DevTools → Network tab → check "Offline". Switch to a theme you haven't viewed yet. Sprite area stays empty, layout below does **not** shift up. Console shows a `Theme sprite failed to load for …` warning.
- [ ] Re-enable network. Inspect the DOM in DevTools — `.theme-sprite-container` is between `#user-info` and `.stats-section` and has `height: 180px`.
- [ ] Inspect the `<img>` element — its `alt` matches the active theme (e.g., "Charmander sprite", "Shiny Eevee sprite").

- [ ] **Step 4: Visual sizing judgment call**

Eyeball the panel with several themes loaded. Does the 180px container:
- Fit comfortably between the user-info text and the "Your Stats" header?
- Display the sprite at a readable size (sprites have transparent backgrounds, so they appear smaller than their pixel dimensions)?
- Crowd the stats below or feel too small?

If it crowds, drop the height in `Backend/PokemonLocations.WebServer/wwwroot/index.html`'s CSS to 140px and re-verify. If it feels too small, bump to 200px. Re-fetch script.js cache-buster (e.g., bump to `v=sprite2`) only if you're testing on a previously-loaded browser session and the CSS change isn't picked up — but since CSS lives in `index.html` (not `script.js`), the cache-buster on script.js doesn't actually gate CSS reloads. A normal hard refresh is enough for CSS changes.

- [ ] **Step 5: Commit (only if you adjusted sizing)**

If you tweaked the container height in Step 4:

```bash
git add Backend/PokemonLocations.WebServer/wwwroot/index.html
git commit -m "fix(frontend): tweak theme sprite container height"
```

If 180px looked right, no commit needed for this task — Tasks 1-3 already cover all changes.

---

## Out of scope (reminder, do not touch)

- **Sprites in the theme picker buttons** — the literal Jira AC mentioned this; explicitly deferred per design discussion.
- **Stats panel layout** — explicitly deferred. Don't move stats around in this PR.
- **Animated sprites** — official artwork is static.
- **localStorage caching of sprite blobs** — relying on browser HTTP cache is sufficient.
- **Preload-all-sprites at startup** — lazy-load on theme change is sufficient.
- **PokeAPI REST endpoint** — using direct GitHub URLs only (see spec §3.1).
- **Adding a JS test framework** — none exists; not creating one for this feature.
