# Frontend Spec - pokemon-locations

## 1. Overview

The frontend is a static website that provides the user-facing interface for the Pokémon Locations application. It is built with raw HTML, CSS, and JavaScript, styled with Bootstrap 5 plus a swappable theme system, and communicates with the web server (BFF) which in turn proxies content from the API.

The frontend never communicates with the API directly. All `fetch()` calls go to the same-origin web server.

| **Function** | **Choice** |
| --- | --- |
| Markup | *Raw HTML* |
| Styling | *Bootstrap 5 (CDN) + per-theme CSS variables + custom CSS* |
| Scripting | *Raw JavaScript (no framework)* |
| Hosting | *ASP.NET 10 web server (`UseStaticFiles()` over `wwwroot/`)* |

Static files live in `Backend/PokemonLocations.WebServer/wwwroot/` and are served by the same ASP.NET process that handles `/account/*`, `/api/*`, and `/health/db`. There is no separate frontend container.

## 2. Architecture

```
Browser ──(HTTP w/ Basic Auth)──▶ Web Server ──(REST w/ Bearer Token)──▶ API Server
                                       │                                      │
                                       ▼                                      ▼
                                 Web Server DB                           API Database
                            (users, per-user state)            (locations, buildings, gyms)
```

The frontend is the **Browser** in this diagram. JS in the browser makes `fetch()` calls to the **Web Server** at the same origin, sending Basic Auth credentials. Bootstrap CSS/JS is loaded from jsDelivr — the only external resource the browser fetches outside the web server.

### 2.1 Backend Base URL

| **Environment** | **Base URL** |
| --- | --- |
| Development (HTTP) | `http://localhost:3001` |
| Development (HTTPS) | `https://localhost:3002` |
| Production | `TBD` |

All `fetch()` calls are same-origin (no CORS). The frontend is served from `wwwroot/` by the same web server that handles the API/account endpoints, so paths like `/api/locations` and `/account/signup` resolve naturally.

## 3. Pages

The frontend is **3 HTML files**, all in `wwwroot/`:

| **Page** | **File** | **Auth Required** |
| --- | --- | --- |
| Main app | `index.html` | Yes (Basic Auth via `auth.js`) |
| Sign In | `signin.html` | No |
| Sign Up | `signup.html` | No |

`index.html` is a single-page application that handles all logged-in user surface — locations, buildings, gyms tracking, badges, notes, stats, and theme switching. Navigating without credentials redirects to `/signin.html`.

### 3.1 Main App (`index.html`)

The application's primary view, behind Basic Auth. Uses `script.js` to render and manage all dynamic content.

**Sections (inline within the page):**
- **Locations list** — populated from `GET /api/locations`; clicking a location loads its detail.
- **Location detail** — name, description, image gallery, embedded video (if `videoUrl` present), and a buildings list. Populated from `GET /api/locations/{id}` plus `GET /api/locations/{id}/buildings`.
- **Building tracking** — clicking a building toggles its visited status via `PUT`/`DELETE /api/me/visited/buildings/{locationId}/{buildingId}`. Building visited status is read from the merged proxy response.
- **Visited progress** — a header strip that summarizes visited buildings per location ("3/5 Visited" / "All Locations Visited" / etc.).
- **Badges** — read via `GET /api/me/badges`, toggled per-badge with `PUT`/`DELETE /api/me/badges/{badge}`.
- **Notes** — per-location notes via `GET /api/me/notes/{locationId}`, `PUT /api/me/notes/{locationId}`, `DELETE /api/me/notes/{locationId}`.
- **Stats** — aggregate stats (gymsComplete, locationsVisited, buildingsVisited) from `GET /api/me/stats`.
- **Theme switcher** — calls `PUT /account/theme` with the chosen theme name; the response triggers a stylesheet swap (see §6).
- **Account controls** — `DELETE /account` for self-deletion (cascades through all per-user tables).

**Initial load behavior:**
1. `auth.js` reads stored credentials from `sessionStorage`. If absent, redirect to `/signin.html`.
2. Fetch `GET /api/me` to confirm credentials and load the current user's profile (display name, theme).
3. Fetch `/api/locations`, `/api/me/stats`, `/api/me/badges` in parallel.
4. Render the locations list; first location is auto-selected.

### 3.2 Sign In (`signin.html`)

Standalone auth page. Pre-applies the user's last-used theme from `sessionStorage` so the form matches the look they're used to.

**Form fields:** email, password.

**Behavior:**
- On submit, the page constructs a Basic Auth header from the form values and probes `GET /api/me`.
- A `200 OK` means credentials are valid: store `{email, password}` in `sessionStorage` (key `pl.auth`) and redirect to `/index.html`.
- A `401 Unauthorized` displays an inline "Invalid email or password" error.
- Other errors display a generic "Something went wrong" message.

There is **no** `POST /account/login` endpoint — sign-in is implicit, since Basic Auth is sent on every authenticated request. The "login form" is just a credential probe.

### 3.3 Sign Up (`signup.html`)

Standalone account creation page.

**Form fields:** email, password (with confirmation), display name.

**Behavior:**
- `POST /account/signup` with `{ email, password, displayName }`.
- On `201 Created`: store credentials in `sessionStorage` and redirect to `/index.html` (the user is logged in immediately).
- On `400 Bad Request`: display inline validation messages.
- On `409 Conflict`: display "Email already registered".

## 4. Authentication Flow

The frontend uses HTTP Basic Authentication. There is no session, no cookie, no `POST /account/login`, no `POST /account/logout`. Every authenticated request carries a fresh `Authorization: Basic <base64(email:password)>` header.

### 4.1 Credential storage

Credentials are stored in `sessionStorage` under the key `pl.auth` as JSON:

```json
{ "email": "ash@pokemon.com", "password": "..." }
```

Using `sessionStorage` (not `localStorage`) means closing the tab logs the user out. There is no "Remember me" feature.

### 4.2 Authenticated fetch helper

`wwwroot/js/auth.js` exposes a `PLAuth` global with one key helper:

```js
const res = await PLAuth.authFetch('/api/me/stats');
```

`authFetch()`:
1. Reads stored credentials from `sessionStorage`.
2. Adds an `Authorization: Basic ...` header if creds are present.
3. Issues the request.
4. If the response is `401 Unauthorized`, clears `sessionStorage` and redirects to `/signin.html` (unless already on a sign-in/sign-up page).

`script.js` wraps this further with `apiFetch(path)` which prepends `/api`, so `apiFetch('/locations')` ends up calling `/api/locations`.

### 4.3 Logout

The user logs out by clicking the logout control in `index.html`. This clears `sessionStorage` and redirects to `/signin.html`. No backend call is made.

## 5. Error Handling

| **Scenario** | **User-Facing Behavior** |
| --- | --- |
| Web server unreachable | "Unable to connect. Please try again later." |
| `401 Unauthorized` from any authenticated request | `auth.js` clears creds and redirects to `/signin.html` |
| `404 Not Found` (location/building) | Inline "Not found" message in the relevant section |
| `409 Conflict` (signup duplicate email) | "Email already registered" inline on the signup form |
| `400 Bad Request` (signup validation) | Specific field-level error messages inline |
| `500 Internal Server Error` | "Something went wrong. Please try again later." |
| `502 Bad Gateway` (API unreachable from web server) | Same as 500 — generic failure message |

Errors are displayed inline within page sections, never as `alert()` or `window.confirm()`.

## 6. Theming

The frontend supports four interchangeable visual themes, each a separate CSS file in `wwwroot/css/themes/`:

| **Theme** | **File** | **Primary Color** |
| --- | --- | --- |
| `bulbasaur` | `bulbasaur.css` | Green |
| `charmander` | `charmander.css` | Red/Orange |
| `squirtle` | `squirtle.css` | Blue |
| `pikachu` | `pikachu.css` | Yellow |

### 6.1 How themes work

Each theme file defines the same set of CSS custom properties (`--theme-primary`, `--theme-primary-light`, `--theme-primary-pale`, `--theme-primary-dark`, `--theme-ring-bg`, `--theme-text`, `--theme-muted`, `--theme-danger`, `--theme-border`, `--theme-check-fill`) on `:root`. All page styles reference those variables, so swapping the active theme is just swapping the linked stylesheet.

Each HTML page includes the active theme via:
```html
<link id="theme-stylesheet" rel="stylesheet" href="/css/themes/bulbasaur.css" />
```

### 6.2 Theme switching

The user chooses a theme in `index.html`. The page calls `PUT /account/theme` with the new theme name. On success, the `<link id="theme-stylesheet">` tag's `href` is updated and the new theme name is written to `sessionStorage` (key `pl.theme`) so future page loads (including `signin.html` after a logout) use it.

`signin.html` and `signup.html` read `pl.theme` from `sessionStorage` on load and apply it before render to avoid a flash of the default theme.

The valid theme names are enforced server-side by a Postgres enum (`user_theme`) on the `users.theme` column.

## 7. Style Guide

The frontend uses Bootstrap 5 (loaded via CDN) plus per-theme CSS custom properties for colors. Bootstrap provides the grid, typography defaults, and component primitives; the theme stylesheets override the accent colors.

### 7.1 Bootstrap

```
https://cdn.jsdelivr.net/npm/bootstrap@5.3.8/dist/css/bootstrap.min.css
https://cdn.jsdelivr.net/npm/bootstrap@5.3.8/dist/js/bootstrap.bundle.min.js
```

### 7.2 Color tokens

All color choices flow through CSS custom properties on `:root`, defined per-theme. Page styles reference the variables — never hard-coded hex values for theme-driven colors. See `wwwroot/css/themes/bulbasaur.css` for the full list of variables every theme must define.

### 7.3 Typography

| **Element** | **Font** | **Weight** |
| --- | --- | --- |
| Body | Inter (Google Fonts), Arial fallback | 400 |
| Headings | Inter | 600/700 |

Inter is loaded from Google Fonts on each page.

### 7.4 Favicon

`favicon-32.png`, `favicon-180.png`, and `favicon-192.png` live in `wwwroot/` and are referenced from the `<head>` of each page (32 for tabs, 180 for Apple touch, 192 for Android home screen).

## 8. Future Considerations

- Standalone pages for locations / buildings / gyms (currently consolidated into the SPA-style `index.html`)
- User-uploaded images per location (SE498-68 — schema and endpoints not yet shipped)
- Persistent login across tab close (`localStorage` instead of `sessionStorage`)
- Loading indicators while fetches are in progress
- Search across locations / buildings / gyms
