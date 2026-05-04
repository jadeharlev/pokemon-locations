-- Seed all 25 Kanto routes plus their canonical named buildings.
-- Routes 1-25 are the connecting paths between cities and dungeons in Pokémon Red.
-- Buildings on each route reflect Bulbapedia's "Places of interest" listings for Pokémon Red specifically.

-- ============================================================
-- Routes 1-25
-- ============================================================

INSERT INTO locations (name, description, video_url) VALUES
    ('Route 1',  'A short grass route connecting Pallet Town and Viridian City. Many trainers begin their journey here, encountering Pidgey and Rattata in the tall grass.', NULL),
    ('Route 2',  'A grass route between Viridian City and Pewter City that runs along the eastern edge of Viridian Forest. The east entrance to Diglett''s Cave can also be reached from this route.', NULL),
    ('Route 3',  'A winding mountain route east of Pewter City, leading toward Mt. Moon. Trainers and wild Pokémon line the path in equal measure.', NULL),
    ('Route 4',  'A short rocky route between Mt. Moon and Cerulean City. Sandshrew and Spearow are common encounters near the cliffside.', NULL),
    ('Route 5',  'A grass route running south from Cerulean City toward Saffron City. The Pokémon Day Care sits along the path.', NULL),
    ('Route 6',  'A grass route between Saffron City and Vermilion City, often patrolled by Bug Catchers.', NULL),
    ('Route 7',  'A short grass route between Saffron City and Celadon City.', NULL),
    ('Route 8',  'A grass route running east from Saffron City toward Lavender Town.', NULL),
    ('Route 9',  'A trainer-heavy route running east from Cerulean City to Rock Tunnel.', NULL),
    ('Route 10', 'A route running south from Rock Tunnel to Lavender Town. The detour to the Power Plant lies along its waterway.', NULL),
    ('Route 11', 'A grass route running east from Vermilion City. The west entrance to Diglett''s Cave is here.', NULL),
    ('Route 12', 'A long route running south from Lavender Town. A legendary Snorlax blocks the path until awakened with a Poké Flute. The Fishing Guru''s house is along the way.', NULL),
    ('Route 13', 'A maze-like route packed with trainers, continuing south toward Fuchsia City.', NULL),
    ('Route 14', 'A grass route between Routes 13 and 15, dotted with bird-keeper trainers.', NULL),
    ('Route 15', 'A grass route leading west into Fuchsia City.', NULL),
    ('Route 16', 'A route west of Celadon City. A second slumbering Snorlax blocks the path leading to Cycling Road.', NULL),
    ('Route 17', 'Cycling Road. A long downhill route accessible only by bicycle, populated by Bikers and Cue Ball trainers.', NULL),
    ('Route 18', 'A short route at the southern end of Cycling Road, leading into Fuchsia City.', NULL),
    ('Route 19', 'A sea route south of Fuchsia City, navigable only with Surf. Tentacool and Tentacruel are abundant.', NULL),
    ('Route 20', 'A sea route running between Fuchsia City and Cinnabar Island. Seafoam Islands lies along the way.', NULL),
    ('Route 21', 'A sea route running north from Cinnabar Island toward Pallet Town, dotted with swimmers and fishermen.', NULL),
    ('Route 22', 'A grass route west of Viridian City, used as the approach to Victory Road. The site of an early rival battle.', NULL),
    ('Route 23', 'The badge-checked approach to the Indigo Plateau. Eight gates verify the player has earned all eight badges before allowing passage.', NULL),
    ('Route 24', 'A grass route running north from Cerulean City. Nugget Bridge and a string of trainers stretch across it.', NULL),
    ('Route 25', 'A grass route east of Route 24, ending at Bill''s Sea Cottage.', NULL);

-- ============================================================
-- Canonical named buildings on routes (per Bulbapedia for Pokémon Red)
-- ============================================================

-- Route 2: Trade House
INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
    (SELECT location_id FROM locations WHERE name = 'Route 2'),
    'Trade House',
    'residential',
    'A small house on Route 2 where the resident offers an in-game trade of Mr. Mime for Abra.',
    NULL
);

-- Route 4: Mt. Moon Pokémon Center (one of only two route-side Centers in Kanto)
INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
    (SELECT location_id FROM locations WHERE name = 'Route 4'),
    'Mt. Moon Pokémon Center',
    'pokemon_center',
    'A standalone Pokémon Center at the western end of Route 4, just east of Mt. Moon. One of only two Centers in Kanto found outside a populated area, and home to the famous Magikarp salesman.',
    NULL
);

-- Route 5: Pokémon Day Care
INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
    (SELECT location_id FROM locations WHERE name = 'Route 5'),
    'Pokémon Day Care',
    'landmark',
    'A small house run by an elderly couple who will raise a Pokémon while the trainer adventures, gaining one experience point per step taken.',
    'The original Pokémon Day Care from the Kanto region.'
);

-- Route 10: Rock Tunnel Pokémon Center (the other route-side Center in Kanto)
INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
    (SELECT location_id FROM locations WHERE name = 'Route 10'),
    'Rock Tunnel Pokémon Center',
    'pokemon_center',
    'A standalone Pokémon Center on Route 10, just south of the Rock Tunnel entrance. One of only two Centers in Kanto found outside a populated area.',
    NULL
);

-- Route 11: Route 11 Gate
INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
    (SELECT location_id FROM locations WHERE name = 'Route 11'),
    'Route 11 Gate',
    'landmark',
    'A two-floor gate building separating Route 11 from the western entrance of Diglett''s Cave. The upper floor hosts an in-game trade and an NPC who awards the Itemfinder once the player has registered 30+ species in the Pokédex.',
    NULL
);

-- Route 12: Lavender Gate
INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
    (SELECT location_id FROM locations WHERE name = 'Route 12'),
    'Lavender Gate',
    'landmark',
    'A passage structure on Route 12 separating its northern portion from the long Silence Bridge that runs south toward Fuchsia City.',
    NULL
);

-- Route 12: Fishing Guru's House
INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
    (SELECT location_id FROM locations WHERE name = 'Route 12'),
    'Fishing Guru''s House',
    'residential',
    'A house south of the Route 11 exit where the younger of the Fishing Brothers offers the trainer the Super Rod.',
    NULL
);

-- Route 22: Pokémon League Reception Gate
INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
    (SELECT location_id FROM locations WHERE name = 'Route 22'),
    'Pokémon League Reception Gate',
    'landmark',
    'A checkpoint building between Route 22 and Route 23 that verifies a trainer has earned all eight Kanto badges before granting passage to the Pokémon League.',
    'The final gatekeeper between Kanto''s trainers and the Indigo Plateau.'
);

-- Route 25: Sea Cottage (Bill's home)
INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
    (SELECT location_id FROM locations WHERE name = 'Route 25'),
    'Sea Cottage',
    'residential',
    'The seaside home of Bill, the renowned Pokémon researcher who designed the PC storage system, located at the eastern end of Route 25.',
    NULL
);
