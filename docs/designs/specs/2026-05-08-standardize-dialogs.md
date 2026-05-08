# SE498-93: Standardize browser dialogs to themed custom modals — Design

**Status:** Draft, pending implementation
**Ticket:** SE498-93 (Jira)
**Author:** Maks Popov
**Date:** 2026-05-08

## 1. Goal

Replace every native browser `alert()` / `confirm()` / `prompt()` in the frontend with theme-consistent custom modals. The site should never trigger a browser-native dialog box anywhere. Locked-theme UX is simplified along the way: drop the lock emoji and the click-time alert, rely on a themed hover tooltip for the unlock condition.

## 2. Current State

`grep -nE "\b(alert|confirm|prompt)\(" wwwroot/script.js` returns:

| Line | Type | Message | Context |
|---|---|---|---|
| 889 | `confirm()` | "Are you sure you want to delete your account? This cannot be undone." | Account-delete flow (`#user-info` area) |
| 897, 900 | `alert()` | "Failed to delete account." | Account-delete failure paths |
| 942, 951 | `alert()` | "Failed to set location." | Home-planet update failure paths (inside `#location-modal`) |
| 960 | `alert()` | `This theme is locked. <rule.label> to unlock it.` | Locked-theme click (inside `#theme-modal`) |
| 975, 983 | `alert()` | "Failed to update theme." | Theme update failure paths (inside `#theme-modal`) |

`prompt()` — none.

Other locked-theme UI:
- `index.html:36-38` — `.theme-option.theme-locked::after { content: " 🔒"; }` (lock emoji)
- `script.js:826` — `btn.title = "Unlock condition: <rule.label>"` (native browser tooltip on hover)

Already-custom (out of scope):
- `#gallery-toast`, gallery inline expand-to-confirm delete X, `#gallery-modal` (image viewer) — all built for SE498-68
- Bootstrap modals: `#theme-modal`, `#location-modal`
- Signin/signup `<div class="alert alert-danger">` inline error boxes

## 3. Architecture

### 3.1 Reusable dialog modal

One Bootstrap-5 modal in `index.html`:

```html
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

Bootstrap 5 modals are accessible by default — focus trap, Escape-to-dismiss, `tabindex="-1"`, `aria-modal`. Theme variables flow through via the `--theme-primary-dark` etc. classes already on the site (button colors etc. inherit from the active theme stylesheet).

### 3.2 JS helper API

Three functions added near the top of `script.js`:

```js
// Promise resolves to true on confirm, false on cancel/Escape/X.
function showConfirm(message, opts = {}) { ... }

// Promise resolves (void) when dismissed.
function showAlert(message, opts = {}) { ... }

// Internal — drives the modal.
function showDialog({ title, message, confirmText, cancelText, variant }) { ... }
```

`opts` for `showAlert`:
- `title?: string` (default `"Notice"`)
- `buttonText?: string` (default `"OK"`)

`opts` for `showConfirm`:
- `title?: string` (default `"Confirm"`)
- `confirmText?: string` (default `"Confirm"`)
- `cancelText?: string` (default `"Cancel"`)
- `variant?: 'default' | 'danger'` — `'danger'` makes the confirm button `btn-danger` (red) instead of `btn-primary`

`showAlert` calls `showDialog` with `cancelText: null` (which hides the Cancel button via `style.display = 'none'`).

`showDialog` returns a Promise that:
- resolves `true` when the confirm button is clicked
- resolves `false` when the modal hides without the confirm click being captured (Cancel, X, Escape, backdrop click)
- registers/unregisters listeners cleanly so calling the helper repeatedly doesn't leak

### 3.3 Refactor sites

**Account delete confirm + failure** (`script.js:889, 897, 900`):

```js
// before
if (!confirm('Are you sure you want to delete your account? This cannot be undone.')) return;
try { ... } catch { alert('Failed to delete account.'); }
// also: if (!res.ok) { alert('Failed to delete account.'); ... }

// after
const ok = await showConfirm('Are you sure you want to delete your account? This cannot be undone.', {
    title: 'Delete account',
    confirmText: 'Delete account',
    cancelText: 'Cancel',
    variant: 'danger'
});
if (!ok) return;
try { ... } catch { await showAlert('Failed to delete account.', { title: 'Error' }); }
// also: if (!res.ok) { await showAlert('Failed to delete account.', { title: 'Error' }); ... }
```

**Set-location failure** (`script.js:942, 951`):

```js
// before
alert('Failed to set location.');
// after
await showAlert('Failed to set location.', { title: 'Error' });
```

**Theme update failure** (`script.js:975, 983`):

```js
// before
alert('Failed to update theme.');
// after
await showAlert('Failed to update theme.', { title: 'Error' });
```

**Locked-theme click** (`script.js:960`): **removed entirely.** The new click handler:

```js
const selectedTheme = btn.dataset.theme;
const resolved = selectedTheme === 'random' ? pickRandomTheme() : selectedTheme;
if (!isThemeUnlocked(resolved)) return;  // no-op; hover tooltip already conveys the condition
// ... rest unchanged
```

### 3.4 Locked-theme visual cleanup

In `index.html`, remove the lock emoji rule:

```css
/* before */
.theme-option.theme-locked::after {
  content: " 🔒";
}

/* after — entire rule deleted */
```

Replace the native `title="..."` tooltip on locked theme buttons with a Bootstrap tooltip:

- The existing `updateThemeButtons` function (`script.js:803`) sets `btn.title = ...` for locked themes. Bootstrap tooltips read the same `title` attribute by default, so the dynamic title-setting logic stays unchanged.
- Initialize Bootstrap tooltips once at page load on every `.theme-option` button. Tooltips on buttons without a title attribute (i.e., unlocked themes) silently no-op.
- Use placement `right` and trigger `hover focus` so keyboard users see it too.

```js
// New: at the end of setupActions() (or in DOMContentLoaded right after theme buttons render)
document.querySelectorAll('.theme-option').forEach(btn => {
    new bootstrap.Tooltip(btn, { placement: 'right', trigger: 'hover focus' });
});
```

When `updateThemeButtons` re-runs (after a stat change unlocks a theme), the title attribute changes. Bootstrap Tooltip reads from the `title` attribute on each show — no manual refresh needed.

### 3.5 Modal-on-modal handling

Three of the alerts fire while another modal is open:
- Theme update failures (line 975, 983) — `#theme-modal` open
- Location update failures (line 942, 951) — `#location-modal` open

Bootstrap 5 supports stacked modals natively — each new modal gets a higher `z-index` automatically. The known visual artifact is **double backdrop darkening** when two modals stack. Two paths:

1. **Accept the stacking.** Let Bootstrap handle it. Verify in manual UX — if the double darkening doesn't look bad, ship as-is.
2. **Add CSS to suppress the inner backdrop:**
   ```css
   .modal-backdrop + .modal-backdrop { opacity: 0; }
   ```
   Only apply if (1) looks bad in practice.

Decision deferred to manual verification (Section 5). Default to (1).

## 4. Edge Cases

- **`showConfirm` resolves `false` on backdrop click and Escape.** This is desired — both are user-cancellation signals.
- **`showAlert` returns void.** Callers `await` for synchronization (e.g., wait until the user dismisses before re-enabling a button).
- **Helpers re-entrant?** No. Only one dialog visible at a time. If a second `showDialog` is called while one is open, the second call queues until the first hides (Bootstrap auto-handles the show-vs-show race; the Promise of the second call resolves correctly).
- **Memory leaks from event listeners.** Each `showDialog` invocation attaches `click` (on confirm) and `hidden.bs.modal` (on the modal element). The `hidden.bs.modal` listener detaches both on cleanup. Verified via repeated open/close.
- **Keyboard navigation.** Bootstrap traps Tab inside the modal. Confirm and Cancel buttons are focusable. Escape closes. The button text in `confirmText` is rendered into the existing `<button>` inside the modal, no recreation.
- **Locked theme click as a non-interactive button.** A locked theme button currently has `disabled = false` (so :hover works) but visually appears greyed. After the change, clicking it does nothing (no modal). The hover tooltip is the only feedback. This matches established patterns elsewhere (e.g., disabled-but-styled UI).

## 5. Testing

### 5.1 Backend

No changes to backend code. No new C# tests required.

### 5.2 Frontend (manual checklist)

After implementation, run through every native-dialog removal site to confirm:

| Action | Expected |
|---|---|
| Click "Delete account" in profile area | Custom modal appears, "Delete account" / "Cancel" buttons, red Delete button (variant: danger). Click Cancel → no-op. |
| Click "Delete account", confirm | Account is deleted; redirected to signin. |
| Trigger account-delete failure (manually fail backend, e.g., stop API) | Custom error modal appears, title "Error", body "Failed to delete account.", single OK button. |
| Update home planet with stack down | Same — error modal, not browser alert. Theme picker can stack on top, no native dialogs. |
| Update theme with stack down | Same — error modal stacked over the theme picker. Verify modal-on-modal looks acceptable. |
| Hover any locked theme button | Bootstrap tooltip appears (themed, positioned right) showing the unlock condition. |
| Click any locked theme button | Nothing happens (no alert, no theme change). Tooltip stays visible while hovering. |
| Inspect the DOM | No 🔒 emoji on any locked theme. |
| `grep -nE "\b(alert\|confirm\|prompt)\(" wwwroot/script.js` | Returns zero matches. |

### 5.3 Accessibility (manual)

| Check | Expected |
|---|---|
| Tab inside the dialog modal | Focus cycles between Confirm and Cancel only (focus trap). |
| Escape key | Modal dismisses; promise resolves false. |
| Screen reader inspection | Modal has `role="dialog"`, `aria-modal="true"`, title labelled. (Bootstrap defaults.) |

## 6. Out of Scope

- The other already-custom dialogs: gallery toast, gallery delete-X inline confirm, gallery image modal, theme/location/dialog modals — already polished, not touched.
- New dialog usages added by future tickets — those should call `showAlert` / `showConfirm` from day one.
- Refactoring the existing inline error boxes on signin/signup — they're already styled and serve a different UX (inline form validation vs. transient action feedback).
- Internationalization of dialog text — not on the project's roadmap.
- Animated transitions beyond Bootstrap's default modal fade.
