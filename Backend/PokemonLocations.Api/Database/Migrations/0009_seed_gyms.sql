INSERT INTO gyms (building_id, gym_type, badge_name, gym_leader, gym_order)
VALUES (
  (SELECT building_id FROM buildings WHERE name = 'Pewter City Gym'),
  'Rock', 'Boulder Badge', 'Brock', 1
);
