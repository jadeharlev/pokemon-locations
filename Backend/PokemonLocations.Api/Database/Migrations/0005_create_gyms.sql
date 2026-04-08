CREATE TABLE gyms (
    gym_id      INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    building_id INT NOT NULL UNIQUE REFERENCES buildings(building_id) ON DELETE CASCADE,
    gym_type    VARCHAR(50)  NOT NULL,
    badge_name  VARCHAR(100) NOT NULL,
    gym_leader  VARCHAR(100) NOT NULL,
    gym_order   INT NOT NULL
);
