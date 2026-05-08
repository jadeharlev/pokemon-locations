// ─── API helper ───
const apiFetch = (path, options = {}) => PLAuth.authFetch(`/api${path}`, options);

// ─── State ───
let allLocations = [];
let selectedLocationId = null;
let currentBadges = new Set();
let noteDebounceTimer = null;
let galleryTimer = null;
let currentLocation = null; // { locationId, name, images, userImages, ... }

function rerenderGallery() {
    if (!currentLocation) return;
    const galleryEl = document.getElementById('image-gallery');
    const merged = [
        ...(currentLocation.images || []).map(img => ({ ...img, isUserImage: false })),
        ...(currentLocation.userImages || []).map(img => ({ ...img, isUserImage: true }))
    ];
    renderGallery(galleryEl, merged, currentLocation.name);
    updateUploadButtonState();
}

function updateUploadButtonState() {
    const btn = document.getElementById('gallery-upload');
    if (!btn || !currentLocation) return;
    const count = (currentLocation.userImages || []).length;
    btn.disabled = count >= 20;
    btn.title = btn.disabled ? '20-image limit reached' : 'Upload images';
}

function showToast(message, persistent = false) {
    let toast = document.getElementById('gallery-toast');
    if (toast) toast.remove();
    toast = document.createElement('div');
    toast.id = 'gallery-toast';
    toast.className = 'gallery-toast';
    const text = document.createElement('span');
    text.textContent = message;
    toast.appendChild(text);
    if (persistent) {
        const close = document.createElement('button');
        close.type = 'button';
        close.textContent = '×';
        close.addEventListener('click', () => toast.remove());
        toast.appendChild(close);
    } else {
        setTimeout(() => toast.remove(), 4000);
    }
    document.body.appendChild(toast);
}

// ─── Gallery carousel + modal ───
const GALLERY_INTERVAL_MS = 5000;
let resumeCarousel = null;

let activeBlobUrls = [];

async function loadUserImageBlob(imageUrl) {
    const res = await PLAuth.authFetch(imageUrl);
    if (!res.ok) throw new Error(`Image fetch failed: ${res.status}`);
    const blob = await res.blob();
    const blobUrl = URL.createObjectURL(blob);
    activeBlobUrls.push(blobUrl);
    return blobUrl;
}

function revokeAllBlobUrls() {
    activeBlobUrls.forEach(URL.revokeObjectURL);
    activeBlobUrls = [];
}

const ALLOWED_MIMES = ['image/png', 'image/jpeg', 'image/webp'];
const MAX_BYTES = 10 * 1024 * 1024;
const CAP_PER_LOCATION = 20;

async function uploadFiles(fileList) {
    if (!currentLocation) return;
    const files = Array.from(fileList);
    const rejected = [];
    const accepted = [];

    for (const f of files) {
        if (!ALLOWED_MIMES.includes(f.type)) {
            rejected.push({ name: f.name, reason: 'unsupported type' });
            continue;
        }
        if (f.size > MAX_BYTES) {
            rejected.push({ name: f.name, reason: 'too large' });
            continue;
        }
        accepted.push(f);
    }

    const remaining = CAP_PER_LOCATION - (currentLocation.userImages || []).length;
    const toUpload = accepted.slice(0, Math.max(0, remaining));
    const overflow = accepted.slice(Math.max(0, remaining));
    overflow.forEach(f => rejected.push({ name: f.name, reason: 'over cap' }));

    let succeeded = 0;
    const failed = [];
    for (const f of toUpload) {
        const fd = new FormData();
        fd.append('file', f, f.name);
        const url = `/api/me/locations/${currentLocation.locationId}/images`;
        const res = await PLAuth.authFetch(url, { method: 'POST', body: fd });
        if (res.ok) {
            const body = await res.json();
            currentLocation.userImages = [body, ...(currentLocation.userImages || [])];
            succeeded++;
        } else {
            let code = `${res.status}`;
            try { code = (await res.json()).error || code; } catch { /* not JSON */ }
            failed.push({ name: f.name, reason: code });
        }
    }

    rerenderGallery();

    const parts = [];
    if (succeeded) parts.push(`${succeeded} uploaded`);
    rejected.forEach(r => parts.push(`${r.name} skipped (${r.reason})`));
    failed.forEach(f => parts.push(`${f.name} failed (${f.reason})`));
    showToast(parts.join(' · '), failed.length > 0 || rejected.length > 0);
}

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
    revokeAllBlobUrls();
    // Remove only dynamic gallery contents (slides, arrows, placeholder text)
    // while preserving the static upload button, file input, and drag overlay.
    galleryEl.querySelectorAll('.gallery-slide, .gallery-arrow').forEach(el => el.remove());
    [...galleryEl.childNodes].forEach(n => {
        if (n.nodeType === Node.TEXT_NODE) galleryEl.removeChild(n);
    });

    if (images.length === 0) {
        // Add placeholder text without nuking the upload button
        const placeholder = document.createTextNode('Image Gallery');
        galleryEl.insertBefore(placeholder, galleryEl.firstChild);
        return;
    }

    const slides = images.map((img, i) => {
        const slide = document.createElement('div');
        slide.className = 'gallery-slide' + (i === 0 ? ' active' : '');

        const bg = document.createElement('img');
        bg.className = 'slide-bg';
        bg.alt = '';
        bg.setAttribute('aria-hidden', 'true');

        const fg = document.createElement('img');
        fg.className = 'slide-fg';
        fg.alt = img.caption || locationName || '';
        fg.addEventListener('click', () => openGalleryModal(fg.src, img.caption || locationName || ''));

        if (img.isUserImage) {
            loadUserImageBlob(img.imageUrl).then(blobUrl => {
                bg.src = blobUrl;
                fg.src = blobUrl;
            }).catch(err => console.error('User image fetch failed:', err));

            const del = document.createElement('button');
            del.type = 'button';
            del.className = 'slide-delete';
            del.setAttribute('aria-label', 'Delete image');
            del.textContent = '×';

            let confirmTimer = null;
            const resetButton = () => {
                del.classList.remove('confirming');
                del.textContent = '×';
                del.setAttribute('aria-label', 'Delete image');
                if (confirmTimer) { clearTimeout(confirmTimer); confirmTimer = null; }
            };

            del.addEventListener('click', async (e) => {
                e.stopPropagation();

                if (!del.classList.contains('confirming')) {
                    // First click → enter confirm state
                    del.classList.add('confirming');
                    del.textContent = 'Click again to delete';
                    del.setAttribute('aria-label', 'Click again to confirm delete');
                    confirmTimer = setTimeout(resetButton, 3000);
                    return;
                }

                // Second click → perform delete
                if (confirmTimer) { clearTimeout(confirmTimer); confirmTimer = null; }

                const res = await PLAuth.authFetch(img.imageUrl, { method: 'DELETE' });
                if (res.ok) {
                    const idx = currentLocation.userImages.findIndex(u => u.imageId === img.imageId);
                    if (idx >= 0) currentLocation.userImages.splice(idx, 1);
                    rerenderGallery();
                    showToast('Image deleted');
                } else {
                    showToast(`Delete failed (${res.status})`, true);
                    resetButton();
                }
            });

            slide.appendChild(del);
        } else {
            bg.src = img.imageUrl || img.url;
            fg.src = img.imageUrl || img.url;
        }

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
        currentLocation = location;
        descEl.textContent = location.description || '';

        // Image gallery
        const merged = [
            ...(location.images || []).map(img => ({ ...img, isUserImage: false })),
            ...(location.userImages || []).map(img => ({ ...img, isUserImage: true }))
        ];
        renderGallery(galleryEl, merged, location.name);
        updateUploadButtonState();

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

const HIDDEN_THEMES = ['shiny-eevee'];
const VALID_THEMES = [...THEMES, ...HIDDEN_THEMES];


const THEME_UNLOCK_RULES = {
    rattata: {
        label: 'Visit 1 location',
        isUnlocked: stats => stats.locationsVisited >= 1
    },
    diglett: {
        label: 'Visit 3 buildings',
        isUnlocked: stats => stats.buildingsVisited >= 3
    },
    geodude: {
        label: 'Visit 5 buildings',
        isUnlocked: stats => stats.buildingsVisited >= 5
    },
    dratini: {
        label: 'Visit 3 locations',
        isUnlocked: stats => stats.locationsVisited >= 3
    },
    dragonite: {
        label: 'Earn 3 badges',
        isUnlocked: stats => stats.gymsComplete >= 3
    },
    mew: {
        label: 'Earn 8 badges',
        isUnlocked: stats => stats.gymsComplete >= 8
    }
};

let currentStats = {
    gymsComplete: 0,
    locationsVisited: 0,
    buildingsVisited: 0
};


const UNLOCKED_THEMES_CACHE_KEY = 'pl.unlockedThemes';

function getUnlockedThemeSet() {
    return new Set(getUnlockedThemes());
}

function rememberUnlockedThemes(unlockedThemes) {
    sessionStorage.setItem(
        UNLOCKED_THEMES_CACHE_KEY,
        JSON.stringify([...unlockedThemes])
    );
}

function getRememberedUnlockedThemes() {
    const raw = sessionStorage.getItem(UNLOCKED_THEMES_CACHE_KEY);

    if (!raw) {
        return null;
    }

    try {
        return new Set(JSON.parse(raw));
    } catch {
        return null;
    }
}

function showUnlockMessage(theme) {
    const message = document.createElement('div');
    message.className = 'theme-unlock-message';
    message.textContent = `🎉 You unlocked ${formatThemeName(theme)}!`;

    document.body.appendChild(message);

    setTimeout(() => {
        message.remove();
    }, 3000);
}

function showConfetti() {
    const confetti = document.createElement('div');
    confetti.className = 'confetti-container';

    for (let i = 0; i < 40; i++) {
        const piece = document.createElement('span');
        piece.className = 'confetti-piece';
        piece.style.left = `${Math.random() * 100}%`;
        piece.style.animationDelay = `${Math.random() * 0.5}s`;
        piece.style.transform = `rotate(${Math.random() * 360}deg)`;
        confetti.appendChild(piece);
    }

    document.body.appendChild(confetti);

    setTimeout(() => {
        confetti.remove();
    }, 2500);
}

function checkForNewThemeUnlocks() {
    const currentlyUnlocked = getUnlockedThemeSet();
    const previouslyUnlocked = getRememberedUnlockedThemes();

    // First time loading the page: remember current unlocks but do not show confetti.
    if (!previouslyUnlocked) {
        rememberUnlockedThemes(currentlyUnlocked);
        return;
    }

    const newlyUnlocked = [...currentlyUnlocked].filter(theme => !previouslyUnlocked.has(theme));

    if (newlyUnlocked.length > 0) {
        newlyUnlocked.forEach(theme => {
            showUnlockMessage(theme);
            showConfetti();
        });
    }

    rememberUnlockedThemes(currentlyUnlocked);
}




function applyTheme(name) {
    if (!VALID_THEMES.includes(name)) name = 'bulbasaur';
    document.documentElement.setAttribute('data-theme', name);
    const link = document.getElementById('theme-stylesheet');
    if (link) link.href = `/css/themes/${name}.css`;
    sessionStorage.setItem(THEME_CACHE_KEY, name);
}


function pickRandomTheme() {
    const currentTheme = document.documentElement.getAttribute('data-theme') || 'bulbasaur';

    const choices = getUnlockedThemes()
        .filter(theme => !HIDDEN_THEMES.includes(theme))
        .filter(theme => theme !== currentTheme);

    if (choices.length === 0) {
        return currentTheme;
    }

    const randomIndex = Math.floor(Math.random() * choices.length);
    return choices[randomIndex];
}


const THEME_DISPLAY_NAMES = {
    bulbasaur: 'Bulbasaur',
    charmander: 'Charmander',
    squirtle: 'Squirtle',
    pikachu: 'Pikachu',
    rattata: 'Rattata',
    diglett: 'Diglett',
    geodude: 'Geodude',
    dratini: 'Dratini',
    mew: 'Mew',
    dragonite: 'Dragonite',
    'shiny-eevee': 'Shiny Eevee'
};

const formatThemeName = (theme) => THEME_DISPLAY_NAMES[theme] || '';

function updateDisplayedThemeName(theme) {
    const themeLabel = document.querySelector('#user-info p:nth-child(2) strong');

    if (themeLabel) {
        themeLabel.textContent = formatThemeName(theme);
    }
}

function isThemeUnlocked(theme) {
    const rule = THEME_UNLOCK_RULES[theme];

    if (!rule) {
        return true;
    }

    return rule.isUnlocked(currentStats);
}

function getUnlockedThemes() {
    return THEMES.filter(theme => isThemeUnlocked(theme));
}

function updateThemeButtons() {
    document.querySelectorAll('.theme-option').forEach(btn => {
        const theme = btn.dataset.theme;

        if (theme === 'random') {
            btn.disabled = false;
            btn.classList.remove('theme-locked');
            btn.removeAttribute('aria-disabled');
            btn.title = 'Randomly choose from your unlocked themes';
            return;
        }

        const unlocked = isThemeUnlocked(theme);
        const rule = THEME_UNLOCK_RULES[theme];

        // Do not disable the button because disabled buttons do not hover well.
        // Instead, visually lock it and block selection in the click handler.
        btn.disabled = false;
        btn.classList.toggle('theme-locked', !unlocked);
        btn.setAttribute('aria-disabled', String(!unlocked));

        if (!unlocked && rule) {
            btn.textContent = formatThemeName(theme);
            btn.title = `Unlock condition: ${rule.label}`;
        } else {
            btn.textContent = btn.dataset.label || formatThemeName(theme);
            btn.title = '';
        }
    });
}






// Apply cached theme synchronously to avoid a flash
applyTheme(sessionStorage.getItem(THEME_CACHE_KEY) || 'bulbasaur');





const KONAMI_CODES = [
    ['ArrowUp', 'ArrowUp', 'ArrowDown', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'ArrowLeft', 'ArrowRight', 'b', 'a'],
    ['ArrowUp', 'ArrowDown', 'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'ArrowLeft', 'ArrowRight', 'a', 'b', 'Enter']
];

let konamiBuffer = [];

function normalizeKonamiKey(event) {
    if (event.key.length === 1) {
        return event.key.toLowerCase();
    }

    return event.key;
}

function matchesKonamiCode(buffer, code) {
    if (buffer.length < code.length) {
        return false;
    }

    const recentKeys = buffer.slice(-code.length);
    return code.every((key, index) => recentKeys[index] === key);
}

function showShinyEeveeMessage() {
    const message = document.createElement('div');
    message.className = 'theme-unlock-message';
    message.textContent = '✨ Shiny Eevee theme activated!';

    document.body.appendChild(message);

    setTimeout(() => {
        message.remove();
    }, 3000);
}


async function activateShinyEeveeTheme() {
    const theme = 'shiny-eevee';  
            
    applyTheme(theme);
                
    try {
        const res = await PLAuth.authFetch('/account/theme', {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ theme })
        });

        if (!res.ok) {
            alert('Shiny Eevee activated, but it could not be saved.');
            return;
        }

        updateDisplayedThemeName(theme);
        showShinyEeveeMessage();

        if (typeof showConfetti === 'function') {
            showConfetti();
        }
    } catch (e) {
        console.error('Failed to save Shiny Eevee theme:', e.message);
        alert('Shiny Eevee activated, but it could not be saved.');
    }
}

function setupKonamiCodeListener() {
    document.addEventListener('keydown', async (event) => {
        const key = normalizeKonamiKey(event);

        konamiBuffer.push(key);

        const maxLength = Math.max(...KONAMI_CODES.map(code => code.length));
        if (konamiBuffer.length > maxLength) {
            konamiBuffer.shift();
        }

        const matched = KONAMI_CODES.some(code => matchesKonamiCode(konamiBuffer, code));

        if (matched) {
            konamiBuffer = [];
            await activateShinyEeveeTheme();
        }
    });
}




// ─── User info ───
async function loadUserInfo() {
    const container = document.getElementById('user-info');

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
}

// ─── Stats ───
async function loadStats() {
    try {
        const res = await apiFetch('/me/stats');
        if (!res.ok) return;
        const stats = await res.json();

        currentStats = stats;

        document.getElementById('stat-gyms').textContent = stats.gymsComplete;
        document.getElementById('stat-locations').textContent = stats.locationsVisited;
        document.getElementById('stat-buildings').textContent = stats.buildingsVisited;

        updateThemeButtons();
        checkForNewThemeUnlocks();
    } catch (e) {
        console.error('Failed to load stats:', e.message);
    }
}



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

// ─── Action buttons ───
function setupActions() {
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
    });

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

                updateDisplayedThemeName(theme);
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

    setupKonamiCodeListener();
    setupNotesAutoSave();
    setupActions();

    const uploadBtn = document.getElementById('gallery-upload');
    const fileInput = document.getElementById('gallery-file-input');
    if (uploadBtn && fileInput) {
        uploadBtn.addEventListener('click', () => fileInput.click());
        fileInput.addEventListener('change', async (e) => {
            if (e.target.files.length > 0) {
                await uploadFiles(e.target.files);
                e.target.value = '';
            }
        });
    }

    const galleryEl = document.getElementById('image-gallery');
    let dragLeaveTimeout = null;

    if (galleryEl) {
        galleryEl.addEventListener('dragenter', (e) => {
            e.preventDefault();
            galleryEl.classList.add('drag-active');
            if (dragLeaveTimeout) { clearTimeout(dragLeaveTimeout); dragLeaveTimeout = null; }
        });
        galleryEl.addEventListener('dragover', (e) => e.preventDefault());
        galleryEl.addEventListener('dragleave', () => {
            if (dragLeaveTimeout) clearTimeout(dragLeaveTimeout);
            dragLeaveTimeout = setTimeout(() => galleryEl.classList.remove('drag-active'), 60);
        });
        galleryEl.addEventListener('drop', async (e) => {
            e.preventDefault();
            galleryEl.classList.remove('drag-active');
            if (e.dataTransfer.files.length > 0) {
                await uploadFiles(e.dataTransfer.files);
            }
        });
    }

    startWeatherTicker();

    // Load all data in parallel where possible
    await Promise.all([
        loadLocations(),
        loadBadges(),
        loadUserInfo(),
        loadStats()
    ]);
});
