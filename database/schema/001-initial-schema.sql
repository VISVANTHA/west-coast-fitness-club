-- West Coast Fitness Club: initial PostgreSQL 16 schema.
-- Mirrors the EF Core model in WestCoastFitness.Infrastructure (verified with
-- `dotnet ef dbcontext script`). Applied automatically by docker-compose on first start.

CREATE TABLE membership_plans (
    id uuid NOT NULL,
    name character varying(80) NOT NULL,
    monthly_price numeric(10,2) NOT NULL,
    max_classes_per_week integer,
    CONSTRAINT "PK_membership_plans" PRIMARY KEY (id),
    CONSTRAINT ck_membership_plans_price CHECK (monthly_price >= 0)
);

CREATE TABLE trainers (
    id uuid NOT NULL,
    full_name character varying(120) NOT NULL,
    specialty character varying(120) NOT NULL,
    CONSTRAINT "PK_trainers" PRIMARY KEY (id)
);

CREATE TABLE members (
    id uuid NOT NULL,
    first_name character varying(80) NOT NULL,
    last_name character varying(80) NOT NULL,
    email character varying(254) NOT NULL,
    phone character varying(30) NOT NULL,
    membership_plan_id uuid NOT NULL,
    joined_on timestamp with time zone NOT NULL,
    CONSTRAINT "PK_members" PRIMARY KEY (id),
    CONSTRAINT "FK_members_membership_plans_membership_plan_id" FOREIGN KEY (membership_plan_id) REFERENCES membership_plans (id) ON DELETE RESTRICT
);

CREATE TABLE classes (
    id uuid NOT NULL,
    title character varying(120) NOT NULL,
    trainer_id uuid NOT NULL,
    location character varying(80) NOT NULL,
    starts_at timestamp with time zone NOT NULL,
    duration_minutes integer NOT NULL,
    capacity integer NOT NULL,
    CONSTRAINT "PK_classes" PRIMARY KEY (id),
    CONSTRAINT "FK_classes_trainers_trainer_id" FOREIGN KEY (trainer_id) REFERENCES trainers (id) ON DELETE RESTRICT,
    CONSTRAINT ck_classes_capacity CHECK (capacity > 0),
    CONSTRAINT ck_classes_duration CHECK (duration_minutes > 0)
);

CREATE TABLE payments (
    id uuid NOT NULL,
    member_id uuid NOT NULL,
    amount numeric(10,2) NOT NULL,
    currency character varying(3) NOT NULL,
    description character varying(200) NOT NULL,
    status character varying(20) NOT NULL,
    reference character varying(80) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_payments" PRIMARY KEY (id),
    CONSTRAINT "FK_payments_members_member_id" FOREIGN KEY (member_id) REFERENCES members (id) ON DELETE CASCADE,
    CONSTRAINT ck_payments_status CHECK (status IN ('Approved', 'Declined'))
    -- No card number, expiry or CVV column exists by design: the API stores only the gateway reference.
);

CREATE TABLE user_accounts (
    id uuid NOT NULL,
    email character varying(254) NOT NULL,
    password_hash character varying(500) NOT NULL,
    role character varying(20) NOT NULL,
    member_id uuid,
    CONSTRAINT "PK_user_accounts" PRIMARY KEY (id),
    CONSTRAINT "FK_user_accounts_members_member_id" FOREIGN KEY (member_id) REFERENCES members (id) ON DELETE CASCADE,
    CONSTRAINT ck_user_accounts_role CHECK (role IN ('Member', 'Administrator'))
);

CREATE TABLE class_bookings (
    id uuid NOT NULL,
    member_id uuid NOT NULL,
    fitness_class_id uuid NOT NULL,
    status character varying(20) NOT NULL,
    booked_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_class_bookings" PRIMARY KEY (id),
    CONSTRAINT "FK_class_bookings_classes_fitness_class_id" FOREIGN KEY (fitness_class_id) REFERENCES classes (id) ON DELETE CASCADE,
    CONSTRAINT "FK_class_bookings_members_member_id" FOREIGN KEY (member_id) REFERENCES members (id) ON DELETE CASCADE,
    CONSTRAINT ck_class_bookings_status CHECK (status IN ('Confirmed', 'Cancelled'))
);

CREATE INDEX "IX_class_bookings_fitness_class_id" ON class_bookings (fitness_class_id);

-- A member can hold only one active booking per class.
CREATE UNIQUE INDEX ux_class_bookings_active_member_class ON class_bookings (member_id, fitness_class_id) WHERE status = 'Confirmed';

CREATE INDEX "IX_classes_starts_at" ON classes (starts_at);

CREATE INDEX "IX_classes_trainer_id" ON classes (trainer_id);

CREATE UNIQUE INDEX "IX_members_email" ON members (email);

CREATE INDEX "IX_members_membership_plan_id" ON members (membership_plan_id);

CREATE UNIQUE INDEX "IX_membership_plans_name" ON membership_plans (name);

CREATE INDEX "IX_payments_member_id" ON payments (member_id);

CREATE UNIQUE INDEX "IX_user_accounts_email" ON user_accounts (email);

CREATE INDEX "IX_user_accounts_member_id" ON user_accounts (member_id);
