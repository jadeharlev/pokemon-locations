const API_BASE_URL = "http://localhost:8080";

async function loadLocations() {
    const list = document.querySelector(".location-list");
    if (!list) return;

    try {
        const response = await fetch(`${API_BASE_URL}/locations`);
        if (!response.ok) throw new Error(`Status: ${response.status}`);

        const locations = await response.json();
        list.replaceChildren();

        locations.forEach(loc => {
            const a = document.createElement("a");
            a.href = `location.html?id=${loc.locationId}`;
            a.className = "location-card";
            a.textContent = loc.name;
            list.appendChild(a);
        });
    } catch (error) {
        list.replaceChildren();
        const p = document.createElement("p");
        p.className = "database-note";
        p.textContent = "Unable to load locations. Please try again later.";
        list.appendChild(p);
        console.error("Failed to load locations:", error.message);
    }
}

async function loadLocationDetail() {
    const titleEl = document.getElementById("location-name");
    const buildingList = document.querySelector(".building-list");
    if (!titleEl || !buildingList) return;

    const params = new URLSearchParams(window.location.search);
    const id = params.get("id");

    if (!id) {
        titleEl.textContent = "Location not found";
        buildingList.replaceChildren();
        return;
    }

    try {
        const locationRes = await fetch(`${API_BASE_URL}/locations/${id}`);
        if (!locationRes.ok) {
            titleEl.textContent = "Location not found";
            buildingList.replaceChildren();
            return;
        }

        const location = await locationRes.json();
        titleEl.textContent = location.name;
        document.title = `Pokemon Locations - ${location.name}`;

        const descEl = document.getElementById("location-description");
        if (descEl && location.description) {
            descEl.textContent = location.description;
        }

        const buildingsRes = await fetch(`${API_BASE_URL}/locations/${id}/buildings`);
        if (!buildingsRes.ok) throw new Error(`Status: ${buildingsRes.status}`);

        const buildings = await buildingsRes.json();
        buildingList.replaceChildren();

        buildings.forEach(b => {
            const a = document.createElement("a");
            a.href = `building.html?id=${b.buildingId}&locationId=${id}`;
            a.className = "building-card";
            a.textContent = b.name;
            buildingList.appendChild(a);
        });
    } catch (error) {
        buildingList.replaceChildren();
        const p = document.createElement("p");
        p.className = "database-note";
        p.textContent = "Unable to load buildings. Please try again later.";
        buildingList.appendChild(p);
        console.error("Failed to load location detail:", error.message);
    }
}

document.addEventListener("DOMContentLoaded", () => {
    loadLocations();
    loadLocationDetail();
});
