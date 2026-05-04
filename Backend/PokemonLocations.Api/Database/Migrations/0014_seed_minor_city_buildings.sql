-- Add the canonical named buildings in the existing Kanto cities that previous seed
-- migrations skipped. Pokémon Centers, marts, gyms, and major landmarks are already covered
-- by 0008 and 0011; this fills in the remaining Bulbapedia-listed buildings for each city.

-- ============================================================
-- Viridian City
-- ============================================================

INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
    (SELECT location_id FROM locations WHERE name = 'Viridian City'),
    'TM Man''s House',
    'residential',
    'A house on the western side of Viridian City, accessible via Cut or Surf, where the resident gives the trainer TM42 (Dream Eater).',
    NULL
);

-- ============================================================
-- Cerulean City
-- ============================================================

INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES
    (
        (SELECT location_id FROM locations WHERE name = 'Cerulean City'),
        'Dontae''s House',
        'residential',
        'A small Cerulean home where the owner offers an in-game trade of his Jynx for a Poliwhirl.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Cerulean City'),
        'Burglarized House',
        'residential',
        'A Cerulean home broken into by Team Rocket; defeating the grunt restores TM28 (Dig) to the owners.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Cerulean City'),
        'Gym Badge Man''s House',
        'residential',
        'A cottage in the northwest corner of Cerulean City, where the resident explains the effects of each of the eight Indigo League badges.',
        NULL
    );

-- ============================================================
-- Lavender Town
-- ============================================================

INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
    (SELECT location_id FROM locations WHERE name = 'Lavender Town'),
    'Name Rater''s House',
    'residential',
    'Home of the Name Rater, who can rename a trainer''s Pokémon for free.',
    NULL
);

-- ============================================================
-- Vermilion City
-- ============================================================

INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES
    (
        (SELECT location_id FROM locations WHERE name = 'Vermilion City'),
        'Pokémon Fan Club',
        'residential',
        'A clubhouse located north of the Vermilion Gym where Pokémon enthusiasts gather. The Chairman awards a Bike Voucher to trainers who patiently hear out his stories about his favorite Pokémon, Rapidash and Fearow.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Vermilion City'),
        'Construction Site',
        'landmark',
        'A construction area in the northeastern part of Vermilion City, where an Old Man''s Machop is preparing the land for a future building. Construction has stalled since Generation I due to insufficient funds.',
        NULL
    );

-- ============================================================
-- Celadon City
-- ============================================================

INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES
    (
        (SELECT location_id FROM locations WHERE name = 'Celadon City'),
        'Celadon Hotel',
        'landmark',
        'A luxurious hotel located in the southeastern part of Celadon City.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Celadon City'),
        'Celadon Condominiums',
        'residential',
        'A residential building located next to the Celadon Pokémon Center.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Celadon City'),
        'Team Rocket Hideout',
        'landmark',
        'An underground Team Rocket base hidden beneath the Rocket Game Corner, reachable via a switch behind a poster.',
        'A multi-floor underground base where Giovanni leads Team Rocket''s Celadon operation.'
    );

-- ============================================================
-- Fuchsia City
-- ============================================================

INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES
    (
        (SELECT location_id FROM locations WHERE name = 'Fuchsia City'),
        'Warden''s House',
        'residential',
        'Home of the Safari Zone Warden. Returning his missing Gold Teeth earns the trainer an HM in exchange.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Fuchsia City'),
        'Pokémon Zoo',
        'landmark',
        'A display facility featuring captive Pokémon, including Chansey, Lapras, Voltorb, Kangaskhan, and Slowpoke, along with the fossil Pokémon revived from Mt. Moon.',
        NULL
    );

-- ============================================================
-- Saffron City
-- ============================================================

INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES
    (
        (SELECT location_id FROM locations WHERE name = 'Saffron City'),
        'Mr. Psychic''s House',
        'residential',
        'Home of Mr. Psychic, who gifts the trainer TM29 (Psychic).',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Saffron City'),
        'Copycat''s House',
        'residential',
        'Home of Copycat, a young Saffron City girl who mimics the trainer''s mannerisms. She trades TM31 (Mimic) in exchange for a Poké Doll.',
        NULL
    );
