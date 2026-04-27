CREATE TYPE user_theme AS ENUM ('bulbasaur', 'charmander', 'squirtle');

CREATE TABLE users (
    user_id       SERIAL       PRIMARY KEY,
    email         VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    display_name  VARCHAR(50)  NOT NULL,
    theme         user_theme   NOT NULL DEFAULT 'bulbasaur',
    created_at    TIMESTAMPTZ  NOT NULL DEFAULT now()
);
