-- Seed one or more canonical screenshots for every Kanto location, replacing
-- the placeholder rows from 0010. Each location gets at least one image
-- (display_order = 1), which acts as the gallery cover. Multi-floor dungeons
-- and split routes get additional rows ordered 1F → upper → basement.

-- Wipe placeholder rows seeded in 0010 so we start from a clean slate.
DELETE FROM location_images;

-- Drop the Underground Path locations: no canonical screenshots are available,
-- and they were only minor connectors beneath Saffron City.
DELETE FROM locations
WHERE name IN ('Underground Path (Routes 5-6)', 'Underground Path (Routes 7-8)');

-- ============================================================
-- Cities (10)
-- ============================================================
INSERT INTO location_images (location_id, image_url, display_order, caption) VALUES
    ((SELECT location_id FROM locations WHERE name = 'Pallet Town'),     '/images/pallet-town.png',     1, 'Pallet Town'),
    ((SELECT location_id FROM locations WHERE name = 'Viridian City'),   '/images/viridian-city.png',   1, 'Viridian City'),
    ((SELECT location_id FROM locations WHERE name = 'Pewter City'),     '/images/pewter-city.png',     1, 'Pewter City'),
    ((SELECT location_id FROM locations WHERE name = 'Cerulean City'),   '/images/cerulean-city.png',   1, 'Cerulean City'),
    ((SELECT location_id FROM locations WHERE name = 'Lavender Town'),   '/images/lavender-town.png',   1, 'Lavender Town'),
    ((SELECT location_id FROM locations WHERE name = 'Vermilion City'),  '/images/vermilion-city.png',  1, 'Vermilion City'),
    ((SELECT location_id FROM locations WHERE name = 'Celadon City'),    '/images/celadon-city.png',    1, 'Celadon City'),
    ((SELECT location_id FROM locations WHERE name = 'Fuchsia City'),    '/images/fuchsia-city.png',    1, 'Fuchsia City'),
    ((SELECT location_id FROM locations WHERE name = 'Saffron City'),    '/images/saffron-city.png',    1, 'Saffron City'),
    ((SELECT location_id FROM locations WHERE name = 'Cinnabar Island'), '/images/cinnabar-island.png', 1, 'Cinnabar Island');

-- ============================================================
-- Routes (25 locations, 26 images: Route 2 has east + west)
-- ============================================================
INSERT INTO location_images (location_id, image_url, display_order, caption) VALUES
    ((SELECT location_id FROM locations WHERE name = 'Route 1'),  '/images/route-1.png',        1, 'Route 1'),
    ((SELECT location_id FROM locations WHERE name = 'Route 2'),  '/images/route-2.png',        1, 'Route 2'),
    ((SELECT location_id FROM locations WHERE name = 'Route 3'),  '/images/route-3.png',        1, 'Route 3'),
    ((SELECT location_id FROM locations WHERE name = 'Route 4'),  '/images/route-4.png',        1, 'Route 4'),
    ((SELECT location_id FROM locations WHERE name = 'Route 5'),  '/images/route-5.png',        1, 'Route 5'),
    ((SELECT location_id FROM locations WHERE name = 'Route 6'),  '/images/route-6.png',        1, 'Route 6'),
    ((SELECT location_id FROM locations WHERE name = 'Route 7'),  '/images/route-7.png',        1, 'Route 7'),
    ((SELECT location_id FROM locations WHERE name = 'Route 8'),  '/images/route-8.png',        1, 'Route 8'),
    ((SELECT location_id FROM locations WHERE name = 'Route 9'),  '/images/route-9.png',        1, 'Route 9'),
    ((SELECT location_id FROM locations WHERE name = 'Route 10'), '/images/route-10-north.png', 1, 'Route 10 (North)'),
    ((SELECT location_id FROM locations WHERE name = 'Route 11'), '/images/route-11.png',       1, 'Route 11'),
    ((SELECT location_id FROM locations WHERE name = 'Route 12'), '/images/route-12.png',       1, 'Route 12'),
    ((SELECT location_id FROM locations WHERE name = 'Route 13'), '/images/route-13.png',       1, 'Route 13'),
    ((SELECT location_id FROM locations WHERE name = 'Route 14'), '/images/route-14.png',       1, 'Route 14'),
    ((SELECT location_id FROM locations WHERE name = 'Route 15'), '/images/route-15.png',       1, 'Route 15'),
    ((SELECT location_id FROM locations WHERE name = 'Route 16'), '/images/route-16.png',       1, 'Route 16'),
    ((SELECT location_id FROM locations WHERE name = 'Route 17'), '/images/route-17.png',       1, 'Route 17'),
    ((SELECT location_id FROM locations WHERE name = 'Route 18'), '/images/route-18.png',       1, 'Route 18'),
    ((SELECT location_id FROM locations WHERE name = 'Route 19'), '/images/route-19.png',       1, 'Route 19'),
    ((SELECT location_id FROM locations WHERE name = 'Route 20'), '/images/route-20.png',       1, 'Route 20'),
    ((SELECT location_id FROM locations WHERE name = 'Route 21'), '/images/route-21.png',       1, 'Route 21'),
    ((SELECT location_id FROM locations WHERE name = 'Route 22'), '/images/route-22.png',       1, 'Route 22'),
    ((SELECT location_id FROM locations WHERE name = 'Route 23'), '/images/route-23.png',       1, 'Route 23'),
    ((SELECT location_id FROM locations WHERE name = 'Route 24'), '/images/route-24.png',       1, 'Route 24'),
    ((SELECT location_id FROM locations WHERE name = 'Route 25'), '/images/route-25.png',       1, 'Route 25');

-- ============================================================
-- Dungeons / special (9 locations, 20 images)
-- Floor ordering: 1F first, then upper floors (2F, 3F), then basements (B1F, B2F, B3F, B4F).
-- ============================================================
INSERT INTO location_images (location_id, image_url, display_order, caption) VALUES
    ((SELECT location_id FROM locations WHERE name = 'Viridian Forest'),    '/images/viridian-forest.png',     1, 'Viridian Forest'),

    ((SELECT location_id FROM locations WHERE name = 'Mt. Moon'),           '/images/mt-moon-1f.png',          1, 'Mt. Moon (1F)'),
    ((SELECT location_id FROM locations WHERE name = 'Mt. Moon'),           '/images/mt-moon-b1f.png',         2, 'Mt. Moon (B1F)'),
    ((SELECT location_id FROM locations WHERE name = 'Mt. Moon'),           '/images/mt-moon-b2f.png',         3, 'Mt. Moon (B2F)'),

    ((SELECT location_id FROM locations WHERE name = 'Diglett''s Cave'),    '/images/digletts-cave.png',       1, 'Diglett''s Cave'),

    ((SELECT location_id FROM locations WHERE name = 'Rock Tunnel'),        '/images/rock-tunnel-1f.png',      1, 'Rock Tunnel (1F)'),
    ((SELECT location_id FROM locations WHERE name = 'Rock Tunnel'),        '/images/rock-tunnel-b1f.png',     2, 'Rock Tunnel (B1F)'),

    ((SELECT location_id FROM locations WHERE name = 'Power Plant'),        '/images/power-plant.png',         1, 'Power Plant'),

    ((SELECT location_id FROM locations WHERE name = 'Cerulean Cave'),      '/images/cerulean-cave-1f.png',    1, 'Cerulean Cave (1F)'),
    ((SELECT location_id FROM locations WHERE name = 'Cerulean Cave'),      '/images/cerulean-cave-2f.png',    2, 'Cerulean Cave (2F)'),
    ((SELECT location_id FROM locations WHERE name = 'Cerulean Cave'),      '/images/cerulean-cave-b1f.png',   3, 'Cerulean Cave (B1F)'),

    ((SELECT location_id FROM locations WHERE name = 'Seafoam Islands'),    '/images/seafoam-islands-1f.png',  1, 'Seafoam Islands (1F)'),
    ((SELECT location_id FROM locations WHERE name = 'Seafoam Islands'),    '/images/seafoam-islands-b1f.png', 2, 'Seafoam Islands (B1F)'),
    ((SELECT location_id FROM locations WHERE name = 'Seafoam Islands'),    '/images/seafoam-islands-b2f.png', 3, 'Seafoam Islands (B2F)'),
    ((SELECT location_id FROM locations WHERE name = 'Seafoam Islands'),    '/images/seafoam-islands-b3f.png', 4, 'Seafoam Islands (B3F)'),
    ((SELECT location_id FROM locations WHERE name = 'Seafoam Islands'),    '/images/seafoam-islands-b4f.png', 5, 'Seafoam Islands (B4F)'),

    ((SELECT location_id FROM locations WHERE name = 'Victory Road'),       '/images/victory-road-1f.png',     1, 'Victory Road (1F)'),
    ((SELECT location_id FROM locations WHERE name = 'Victory Road'),       '/images/victory-road-2f.png',     2, 'Victory Road (2F)'),
    ((SELECT location_id FROM locations WHERE name = 'Victory Road'),       '/images/victory-road-3f.png',     3, 'Victory Road (3F)'),

    ((SELECT location_id FROM locations WHERE name = 'Indigo Plateau'),     '/images/indigo-plateau.png',      1, 'Indigo Plateau');
