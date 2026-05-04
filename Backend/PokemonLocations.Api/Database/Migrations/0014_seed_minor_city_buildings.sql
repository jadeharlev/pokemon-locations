-- Fill in the canonical named buildings in the existing 10 Kanto cities that previous seed
-- migrations skipped. Pokémon Centers, marts, gyms, and major landmarks are already covered;
-- this adds the smaller story-relevant buildings so each city's building list reads complete.

-- ============================================================
-- Viridian City
-- ============================================================

INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
    (SELECT location_id FROM locations WHERE name = 'Viridian City'),
    'Old Man''s House',
    'residential',
    'Home of the Old Man who demonstrates how to catch a Pokémon to new trainers.',
    NULL
);

-- ============================================================
-- Cerulean City
-- ============================================================

INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
    (SELECT location_id FROM locations WHERE name = 'Cerulean City'),
    'Burgled House',
    'residential',
    'A small Cerulean City home recently broken into by Team Rocket.',
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
        'landmark',
        'A meeting hall for passionate Pokémon enthusiasts. The chairman rewards trainers who hear out his stories with a Bike Voucher.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Vermilion City'),
        'Pidgey Owner''s House',
        'residential',
        'A small home where a trainer offers an in-game trade involving a Spearow.',
        NULL
    );

-- ============================================================
-- Lavender Town
-- ============================================================

INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES
    (
        (SELECT location_id FROM locations WHERE name = 'Lavender Town'),
        'Name Rater''s House',
        'residential',
        'Home of the Name Rater, who can rename a trainer''s Pokémon for free.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Lavender Town'),
        'Volunteer Pokémon House',
        'residential',
        'A small house sheltering Pokémon orphaned by Team Rocket''s actions in Pokémon Tower.',
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
        'A modest hotel on the eastern side of Celadon City, frequented by visiting trainers.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Celadon City'),
        'Celadon Restaurant',
        'landmark',
        'A small diner where a Team Rocket grunt drops a key item if defeated.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Celadon City'),
        'Eevee Gift House',
        'residential',
        'A house in the Celadon Mansion area where a kind man gifts the trainer an Eevee.',
        NULL
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
        'Home of the Safari Zone Warden. The trainer can return his missing Gold Teeth for an HM in exchange.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Fuchsia City'),
        'Move Deleter''s House',
        'landmark',
        'Home of the Move Deleter, who can permanently remove a Pokémon''s known move - including HM moves.',
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
        'Home of Mr. Psychic, who gifts the trainer the TM for the move Psychic.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Saffron City'),
        'Copycat''s House',
        'residential',
        'Home of Copycat, a young Saffron City trainer who mimics the player''s mannerisms.',
        NULL
    );
