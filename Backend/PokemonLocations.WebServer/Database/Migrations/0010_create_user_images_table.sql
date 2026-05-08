CREATE TABLE user_images (
    image_id          UUID PRIMARY KEY,
    user_id           INTEGER NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    location_id       INTEGER NOT NULL,
    file_path         VARCHAR(500) NOT NULL,
    original_filename VARCHAR(255) NOT NULL,
    content_type      VARCHAR(50)  NOT NULL,
    byte_size         INTEGER      NOT NULL,
    uploaded_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX ix_user_images_user_location
    ON user_images (user_id, location_id, uploaded_at DESC);
