-- Seed Kanto's overworld dungeons and the Indigo Plateau, plus the Indigo Plateau's buildings.

-- ============================================================
-- Dungeon and overworld locations
-- ============================================================

INSERT INTO locations (name, description, video_url) VALUES
    (
        'Viridian Forest',
        'A woodland maze on Route 2 between Viridian City and Pewter City. Bug Catchers wait to challenge trainers, and wild Caterpie, Weedle, and Pidgey populate the tall grass; for many trainers it is their first major dungeon.',
        NULL
    ),
    (
        'Mt. Moon',
        'A three-floor cave system between Pewter City and Cerulean City, one of the few places where wild Clefairy can be found. The mountain is a source of Moon Stones formed from meteorite shards, and it is the site of the Dome Fossil / Helix Fossil choice after Team Rocket''s defeat.',
        NULL
    ),
    (
        'Diglett''s Cave',
        'An underground tunnel dug by wild Diglett and Dugtrio that connects Route 2 (near Pewter City) to Route 11 (near Vermilion City), populated almost entirely by those two species.',
        NULL
    ),
    (
        'Rock Tunnel',
        'A naturally formed two-floor underground cave on Route 10, connecting Cerulean City to Lavender Town. Pitch-black inside and requires a light source (Flash) to navigate; home to Zubat, Geodude, Machop, and Onix.',
        NULL
    ),
    (
        'Power Plant',
        'An abandoned electrical facility located south of the Route 10 Pokémon Center, accessible only by Surfing. Houses numerous Electric-type Pokémon and serves as the roost of the legendary Zapdos.',
        NULL
    ),
    (
        'Cerulean Cave',
        'A post-game cave in the northwest corner of Cerulean City, accessible only after the player defeats the Elite Four and enters the Hall of Fame. The legendary Mewtwo, having escaped the Pokémon Mansion, dwells in its deepest chamber.',
        NULL
    ),
    (
        'Seafoam Islands',
        'A pair of caves on Route 20 between Fuchsia City and Cinnabar Island. The puzzle requires using Strength to push boulders into the water, blocking the current so trainers can descend to the lowest floor where the legendary Articuno resides.',
        NULL
    ),
    (
        'Victory Road',
        'A mountain dungeon on Route 23 leading to the Indigo Plateau. Strength is required to push boulders onto pressure switches, and Flash is needed to see through its dark interior.',
        NULL
    ),
    (
        'Indigo Plateau',
        'The capital of the Kanto Pokémon League and the final destination for trainers seeking to challenge the Elite Four and Champion. A trainer must hold all eight Kanto badges before being granted entry.',
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
