-- ============================================================
-- FIXED / CLEANED DDL (runs as-is)
-- - creates missing schemas (loinc, med)
-- - enables pg_trgm (for gin_trgm_ops)
-- - fixes broken/duplicated CREATE TABLE blocks
-- - removes impossible FK: NOT NULL + ON DELETE SET NULL
-- - normalizes: tenant-scoped patient & studyname
-- - uses IDENTITY instead of manual nextval sequences
-- ============================================================

CREATE SCHEMA IF NOT EXISTS app;
CREATE SCHEMA IF NOT EXISTS loinc;
CREATE SCHEMA IF NOT EXISTS med;

CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- =============================
-- Tenancy (multi-PACS per account)
-- =============================
CREATE TABLE IF NOT EXISTS app.tenant
(
    id         bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    account_id bigint NOT NULL,
    pacs_key   text   NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_tenant_account_pacs UNIQUE (account_id, pacs_key)
)
TABLESPACE pg_default;

ALTER TABLE app.tenant OWNER TO postgres;

-- =============================
-- LOINC tables
-- =============================
CREATE TABLE IF NOT EXISTS loinc.loinc_term
(
    loinc_num text NOT NULL,
    component text,
    property text,
    time_aspct text,
    system text,
    scale_typ text,
    method_typ text,
    class text,
    version_last_changed text,
    chng_type text,
    definition_description text,
    status text,
    consumer_name text,
    classtype text,
    formula text,
    exmpl_answers text,
    survey_quest_text text,
    survey_quest_src text,
    unitsrequired text,
    relatednames2 text,
    shortname text,
    order_obs text,
    hl7_field_subfield_id text,
    external_copyright_notice text,
    example_units text,
    long_common_name text,
    example_ucum_units text,
    status_reason text,
    status_text text,
    change_reason_public text,
    common_test_rank text,
    common_order_rank text,
    hl7_attachment_structure text,
    external_copyright_link text,
    paneltype text,
    ask_at_order_entry text,
    associated_observations text,
    version_first_released text,
    valid_hl7_attachment_request text,
    display_name text,
    CONSTRAINT loinc_term_pkey PRIMARY KEY (loinc_num)
)
TABLESPACE pg_default;

ALTER TABLE loinc.loinc_term OWNER TO postgres;

-- Trigram indexes
CREATE INDEX IF NOT EXISTS idx_loinc_component_trgm
    ON loinc.loinc_term USING gin (component gin_trgm_ops)
    TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS idx_loinc_lcn_trgm
    ON loinc.loinc_term USING gin (long_common_name gin_trgm_ops)
    TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS idx_loinc_system_trgm
    ON loinc.loinc_term USING gin (system gin_trgm_ops)
    TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS idx_loinc_term_class
    ON loinc.loinc_term USING btree (class ASC NULLS LAST)
    TABLESPACE pg_default;

-- map_to
CREATE TABLE IF NOT EXISTS loinc.map_to
(
    loinc   text NOT NULL,
    map_to  text NOT NULL,
    comment text,
    CONSTRAINT map_to_pkey PRIMARY KEY (loinc, map_to),
    CONSTRAINT map_to_loinc_fkey FOREIGN KEY (loinc)
        REFERENCES loinc.loinc_term (loinc_num)
        ON DELETE CASCADE,
    CONSTRAINT map_to_map_to_fkey FOREIGN KEY (map_to)
        REFERENCES loinc.loinc_term (loinc_num)
        ON DELETE CASCADE
)
TABLESPACE pg_default;

ALTER TABLE loinc.map_to OWNER TO postgres;

-- part
CREATE TABLE IF NOT EXISTS loinc.part
(
    part_number       text NOT NULL,
    part_type_name    text,
    part_name         text,
    part_display_name text,
    status            text,
    CONSTRAINT part_pkey PRIMARY KEY (part_number)
)
TABLESPACE pg_default;

ALTER TABLE loinc.part OWNER TO postgres;

-- rplaybook
CREATE TABLE IF NOT EXISTS loinc.rplaybook
(
    rp_id              bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    loinc_number       text NOT NULL,
    long_common_name   text,
    part_number        text NOT NULL,
    part_type_name     text,
    part_name          text,
    part_sequence_order text NOT NULL,
    rid                text,
    preferred_name     text,
    rpid               text,
    long_name          text,

    CONSTRAINT rplaybook_loinc_number_fkey FOREIGN KEY (loinc_number)
        REFERENCES loinc.loinc_term (loinc_num)
        ON DELETE CASCADE,

    -- FIX: part_number is NOT NULL, so ON DELETE SET NULL is impossible.
    -- Use RESTRICT (or CASCADE if you prefer).
    CONSTRAINT rplaybook_part_number_fkey FOREIGN KEY (part_number)
        REFERENCES loinc.part (part_number)
        ON DELETE RESTRICT
)
TABLESPACE pg_default;

ALTER TABLE loinc.rplaybook OWNER TO postgres;

CREATE INDEX IF NOT EXISTS idx_rplaybook_loinc
    ON loinc.rplaybook USING btree (loinc_number ASC NULLS LAST)
    TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS idx_rplaybook_partseq
    ON loinc.rplaybook USING btree (part_sequence_order ASC NULLS LAST)
    TABLESPACE pg_default;

-- =============================
-- med schema tables (tenant-scoped)
-- =============================

-- Patients
CREATE TABLE IF NOT EXISTS med.patient
(
    id             bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    tenant_id      bigint NOT NULL REFERENCES app.tenant(id) ON DELETE RESTRICT,
    patient_number text NOT NULL,
    patient_name   text NOT NULL,
    is_male        boolean NOT NULL,
    birth_date     date,
    created_at     timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_patient__tenant_patient_number UNIQUE (tenant_id, patient_number)
)
TABLESPACE pg_default;

ALTER TABLE med.patient OWNER TO postgres;

-- Study name / report container (your original had rad_report + rad_studyname mixed)
CREATE TABLE IF NOT EXISTS med.rad_studyname
(
    id         bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    tenant_id  bigint NOT NULL REFERENCES app.tenant(id) ON DELETE RESTRICT,
    studyname  text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_rad_studyname UNIQUE (tenant_id, studyname)
)
TABLESPACE pg_default;

ALTER TABLE med.rad_studyname OWNER TO postgres;

-- Studies
CREATE TABLE IF NOT EXISTS med.rad_study
(
    id            bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    tenant_id     bigint NOT NULL REFERENCES app.tenant(id) ON DELETE RESTRICT,
    patient_id    bigint NOT NULL REFERENCES med.patient(id) ON DELETE CASCADE,
    studyname_id  bigint NOT NULL REFERENCES med.rad_studyname(id) ON DELETE RESTRICT,
    study_datetime timestamptz NOT NULL,
    created_at    timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_rad_study UNIQUE (tenant_id, patient_id, studyname_id, study_datetime)
)
TABLESPACE pg_default;

ALTER TABLE med.rad_study OWNER TO postgres;

CREATE INDEX IF NOT EXISTS idx_study_patient_when
    ON med.rad_study USING btree (patient_id ASC NULLS LAST, study_datetime DESC NULLS FIRST)
    TABLESPACE pg_default;

-- Reports
CREATE TABLE IF NOT EXISTS med.rad_report
(
    id            bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    tenant_id     bigint NOT NULL REFERENCES app.tenant(id) ON DELETE RESTRICT,
    study_id      bigint NOT NULL REFERENCES med.rad_study(id) ON DELETE CASCADE,
    is_created    boolean NOT NULL DEFAULT true,
    is_mine       boolean NOT NULL DEFAULT false,
    created_by    text,
    report_datetime timestamptz,
    created_at    timestamptz NOT NULL DEFAULT now(),
    report        jsonb NOT NULL DEFAULT '{}'::jsonb,
    CONSTRAINT uq_rad_report__studyid_reportdt UNIQUE (study_id, report_datetime)
)
TABLESPACE pg_default;

ALTER TABLE med.rad_report OWNER TO postgres;

CREATE INDEX IF NOT EXISTS idx_rad_report_report_gin
    ON med.rad_report USING gin (report)
    TABLESPACE pg_default;

-- =============================
-- Technique components (tenant/account scoped as in your text)
-- =============================

CREATE TABLE IF NOT EXISTS med.rad_technique_prefix
(
    id           bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    account_id   bigint NOT NULL,
    prefix_text  text NOT NULL,
    display_order integer NOT NULL DEFAULT 0,
    created_at   timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_rad_technique_prefix_text UNIQUE (account_id, prefix_text)
)
TABLESPACE pg_default;

ALTER TABLE med.rad_technique_prefix OWNER TO postgres;

CREATE TABLE IF NOT EXISTS med.rad_technique_suffix
(
    id           bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    account_id   bigint NOT NULL,
    suffix_text  text NOT NULL,
    display_order integer NOT NULL DEFAULT 0,
    created_at   timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_rad_technique_suffix_text UNIQUE (account_id, suffix_text)
)
TABLESPACE pg_default;

ALTER TABLE med.rad_technique_suffix OWNER TO postgres;

CREATE TABLE IF NOT EXISTS med.rad_technique_tech
(
    id           bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    account_id   bigint NOT NULL,
    tech_text    text NOT NULL,
    display_order integer NOT NULL DEFAULT 0,
    created_at   timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_rad_technique_tech_text UNIQUE (account_id, tech_text)
)
TABLESPACE pg_default;

ALTER TABLE med.rad_technique_tech OWNER TO postgres;

-- Technique combination (account-scoped)
CREATE TABLE IF NOT EXISTS med.rad_technique_combination
(
    id              bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    account_id      bigint NOT NULL,
    combination_name text,
    created_at      timestamptz NOT NULL DEFAULT now()
)
TABLESPACE pg_default;

ALTER TABLE med.rad_technique_combination OWNER TO postgres;

-- Individual technique (prefix + tech + suffix), account-scoped
CREATE TABLE IF NOT EXISTS med.rad_technique
(
    id         bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    account_id bigint NOT NULL,
    prefix_id  bigint REFERENCES med.rad_technique_prefix(id) ON DELETE RESTRICT,
    tech_id    bigint NOT NULL REFERENCES med.rad_technique_tech(id) ON DELETE RESTRICT,
    suffix_id  bigint REFERENCES med.rad_technique_suffix(id) ON DELETE RESTRICT,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_rad_technique_components UNIQUE (account_id, prefix_id, tech_id, suffix_id)
)
TABLESPACE pg_default;

ALTER TABLE med.rad_technique OWNER TO postgres;

CREATE INDEX IF NOT EXISTS idx_rad_technique_tech_id
    ON med.rad_technique USING btree (tech_id ASC NULLS LAST)
    TABLESPACE pg_default;

-- Combination items (account-scoped; points to combination + technique)
CREATE TABLE IF NOT EXISTS med.rad_technique_combination_item
(
    id            bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    combination_id bigint NOT NULL REFERENCES med.rad_technique_combination(id) ON DELETE CASCADE,
    technique_id   bigint NOT NULL REFERENCES med.rad_technique(id) ON DELETE RESTRICT,
    sequence_order integer NOT NULL DEFAULT 0,
    created_at     timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_rad_technique_combination_item UNIQUE (combination_id, technique_id, sequence_order)
)
TABLESPACE pg_default;

ALTER TABLE med.rad_technique_combination_item OWNER TO postgres;

CREATE INDEX IF NOT EXISTS idx_rad_technique_combination_item_combination
    ON med.rad_technique_combination_item USING btree (combination_id ASC NULLS LAST)
    TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS idx_rad_technique_combination_item_technique
    ON med.rad_technique_combination_item USING btree (technique_id ASC NULLS LAST)
    TABLESPACE pg_default;

-- Study-technique link (tenant-scoped studies, points to combination)
CREATE TABLE IF NOT EXISTS med.rad_study_technique_combination
(
    id            bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    study_id      bigint NOT NULL REFERENCES med.rad_study(id) ON DELETE CASCADE,
    combination_id bigint NOT NULL REFERENCES med.rad_technique_combination(id) ON DELETE CASCADE,
    is_default    boolean NOT NULL DEFAULT false,
    created_at    timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_studyname_combination UNIQUE (study_id, combination_id)
)
TABLESPACE pg_default;

ALTER TABLE med.rad_study_technique_combination OWNER TO postgres;

CREATE INDEX IF NOT EXISTS idx_rad_study_technique_combination_combination
    ON med.rad_study_technique_combination USING btree (combination_id ASC NULLS LAST)
    TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS idx_rad_study_technique_combination_study
    ON med.rad_study_technique_combination USING btree (study_id ASC NULLS LAST)
    TABLESPACE pg_default;

-- =============================
-- Views
-- =============================

CREATE OR REPLACE VIEW med.v_technique_display AS
SELECT t.id,
       COALESCE(tp.prefix_text, '') AS prefix_text,
       tt.tech_text,
       COALESCE(ts.suffix_text, '') AS suffix_text,
       trim(concat_ws(' ', NULLIF(tp.prefix_text,''), tt.tech_text, NULLIF(ts.suffix_text,''))) AS technique_display
FROM med.rad_technique t
LEFT JOIN med.rad_technique_prefix tp ON tp.id = t.prefix_id
JOIN med.rad_technique_tech tt ON tt.id = t.tech_id
LEFT JOIN med.rad_technique_suffix ts ON ts.id = t.suffix_id;

CREATE OR REPLACE VIEW med.v_technique_combination_display AS
SELECT c.id,
       c.combination_name,
       string_agg(v.technique_display, ' + ' ORDER BY i.sequence_order) AS combination_display
FROM med.rad_technique_combination c
JOIN med.rad_technique_combination_item i ON i.combination_id = c.id
JOIN med.v_technique_display v ON v.id = i.technique_id
GROUP BY c.id, c.combination_name;
