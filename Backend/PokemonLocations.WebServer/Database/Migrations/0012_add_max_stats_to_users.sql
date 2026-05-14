ALTER TABLE users
    ADD COLUMN max_gyms_complete     INTEGER NOT NULL DEFAULT 0,
    ADD COLUMN max_locations_visited INTEGER NOT NULL DEFAULT 0,
    ADD COLUMN max_buildings_visited INTEGER NOT NULL DEFAULT 0;

UPDATE users u SET
    max_gyms_complete = COALESCE(
        (SELECT COUNT(*) FROM user_badges WHERE user_id = u.user_id), 0),
    max_locations_visited = COALESCE(
        (SELECT COUNT(DISTINCT location_id) FROM user_visited_buildings
          WHERE user_id = u.user_id AND location_id IS NOT NULL), 0),
    max_buildings_visited = COALESCE(
        (SELECT COUNT(*) FROM user_visited_buildings WHERE user_id = u.user_id), 0);
