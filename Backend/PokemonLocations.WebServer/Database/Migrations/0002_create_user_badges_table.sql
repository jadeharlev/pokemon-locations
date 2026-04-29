CREATE TYPE badge AS ENUM (
    'boulder',
    'cascade',
    'thunder',
    'rainbow',
    'soul',
    'marsh',
    'volcano',
    'earth'
);

CREATE TABLE user_badges (
    user_id INTEGER NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    badge   badge   NOT NULL,
    PRIMARY KEY (user_id, badge)
);
