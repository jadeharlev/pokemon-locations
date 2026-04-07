CREATE TABLE buildings (
    building_id          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    location_id          INT NOT NULL REFERENCES locations(location_id) ON DELETE CASCADE,
    name                 VARCHAR(255) NOT NULL,
    building_type        building_type NOT NULL,
    description          TEXT,
    landmark_description TEXT
);

CREATE INDEX ix_buildings_location_id ON buildings(location_id);
