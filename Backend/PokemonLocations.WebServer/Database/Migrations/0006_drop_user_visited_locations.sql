-- "Locations visited" is now derived from "user has at least one visited building in that
-- location" rather than a separately-tracked table. Persist the building's location_id on
-- user_visited_buildings so the count can be computed without a cross-DB join, and drop
-- the now-unused user_visited_locations table.
--
-- The DELETE clears existing rows because the new NOT NULL column has no default. This is
-- a one-time dev-data clear; this project has no production data yet.

DELETE FROM user_visited_buildings;

ALTER TABLE user_visited_buildings
    ADD COLUMN location_id INTEGER NOT NULL;

DROP TABLE user_visited_locations;
