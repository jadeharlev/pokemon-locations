ALTER TABLE user_visited_buildings
ADD COLUMN IF NOT EXISTS location_id INTEGER;

DELETE FROM user_visited_buildings
WHERE location_id IS NULL;

ALTER TABLE user_visited_buildings
ALTER COLUMN location_id SET NOT NULL;

ALTER TABLE user_visited_buildings
DROP CONSTRAINT IF EXISTS user_visited_buildings_pkey;

ALTER TABLE user_visited_buildings
ADD CONSTRAINT user_visited_buildings_pkey
PRIMARY KEY (user_id, location_id, building_id);
