INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
  (SELECT location_id FROM locations WHERE name = 'Pallet Town'),
  'Oak Pokémon Research Lab', 'lab',
  'The Pokémon research lab of Professor Oak.', NULL
);

INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
  (SELECT location_id FROM locations WHERE name = 'Pallet Town'),
  'Player''s House', 'residential',
  'Your home in Pallet Town.', NULL
);

INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
  (SELECT location_id FROM locations WHERE name = 'Pallet Town'),
  'Rival''s House', 'residential',
  'Home of Professor Oak''s grandson.', NULL
);

INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
  (SELECT location_id FROM locations WHERE name = 'Pewter City'),
  'Pewter City Gym', 'gym',
  'The official Pewter City Pokémon Gym.', NULL
);

INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
  (SELECT location_id FROM locations WHERE name = 'Pewter City'),
  'Pewter Pokémon Center', 'pokemon_center',
  'Heal your Pokémon after a tough battle.', NULL
);

INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
  (SELECT location_id FROM locations WHERE name = 'Pewter City'),
  'Pewter Poké Mart', 'poke_mart',
  'Stock up on Poke Balls and Potions.', NULL
);

INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
  (SELECT location_id FROM locations WHERE name = 'Pewter City'),
  'Pewter Museum of Science', 'landmark',
  'A museum showcasing rare fossils and space exhibits.',
  'Features a Kabutops fossil and a Space Shuttle exhibit on the upper floor.'
);

INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
  (SELECT location_id FROM locations WHERE name = 'Lavender Town'),
  'Pokémon Tower', 'landmark',
  'A memorial tower for departed Pokémon.',
  'A seven-floor tower full of battles.'
);

INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
  (SELECT location_id FROM locations WHERE name = 'Lavender Town'),
  'Lavender Pokémon Center', 'pokemon_center',
  'Heal your Pokémon in Lavender Town.', NULL
);

INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
  (SELECT location_id FROM locations WHERE name = 'Lavender Town'),
  'Lavender Poké Mart', 'poke_mart',
  'A small shop with essential supplies.', NULL
);

INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
  (SELECT location_id FROM locations WHERE name = 'Lavender Town'),
  'Mr. Fuji''s House', 'residential',
  'Home of Mr. Fuji.', NULL
);
