CREATE TABLE IF NOT EXISTS favorite_buildings (
    favorite_id SERIAL PRIMARY KEY,
    user_id INT NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    building_id INT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE (user_id, building_id)
);
