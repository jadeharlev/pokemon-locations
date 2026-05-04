-- Seed all 25 Kanto routes plus their canonical named buildings.
-- Routes 1-25 are the connecting paths between cities and dungeons in Pokémon Red.

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
-- Canonical named buildings on routes
-- ============================================================

-- Route 5: Pokémon Day Care
INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
    (SELECT location_id FROM locations WHERE name = 'Route 5'),
    'Pokémon Day Care',
    'landmark',
    'A small house run by an elderly couple who will raise a Pokémon while the trainer adventures.',
    'The original Pokémon Day Care from the Kanto region.'
);

-- Route 12: Fishing Guru's House
INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
    (SELECT location_id FROM locations WHERE name = 'Route 12'),
    'Fishing Guru''s House',
    'residential',
    'Home of a fishing enthusiast who gives the player the Super Rod.',
    NULL
);

-- Route 17: Cycling Road Rest House
INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
    (SELECT location_id FROM locations WHERE name = 'Route 17'),
    'Cycling Road Rest House',
    'landmark',
    'A small rest stop along Cycling Road where bikers can take a break.',
    'A welcome reprieve in the middle of Kanto''s longest cycling route.'
);

-- Route 25: Bill's Sea Cottage
INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
    (SELECT location_id FROM locations WHERE name = 'Route 25'),
    'Bill''s Sea Cottage',
    'residential',
    'Seaside home of Bill, the renowned Pokémon researcher who designed the PC storage system.',
    NULL
);
