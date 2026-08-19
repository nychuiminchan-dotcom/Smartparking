// Leaflet map interop module for Smart Parking Navigator
let map = null;
let markersLayer = null;
let selectedMarker = null;

export function initMap(dotNetRef, containerId) {
    if (map) {
        map.remove();
        map = null;
    }

    map = L.map(containerId, { zoomControl: true }).setView([1.3521, 103.8198], 13);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
        maxZoom: 19
    }).addTo(map);

    markersLayer = L.layerGroup().addTo(map);

    map.on('click', () => {
        dotNetRef.invokeMethodAsync('OnMapClick');
    });

    return true;
}

export function setView(lat, lng, zoom) {
    if (map) map.setView([lat, lng], zoom ?? 15);
}

export function updateMarkers(dotNetRef, carparks) {
    if (!map || !markersLayer) return;
    markersLayer.clearLayers();

    for (const cp of carparks) {
        const available = cp.lots?.filter(l => l.lotType === 'C').reduce((s, l) => s + l.lotsAvailable, 0) ?? 0;
        const total = cp.lots?.filter(l => l.lotType === 'C').reduce((s, l) => s + l.totalLots, 0) ?? 0;
        const pct = total > 0 ? Math.round((1 - available / total) * 100) : null;
        const color = cp.availabilityStatus === 0 && total > 0
            ? (pct < 50 ? '#22c55e' : pct < 85 ? '#f59e0b' : '#ef4444')
            : '#6b7280';

        const icon = L.divIcon({
            html: `<div style="background:${color};color:#fff;border-radius:50%;width:32px;height:32px;display:flex;align-items:center;justify-content:center;font-size:11px;font-weight:700;border:2px solid #fff;box-shadow:0 2px 4px rgba(0,0,0,.4)">${total > 0 ? available : '?'}</div>`,
            className: '',
            iconSize: [32, 32],
            iconAnchor: [16, 16]
        });

        const marker = L.marker([cp.latitude, cp.longitude], { icon })
            .addTo(markersLayer);

        marker.on('click', (e) => {
            L.DomEvent.stopPropagation(e);
            dotNetRef.invokeMethodAsync('OnMarkerClick', cp.id);
        });
    }
}

export function clearMarkers() {
    if (markersLayer) markersLayer.clearLayers();
}

export function fitBounds(carparks) {
    if (!map || carparks.length === 0) return;
    const latlngs = carparks.map(cp => [cp.latitude, cp.longitude]);
    map.fitBounds(latlngs, { padding: [40, 40], maxZoom: 16 });
}

export function highlightMarker(lat, lng) {
    if (map) map.setView([lat, lng], map.getZoom());
}

export function destroyMap() {
    if (map) {
        map.remove();
        map = null;
        markersLayer = null;
    }
}
