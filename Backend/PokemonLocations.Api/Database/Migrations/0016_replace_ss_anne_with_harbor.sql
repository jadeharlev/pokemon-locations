-- Replace the 'S.S. Anne' building under Vermilion City with 'Vermilion Harbor'
-- to match Bulbapedia's canonical building list for Vermilion City. The S.S. Anne is
-- a ship that docks at Vermilion Harbor; Bulbapedia treats the harbor (the dock) as
-- the city's landmark, not the ship.

DELETE FROM buildings
WHERE name = 'S.S. Anne'
  AND location_id = (SELECT location_id FROM locations WHERE name = 'Vermilion City');

INSERT INTO buildings (location_id, name, building_type, description, landmark_description)
VALUES (
    (SELECT location_id FROM locations WHERE name = 'Vermilion City'),
    'Vermilion Harbor',
    'landmark',
    'One of the larger docks in the Kanto region, where ships such as the S.S. Anne dock during their voyages.',
    'The harbor serves as Vermilion City''s gateway to the sea and to other regions.'
);
