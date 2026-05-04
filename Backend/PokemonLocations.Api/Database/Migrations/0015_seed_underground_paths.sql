-- Seed Kanto's two Underground Paths. Each is a separate top-level location in Pokémon Red,
-- with its own map screen connecting two routes beneath Saffron City.

INSERT INTO locations (name, description, video_url) VALUES
    (
        'Underground Path (Routes 5-6)',
        'A subterranean tunnel running beneath Saffron City that links Route 5 (north of Saffron) with Route 6 (south of Saffron). Trainers can use it to bypass Saffron when its city gates are blocked.',
        NULL
    ),
    (
        'Underground Path (Routes 7-8)',
        'A subterranean tunnel running beneath Saffron City that links Route 7 (west of Saffron, near Celadon) with Route 8 (east of Saffron, near Lavender). Useful for moving across central Kanto when Saffron is inaccessible.',
        NULL
    );
