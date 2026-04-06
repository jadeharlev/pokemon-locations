# Frontend Spec - pokemon-locations

## 1. Overview

The frontend is a static website served by nginx that provides the user-facing interface for the Pokémon Locations application. It is built with raw HTML, CSS, and JavaScript, styled with Bootstrap, and communicates with the backend (web server) to display content and manage user-specific features like favorites.

The frontend does **not** communicate with the API directly. All data requests go through the backend, which handles authentication and proxies content from the API.

| **Function** | **Choice** |
| --- | --- |
| Markup | *Raw HTML* |
| Styling | *Bootstrap 5 (CDN) + custom CSS* |
| Scripting | *Raw JavaScript (no framework)* |
| Hosting | *nginx (containerized via Docker/Podman)* |

## 2. Architecture

Per the course architecture, the full request flow is:

```
Website (Browser) ──(HTTP w/ Basic Auth)──▶ Web Server ──(REST w/ Bearer Token)──▶ API Server
                                                │                                      │
                                                ▼                                      ▼
                                          Web Server DB                           API Database
```

The frontend is the **Website (Browser)** in this diagram. It is a collection of static `.html`, `.css`, and `.js` files served by an nginx container. JavaScript in the browser makes `fetch()` calls to the **Web Server (backend)** using Basic Authentication. The backend authenticates the user, proxies content requests to the API Server using Bearer Tokens, and returns HTML or JSON responses to the browser.

The frontend also loads Bootstrap CSS directly from Bootstrap, the only external resource the frontend fetches outside of the backend.

### 2.1 Backend Base URL

All JavaScript `fetch()` calls go to the backend (web server):

| **Environment** | **Base URL** |
| --- | --- |
| Development | `http://localhost:8082` |
| Production | `TBD` |

## 3. Pages

The frontend consists of the following pages. Wireframes for each page are located in `docs/wireframes/`.

| **Page** | **File** | **Auth Required** | **Wireframe(s)** |
| --- | --- | --- | --- |
| Home | `index.html` | No | — |
| Locations List | `locations.html` | No | `locations.png` |
| Location Detail | `location.html` | No | `location.png`, `location_favorite.png` |
| Building Detail | `building.html` | No | `building.png`, `building_favorite.png` |
| My Favorites | `favorites.html` | Yes | `favorite_locations.png` |
| Sign In / Sign Up | `signin.html` | No | `sign_in_blank.png`, `sign_in_filled.png`, `sign_in_username.png` |

### 3.1 Home (`index.html`)

The landing page for the site. Introduces the application and directs users to browse locations.

**Elements:**
- Navigation bar (shared across all pages)
- Card with site title, description, and a "Browse Locations" button linking to `locations.html`
- Helper text beneath the button

**Data:** None — this is a static page with no backend calls.

### 3.2 Locations List (`locations.html`)

Displays all Pokémon Red locations as a scrollable list of clickable cards.

**Elements:**
- Navigation bar
- Search bar for client-side filtering of location names
- List of location cards, each displaying the location name
- Each card links to `location.html?id={locationId}`

**Data:** On page load, fetches `GET /locations` from the backend and dynamically renders the location cards.

**Behavior:**
- The search bar filters the displayed locations by name as the user types (client-side, no additional backend calls)
- If the backend returns an error, a user-friendly error message is displayed in place of the location list

### 3.3 Location Detail (`location.html`)

Displays detailed information about a single location, including its images and buildings.

**Elements:**
- Navigation bar
- Location name and description
- Image gallery (from the location's images, ordered by `displayOrder`)
- Video embed if a `videoUrl` is present
- List of buildings within the location, each linking to `building.html?id={buildingId}&locationId={locationId}`
- Favorite button (visible when logged in) to add/remove the location from favorites

**Data:** On page load, reads `locationId` from the URL query string and fetches `GET /locations/{locationId}` from the backend. The response includes images and buildings.

**Behavior:**
- If `locationId` is missing or invalid, display an error message
- If the backend returns `404`, display a "Location not found" message
- The favorite button toggles between favorited/unfavorited states. Clicking it calls `POST /favorites/locations/{locationId}` or `DELETE /favorites/locations/{locationId}` on the backend
- The favorite button is only visible if the user is logged in

### 3.4 Building Detail (`building.html`)

Displays detailed information about a single building. If the building is a gym, gym-specific details are shown.

**Elements:**
- Navigation bar
- Building name, type, and description
- If the building type is `landmark`, the landmark description is displayed
- If the building type is `gym`, gym details are shown: gym type, badge name, gym leader, gym order
- Link back to the parent location
- Favorite button (visible when logged in) to add/remove the building from favorites

**Data:** On page load, reads `buildingId` and `locationId` from the URL query string and fetches `GET /locations/{locationId}/buildings/{buildingId}` from the backend.

**Behavior:**
- If `buildingId` or `locationId` is missing or invalid, display an error message
- If the backend returns `404`, display a "Building not found" message
- The favorite button works the same as on the location detail page, calling `POST /favorites/buildings/{buildingId}` or `DELETE /favorites/buildings/{buildingId}`
- The favorite button is only visible if the user is logged in

### 3.5 My Favorites (`favorites.html`)

Displays the logged-in user's favorited locations and buildings.

**Elements:**
- Navigation bar
- List of favorite locations, each linking to the corresponding location detail page
- List of favorite buildings, each linking to the corresponding building detail page
- Each favorite shows the resource name and a remove button

**Data:** On page load, fetches `GET /favorites/locations` and `GET /favorites/buildings` from the backend.

**Behavior:**
- If the user is not logged in, redirect to `signin.html`
- Clicking the remove button calls `DELETE /favorites/locations/{locationId}` or `DELETE /favorites/buildings/{buildingId}` and removes the item from the displayed list
- If either favorites list is empty, display a message like "No favorite locations yet"

### 3.6 Sign In / Sign Up (`signin.html`)

A combined page for user authentication and account creation.

**Elements:**
- Navigation bar
- Form with email and password fields
- "Sign In" button
- "Sign Up" button or toggle to switch to registration mode
- Error messages displayed inline for validation failures or incorrect credentials

**Data:**
- Sign in: `POST /account/login` with `{ "email", "password" }`
- Sign up: `POST /account/register` with `{ "email", "password" }`

**Behavior:**
- On successful login, store the user's credentials (email/password) for subsequent Basic Auth requests and redirect to the previous page or `index.html`
- On successful registration, display a success message and prompt the user to sign in
- If the backend returns `401` on login, display "Invalid email or password"
- If the backend returns `409` on registration, display "Email already registered"
- If the backend returns `400`, display the relevant validation errors (e.g., "Password must be at least 8 characters")

## 4. Navigation

A consistent navigation bar appears on every page.

**Elements:**
- **Left side:** HOME, LOCATIONS, FAVORITES links
- **Right side:** Search icon (expands into a search input on hover/focus), Sign In link

**Behavior:**
- The currently active page link is highlighted (blue, `#2457ff`)
- The FAVORITES link directs to `favorites.html` (requires login — redirects to sign in if not authenticated)
- The Sign In link changes to the user's email or "Account" when logged in
- The nav search input provides client-side filtering where applicable (e.g., on the locations page)

## 5. Authentication Flow

The frontend uses Basic Authentication to communicate with the backend. Credentials are stored client-side after login and included in the `Authorization` header on every authenticated request.

### 5.1 Storing Credentials

After a successful login (`POST /account/login` returns `200`), the frontend stores the user's email and password in memory (JavaScript variable) for the duration of the browser session. These are used to create the Basic Auth header:

```
Authorization: Basic <base64(email:password)>
```

### 5.2 Authenticated Requests

All `fetch()` calls to protected backend endpoints include the Basic Auth header. If the backend returns `401`, the frontend redirects the user to `signin.html`.

### 5.3 Logout

The user can log out by clicking a logout option in the navigation. This clears the stored credentials from memory and redirects to `index.html`. No backend call is needed.

## 6. Error Handling

The frontend handles errors from the backend and displays user-friendly messages.

| **Scenario** | **User-Facing Behavior** |
| --- | --- |
| Backend unreachable | Display "Unable to connect. Please try again later." |
| `401 Unauthorized` | Redirect to `signin.html` |
| `404 Not Found` | Display "[Resource] not found" message on the page |
| `409 Conflict` (duplicate favorite) | Display "Already in your favorites" |
| `409 Conflict` (duplicate email) | Display "Email already registered" |
| `400 Bad Request` | Display the specific validation error(s) returned by the backend |
| `500 Internal Server Error` | Display "Something went wrong. Please try again later." |

Error messages are displayed inline on the page, not as browser alerts.

## 7. Style Guide

The frontend uses **Bootstrap 5** (loaded via CDN) as its base styling framework, supplemented by custom CSS for page-specific layouts and components.

### 7.1 Bootstrap

Bootstrap is loaded from the jsDelivr CDN:
- CSS: `https://cdn.jsdelivr.net/npm/bootstrap@5.3.8/dist/css/bootstrap.min.css`
- JS: `https://cdn.jsdelivr.net/npm/bootstrap@5.3.8/dist/js/bootstrap.bundle.min.js`

Bootstrap provides the base grid system, typography defaults, and component styles (buttons, forms, modals, etc.).

### 7.2 Colors

| **Role** | **Value** | **Usage** |
| --- | --- | --- |
| Primary | `#2457ff` | Active nav links, primary buttons ("Browse Locations") |
| Primary Hover | `#1b45cc` | Button hover state |
| Link Hover | `#7db7ff` | Nav link hover (non-active) |
| Background | `#ffffff` | Page background |
| Card Background | `#ececec` / `#d9d9d9` | Main card, location cards, search bar |
| Card Hover | `#bfbfbf` / `#c9c9c9` | Hover states for interactive cards |
| Nav Background | `#f3f3f3` | Navigation bar |
| Text Primary | `#111111` | Body text, headings |
| Text Secondary | `#333333` / `#555555` | Descriptions, helper text |
| Nav Border | `#222222` | Bottom border on the navigation bar |

### 7.3 Typography

| **Element** | **Font** | **Size** | **Weight** |
| --- | --- | --- | --- |
| Body | Arial, sans-serif | — | 400 |
| Site Title | Arial, sans-serif | 72px (desktop), 50px (tablet), 38px (mobile) | 700 |
| Nav Links | Arial, sans-serif | 22px (desktop), 18px (tablet) | 700 |
| Location Cards | Arial, sans-serif | 66px (desktop), 42px (tablet), 30px (mobile) | 700 |
| Site Description | Arial, sans-serif | 30px (desktop), 22px (tablet), 18px (mobile) | 400 |
| Buttons | Arial, sans-serif | 24px (desktop), 20px (tablet), 18px (mobile) | 700 |
| Helper Text | Arial, sans-serif | 18px (desktop), 16px (mobile) | 400 |

### 7.4 Responsive Breakpoints

The frontend uses custom CSS queries for responsive design:

| **Breakpoint** | **Target** |
| --- | --- |
| `> 992px` | Desktop |
| `601px – 992px` | Tablet |
| `≤ 600px` | Mobile |

On mobile, the navigation bar stacks vertically, cards shrink in height and font size, and content areas expand to 90% width.

### 7.5 Component Patterns

- **Cards:** Rectangular, no border-radius on location cards, 24px border-radius on the hero card. Centered text, bold. Hover state darkens the background.
- **Buttons:** Rounded corners (14px border-radius), bold text, primary blue background with darker hover state.
- **Search bars:** Rounded (18px border-radius), gray background, focus state adds a blue box-shadow outline (`#9ec5ff`).
- **Navigation:** Fixed top bar with gray background and a bottom border. Links are uppercase and bold.

## 8. Future Considerations

- Dynamic rendering of location cards from backend data (replacing hardcoded cards)
- Location detail and building detail pages
- Favorites page implementation
- Sign in / sign up page implementation
- Persistent login state (e.g., `localStorage`) vs in-memory only
- Client-side search across buildings and favorites, not just locations
- Loading indicators while backend requests are in progress
