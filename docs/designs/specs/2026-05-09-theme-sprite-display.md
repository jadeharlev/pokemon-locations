# SE498-92: Pokémon sprite display for active theme — Design

**Status:** Draft, pending implementation
**Ticket:** SE498-92 (Jira)
**Author:** Maks Popov
**Date:** 2026-05-09

## 1. Goal

When the user has an active theme (e.g., Bulbasaur, Charmander), display the corresponding Pokémon's official artwork in the user-info area on the main page. The sprite updates whenever the active theme changes — picker selection, Random resolution, or the Shiny Eevee easter egg.

This narrows the literal Jira AC ("each theme option in the theme switcher displays an image"). Per design discussion, the picker-button sprite variant is **out of scope** for this ticket and may be addressed as a follow-up. The single-sprite display in the user-info area is the only deliverable here.

## 2. Current State

Relevant existing structure:

- `index.html:857-878` — `#status-panel` contains `#user-info` (loading text + theme name), `.stats-section` (3 stat lines), `.action-buttons`. No sprite anywhere.
- `script.js` — `applyTheme(theme)` is the single chokepoint for theme changes (sets `<html>`'s `data-theme`, swaps the linked stylesheet). It is called from:
  - `loadUserInfo()` at startup — for the persisted preference (or random-resolved if pref is `random`)
  - The theme picker click handler — when the user picks a new theme
  - `activateShinyEeveeTheme()` — when the Konami code unlocks Shiny Eevee
- Theme list: 11 visible themes in `THEMES` (`bulbasaur`, `charmander`, `squirtle`, `pikachu`, `rattata`, `diglett`, `geodude`, `dratini`, `dragonite`, `mew`) plus `shiny-eevee` in `HIDDEN_THEMES`.

## 3. Architecture

### 3.1 Sprite source

Direct GitHub URLs from the PokeAPI sprites repo. No JSON API calls.

- Standard themes: `https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/other/official-artwork/{id}.png`
- Shiny Eevee: `https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/other/official-artwork/shiny/133.png`

The Jira AC explicitly mentions both URL patterns (REST API and direct GitHub). Direct URL is chosen because:

1. One network round-trip per sprite (no JSON fetch + image fetch chain).
2. No PokeAPI rate-limit exposure (sprites repo is on GitHub's CDN).
3. Browser handles caching, loading, and errors via standard `<img>` semantics — no JS fetch wrapper needed.

### 3.2 Theme → sprite URL map

A single hardcoded JS const in `script.js`. Pokémon IDs are stable (they have not changed since Gen 1 was first cataloged), so this map is effectively immutable.

```js
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
```

### 3.3 DOM layout

New container in `index.html`, inserted between `#user-info` and `.stats-section` inside the existing `#status-panel`:

```html
<div class="user-info" id="user-info"> … </div>

<div class="theme-sprite-container">
  <img id="theme-sprite" class="theme-sprite" alt="" />
</div>

<div class="stats-section"> … </div>
```

CSS:

```css
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

Container reserves a fixed `180px` height regardless of image load state, preventing layout shift.

### 3.4 Update logic

A single new function in `script.js`:

```js
function updateThemeSprite(theme) {
    const img = document.getElementById('theme-sprite');
    if (!img) return;

    const url = THEME_TO_SPRITE_URL[theme];
    if (!url) {
        img.classList.remove('loaded');
        img.removeAttribute('src');
        return;
    }

    img.classList.remove('loaded');
    img.alt = `${formatThemeName(theme)} sprite`;
    img.onload = () => img.classList.add('loaded');
    img.onerror = () => {
        console.warn(`Theme sprite failed to load for ${theme}: ${url}`);
        img.classList.remove('loaded');
    };
    img.src = url;
}
```

Hooked into `applyTheme(theme)` as the last statement:

```js
function applyTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    document.getElementById('theme-stylesheet').href = `/css/themes/${theme}.css`;
    updateThemeSprite(theme);  // NEW
}
```

Because `applyTheme` is the single chokepoint for every theme change, this one hook covers:

- Initial page load via `loadUserInfo`
- Theme picker selection (resolved from `random` if needed before `applyTheme` is called)
- Shiny Eevee Konami activation
- Any future theme-change path that goes through `applyTheme`

No separate initial render call is needed.

### 3.5 Random theme behavior

Random preference is resolved to a concrete theme **before** `applyTheme` is called (in both `loadUserInfo` and the theme picker click handler). So the sprite naturally shows the resolved theme's Pokémon — no special-casing in `updateThemeSprite`.

## 4. Edge Cases

- **Sprite 404 or network error:** `onerror` fires, sprite stays hidden (opacity 0), container keeps its 180px reserved space, layout does not shift. Console warning logged for debugging.
- **Unknown theme name** (defensive — not expected in normal flow): `THEME_TO_SPRITE_URL[theme]` is undefined, function clears the `src` attribute and returns early. Container shows empty space.
- **Slow first load:** Container is empty during fetch. After the first load per sprite, browser cache serves subsequent loads instantly.
- **Theme rapidly toggled:** Each `applyTheme` call sets a new `img.src`; the browser cancels any pending load for the previous URL. The most-recently-set sprite wins. `onload`/`onerror` for the cancelled load do not fire.
- **Image-element re-use:** Since `onload`/`onerror` are reassigned (not `addEventListener`), each call cleanly replaces the previous handlers. No listener leak.

## 5. Testing

### 5.1 Backend

No backend changes. No new C# tests required.

### 5.2 Frontend (manual checklist)

After implementation, walk through:

| Action | Expected |
|---|---|
| Sign in fresh, default theme is bulbasaur | Bulbasaur official artwork appears in the new container, ~180px tall, centered |
| Open theme picker, click each unlocked theme one by one | Sprite updates to match each Pokémon. First load briefly empty, subsequent loads instant |
| Set theme to Random, refresh page | Sprite shows whichever Pokémon `pickRandomTheme` resolved to this session |
| Activate Shiny Eevee via Konami code | Sprite updates to the **shiny** Eevee artwork (alternate color palette) |
| Disable network in DevTools, switch theme | Sprite area stays empty, layout below does not shift up |
| Inspect DOM | `.theme-sprite-container` is between `#user-info` and `.stats-section`, has `height: 180px` |
| `<img>` element | Has `alt` text matching the active theme (accessibility) |

### 5.3 Visual verification

Sizing target is **180px tall** as a starting point. After implementation, eyeball the panel and adjust if it crowds the stats or feels too small. Width auto-fits via `max-width: 100%; object-fit: contain;`.

## 6. Out of Scope

- **Sprites in the theme picker buttons** — the literal Jira AC mentioned this; deferred to a follow-up if pursued at all.
- **Animated sprites** — official artwork is static. Showdown animated sprites and the Pokémon HOME 3D renders were considered and rejected (see the design discussion).
- **Persistent localStorage caching** — relying on browser HTTP cache is sufficient for sprite assets that are ~50-200KB each.
- **Preload-all-sprites at startup** — lazy-load on theme change is sufficient given browser cache.
- **Stats panel layout changes** — explicitly deferred per user direction. Revisit if the new sprite container creates visual crowding.
- **Picker-button sprite for Shiny Eevee** — not applicable; Shiny Eevee is intentionally hidden from the picker.
