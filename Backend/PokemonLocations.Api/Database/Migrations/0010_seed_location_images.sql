INSERT INTO location_images (location_id, image_url, display_order, caption)
VALUES (
  (SELECT location_id FROM locations WHERE name = 'Pallet Town'),
  '/images/pallet-town-overview.png', 1, 'Pallet Town overview'
);

INSERT INTO location_images (location_id, image_url, display_order, caption)
VALUES (
  (SELECT location_id FROM locations WHERE name = 'Pallet Town'),
  '/images/pallet-town-sign.png', 2, 'Pallet Town welcome sign: ''Shades of your journey await!'''
);

INSERT INTO location_images (location_id, image_url, display_order, caption)
VALUES (
  (SELECT location_id FROM locations WHERE name = 'Pewter City'),
  '/images/pewter-city-overview.png', 1, 'Pewter City overview'
);

INSERT INTO location_images (location_id, image_url, display_order, caption)
VALUES (
  (SELECT location_id FROM locations WHERE name = 'Pewter City'),
  '/images/pewter-city-museum.png', 2, 'Pewter Museum of Science'
);

INSERT INTO location_images (location_id, image_url, display_order, caption)
VALUES (
  (SELECT location_id FROM locations WHERE name = 'Lavender Town'),
  '/images/lavender-town-overview.png', 1, 'Lavender Town overview'
);

INSERT INTO location_images (location_id, image_url, display_order, caption)
VALUES (
  (SELECT location_id FROM locations WHERE name = 'Lavender Town'),
  '/images/lavender-town-tower.png', 2, 'Pokémon Tower: ''Becalm the Spirits of POKéMON!'''
);
