// ─── API helper ───
const apiFetch = (path, options = {}) => PLAuth.authFetch(`/api${path}`, options);

// ─── State ───
let allLocations = [];
let selectedLocationId = null;
let currentBadges = new Set();
let noteDebounceTimer = null;

// ─── Badge definitions (order matches Figma) ───
const BADGES = [
    { key: 'boulder',  label: 'Boulder Badge' },
    { key: 'cascade',  label: 'Cascade Badge' },
    { key: 'thunder',  label: 'Thunder Badge' },
    { key: 'rainbow',  label: 'Rainbow Badge' },
    { key: 'soul',     label: 'Soul Badge' },
    { key: 'marsh',    label: 'Marsh Badge' },
    { key: 'volcano',  label: 'Volcano Badge' },
    { key: 'earth',    label: 'Earth Badge' }
];

// ─── Ring chart ───
const RING_CIRCUMFERENCE = 2 * Math.PI * 101; // r=101 from SVG

function updateRing(earned) {
    const fill = document.getElementById('ring-fill');
    const label = document.getElementById('ring-label');
    const offset = RING_CIRCUMFERENCE - (earned / 8) * RING_CIRCUMFERENCE;
    fill.setAttribute('stroke-dashoffset', offset.toString());
    label.textContent = `${earned}/8`;
}

// ─── Badge list ───
function renderBadges() {
    const list = document.getElementById('badge-list');
    list.replaceChildren();

    BADGES.forEach(badge => {
        const li = document.createElement('li');
        li.className = 'badge-item';
        li.setAttribute('role', 'checkbox');
        li.setAttribute('aria-checked', currentBadges.has(badge.key));
        li.id = `badge-${badge.key}`;

        const check = document.createElement('span');
        check.className = `badge-check${currentBadges.has(badge.key) ? ' earned' : ''}`;

        const text = document.createElement('span');
        text.textContent = badge.label;

        li.append(check, text);
        li.addEventListener('click', () => toggleBadge(badge.key));
        list.appendChild(li);
    });

    updateRing(currentBadges.size);
}

async function toggleBadge(key) {
    const earned = currentBadges.has(key);
    const method = earned ? 'DELETE' : 'PUT';

    try {
        const res = await apiFetch(`/me/badges/${key}`, { method });
        if (res.ok) {
            if (earned) currentBadges.delete(key); else currentBadges.add(key);
            renderBadges();
            await loadStats();
        }
    } catch (e) {
        console.error('Badge toggle failed:', e.message);
    }
}

async function loadBadges() {
    try {
        const res = await apiFetch('/me/badges');
        if (!res.ok) return;
        const badges = await res.json();
        currentBadges = new Set(badges);
    } catch (e) {
        console.error('Failed to load badges:', e.message);
    }
    renderBadges();
}

// ─── Locations dropdown ───
function fitSelectWidth(select) {
    const opt = select.options[select.selectedIndex];
    if (!opt) return;
    const probe = document.createElement('span');
    probe.textContent = opt.textContent;
    const cs = getComputedStyle(select);
    Object.assign(probe.style, {
        position: 'absolute',
        visibility: 'hidden',
        whiteSpace: 'pre',
        font: cs.font,
        letterSpacing: cs.letterSpacing,
    });
    document.body.appendChild(probe);
    const textWidth = probe.getBoundingClientRect().width;
    probe.remove();
    const paddingRight = parseFloat(cs.paddingRight) || 0;
    select.style.width = `${Math.ceil(textWidth + paddingRight + 2)}px`;
}

async function loadLocations() {
    const select = document.getElementById('location-select');
    try {
        const res = await apiFetch('/locations');
        if (!res.ok) throw new Error(`Status: ${res.status}`);
        allLocations = await res.json();

        select.replaceChildren();
        allLocations.forEach(loc => {
            const opt = document.createElement('option');
            opt.value = loc.locationId;
            opt.textContent = loc.name;
            select.appendChild(opt);
        });

        if (allLocations.length > 0) {
            const savedLocationId = localStorage.getItem('selectedLocationId');
            if (savedLocationId && allLocations.some(l => l.locationId == savedLocationId)) {
                selectedLocationId = parseInt(savedLocationId, 10);
            } else {
                selectedLocationId = allLocations[0].locationId;
            }
            select.value = selectedLocationId;
            fitSelectWidth(select);
            await selectLocation(selectedLocationId);
        }
    } catch (e) {
        select.replaceChildren();
        const opt = document.createElement('option');
        opt.textContent = 'Unable to load locations';
        select.appendChild(opt);
        console.error('Failed to load locations:', e.message);
    }
}

async function selectLocation(locationId) {
    selectedLocationId = locationId;
    await Promise.all([
        loadLocationDetail(locationId),
        loadBuildings(locationId),
        loadNote(locationId)
    ]);
}

// ─── Location detail (center column) ───
async function loadLocationDetail(locationId) {
    const statusEl = document.getElementById('location-status');
    const descEl = document.getElementById('location-description');
    const galleryEl = document.getElementById('image-gallery');

    statusEl.textContent = '';
    descEl.textContent = '';

    try {
        const res = await apiFetch(`/locations/${locationId}`);
        if (!res.ok) {
            descEl.textContent = 'Location not found';
            return;
        }

        const location = await res.json();
        descEl.textContent = location.description || '';

        // Image gallery
        const images = [
            ...(location.images || []),
            ...(location.userImages || [])
        ];

        galleryEl.replaceChildren();
        if (images.length > 0) {
            images.forEach(img => {
                const imgEl = document.createElement('img');
                imgEl.src = img.imageUrl || img.url;
                imgEl.alt = img.caption || location.name;
                galleryEl.appendChild(imgEl);
            });
        } else {
            galleryEl.textContent = 'Image Gallery';
        }

        // Status is computed after buildings load — see updateLocationStatus()
    } catch (e) {
        descEl.textContent = 'Error loading location';
        console.error('Failed to load location detail:', e.message);
    }
}

function updateLocationStatus(buildings) {
    const statusEl = document.getElementById('location-status');
    if (!buildings || buildings.length === 0) {
        statusEl.innerHTML = 'Status: <span class="status-not-visited">No Buildings</span>';
        return;
    }

    const allVisited = buildings.every(b => b.visited);
    const noneVisited = buildings.every(b => !b.visited);

    if (allVisited) {
        statusEl.innerHTML = 'Status: <span class="status-visited">All Locations Visited</span>';
    } else if (noneVisited) {
        statusEl.innerHTML = 'Status: <span class="status-not-visited">No Locations Visited</span>';
    } else {
        const count = buildings.filter(b => b.visited).length;
        statusEl.innerHTML = `Status: <span class="status-not-visited">${count}/${buildings.length} Visited</span>`;
    }
}

// ─── Buildings list (column 3) ───
let currentBuildings = [];

async function loadBuildings(locationId) {
    const list = document.getElementById('building-list');
    list.replaceChildren();

    try {
        const res = await apiFetch(`/locations/${locationId}/buildings`);
        if (!res.ok) throw new Error(`Status: ${res.status}`);
        currentBuildings = await res.json();

        renderBuildings();
        updateLocationStatus(currentBuildings);
    } catch (e) {
        const li = document.createElement('li');
        li.className = 'building-item';
        li.textContent = 'Unable to load buildings';
        list.appendChild(li);
        console.error('Failed to load buildings:', e.message);
    }
}

function renderBuildings() {
    const list = document.getElementById('building-list');
    list.replaceChildren();

    currentBuildings.forEach(b => {
        const li = document.createElement('li');
        li.className = 'building-item';
        li.id = `building-${b.buildingId}`;
        li.setAttribute('role', 'checkbox');
        li.setAttribute('aria-checked', b.visited);

        const check = document.createElement('span');
        check.className = `building-check${b.visited ? ' visited' : ''}`;

        const text = document.createElement('span');
        text.textContent = b.name;

        li.append(check, text);
        li.addEventListener('click', () => toggleBuildingVisited(b));
        list.appendChild(li);
    });
}

async function toggleBuildingVisited(building) {
    const method = building.visited ? 'DELETE' : 'PUT';

    try {
        const res = await apiFetch(
            `/me/visited/buildings/${selectedLocationId}/${building.buildingId}`,
            { method }
        );
        if (res.ok) {
            building.visited = !building.visited;
            renderBuildings();
            updateLocationStatus(currentBuildings);
            await loadStats();
        }
    } catch (e) {
        console.error('Building toggle failed:', e.message);
    }
}

// ─── Notes ───
async function loadNote(locationId) {
    const textarea = document.getElementById('notes-area');
    textarea.value = '';

    try {
        const res = await apiFetch(`/me/notes/${locationId}`);
        if (res.ok) {
            const data = await res.json();
            textarea.value = data.noteText || '';
        }
        // 404 means no note yet — leave blank
    } catch (e) {
        console.error('Failed to load note:', e.message);
    }
}

function setupNotesAutoSave() {
    const textarea = document.getElementById('notes-area');
    const saved = document.getElementById('notes-saved');

    textarea.addEventListener('input', () => {
        clearTimeout(noteDebounceTimer);
        saved.classList.remove('visible');

        noteDebounceTimer = setTimeout(async () => {
            if (!selectedLocationId) return;

            const text = textarea.value.trim();
            try {
                if (text === '') {
                    await apiFetch(`/me/notes/${selectedLocationId}`, { method: 'DELETE' });
                } else {
                    await apiFetch(`/me/notes/${selectedLocationId}`, {
                        method: 'PUT',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ noteText: text })
                    });
                }
                saved.classList.add('visible');
                setTimeout(() => saved.classList.remove('visible'), 2000);
            } catch (e) {
                console.error('Failed to save note:', e.message);
            }
        }, 800);
    });
}

// ─── Themes ───
const THEMES = ['bulbasaur', 'charmander', 'squirtle', 'pikachu'];
const THEME_CACHE_KEY = 'pl.theme';

function applyTheme(name) {
    if (!THEMES.includes(name)) name = 'bulbasaur';
    document.documentElement.setAttribute('data-theme', name);
    const link = document.getElementById('theme-stylesheet');
    if (link) link.href = `/css/themes/${name}.css`;
    sessionStorage.setItem(THEME_CACHE_KEY, name);
}

const formatThemeName = (t) => t ? t.charAt(0).toUpperCase() + t.slice(1) : '';

// Apply cached theme synchronously to avoid a flash
applyTheme(sessionStorage.getItem(THEME_CACHE_KEY) || 'bulbasaur');

// ─── User info ───
async function loadUserInfo() {
    const container = document.getElementById('user-info');

    try {
        const res = await apiFetch('/me');
        if (!res.ok) throw new Error(`Status: ${res.status}`);
        const user = await res.json();

        applyTheme(user.theme);

        container.innerHTML = `
            <p>Logged in as: <strong>${escapeHtml(user.displayName)}</strong></p>
            <p>Theme: <strong>${escapeHtml(formatThemeName(user.theme))}</strong></p>
        `;
    } catch (e) {
        container.innerHTML = '<p class="loading-text">Not signed in</p>';
        console.error('Failed to load user info:', e.message);
    }
}

// ─── Stats ───
async function loadStats() {
    try {
        const res = await apiFetch('/me/stats');
        if (!res.ok) return;
        const stats = await res.json();

        document.getElementById('stat-gyms').textContent = stats.gymsComplete;
        document.getElementById('stat-locations').textContent = stats.locationsVisited;
        document.getElementById('stat-buildings').textContent = stats.buildingsVisited;
    } catch (e) {
        console.error('Failed to load stats:', e.message);
    }
}

// ─── Action buttons ───
function setupActions() {
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

    document.getElementById('btn-log-out').addEventListener('click', () => {
        PLAuth.clearCreds();
        window.location.href = '/signin.html';
    });

    const themeModalEl = document.getElementById('theme-modal');
    const themeModal = new bootstrap.Modal(themeModalEl);

    document.getElementById('btn-change-theme').addEventListener('click', () => {
        themeModal.show();
    });

    document.querySelectorAll('.theme-option').forEach(btn => {
        btn.addEventListener('click', async () => {
            const theme = btn.dataset.theme;
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
}

// ─── Utilities ───
function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}

// ─── Bootstrap ───
document.addEventListener('DOMContentLoaded', async () => {
    PLAuth.requireAuth();
    if (!PLAuth.getCreds()) return;

    // Set up event listeners
    document.getElementById('location-select').addEventListener('change', (e) => {
        fitSelectWidth(e.target);
        const id = parseInt(e.target.value, 10);
        if (!isNaN(id)) {
            localStorage.setItem('selectedLocationId', id);
            selectLocation(id);
        }
    });

    setupNotesAutoSave();
    setupActions();

    // Load all data in parallel where possible
    await Promise.all([
        loadLocations(),
        loadBadges(),
        loadUserInfo(),
        loadStats()
    ]);
});
