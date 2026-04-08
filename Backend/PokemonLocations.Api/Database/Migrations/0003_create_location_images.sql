CREATE TABLE location_images (
    image_id      INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    location_id   INT NOT NULL REFERENCES locations(location_id) ON DELETE CASCADE,
    image_url     VARCHAR(500) NOT NULL,
    display_order INT NOT NULL DEFAULT 0,
    caption       VARCHAR(255)
);

CREATE INDEX ix_location_images_location_id ON location_images(location_id);
