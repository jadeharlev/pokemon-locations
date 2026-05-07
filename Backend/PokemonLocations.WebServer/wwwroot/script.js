// ─── API helper ───
const apiFetch = (path, options = {}) => PLAuth.authFetch(`/api${path}`, options);

// ─── State ───
let allLocations = [];
let selectedLocationId = null;
let currentBadges = new Set();
let noteDebounceTimer = null;
let galleryTimer = null;

// ─── Gallery carousel + modal ───
const GALLERY_INTERVAL_MS = 5000;
let resumeCarousel = null;

function openGalleryModal(src, caption) {
    const modal = document.getElementById('gallery-modal');
    const img = document.getElementById('modal-image');
    const captionEl = document.getElementById('modal-caption');
    if (!modal || !img) return;

    if (galleryTimer) {
        clearInterval(galleryTimer);
        galleryTimer = null;
    }

    img.src = src;
    img.alt = caption || '';
    if (caption) {
        captionEl.textContent = caption;
        captionEl.style.display = 'block';
    } else {
        captionEl.style.display = 'none';
    }
    modal.classList.add('open');
    document.body.style.overflow = 'hidden';
}

function closeGalleryModal() {
    const modal = document.getElementById('gallery-modal');
    const img = document.getElementById('modal-image');
    if (!modal) return;
    modal.classList.remove('open');
    if (img) img.src = '';
    document.body.style.overflow = '';
    if (resumeCarousel) resumeCarousel();
}

function setupGalleryModal() {
    const modal = document.getElementById('gallery-modal');
    const closeBtn = document.getElementById('modal-close');
    if (!modal || !closeBtn) return;

    closeBtn.addEventListener('click', closeGalleryModal);
    modal.addEventListener('click', (e) => {
        if (e.target === modal) closeGalleryModal();
    });
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && modal.classList.contains('open')) closeGalleryModal();
    });
}

function renderGallery(galleryEl, images, locationName) {
    if (galleryTimer) {
        clearInterval(galleryTimer);
        galleryTimer = null;
    }
    resumeCarousel = null;
    galleryEl.replaceChildren();

    if (images.length === 0) {
        galleryEl.textContent = 'Image Gallery';
        return;
    }

    const slides = images.map((img, i) => {
        const slide = document.createElement('div');
        slide.className = 'gallery-slide' + (i === 0 ? ' active' : '');

        const bg = document.createElement('img');
        bg.className = 'slide-bg';
        bg.src = img.imageUrl || img.url;
        bg.alt = '';
        bg.setAttribute('aria-hidden', 'true');

        const fg = document.createElement('img');
        fg.className = 'slide-fg';
        fg.src = img.imageUrl || img.url;
        fg.alt = img.caption || locationName || '';
        fg.addEventListener('click', () => openGalleryModal(fg.src, img.caption || locationName || ''));

        slide.append(bg, fg);
        galleryEl.appendChild(slide);
        return slide;
    });

    if (slides.length === 1) return;

    let active = 0;
    const goTo = (target) => {
        const next = ((target % slides.length) + slides.length) % slides.length;
        if (next === active) return;
        slides[active].classList.remove('active');
        slides[next].classList.add('active');
        active = next;
    };
    const startTimer = () => {
        if (galleryTimer) clearInterval(galleryTimer);
        galleryTimer = setInterval(() => goTo(active + 1), GALLERY_INTERVAL_MS);
    };
    resumeCarousel = startTimer;

    const prevBtn = document.createElement('button');
    prevBtn.type = 'button';
    prevBtn.className = 'gallery-arrow prev';
    prevBtn.setAttribute('aria-label', 'Previous image');
    prevBtn.textContent = '‹';
    prevBtn.addEventListener('click', (e) => {
        e.stopPropagation();
        goTo(active - 1);
        startTimer();
    });
    galleryEl.appendChild(prevBtn);

    const nextBtn = document.createElement('button');
    nextBtn.type = 'button';
    nextBtn.className = 'gallery-arrow next';
    nextBtn.setAttribute('aria-label', 'Next image');
    nextBtn.textContent = '›';
    nextBtn.addEventListener('click', (e) => {
        e.stopPropagation();
        goTo(active + 1);
        startTimer();
    });
    galleryEl.appendChild(nextBtn);

    startTimer();
}

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

async function loadLocations() {
    const locationDropdownButton = document.getElementById('location-dropdown-button');
    const locationDropdownMenu = document.getElementById('location-dropdown-menu');

    try {
        const res = await apiFetch('/locations');
        if (!res.ok) throw new Error(`Status ${res.status}`);
        allLocations = await res.json();

        locationDropdownMenu.replaceChildren();

        allLocations.forEach(loc => {
            const li = document.createElement('li');

            const button = document.createElement('button');
            button.className = 'dropdown-item';
            button.type = 'button';
            button.textContent = loc.name;
            button.dataset.locationId = loc.locationId;

            button.addEventListener('click', async () => {
                await selectLocation(loc.locationId);
            });

            li.appendChild(button);
            locationDropdownMenu.appendChild(li);
        });

        if (allLocations.length > 0) {
            const savedLocationId = localStorage.getItem('selectedLocationId');

            let selectedLocationId;
            if (savedLocationId && allLocations.some(l => l.locationId == savedLocationId)) {
                selectedLocationId = parseInt(savedLocationId, 10);
            } else {
                selectedLocationId = allLocations[0].locationId;
            }

            await selectLocation(selectedLocationId);
        }
    } catch (e) {
        if (locationDropdownButton) {
            locationDropdownButton.textContent = 'Unable to load locations';
        }

        if (locationDropdownMenu) {
            locationDropdownMenu.replaceChildren();
        }

        console.error('Failed to load locations:', e.message);
    }
}

async function selectLocation(locationId) {
    selectedLocationId = locationId;
    localStorage.setItem('selectedLocationId', locationId);

    const locationDropdownButton = document.getElementById('location-dropdown-button');
    const selectedLocation = allLocations.find(l => l.locationId == locationId);

    if (locationDropdownButton && selectedLocation) {
        locationDropdownButton.textContent = selectedLocation.name;
    }

    document.querySelectorAll('#location-dropdown-menu .dropdown-item').forEach(item => {
        item.classList.toggle('active', item.dataset.locationId == locationId);
    });

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
        renderGallery(galleryEl, images, location.name);

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
const THEMES = [
  'bulbasaur',
  'charmander',
  'squirtle',
  'pikachu',
  'rattata',
  'diglett',
  'geodude',
  'dratini',
  'mew',
  'dragonite'
];
const THEME_CACHE_KEY = 'pl.theme';

function applyTheme(name) {
    if (!THEMES.includes(name)) name = 'bulbasaur';
    document.documentElement.setAttribute('data-theme', name);
    const link = document.getElementById('theme-stylesheet');
    if (link) link.href = `/css/themes/${name}.css`;
    sessionStorage.setItem(THEME_CACHE_KEY, name);
}

function pickRandomTheme() {
    const currentTheme = document.documentElement.getAttribute('data-theme') || 'bulbasaur';
    const choices = THEMES.filter(theme => theme !== currentTheme);

    if (choices.length === 0) {
        return currentTheme;
    }

    const randomIndex = Math.floor(Math.random() * choices.length);
    return choices[randomIndex];
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
        setHomePlanet(user.permanentPlanetName ?? null);

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

    const locationModalEl = document.getElementById('location-modal');
    const locationModal = new bootstrap.Modal(locationModalEl);
    const openLocationModal = () => locationModal.show();
    document.getElementById('btn-set-location').addEventListener('click', openLocationModal);
    document.getElementById('current-location').addEventListener('click', openLocationModal);

    document.getElementById('location-options').addEventListener('click', async (event) => {
        const btn = event.target.closest('.location-option');
        if (!btn) return;
        const planetName = btn.dataset.planet;
        const previousName = weatherState.homePlanetName;
        const previousTemp = weatherState.homePlanetTemp;

        setHomePlanet(planetName);

        try {
            const res = await PLAuth.authFetch('/account/permanent-planet', {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ planetName })
            });
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
    });

    document.querySelectorAll('.theme-option').forEach(btn => {
        btn.addEventListener('click', async () => {
            const selectedTheme = btn.dataset.theme;
            const theme = selectedTheme === 'random' ? pickRandomTheme() : selectedTheme;

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

// ─── Weather ticker + home planet indicator ───
const TICKER_FRAME_MS = 4000;

const weatherState = {
    planets: [],
    homePlanetName: null,
    homePlanetTemp: null
};

function randomInt(min, max) {
    const lo = Math.ceil(min);
    const hi = Math.floor(max);
    return Math.floor(Math.random() * (hi - lo + 1)) + lo;
}

function findPlanet(name) {
    if (!name) return null;
    const lower = name.toLowerCase();
    return weatherState.planets.find(p => p.name.toLowerCase() === lower) ?? null;
}

function renderCurrentLocation() {
    const el = document.getElementById('current-location');
    if (!el) return;
    const name = weatherState.homePlanetName;
    if (!name) {
        el.textContent = 'Current: not set';
        return;
    }
    const temp = weatherState.homePlanetTemp;
    el.textContent = temp == null
        ? `Current: ${name}`
        : `Current: ${temp}° (${name})`;
}

function setHomePlanet(name) {
    weatherState.homePlanetName = name;
    const planet = findPlanet(name);
    weatherState.homePlanetTemp = planet
        ? randomInt(planet.minTemp, planet.maxTemp)
        : null;
    renderCurrentLocation();
}

function populateLocationModal() {
    const container = document.getElementById('location-options');
    if (!container) return;
    if (weatherState.planets.length === 0) {
        container.replaceChildren();
        const empty = document.createElement('p');
        empty.className = 'loading-text';
        empty.textContent = 'No planets available.';
        container.appendChild(empty);
        return;
    }
    container.replaceChildren();
    weatherState.planets.forEach(planet => {
        const btn = document.createElement('button');
        btn.className = 'btn btn-outline-secondary location-option';
        btn.dataset.planet = planet.name;
        btn.textContent = planet.name;
        container.appendChild(btn);
    });
}

async function startWeatherTicker() {
    const host = document.getElementById('weather-ticker');
    if (!host) return;

    let planets;
    try {
        const res = await apiFetch('/planets');
        if (!res.ok) return;
        planets = await res.json();
    } catch {
        return;
    }
    if (!Array.isArray(planets) || planets.length === 0) return;

    weatherState.planets = planets;
    populateLocationModal();
    if (weatherState.homePlanetName && weatherState.homePlanetTemp == null) {
        setHomePlanet(weatherState.homePlanetName);
    }

    let index = 0;
    const showNext = () => {
        if (document.hidden) return;
        const planet = planets[index % planets.length];
        index += 1;

        const temp = randomInt(planet.minTemp, planet.maxTemp);
        const item = document.createElement('div');
        item.className = 'ticker-item';
        item.textContent = `${planet.name}: ${temp}° C`;
        host.replaceChildren(item);

        if (weatherState.homePlanetName &&
            planet.name.toLowerCase() === weatherState.homePlanetName.toLowerCase()) {
            weatherState.homePlanetTemp = temp;
            renderCurrentLocation();
        }
    };

    showNext();
    setInterval(showNext, TICKER_FRAME_MS);
}

// ─── Bootstrap ───
document.addEventListener('DOMContentLoaded', async () => {
    PLAuth.requireAuth();
    if (!PLAuth.getCreds()) return;

    setupGalleryModal();

    // Set up event listeners

    setupNotesAutoSave();
    setupActions();

    startWeatherTicker();

    // Load all data in parallel where possible
    await Promise.all([
        loadLocations(),
        loadBadges(),
        loadUserInfo(),
        loadStats()
    ]);
});
