-- One-time fix-up for the production Supabase database, run once before the Baseline migration is recorded as applied.
--
-- The schema was originally built by hand, so it differs from what the Baseline migration describes
-- (checked against the live database on 2026-11-03):
--   * the foreign-key indexes EF expects (IX_...) were never created;
--   * uniqueness is enforced by UNIQUE constraints where EF expects unique indexes of the same name,
--     and EF's later migrations drop them as indexes (e.g. ClientPhoneUniquePerStudio drops client_phone_key);
--   * the invitation's unique constraints have other names than EF's indexes;
--   * booking_service.updated_at is "timestamp without time zone" instead of timestamptz.
-- Differences that are kept on purpose: extra column defaults (EF always sends values), the CHECK on
-- studio_invitation.status, role.id without an identity, and d."user".id's foreign key to auth.users.
--
-- Everything runs in one transaction: if any statement fails, nothing is changed.

BEGIN;

-- Unique constraints become unique indexes with the same name, as the Baseline creates them
ALTER TABLE d.booking_status DROP CONSTRAINT booking_status_name_key;
CREATE UNIQUE INDEX booking_status_name_key ON d.booking_status (name);

ALTER TABLE d.client DROP CONSTRAINT client_phone_key;
CREATE UNIQUE INDEX client_phone_key ON d.client (phone);

ALTER TABLE d.payment DROP CONSTRAINT payment_stripe_payment_intent_id_key;
CREATE UNIQUE INDEX payment_stripe_payment_intent_id_key ON d.payment (stripe_payment_intent_id);

ALTER TABLE d.role DROP CONSTRAINT role_role_name_key;
CREATE UNIQUE INDEX role_role_name_key ON d.role (role_name);

ALTER TABLE d.stripe_event DROP CONSTRAINT stripe_event_stripe_event_id_key;
CREATE UNIQUE INDEX stripe_event_stripe_event_id_key ON d.stripe_event (stripe_event_id);

ALTER TABLE d.studio DROP CONSTRAINT studio_stripe_customer_id_key;
CREATE UNIQUE INDEX studio_stripe_customer_id_key ON d.studio (stripe_customer_id);

ALTER TABLE d.studio_subscription DROP CONSTRAINT studio_subscription_stripe_subscription_id_key;
CREATE UNIQUE INDEX studio_subscription_stripe_subscription_id_key ON d.studio_subscription (stripe_subscription_id);

ALTER TABLE d.subscription_plan DROP CONSTRAINT subscription_plan_stripe_price_id_key;
CREATE UNIQUE INDEX subscription_plan_stripe_price_id_key ON d.subscription_plan (stripe_price_id);

ALTER TABLE d.subscription_plan DROP CONSTRAINT subscription_plan_stripe_product_id_key;
CREATE UNIQUE INDEX subscription_plan_stripe_product_id_key ON d.subscription_plan (stripe_product_id);

ALTER TABLE d."user" DROP CONSTRAINT user_email_key;
CREATE UNIQUE INDEX user_email_key ON d."user" (email);

-- The invitation's uniqueness, under EF's index names
ALTER TABLE d.studio_invitation DROP CONSTRAINT studio_invitation_studio_id_email_key;
CREATE UNIQUE INDEX "IX_studio_invitation_studio_id_email" ON d.studio_invitation (studio_id, email);

ALTER TABLE d.studio_invitation DROP CONSTRAINT studio_invitation_token_key;
CREATE UNIQUE INDEX "IX_studio_invitation_token" ON d.studio_invitation (token);

-- Foreign-key indexes the Baseline creates
CREATE INDEX "IX_booking_client_id" ON d.booking (client_id);
CREATE INDEX "IX_booking_created_by" ON d.booking (created_by);
CREATE INDEX "IX_booking_status_id" ON d.booking (status_id);
CREATE INDEX "IX_booking_studio_id" ON d.booking (studio_id);
CREATE INDEX "IX_booking_vehicle_id" ON d.booking (vehicle_id);
CREATE INDEX "IX_booking_service_service_id" ON d.booking_service (service_id);
CREATE INDEX "IX_client_studio_id" ON d.client (studio_id);
CREATE INDEX "IX_inventory_category_id" ON d.inventory (category_id);
CREATE INDEX "IX_inventory_studio_id" ON d.inventory (studio_id);
CREATE INDEX "IX_inventory_category_studio_id" ON d.inventory_category (studio_id);
CREATE INDEX "IX_payment_studio_id" ON d.payment (studio_id);
CREATE INDEX "IX_payment_subscription_id" ON d.payment (subscription_id);
CREATE INDEX "IX_service_studio_id" ON d.service (studio_id);
CREATE INDEX "IX_stripe_event_studio_id" ON d.stripe_event (studio_id);
CREATE INDEX "IX_stripe_event_subscription_id" ON d.stripe_event (subscription_id);
CREATE INDEX "IX_studio_invitation_invited_by" ON d.studio_invitation (invited_by);
CREATE INDEX "IX_studio_invitation_role_id" ON d.studio_invitation (role_id);
CREATE INDEX "IX_studio_member_role_id" ON d.studio_member (role_id);
CREATE INDEX "IX_studio_member_studio_id" ON d.studio_member (studio_id);
CREATE INDEX "IX_studio_subscription_plan_id" ON d.studio_subscription (plan_id);
CREATE INDEX "IX_studio_subscription_studio_id" ON d.studio_subscription (studio_id);
CREATE INDEX "IX_vehicle_client_id" ON d.vehicle (client_id);
CREATE INDEX "IX_vehicle_studio_id" ON d.vehicle (studio_id);

-- The stored values are UTC (the application only writes UTC), so they are read as UTC
ALTER TABLE d.booking_service
    ALTER COLUMN updated_at TYPE timestamp with time zone USING updated_at AT TIME ZONE 'UTC';

-- Record the Baseline as applied in place of the old InitialSync; the schema now matches it
DELETE FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260301134918_InitialSync';
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261026120857_Baseline', '10.0.3');

COMMIT;
