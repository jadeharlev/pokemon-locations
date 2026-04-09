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

const BUILDING_TYPE_LABELS = {
    0: "Gym",
    1: "Pokemon Center",
    2: "Poke Mart",
    3: "Residential",
    4: "Landmark",
    5: "Lab"
};

async function loadBuildingDetail() {
    const titleEl = document.getElementById("building-name");
    const detailsList = document.getElementById("building-details");
    if (!titleEl || !detailsList) return;

    const params = new URLSearchParams(window.location.search);
    const buildingId = params.get("id");
    const locationId = params.get("locationId");

    if (!buildingId || !locationId) {
        titleEl.textContent = "Building not found";
        return;
    }

    const backLink = document.getElementById("back-link");
    if (backLink) {
        backLink.href = `location.html?id=${locationId}`;
    }

    try {
        const res = await fetch(`${API_BASE_URL}/locations/${locationId}/buildings/${buildingId}`);
        if (!res.ok) {
            titleEl.textContent = "Building not found";
            return;
        }

        const building = await res.json();
        titleEl.textContent = building.name;
        document.title = `Pokemon Locations - ${building.name}`;

        detailsList.replaceChildren();

        const addDetail = (label, value) => {
            const li = document.createElement("li");
            const span = document.createElement("span");
            span.className = "detail-label";
            span.textContent = label + ": ";
            li.appendChild(span);
            li.appendChild(document.createTextNode(value));
            detailsList.appendChild(li);
        };

        const typeLabel = BUILDING_TYPE_LABELS[building.buildingType] || "Unknown";
        addDetail("Type", typeLabel);

        if (building.description) {
            addDetail("Description", building.description);
        }

        if (building.landmarkDescription) {
            addDetail("Landmark", building.landmarkDescription);
        }

        if (building.gym) {
            addDetail("Gym Type", building.gym.gymType);
            addDetail("Badge", building.gym.badgeName);
            addDetail("Gym Leader", building.gym.gymLeader);
            addDetail("Gym Order", building.gym.gymOrder);
        }
    } catch (error) {
        detailsList.replaceChildren();
        const p = document.createElement("p");
        p.className = "database-note";
        p.textContent = "Unable to load building details. Please try again later.";
        detailsList.appendChild(p);
        console.error("Failed to load building detail:", error.message);
    }
}

document.addEventListener("DOMContentLoaded", () => {
    loadLocations();
    loadLocationDetail();
    loadBuildingDetail();
});
