-- Bootstrap EF Core migration history for existing databases
--
-- Run this ONCE on any database that was previously managed by dbmate.
-- It seeds the __EFMigrationsHistory table so EF Core knows the full
-- current schema is already applied and skips the InitialSchema migration.
--
-- Note: UseSnakeCaseNamingConvention() applies to the history table columns
-- too, so column names are migration_id and product_version (snake_case).
--
-- The ExpandFileStorageSchema migration will still run automatically on
-- startup, adding the new file_storage columns that were not in the
-- original dbmate schema.

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY (migration_id)
);

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20260427153635_InitialSchema', '10.0.4')
ON CONFLICT DO NOTHING;
