-- Reference catalogue for local development: plans, Arizona trainers and a rolling class schedule.
-- Contains no member, account or payment data. Class start times are relative to first start-up.

INSERT INTO membership_plans (id, name, monthly_price, max_classes_per_week) VALUES
    (gen_random_uuid(), 'Basic', 29.99, 3),
    (gen_random_uuid(), 'Plus', 49.99, 5),
    (gen_random_uuid(), 'Premium', 79.99, NULL);

INSERT INTO trainers (id, full_name, specialty) VALUES
    ('11111111-1111-4111-8111-111111111111', 'Dana Whitfield', 'Indoor cycling'),
    ('22222222-2222-4222-8222-222222222222', 'Marcos Ortega', 'Strength and conditioning'),
    ('33333333-3333-4333-8333-333333333333', 'Priya Nair', 'Yoga and mobility'),
    ('44444444-4444-4444-8444-444444444444', 'Kayla Brooks', 'HIIT');

INSERT INTO classes (id, title, trainer_id, location, starts_at, duration_minutes, capacity) VALUES
    (gen_random_uuid(), 'Desert Sunrise Spin', '11111111-1111-4111-8111-111111111111', 'Phoenix',
        date_trunc('day', now()) + interval '1 day 13 hours', 45, 20),
    (gen_random_uuid(), 'Camelback Strength', '22222222-2222-4222-8222-222222222222', 'Scottsdale',
        date_trunc('day', now()) + interval '1 day 18 hours', 60, 15),
    (gen_random_uuid(), 'Saguaro Flow Yoga', '33333333-3333-4333-8333-333333333333', 'Tucson',
        date_trunc('day', now()) + interval '2 days 14 hours', 60, 25),
    (gen_random_uuid(), 'Monsoon HIIT', '44444444-4444-4444-8444-444444444444', 'Tempe',
        date_trunc('day', now()) + interval '3 days 1 hour', 30, 18),
    (gen_random_uuid(), 'Sonoran Spin Endurance', '11111111-1111-4111-8111-111111111111', 'Mesa',
        date_trunc('day', now()) + interval '4 days 15 hours', 60, 20);
