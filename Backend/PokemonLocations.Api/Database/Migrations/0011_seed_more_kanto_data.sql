-- Seed additional Kanto locations, buildings, gyms, and location images.
-- This expands the demo data so the app has all 8 Kanto gyms and more complete location detail pages.

-- ============================================================
-- New locations
-- Existing locations already include:
-- Pallet Town, Viridian City, Pewter City, Cerulean City, Lavender Town
-- ============================================================

INSERT INTO locations (name, description, video_url)
VALUES
    (
        'Vermilion City',
        'A busy port city in southern Kanto. It is home to the S.S. Anne and the third Kanto gym, led by the electric-type specialist Lt. Surge.',
        NULL
    ),
    (
        'Celadon City',
        'A large city known for its department store, game corner, and the fourth Kanto gym, led by the grass-type specialist Erika.',
        NULL
    ),
    (
        'Fuchsia City',
        'A southern Kanto city known for the Safari Zone and the fifth Kanto gym, led by the poison-type specialist Koga.',
        NULL
    ),
    (
        'Saffron City',
        'A major city in central Kanto. It is known for Silph Co., the Fighting Dojo, and the sixth Kanto gym, led by the psychic-type specialist Sabrina.',
        NULL
    ),
    (
        'Cinnabar Island',
        'A small island south of Pallet Town. It is home to the Pokémon Mansion and the seventh Kanto gym, led by the fire-type specialist Blaine.',
        NULL
    );

-- ============================================================
-- Buildings for existing locations that were missing details
-- ============================================================

-- Viridian City
INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES
    (
        (SELECT location_id FROM locations WHERE name = 'Viridian City'),
        'Viridian Pokémon Center',
        'pokemon_center',
        'A place for trainers to heal their Pokémon before entering Viridian Forest or challenging the final gym.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Viridian City'),
        'Viridian Poké Mart',
        'poke_mart',
        'A shop that sells basic trainer supplies for the early part of the journey.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Viridian City'),
        'Viridian City Gym',
        'gym',
        'The final Kanto Gym, led by Giovanni.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Viridian City'),
        'Trainer School',
        'landmark',
        'A small school where new trainers can learn about status conditions and battle basics.',
        'A helpful early-game landmark for learning important trainer tips.'
    );

-- Cerulean City
INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES
    (
        (SELECT location_id FROM locations WHERE name = 'Cerulean City'),
        'Cerulean Pokémon Center',
        'pokemon_center',
        'A place for trainers to heal their Pokémon after battles near Nugget Bridge.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Cerulean City'),
        'Cerulean Poké Mart',
        'poke_mart',
        'A shop that sells useful supplies for exploring northern Kanto.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Cerulean City'),
        'Cerulean City Gym',
        'gym',
        'The second Kanto Gym, led by Misty.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Cerulean City'),
        'Bike Shop',
        'landmark',
        'A shop famous for selling bicycles.',
        'The Bike Shop is one of Cerulean City''s most recognizable buildings.'
    );

-- ============================================================
-- Buildings for new locations
-- ============================================================

-- Vermilion City
INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES
    (
        (SELECT location_id FROM locations WHERE name = 'Vermilion City'),
        'Vermilion Pokémon Center',
        'pokemon_center',
        'A place for trainers to heal their Pokémon before boarding the S.S. Anne or challenging the gym.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Vermilion City'),
        'Vermilion Poké Mart',
        'poke_mart',
        'A shop that sells supplies for trainers traveling through Vermilion City.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Vermilion City'),
        'Vermilion City Gym',
        'gym',
        'The third Kanto Gym, led by Lt. Surge.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Vermilion City'),
        'S.S. Anne',
        'landmark',
        'A luxury cruise ship docked in Vermilion Harbor.',
        'The S.S. Anne is a major story location where the player receives Cut.'
    );

-- Celadon City
INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES
    (
        (SELECT location_id FROM locations WHERE name = 'Celadon City'),
        'Celadon Pokémon Center',
        'pokemon_center',
        'A place for trainers to heal their Pokémon while exploring Celadon City.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Celadon City'),
        'Celadon Department Store',
        'poke_mart',
        'A large multi-floor store with many items for trainers.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Celadon City'),
        'Celadon City Gym',
        'gym',
        'The fourth Kanto Gym, led by Erika.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Celadon City'),
        'Rocket Game Corner',
        'landmark',
        'A game corner that hides a Team Rocket secret.',
        'The Rocket Game Corner leads to Team Rocket''s hidden hideout.'
    );

-- Fuchsia City
INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES
    (
        (SELECT location_id FROM locations WHERE name = 'Fuchsia City'),
        'Fuchsia Pokémon Center',
        'pokemon_center',
        'A place for trainers to heal their Pokémon before visiting the Safari Zone or gym.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Fuchsia City'),
        'Fuchsia Poké Mart',
        'poke_mart',
        'A shop that sells supplies for trainers exploring southern Kanto.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Fuchsia City'),
        'Fuchsia City Gym',
        'gym',
        'The fifth Kanto Gym, led by Koga.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Fuchsia City'),
        'Safari Zone',
        'landmark',
        'A large preserve where trainers can encounter rare Pokémon.',
        'The Safari Zone is known for rare Pokémon and important items.'
    );

-- Saffron City
INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES
    (
        (SELECT location_id FROM locations WHERE name = 'Saffron City'),
        'Saffron Pokémon Center',
        'pokemon_center',
        'A place for trainers to heal their Pokémon in central Kanto.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Saffron City'),
        'Saffron Poké Mart',
        'poke_mart',
        'A shop that sells supplies for trainers traveling through Saffron City.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Saffron City'),
        'Saffron City Gym',
        'gym',
        'The sixth Kanto Gym, led by Sabrina.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Saffron City'),
        'Silph Co.',
        'landmark',
        'A major company building taken over by Team Rocket.',
        'Silph Co. is one of the largest story locations in Saffron City.'
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Saffron City'),
        'Fighting Dojo',
        'landmark',
        'A battle-focused dojo near the Saffron City Gym.',
        'The Fighting Dojo is a notable side location where trainers can battle martial artists.'
    );

-- Cinnabar Island
INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES
    (
        (SELECT location_id FROM locations WHERE name = 'Cinnabar Island'),
        'Cinnabar Pokémon Center',
        'pokemon_center',
        'A place for trainers to heal their Pokémon after traveling across the sea.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Cinnabar Island'),
        'Cinnabar Poké Mart',
        'poke_mart',
        'A shop that sells supplies for trainers exploring Cinnabar Island.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Cinnabar Island'),
        'Cinnabar Island Gym',
        'gym',
        'The seventh Kanto Gym, led by Blaine.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Cinnabar Island'),
        'Pokémon Mansion',
        'landmark',
        'A ruined mansion connected to Pokémon research history.',
        'The Pokémon Mansion is an important location for unlocking the Cinnabar Island Gym.'
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Cinnabar Island'),
        'Pokémon Lab',
        'lab',
        'A research lab where scientists study rare Pokémon fossils.',
        NULL
    );

-- ============================================================
-- Gyms
-- Pewter City Gym already exists, so this adds the other 7.
-- ============================================================

INSERT INTO gyms (building_id, gym_type, badge_name, gym_leader, gym_order)
VALUES
    (
        (SELECT building_id FROM buildings WHERE name = 'Cerulean City Gym'),
        'Water',
        'Cascade Badge',
        'Misty',
        2
    ),
    (
        (SELECT building_id FROM buildings WHERE name = 'Vermilion City Gym'),
        'Electric',
        'Thunder Badge',
        'Lt. Surge',
        3
    ),
    (
        (SELECT building_id FROM buildings WHERE name = 'Celadon City Gym'),
        'Grass',
        'Rainbow Badge',
        'Erika',
        4
    ),
    (
        (SELECT building_id FROM buildings WHERE name = 'Fuchsia City Gym'),
        'Poison',
        'Soul Badge',
        'Koga',
        5
    ),
    (
        (SELECT building_id FROM buildings WHERE name = 'Saffron City Gym'),
        'Psychic',
        'Marsh Badge',
        'Sabrina',
        6
    ),
    (
        (SELECT building_id FROM buildings WHERE name = 'Cinnabar Island Gym'),
        'Fire',
        'Volcano Badge',
        'Blaine',
        7
    ),
    (
        (SELECT building_id FROM buildings WHERE name = 'Viridian City Gym'),
        'Ground',
        'Earth Badge',
        'Giovanni',
        8
    );

-- ============================================================
-- Location images
-- Adds images for Viridian, Cerulean, and the 5 new locations.
-- Existing images already cover Pallet Town, Pewter City, and Lavender Town.
-- ============================================================

INSERT INTO location_images (location_id, image_url, display_order, caption)
VALUES
    (
        (SELECT location_id FROM locations WHERE name = 'Viridian City'),
        '/images/viridian-city-overview.png',
        1,
        'Viridian City overview'
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Viridian City'),
        '/images/viridian-city-gym.png',
        2,
        'Viridian City Gym'
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Cerulean City'),
        '/images/cerulean-city-overview.png',
        1,
        'Cerulean City overview'
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Cerulean City'),
        '/images/cerulean-city-gym.png',
        2,
        'Cerulean City Gym'
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Vermilion City'),
        '/images/vermilion-city-overview.png',
        1,
        'Vermilion City overview'
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Vermilion City'),
        '/images/ss-anne.png',
        2,
        'S.S. Anne'
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Celadon City'),
        '/images/celadon-city-overview.png',
        1,
        'Celadon City overview'
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Celadon City'),
        '/images/celadon-department-store.png',
        2,
        'Celadon Department Store'
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Fuchsia City'),
        '/images/fuchsia-city-overview.png',
        1,
        'Fuchsia City overview'
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Fuchsia City'),
        '/images/safari-zone.png',
        2,
        'Safari Zone'
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Saffron City'),
        '/images/saffron-city-overview.png',
        1,
        'Saffron City overview'
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Saffron City'),
        '/images/silph-co.png',
        2,
        'Silph Co.'
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Cinnabar Island'),
        '/images/cinnabar-island-overview.png',
        1,
        'Cinnabar Island overview'
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Cinnabar Island'),
        '/images/pokemon-mansion.png',
        2,
        'Pokémon Mansion'
    );

