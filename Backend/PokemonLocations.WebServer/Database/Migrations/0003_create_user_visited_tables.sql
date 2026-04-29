CREATE TABLE user_visited_locations (
    user_id     INTEGER NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    location_id INTEGER NOT NULL,
    PRIMARY KEY (user_id, location_id)
);

CREATE TABLE user_visited_buildings (
    user_id     INTEGER NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    building_id INTEGER NOT NULL,
    PRIMARY KEY (user_id, building_id)
);
