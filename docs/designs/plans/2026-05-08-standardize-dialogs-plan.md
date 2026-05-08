# SE498-93 Standardize Dialogs Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace all 8 native `alert()`/`confirm()` calls in the WebServer frontend with a themed Bootstrap modal helper, drop the lock emoji and click-time alert on locked themes in favor of a Bootstrap tooltip on hover.

**Architecture:** Add one reusable `#dialog-modal` to `index.html` and three Promise-returning helpers (`showAlert`, `showConfirm`, internal `showDialog`) in `script.js`. Migrate every native dialog call site to the helpers. Remove `🔒` content rule from CSS, delete the click-time alert on locked themes (no-op instead), and wire Bootstrap tooltips onto all theme-option buttons so the existing `title` attribute renders themed.

**Tech Stack:** Bootstrap 5.3.8 (already loaded), vanilla JS, vanilla CSS. No new dependencies. No backend changes. No automated tests added (frontend-only behavioral change in an app with no JS test harness; verified via manual UX checklist).

---

## Notes for the Engineer

- **All file paths** are relative to repo root (`/Users/makspopov/Documents/SE498/src/pokemon-locations`).
- **The frontend has no JS test framework.** Verification is via manual UX. Each migration task ends with a `grep` check (proves the native call is gone) plus a manual checklist (proves the replacement works). Do NOT add a Jest/Vitest harness — that's out of scope.
- **Run the stack with `podman` not `docker`.** The user's machine uses podman-compose. Start command:
  ```bash
  podman-compose -f docker-compose.yml -f docker-compose.capstone.yml -f docker-compose.integrated.yml -f docker-compose.debug.yml up --build
  ```
  Then open `http://localhost:5000` (the WebServer). Sign in with any test account.
- **Line numbers in this plan reflect the state at branch HEAD `1a93818`.** They will shift as tasks land — re-grep before each edit if uncertain.
- **Do not commit between tasks unless the task says to.** Each task ends with one explicit commit step.
- **Spec reference:** `docs/designs/specs/2026-05-08-standardize-dialogs.md`. Re-read it if anything below is ambiguous.

---

## File Map

| File | What changes |
|---|---|
| `Backend/PokemonLocations.WebServer/wwwroot/index.html` | Add `#dialog-modal` markup before `</body>`; remove `.theme-option.theme-locked::after { content: " 🔒"; }` rule. |
| `Backend/PokemonLocations.WebServer/wwwroot/script.js` | Add `showDialog`, `showAlert`, `showConfirm` helpers near top (after `escapeHtml` or before `setupActions`); migrate 8 call sites; remove locked-theme click alert; init Bootstrap tooltips on theme-option buttons. |

No new files. No deleted files. No backend changes.

---

## Task 1: Add the reusable dialog modal markup

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/wwwroot/index.html` (insert before line 880, after the `<script>` tag block)

- [ ] **Step 1: Add the dialog modal markup**

In `Backend/PokemonLocations.WebServer/wwwroot/index.html`, find this block (currently around lines 873-880):

```html
  <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.8/dist/js/bootstrap.bundle.min.js"></script>
  <script src="js/auth.js" defer></script>
  <script src="script.js?v=gallery2" defer></script>
  <div class="gallery-modal" id="gallery-modal" role="dialog" aria-modal="true" aria-label="Image viewer">
    <button type="button" class="modal-close" id="modal-close" aria-label="Close">×</button>
    <img id="modal-image" alt="" />
    <div class="modal-caption" id="modal-caption"></div>
  </div>
```

Replace the `script.js` line's cache-buster from `v=gallery2` to `v=dialogs1` (forces clients to refetch after our JS changes), and insert the dialog modal between the gallery-modal div and `</body>`:

```html
  <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.8/dist/js/bootstrap.bundle.min.js"></script>
  <script src="js/auth.js" defer></script>
  <script src="script.js?v=dialogs1" defer></script>
  <div class="gallery-modal" id="gallery-modal" role="dialog" aria-modal="true" aria-label="Image viewer">
    <button type="button" class="modal-close" id="modal-close" aria-label="Close">×</button>
    <img id="modal-image" alt="" />
    <div class="modal-caption" id="modal-caption"></div>
  </div>

  <div class="modal fade" id="dialog-modal" tabindex="-1" aria-labelledby="dialog-title" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered">
      <div class="modal-content">
        <div class="modal-header">
          <h5 class="modal-title" id="dialog-title">Notice</h5>
          <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
        </div>
        <div class="modal-body" id="dialog-message"></div>
        <div class="modal-footer">
          <button type="button" class="btn btn-secondary" id="dialog-cancel" data-bs-dismiss="modal">Cancel</button>
          <button type="button" class="btn btn-primary" id="dialog-confirm">OK</button>
        </div>
      </div>
    </div>
  </div>
```

- [ ] **Step 2: Verify the modal renders without breaking the page**

Bring up the stack (`podman-compose -f docker-compose.yml -f docker-compose.capstone.yml -f docker-compose.integrated.yml -f docker-compose.debug.yml up --build` — or if already running, just rebuild webserver: `podman-compose ... up -d --build webserver`). Open `http://localhost:5000`, sign in. The page should render normally — no visible modal, no JS errors in the browser console.

In the browser DevTools console run:
```js
document.getElementById('dialog-modal') !== null
```
Expected: `true`.

- [ ] **Step 3: Commit**

```bash
git add Backend/PokemonLocations.WebServer/wwwroot/index.html
git commit -m "feat(frontend): add reusable #dialog-modal markup"
```

---

## Task 2: Add `showDialog` / `showAlert` / `showConfirm` helpers

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/wwwroot/script.js` (insert helpers immediately before the `setupActions` function — currently around line 887)

- [ ] **Step 1: Insert the helper functions**

In `Backend/PokemonLocations.WebServer/wwwroot/script.js`, find the comment `// ─── Action buttons ───` (currently line 887). Insert the following block immediately *before* that comment, with one blank line separating it from the preceding code:

```js
// ─── Custom dialog helpers ───
// Promise-based replacements for native alert() / confirm().
// Resolves true on confirm-click, false on any dismiss path
// (Cancel, X, Escape, backdrop click).
function showDialog({ title, message, confirmText, cancelText, variant }) {
    const modalEl = document.getElementById('dialog-modal');
    const titleEl = document.getElementById('dialog-title');
    const messageEl = document.getElementById('dialog-message');
    const confirmBtn = document.getElementById('dialog-confirm');
    const cancelBtn = document.getElementById('dialog-cancel');

    titleEl.textContent = title;
    messageEl.textContent = message;
    confirmBtn.textContent = confirmText;

    confirmBtn.classList.remove('btn-primary', 'btn-danger');
    confirmBtn.classList.add(variant === 'danger' ? 'btn-danger' : 'btn-primary');

    if (cancelText === null) {
        cancelBtn.style.display = 'none';
    } else {
        cancelBtn.style.display = '';
        cancelBtn.textContent = cancelText;
    }

    return new Promise(resolve => {
        let resolved = false;
        const onConfirm = () => {
            resolved = true;
            modal.hide();
        };
        const onHidden = () => {
            confirmBtn.removeEventListener('click', onConfirm);
            modalEl.removeEventListener('hidden.bs.modal', onHidden);
            resolve(resolved);
        };
        confirmBtn.addEventListener('click', onConfirm);
        modalEl.addEventListener('hidden.bs.modal', onHidden);

        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();
    });
}

function showAlert(message, opts = {}) {
    return showDialog({
        title: opts.title ?? 'Notice',
        message,
        confirmText: opts.buttonText ?? 'OK',
        cancelText: null,
        variant: 'default'
    });
}

function showConfirm(message, opts = {}) {
    return showDialog({
        title: opts.title ?? 'Confirm',
        message,
        confirmText: opts.confirmText ?? 'Confirm',
        cancelText: opts.cancelText ?? 'Cancel',
        variant: opts.variant ?? 'default'
    });
}

```

(Note: trailing blank line is intentional — leaves separation before `// ─── Action buttons ───`.)

- [ ] **Step 2: Smoke-test the helpers from the browser console**

Reload `http://localhost:5000` (the cache-buster bump from Task 1 forces a fresh fetch of `script.js`). Open DevTools console and run:

```js
await showAlert('Helpers are wired up.', { title: 'Test' })
```
Expected: a centered modal appears with title "Test", body "Helpers are wired up.", and one OK button. Click OK — promise resolves to `undefined`.

Then run:
```js
await showConfirm('Pick one.', { variant: 'danger' })
```
Expected: a modal appears with title "Confirm", a red Confirm button, and a Cancel button. Click Confirm → resolves `true`. Re-open and press Escape → resolves `false`. Re-open and click backdrop → resolves `false`.

- [ ] **Step 3: Commit**

```bash
git add Backend/PokemonLocations.WebServer/wwwroot/script.js
git commit -m "feat(frontend): add showDialog/showAlert/showConfirm helpers"
```

---

## Task 3: Migrate account-delete confirm + 2 alerts

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/wwwroot/script.js` (lines 889-904 at branch HEAD `1a93818`; re-grep to confirm)

- [ ] **Step 1: Replace the account-delete handler block**

Find the block in `Backend/PokemonLocations.WebServer/wwwroot/script.js` that currently reads:

```js
    document.getElementById('btn-delete-account').addEventListener('click', async () => {
        if (!confirm('Are you sure you want to delete your account? This cannot be undone.')) return;

        try {
            const res = await PLAuth.authFetch('/account', { method: 'DELETE' });
            if (res.ok) {
                PLAuth.clearCreds();
                window.location.href = '/signin.html';
            } else {
                alert('Failed to delete account.');
            }
        } catch (e) {
            alert('Failed to delete account.');
            console.error('Delete account failed:', e.message);
        }
    });
```

Replace it with:

```js
    document.getElementById('btn-delete-account').addEventListener('click', async () => {
        const ok = await showConfirm('Are you sure you want to delete your account? This cannot be undone.', {
            title: 'Delete account',
            confirmText: 'Delete account',
            cancelText: 'Cancel',
            variant: 'danger'
        });
        if (!ok) return;

        try {
            const res = await PLAuth.authFetch('/account', { method: 'DELETE' });
            if (res.ok) {
                PLAuth.clearCreds();
                window.location.href = '/signin.html';
            } else {
                await showAlert('Failed to delete account.', { title: 'Error' });
            }
        } catch (e) {
            await showAlert('Failed to delete account.', { title: 'Error' });
            console.error('Delete account failed:', e.message);
        }
    });
```

- [ ] **Step 2: Manually verify**

Reload the app. Click "Delete account":
- Expected: red modal "Delete account" with "Delete account" / "Cancel" buttons. Click Cancel — modal closes, account remains. Re-click and confirm — account deletes, redirect to signin (use a throwaway account!).

To exercise failure path without deleting: stop the WebServer's API connection (`podman stop pokemon-locations_api_1` or rename `/api/account` route temporarily) and click confirm. Expected: an "Error" modal with "Failed to delete account." and a single OK button. Bring the API back up afterward.

- [ ] **Step 3: Commit**

```bash
git add Backend/PokemonLocations.WebServer/wwwroot/script.js
git commit -m "feat(frontend): migrate account-delete dialog to showConfirm/showAlert"
```

---

## Task 4: Migrate set-location failure alerts (2 sites)

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/wwwroot/script.js` (lines 943 and 952 at branch HEAD)

- [ ] **Step 1: Replace both set-location alerts**

Find the block (currently around lines 924-954) and update only the two `alert(...)` lines and the surrounding `try`/`catch` to await:

Before:
```js
            if (!res.ok) {
                weatherState.homePlanetName = previousName;
                weatherState.homePlanetTemp = previousTemp;
                renderCurrentLocation();
                alert('Failed to set location.');
                return;
            }
            locationModal.hide();
        } catch (e) {
            weatherState.homePlanetName = previousName;
            weatherState.homePlanetTemp = previousTemp;
            renderCurrentLocation();
            console.error('Set location failed:', e.message);
            alert('Failed to set location.');
        }
```

After:
```js
            if (!res.ok) {
                weatherState.homePlanetName = previousName;
                weatherState.homePlanetTemp = previousTemp;
                renderCurrentLocation();
                await showAlert('Failed to set location.', { title: 'Error' });
                return;
            }
            locationModal.hide();
        } catch (e) {
            weatherState.homePlanetName = previousName;
            weatherState.homePlanetTemp = previousTemp;
            renderCurrentLocation();
            console.error('Set location failed:', e.message);
            await showAlert('Failed to set location.', { title: 'Error' });
        }
```

(The enclosing `addEventListener('click', async (event) => {` is already `async`, so `await` is valid.)

- [ ] **Step 2: Manually verify**

Reload. To trigger failure: stop the API container, then open the location picker and click any planet. Expected: themed Error modal **stacked on top** of the open `#location-modal`. Confirm:
- The error modal's confirm OK is clickable.
- After dismissing, the location modal is still open and the home planet is reverted to the previous value.
- No native browser alert appears.

If the double-darkened backdrop looks bad (two stacked `.modal-backdrop` elements), proceed for now — final modal-on-modal polish is folded into Task 7.

Restart the API afterward.

- [ ] **Step 3: Commit**

```bash
git add Backend/PokemonLocations.WebServer/wwwroot/script.js
git commit -m "feat(frontend): migrate set-location failure alerts to showAlert"
```

---

## Task 5: Migrate theme-update failure alerts (2 sites)

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/wwwroot/script.js` (lines 976 and 984 at branch HEAD)

- [ ] **Step 1: Replace both theme-update alerts**

Find the block (currently around lines 968-986) and replace:

Before:
```js
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
```

After:
```js
            try {
                const res = await PLAuth.authFetch('/account/theme', {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ theme: selectedTheme })
                });
                if (!res.ok) {
                    applyTheme(previous);
                    await showAlert('Failed to update theme.', { title: 'Error' });
                    return;
                }
                const themeLabel = document.querySelector('#user-info p:nth-child(2) strong');
                if (themeLabel) themeLabel.textContent = formatThemeName(selectedTheme);
                themeModal.hide();
            } catch (e) {
                applyTheme(previous);
                await showAlert('Failed to update theme.', { title: 'Error' });
                console.error('Theme update failed:', e.message);
            }
```

- [ ] **Step 2: Manually verify**

Reload. Stop the API, open the theme picker, click an unlocked theme. Expected: themed Error modal stacked over `#theme-modal`. After dismiss, the original theme remains applied. No native browser alert.

Restart the API afterward.

- [ ] **Step 3: Commit**

```bash
git add Backend/PokemonLocations.WebServer/wwwroot/script.js
git commit -m "feat(frontend): migrate theme-update failure alerts to showAlert"
```

---

## Task 6: Locked-theme cleanup (drop alert, drop emoji, add tooltip)

**Files:**
- Modify: `Backend/PokemonLocations.WebServer/wwwroot/index.html` (CSS rule at lines 36-38)
- Modify: `Backend/PokemonLocations.WebServer/wwwroot/script.js` (locked-theme click block at lines 960-963 and `setupActions` end)

- [ ] **Step 1: Remove the lock emoji CSS rule**

In `Backend/PokemonLocations.WebServer/wwwroot/index.html`, delete this rule (currently at lines 36-38) entirely:

```css
    .theme-option.theme-locked::after {
      content: " 🔒";
    }

```

(Delete the rule plus the trailing blank line so two blank lines don't collapse oddly.)

- [ ] **Step 2: Replace the locked-theme click alert with a no-op**

In `Backend/PokemonLocations.WebServer/wwwroot/script.js`, find this block (currently around lines 957-963):

```js
        btn.addEventListener('click', async () => {
            const selectedTheme = btn.dataset.theme;
            const resolved = selectedTheme === 'random' ? pickRandomTheme() : selectedTheme;
            if (!isThemeUnlocked(resolved)) {
                alert(`This theme is locked. ${THEME_UNLOCK_RULES[resolved].label} to unlock it.`);
                return;
            }
```

Replace with:

```js
        btn.addEventListener('click', async () => {
            const selectedTheme = btn.dataset.theme;
            const resolved = selectedTheme === 'random' ? pickRandomTheme() : selectedTheme;
            if (!isThemeUnlocked(resolved)) return;
```

- [ ] **Step 3: Initialize Bootstrap tooltips on every theme-option button**

In `Backend/PokemonLocations.WebServer/wwwroot/script.js`, find the end of `setupActions` (currently line 988 `});` followed by `}` on line 989). Insert tooltip initialization just before the closing `}` of `setupActions`. The function should end like this:

Before:
```js
            } catch (e) {
                applyTheme(previous);
                await showAlert('Failed to update theme.', { title: 'Error' });
                console.error('Theme update failed:', e.message);
            }
        });
    });
}
```

After:
```js
            } catch (e) {
                applyTheme(previous);
                await showAlert('Failed to update theme.', { title: 'Error' });
                console.error('Theme update failed:', e.message);
            }
        });
    });

    document.querySelectorAll('.theme-option').forEach(btn => {
        new bootstrap.Tooltip(btn, { placement: 'right', trigger: 'hover focus' });
    });
}
```

The existing `updateThemeButtons` function (around line 803) already sets `btn.title = '...'` for locked themes and `btn.title = ''` for unlocked. Bootstrap's Tooltip reads the `title` attribute on each show, so when `updateThemeButtons` re-runs after a stat change unlocks a theme, the tooltip text updates automatically — no manual refresh needed.

- [ ] **Step 4: Manually verify locked-theme UX**

Reload. Open the theme picker. Confirm:
- **No 🔒 emoji** anywhere on locked theme buttons (use a throwaway account that hasn't earned everything; locked themes are still greyed/strikethrough-styled via the existing `.theme-option.theme-locked` rule).
- **Hover over a locked theme button**: Bootstrap tooltip appears to the right after a brief delay, themed (white-on-dark default Bootstrap styling), showing text like "Unlock condition: Visit 5 locations".
- **Click a locked theme button**: nothing happens. No alert, no theme change, no console error.
- **Hover over the Random option**: tooltip says "Randomly choose from your unlocked themes".
- **Hover over an unlocked specific theme**: no tooltip (because `btn.title` is set to `''`).
- **Tab to a locked theme button**: tooltip appears (because `trigger: 'hover focus'`).

- [ ] **Step 5: Commit**

```bash
git add Backend/PokemonLocations.WebServer/wwwroot/index.html Backend/PokemonLocations.WebServer/wwwroot/script.js
git commit -m "feat(frontend): replace locked-theme alert with Bootstrap tooltip"
```

---

## Task 7: Final verification + modal-on-modal polish

**Files:**
- Possibly modify: `Backend/PokemonLocations.WebServer/wwwroot/index.html` (only if Step 2 below confirms a visual issue)

- [ ] **Step 1: Grep for any remaining native dialog calls**

Run:
```bash
grep -nE "\b(alert|confirm|prompt)\(" Backend/PokemonLocations.WebServer/wwwroot/script.js
```

Expected output: **empty.** Zero matches.

If anything appears, that call site needs migration — go back and fix it before continuing. (The substring `alert` appears in CSS class names like `alert-danger` in `signin.html` / `signup.html` — those are inline error boxes and out of scope, and the grep above only scans `script.js` so they won't show up.)

- [ ] **Step 2: Modal-on-modal stacking check**

Reload. Stop the API. Open the location picker, click a planet — observe the stacked Error modal. Then re-open the theme picker, click an unlocked theme — observe the stacked Error modal there too.

Acceptance: the inner (Error) modal sits clearly on top, its content is readable, the close paths (OK/X/Escape/backdrop) all work and return focus to the underlying modal.

If the **double-darkened backdrop** is visually distracting (two stacked `.modal-backdrop` elements compounding opacity), add this CSS rule to `Backend/PokemonLocations.WebServer/wwwroot/index.html` inside the existing `<style>` block (next to other modal rules — anywhere in the stylesheet works, but a logical spot is right after the `.gallery-modal` rules around line 522):

```css
    /* When a second modal stacks on top, suppress the inner backdrop
       so the outer modal's backdrop alone provides the dim. */
    .modal-backdrop + .modal-backdrop {
      opacity: 0;
    }
```

If the double-darkening looks fine, **skip this CSS addition** — leaner is better.

Restart the API after testing.

- [ ] **Step 3: Full UX checklist walkthrough**

With the API healthy, walk through every native-dialog removal site one last time and confirm each behaves per the spec's Section 5.2 manual checklist (`docs/designs/specs/2026-05-08-standardize-dialogs.md`):

- [ ] Click Delete account → red themed confirm modal with proper labels. Cancel works. Confirm with API down → themed Error modal.
- [ ] Update home planet with API down → themed Error modal stacked on location picker.
- [ ] Update theme with API down → themed Error modal stacked on theme picker.
- [ ] Hover any locked theme → Bootstrap tooltip with unlock condition.
- [ ] Click any locked theme → nothing happens.
- [ ] No 🔒 emoji visible on any locked theme.
- [ ] Tab/Escape work in dialog modal (focus trap + Escape-to-dismiss).
- [ ] No browser-native dialogs anywhere on the site, in any flow.

- [ ] **Step 4: Commit (only if Step 2 added the CSS rule)**

If you added the `.modal-backdrop + .modal-backdrop` rule:
```bash
git add Backend/PokemonLocations.WebServer/wwwroot/index.html
git commit -m "fix(frontend): suppress stacked-modal double backdrop"
```

If not, no commit needed for this task — Tasks 1-6 already cover all changes.

---

## Out of scope (reminder, do not touch)

- Gallery toast (`#gallery-toast`) — already custom and themed (SE498-68).
- Gallery delete X inline expand-to-confirm — already custom (SE498-68).
- Gallery image viewer (`#gallery-modal`) — already custom (SE498-68).
- Existing Bootstrap modals `#theme-modal` and `#location-modal` — those keep their existing structure; we only add `#dialog-modal` alongside them.
- `signin.html` / `signup.html` inline error boxes (`<div class="alert alert-danger">`) — different UX (form validation), already styled, out of scope.
- Internationalization of dialog text.
- Adding a JS test framework — none exists; not creating one.
