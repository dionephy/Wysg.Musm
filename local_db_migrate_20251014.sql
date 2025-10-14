-- =============================
-- Migration from before to after schema
-- =============================

-- 1. Create new tenant table for multi-PACS support
CREATE TABLE IF NOT EXISTS app.tenant
(
    id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    account_id  bigint NOT NULL,
    pacs_key    text   NOT NULL,
    created_at  timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT uq_tenant_account_pacs UNIQUE (account_id, pacs_key)
) TABLESPACE pg_default;

ALTER TABLE app.tenant OWNER TO postgres;

-- 2. Add tenant_id to patient table
ALTER TABLE med.patient ADD COLUMN tenant_id bigint;

-- Update existing patients (you'll need to decide how to assign tenant_id for existing data)
-- Example: UPDATE med.patient SET tenant_id = 1 WHERE tenant_id IS NULL;

ALTER TABLE med.patient ALTER COLUMN tenant_id SET NOT NULL;
ALTER TABLE med.patient ADD CONSTRAINT fk_patient_tenant 
    FOREIGN KEY (tenant_id) REFERENCES app.tenant(id) ON DELETE RESTRICT;

-- Drop old unique constraint and add new one with tenant_id
ALTER TABLE med.patient DROP CONSTRAINT patient_patient_number_key;
ALTER TABLE med.patient ADD CONSTRAINT uq_patient__tenant_patient_number 
    UNIQUE (tenant_id, patient_number);

-- 3. Rename technique tables to rad_technique_* and add account_id
-- Create new rad_technique_prefix table
CREATE TABLE IF NOT EXISTS med.rad_technique_prefix
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    account_id bigint NOT NULL,
    prefix_text text COLLATE pg_catalog."default" NOT NULL,
    display_order integer NOT NULL DEFAULT 0,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT rad_technique_prefix_pkey PRIMARY KEY (id),
    CONSTRAINT uq_rad_technique_prefix_text UNIQUE (account_id, prefix_text)
) TABLESPACE pg_default;

ALTER TABLE med.rad_technique_prefix OWNER to postgres;

-- Migrate data from old technique_prefix to new rad_technique_prefix
-- You'll need to decide account_id for existing data
-- INSERT INTO med.rad_technique_prefix (account_id, prefix_text, display_order, created_at)
-- SELECT 1, prefix_text, display_order, created_at FROM med.technique_prefix;

-- Create new rad_technique_tech table
CREATE TABLE IF NOT EXISTS med.rad_technique_tech
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    account_id bigint NOT NULL,
    tech_text text COLLATE pg_catalog."default" NOT NULL,
    display_order integer NOT NULL DEFAULT 0,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT rad_technique_tech_pkey PRIMARY KEY (id),
    CONSTRAINT uq_rad_technique_tech_text UNIQUE (account_id, tech_text)
) TABLESPACE pg_default;

ALTER TABLE med.rad_technique_tech OWNER to postgres;

-- Migrate data from old technique_tech
-- INSERT INTO med.rad_technique_tech (account_id, tech_text, display_order, created_at)
-- SELECT 1, tech_text, display_order, created_at FROM med.technique_tech;

-- Create new rad_technique_suffix table
CREATE TABLE IF NOT EXISTS med.rad_technique_suffix
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    account_id bigint NOT NULL,
    suffix_text text COLLATE pg_catalog."default" NOT NULL,
    display_order integer NOT NULL DEFAULT 0,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT rad_technique_suffix_pkey PRIMARY KEY (id),
    CONSTRAINT uq_rad_technique_suffix_text UNIQUE (account_id, suffix_text)
) TABLESPACE pg_default;

ALTER TABLE med.rad_technique_suffix OWNER to postgres;

-- Migrate data from old technique_suffix
-- INSERT INTO med.rad_technique_suffix (account_id, suffix_text, display_order, created_at)
-- SELECT 1, suffix_text, display_order, created_at FROM med.technique_suffix;

-- 4. Create new rad_technique table (replaces technique)
CREATE TABLE IF NOT EXISTS med.rad_technique
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    account_id bigint NOT NULL,
    prefix_id bigint,
    tech_id bigint NOT NULL,
    suffix_id bigint,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT rad_technique_pkey PRIMARY KEY (id),
    CONSTRAINT uq_rad_technique_components UNIQUE (account_id, prefix_id, tech_id, suffix_id),
    CONSTRAINT rad_technique_prefix_id_fkey FOREIGN KEY (prefix_id)
        REFERENCES med.rad_technique_prefix (id) MATCH SIMPLE ON UPDATE NO ACTION ON DELETE RESTRICT,
    CONSTRAINT rad_technique_suffix_id_fkey FOREIGN KEY (suffix_id)
        REFERENCES med.rad_technique_suffix (id) MATCH SIMPLE ON UPDATE NO ACTION ON DELETE RESTRICT,
    CONSTRAINT rad_technique_tech_id_fkey FOREIGN KEY (tech_id)
        REFERENCES med.rad_technique_tech (id) MATCH SIMPLE ON UPDATE NO ACTION ON DELETE RESTRICT
) TABLESPACE pg_default;

ALTER TABLE med.rad_technique OWNER to postgres;

CREATE INDEX IF NOT EXISTS idx_rad_technique_tech_id
    ON med.rad_technique USING btree (tech_id ASC NULLS LAST);

-- Migrate data from old technique table
-- INSERT INTO med.rad_technique (account_id, prefix_id, tech_id, suffix_id, created_at)
-- SELECT 1, prefix_id, tech_id, suffix_id, created_at FROM med.technique;

-- 5. Create new rad_technique_combination table (replaces technique_combination)
CREATE TABLE IF NOT EXISTS med.rad_technique_combination
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    account_id bigint NOT NULL,
    combination_name text COLLATE pg_catalog."default",
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT rad_technique_combination_pkey PRIMARY KEY (id)
) TABLESPACE pg_default;

ALTER TABLE med.rad_technique_combination OWNER to postgres;

-- Migrate data
-- INSERT INTO med.rad_technique_combination (account_id, combination_name, created_at)
-- SELECT 1, combination_name, created_at FROM med.technique_combination;

-- 6. Create new rad_technique_combination_item (replaces technique_combination_item)
CREATE TABLE IF NOT EXISTS med.rad_technique_combination_item
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    combination_id bigint NOT NULL,
    technique_id bigint NOT NULL,
    sequence_order integer NOT NULL DEFAULT 0,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT rad_technique_combination_item_pkey PRIMARY KEY (id),
    CONSTRAINT uq_rad_technique_combination_item UNIQUE (combination_id, technique_id, sequence_order),
    CONSTRAINT rad_technique_combination_item_combination_id_fkey FOREIGN KEY (combination_id)
        REFERENCES med.rad_technique_combination (id) MATCH SIMPLE ON UPDATE NO ACTION ON DELETE CASCADE,
    CONSTRAINT rad_technique_combination_item_technique_id_fkey FOREIGN KEY (technique_id)
        REFERENCES med.rad_technique (id) MATCH SIMPLE ON UPDATE NO ACTION ON DELETE RESTRICT
) TABLESPACE pg_default;

ALTER TABLE med.rad_technique_combination_item OWNER to postgres;

CREATE INDEX IF NOT EXISTS idx_rad_technique_combination_item_combination
    ON med.rad_technique_combination_item USING btree (combination_id ASC NULLS LAST);
CREATE INDEX IF NOT EXISTS idx_rad_technique_combination_item_technique
    ON med.rad_technique_combination_item USING btree (technique_id ASC NULLS LAST);

-- 7. Update rad_studyname to add tenant_id
ALTER TABLE med.rad_studyname ADD COLUMN tenant_id bigint;

-- Update existing studynames
-- UPDATE med.rad_studyname SET tenant_id = 1 WHERE tenant_id IS NULL;

ALTER TABLE med.rad_studyname ALTER COLUMN tenant_id SET NOT NULL;
ALTER TABLE med.rad_studyname ADD CONSTRAINT fk_rad_studyname_tenant 
    FOREIGN KEY (tenant_id) REFERENCES app.tenant(id) ON DELETE RESTRICT;

-- Drop old unique constraint and add new one with tenant_id
ALTER TABLE med.rad_studyname DROP CONSTRAINT uq_rad_studyname;
ALTER TABLE med.rad_studyname ADD CONSTRAINT uq_rad_studyname 
    UNIQUE (tenant_id, studyname);

-- 8. Update foreign key references in rad_studyname_technique_combination
ALTER TABLE med.rad_studyname_technique_combination 
    DROP CONSTRAINT rad_studyname_technique_combination_combination_id_fkey;
ALTER TABLE med.rad_studyname_technique_combination 
    ADD CONSTRAINT rad_studyname_technique_combination_combination_id_fkey 
    FOREIGN KEY (combination_id) REFERENCES med.rad_technique_combination (id) 
    MATCH SIMPLE ON UPDATE NO ACTION ON DELETE CASCADE;

-- 9. Update foreign key references in rad_study_technique_combination
ALTER TABLE med.rad_study_technique_combination 
    DROP CONSTRAINT rad_study_technique_combination_combination_id_fkey;
ALTER TABLE med.rad_study_technique_combination 
    ADD CONSTRAINT rad_study_technique_combination_combination_id_fkey 
    FOREIGN KEY (combination_id) REFERENCES med.rad_technique_combination (id) 
    MATCH SIMPLE ON UPDATE NO ACTION ON DELETE RESTRICT;



    -- 10. Create views for technique display
    BEGIN;
-- Drop dependent first (or cascade on the base) 
DROP VIEW IF EXISTS med.v_technique_combination_display; DROP VIEW IF EXISTS med.v_technique_display;
-- Recreate base view 
CREATE VIEW med.v_technique_display AS SELECT t.id, COALESCE(tp.prefix_text, '') AS prefix_text, tt.tech_text, COALESCE(ts.suffix_text, '') AS suffix_text, trim(concat_ws(' ', NULLIF(tp.prefix_text,''), tt.tech_text, NULLIF(ts.suffix_text,''))) AS technique_display FROM med.rad_technique t LEFT JOIN med.rad_technique_prefix tp ON tp.id = t.prefix_id JOIN med.rad_technique_tech tt ON tt.id = t.tech_id LEFT JOIN med.rad_technique_suffix ts ON ts.id = t.suffix_id;
-- Recreate dependent view 
CREATE VIEW med.v_technique_combination_display AS SELECT c.id, c.combination_name, string_agg(v.technique_display, ' + ' ORDER BY i.sequence_order) AS combination_display FROM med.rad_technique_combination c JOIN med.rad_technique_combination_item i ON i.combination_id = c.id JOIN med.v_technique_display v ON v.id = i.technique_id GROUP BY c.id, c.combination_name;
COMMIT;



    -- 11. Drop old technique tables (after data migration is confirmed)
-- DROP TABLE IF EXISTS med.technique_combination_item CASCADE;
-- DROP TABLE IF EXISTS med.technique_combination CASCADE;
-- DROP TABLE IF EXISTS med.technique CASCADE;
-- DROP TABLE IF EXISTS med.technique_prefix CASCADE;
-- DROP TABLE IF EXISTS med.technique_tech CASCADE;
-- DROP TABLE IF EXISTS med.technique_suffix CASCADE;