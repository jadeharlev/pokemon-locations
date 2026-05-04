-- Polish location and building descriptions in already-merged migrations
-- (0006, 0007, 0008, 0011) so they line up with Bulbapedia's canonical descriptions
-- for Pokémon Red specifically. This is descriptive-text only; no rows added or removed.

-- ============================================================
-- Locations
-- ============================================================

UPDATE locations
SET description = 'A small town covered in a beautiful hue of purple, located in northeast Kanto. Known for its ghost sightings and the Pokémon Tower, a graveyard for departed Pokémon.'
WHERE name = 'Lavender Town';

UPDATE locations
SET description = 'A southern Kanto port city, bathed in orange by the setting sun. Vermilion Harbor serves as the docking point for the S.S. Anne, and the city is home to the third Kanto gym led by the electric-type specialist Lt. Surge.'
WHERE name = 'Vermilion City';

-- ============================================================
-- Buildings - Pallet Town (originally 0008)
-- ============================================================

UPDATE buildings
SET description = 'The player''s home in Pallet Town. The bedroom contains a PC, bed, and television; the player''s mother lives here and can fully restore the player''s Pokémon team.'
WHERE name = 'Player''s House'
  AND location_id = (SELECT location_id FROM locations WHERE name = 'Pallet Town');

UPDATE buildings
SET description = 'Home of the player''s rival. The rival''s older sister Daisy lives here and gives the player a Town Map after the Pokédex is received.'
WHERE name = 'Rival''s House'
  AND location_id = (SELECT location_id FROM locations WHERE name = 'Pallet Town');

UPDATE buildings
SET description = 'Professor Oak''s research laboratory. It contains his three aides, shelves of books, a table with three Poké Balls, and the Pokédexes that new trainers receive at the start of their journey.'
WHERE name = 'Oak Pokémon Research Lab'
  AND location_id = (SELECT location_id FROM locations WHERE name = 'Pallet Town');

-- ============================================================
-- Buildings - Pewter City (originally 0008)
-- ============================================================

UPDATE buildings
SET description = 'A two-floor public museum, accessible for $50 admission.',
    landmark_description = 'The first floor displays Pokémon fossil exhibits, and the second floor holds a space exhibit with a Moon Stone on display.'
WHERE name = 'Pewter Museum of Science'
  AND location_id = (SELECT location_id FROM locations WHERE name = 'Pewter City');

-- ============================================================
-- Buildings - Lavender Town (originally 0008)
-- ============================================================

UPDATE buildings
SET description = 'A seven-floor graveyard housing hundreds of graves of deceased Pokémon.',
    landmark_description = 'Populated by Channeler trainers and ghost-type Pokémon, and used as a hideout by Team Rocket during the Marowak crisis.'
WHERE name = 'Pokémon Tower'
  AND location_id = (SELECT location_id FROM locations WHERE name = 'Lavender Town');

UPDATE buildings
SET description = 'The Lavender Volunteer Pokémon House, founded by Mr. Fuji as a sanctuary for abandoned and orphaned Pokémon.'
WHERE name = 'Mr. Fuji''s House'
  AND location_id = (SELECT location_id FROM locations WHERE name = 'Lavender Town');

-- ============================================================
-- Buildings - Viridian City (originally 0011)
-- ============================================================

UPDATE buildings
SET description = 'The eighth and final Kanto Gym, led by the Ground-type specialist Giovanni. Features one-way spin tiles and awards the Earth Badge upon victory.'
WHERE name = 'Viridian City Gym'
  AND location_id = (SELECT location_id FROM locations WHERE name = 'Viridian City');

-- ============================================================
-- Buildings - Cerulean City (originally 0011)
-- ============================================================

UPDATE buildings
SET description = 'The second Kanto Gym, led by the Water-type specialist Misty. Designed like an indoor swimming pool with platforms above the water; awards the Cascade Badge.'
WHERE name = 'Cerulean City Gym'
  AND location_id = (SELECT location_id FROM locations WHERE name = 'Cerulean City');

UPDATE buildings
SET description = 'A shop selling bicycles, with a list price of ₽1,000,000.',
    landmark_description = 'Trainers obtain a Bicycle for free by redeeming the Bike Voucher earned from the Vermilion Pokémon Fan Club rather than paying the prohibitive list price.'
WHERE name = 'Bike Shop'
  AND location_id = (SELECT location_id FROM locations WHERE name = 'Cerulean City');

-- ============================================================
-- Buildings - Vermilion City (originally 0011)
-- ============================================================

UPDATE buildings
SET description = 'The third Kanto Gym, led by the Electric-type specialist Lt. Surge. Two switches hidden inside fifteen trash cans must be found to unlock the leader''s chamber; awards the Thunder Badge.'
WHERE name = 'Vermilion City Gym'
  AND location_id = (SELECT location_id FROM locations WHERE name = 'Vermilion City');

-- ============================================================
-- Buildings - Celadon City (originally 0011)
-- ============================================================

UPDATE buildings
SET description = 'The fourth Kanto Gym, led by the Grass-type specialist Erika. The garden-filled gym requires Cut to reach the leader; awards the Rainbow Badge.'
WHERE name = 'Celadon City Gym'
  AND location_id = (SELECT location_id FROM locations WHERE name = 'Celadon City');

UPDATE buildings
SET description = 'The largest building in Celadon City and the largest shop in Kanto. A six-story department store including a rooftop floor with various item vendors.'
WHERE name = 'Celadon Department Store'
  AND location_id = (SELECT location_id FROM locations WHERE name = 'Celadon City');

UPDATE buildings
SET description = 'A coin-based gambling parlor with slot machines, run as a front by Team Rocket.',
    landmark_description = 'A switch hidden behind a poster on the back wall reveals a staircase to the Team Rocket Hideout below.'
WHERE name = 'Rocket Game Corner'
  AND location_id = (SELECT location_id FROM locations WHERE name = 'Celadon City');

-- ============================================================
-- Buildings - Fuchsia City (originally 0011)
-- ============================================================

UPDATE buildings
SET description = 'The fifth Kanto Gym, led by the Poison-type specialist Koga. Awards the Soul Badge to victorious trainers.'
WHERE name = 'Fuchsia City Gym'
  AND location_id = (SELECT location_id FROM locations WHERE name = 'Fuchsia City');

UPDATE buildings
SET description = 'A special Pokémon preserve where trainers can catch rare species using Safari Balls.',
    landmark_description = 'Costs ₽500 to enter. Visitors receive 30 Safari Balls per visit and a fixed step limit before being escorted out.'
WHERE name = 'Safari Zone'
  AND location_id = (SELECT location_id FROM locations WHERE name = 'Fuchsia City');

-- ============================================================
-- Buildings - Saffron City (originally 0011)
-- ============================================================

UPDATE buildings
SET description = 'The sixth Kanto Gym, led by the Psychic-type specialist Sabrina. A maze of nine rooms connected by warp panels; awards the Marsh Badge.'
WHERE name = 'Saffron City Gym'
  AND location_id = (SELECT location_id FROM locations WHERE name = 'Saffron City');

UPDATE buildings
SET description = 'An eleven-floor skyscraper that serves as the headquarters of the world''s leading Pokémon technology manufacturer.',
    landmark_description = 'Temporarily seized by Team Rocket; the player liberates the company by defeating Giovanni on the top floor.'
WHERE name = 'Silph Co.'
  AND location_id = (SELECT location_id FROM locations WHERE name = 'Saffron City');

UPDATE buildings
SET description = 'An unofficial gym specializing in Fighting-type Pokémon, led by the Karate Master.',
    landmark_description = 'Defeating the Karate Master earns the trainer a choice between Hitmonlee and Hitmonchan.'
WHERE name = 'Fighting Dojo'
  AND location_id = (SELECT location_id FROM locations WHERE name = 'Saffron City');

-- ============================================================
-- Buildings - Cinnabar Island (originally 0011)
-- ============================================================

UPDATE buildings
SET description = 'The seventh Kanto Gym, led by the Fire-type specialist Blaine. Locked behind the Secret Key found in the Pokémon Mansion; awards the Volcano Badge.'
WHERE name = 'Cinnabar Island Gym'
  AND location_id = (SELECT location_id FROM locations WHERE name = 'Cinnabar Island');

UPDATE buildings
SET description = 'A decrepit, burned-down mansion at the heart of Cinnabar Island.',
    landmark_description = 'Hides the Secret Key needed to unlock the Cinnabar Gym, along with journals describing Mewtwo''s creation.'
WHERE name = 'Pokémon Mansion'
  AND location_id = (SELECT location_id FROM locations WHERE name = 'Cinnabar Island');

UPDATE buildings
SET description = 'A research laboratory founded by Dr. Fuji where scientists revive Pokémon from fossils and conduct in-game trades.'
WHERE name = 'Pokémon Lab'
  AND location_id = (SELECT location_id FROM locations WHERE name = 'Cinnabar Island');
