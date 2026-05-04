-- Seed Kanto's overworld dungeons and the Indigo Plateau, plus the Indigo Plateau's buildings.

-- ============================================================
-- Dungeon and overworld locations
-- ============================================================

INSERT INTO locations (name, description, video_url) VALUES
    (
        'Viridian Forest',
        'A dense maze of trees north of Viridian City. Bug-types like Caterpie, Weedle, and Pikachu hide in the tall grass while Bug Catchers wait to challenge new trainers.',
        NULL
    ),
    (
        'Mt. Moon',
        'A multi-floor cave system between Pewter City and Cerulean City. Home to Clefairy, Zubat, and Geodude, and the site of the Dome and Helix fossil discovery.',
        NULL
    ),
    (
        'Diglett''s Cave',
        'An underground tunnel connecting Route 2 and Route 11, populated almost entirely by Diglett and Dugtrio.',
        NULL
    ),
    (
        'Rock Tunnel',
        'A pitch-black cave east of Cerulean City that requires Flash to navigate. Connects Route 9 to Route 10 and is filled with rock-types.',
        NULL
    ),
    (
        'Power Plant',
        'An abandoned electrical facility off Route 10. Wild electric-types roam its halls, and the legendary Zapdos rests in its depths.',
        NULL
    ),
    (
        'Cerulean Cave',
        'A treacherous post-game cave just north of Cerulean City, accessible only after defeating the Elite Four. The legendary Mewtwo dwells in its deepest chamber.',
        NULL
    ),
    (
        'Seafoam Islands',
        'A frozen island chain south of Fuchsia City. Strength and Surf are required to navigate its ice block puzzles. The legendary Articuno can be found within.',
        NULL
    ),
    (
        'Victory Road',
        'A treacherous mountain trail leading to the Indigo Plateau. Strength and boulder puzzles bar all but the most prepared trainers.',
        NULL
    ),
    (
        'Indigo Plateau',
        'The seat of the Pokémon League. The Elite Four and Champion battle here in succession, awaiting any trainer who can earn all eight Kanto badges.',
        NULL
    );

-- ============================================================
-- Buildings at Indigo Plateau
-- ============================================================

INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES
    (
        (SELECT location_id FROM locations WHERE name = 'Indigo Plateau'),
        'Indigo Plateau Pokémon Center',
        'pokemon_center',
        'The final Pokémon Center before the Elite Four. Trainers prepare here for the toughest battles in Kanto.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Indigo Plateau'),
        'Indigo Plateau Poké Mart',
        'poke_mart',
        'A high-end shop attached to the Indigo Plateau Pokémon Center, stocked with the strongest items for endgame trainers.',
        NULL
    ),
    (
        (SELECT location_id FROM locations WHERE name = 'Indigo Plateau'),
        'Pokémon League',
        'landmark',
        'The grand hall housing the Elite Four and the Pokémon Champion. A trainer who defeats all five becomes the new Champion of Kanto.',
        'Lorelei, Bruno, Agatha, and Lance defend the Elite Four chambers; the Champion''s room lies beyond.'
    );
