CREATE SCHEMA IF NOT EXISTS app;

-- =============================
-- Tenancy (multi-PACS per account)
-- =============================
CREATE TABLE IF NOT EXISTS app.tenant
(
    id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    account_id  bigint NOT NULL,
    pacs_key    text   NOT NULL,
    created_at  timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT uq_tenant_account_pacs UNIQUE (account_id, pacs_key)
)
TABLESPACE pg_default;

ALTER TABLE app.tenant OWNER TO postgres;

-- =============================
-- LOINC tables (unchanged)
-- =============================
CREATE TABLE IF NOT EXISTS loinc.loinc_term
(
    loinc_num text COLLATE pg_catalog."default" NOT NULL,
    component text COLLATE pg_catalog."default",
    property text COLLATE pg_catalog."default",
    time_aspct text COLLATE pg_catalog."default",
    system text COLLATE pg_catalog."default",
    scale_typ text COLLATE pg_catalog."default",
    method_typ text COLLATE pg_catalog."default",
    class text COLLATE pg_catalog."default",
    version_last_changed text COLLATE pg_catalog."default",
    chng_type text COLLATE pg_catalog."default",
    definition_description text COLLATE pg_catalog."default",
    status text COLLATE pg_catalog."default",
    consumer_name text COLLATE pg_catalog."default",
    classtype text COLLATE pg_catalog."default",
    formula text COLLATE pg_catalog."default",
    exmpl_answers text COLLATE pg_catalog."default",
    survey_quest_text text COLLATE pg_catalog."default",
    survey_quest_src text COLLATE pg_catalog."default",
    unitsrequired text COLLATE pg_catalog."default",
    relatednames2 text COLLATE pg_catalog."default",
    shortname text COLLATE pg_catalog."default",
    order_obs text COLLATE pg_catalog."default",
    hl7_field_subfield_id text COLLATE pg_catalog."default",
    external_copyright_notice text COLLATE pg_catalog."default",
    example_units text COLLATE pg_catalog."default",
    long_common_name text COLLATE pg_catalog."default",
    example_ucum_units text COLLATE pg_catalog."default",
    status_reason text COLLATE pg_catalog."default",
    status_text text COLLATE pg_catalog."default",
    change_reason_public text COLLATE pg_catalog."default",
    common_test_rank text COLLATE pg_catalog."default",
    common_order_rank text COLLATE pg_catalog."default",
    hl7_attachment_structure text COLLATE pg_catalog."default",
    external_copyright_link text COLLATE pg_catalog."default",
    paneltype text COLLATE pg_catalog."default",
    ask_at_order_entry text COLLATE pg_catalog."default",
    associated_observations text COLLATE pg_catalog."default",
    version_first_released text COLLATE pg_catalog."default",
    valid_hl7_attachment_request text COLLATE pg_catalog."default",
    display_name text COLLATE pg_catalog."default",
    CONSTRAINT loinc_term_pkey PRIMARY KEY (loinc_num)
)

TABLESPACE pg_default;

ALTER TABLE loinc.loinc_term
    OWNER to postgres;
ALTER TABLE loinc.loinc_term OWNER to postgres;

-- Index: loinc.idx_loinc_component_trgm
-- Indexes for loinc_term
CREATE INDEX IF NOT EXISTS idx_loinc_component_trgm
    ON loinc.loinc_term USING gin
    (component COLLATE pg_catalog."default" gin_trgm_ops)
    TABLESPACE pg_default;
-- Index: loinc.idx_loinc_lcn_trgm
CREATE INDEX IF NOT EXISTS idx_loinc_lcn_trgm
    ON loinc.loinc_term USING gin
    (long_common_name COLLATE pg_catalog."default" gin_trgm_ops)
    TABLESPACE pg_default;
-- Index: loinc.idx_loinc_system_trgm
CREATE INDEX IF NOT EXISTS idx_loinc_system_trgm
    ON loinc.loinc_term USING gin
    (system COLLATE pg_catalog."default" gin_trgm_ops)
    TABLESPACE pg_default;
-- Index: loinc.idx_loinc_term_class
CREATE INDEX IF NOT EXISTS idx_loinc_term_class
    ON loinc.loinc_term USING btree
    (class COLLATE pg_catalog."default" ASC NULLS LAST)
    TABLESPACE pg_default;



CREATE TABLE IF NOT EXISTS loinc.map_to
(
    loinc text COLLATE pg_catalog."default" NOT NULL,
    map_to text COLLATE pg_catalog."default" NOT NULL,
    comment text COLLATE pg_catalog."default",
    CONSTRAINT map_to_pkey PRIMARY KEY (loinc, map_to),
    CONSTRAINT map_to_loinc_fkey FOREIGN KEY (loinc)
        REFERENCES loinc.loinc_term (loinc_num) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE,
    CONSTRAINT map_to_map_to_fkey FOREIGN KEY (map_to)
        REFERENCES loinc.loinc_term (loinc_num) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)

TABLESPACE pg_default;

ALTER TABLE loinc.map_to
    OWNER to postgres;
ALTER TABLE loinc.map_to OWNER to postgres;




CREATE TABLE IF NOT EXISTS loinc.part
(
    part_number text COLLATE pg_catalog."default" NOT NULL,
    part_type_name text COLLATE pg_catalog."default",
    part_name text COLLATE pg_catalog."default",
    part_display_name text COLLATE pg_catalog."default",
    status text COLLATE pg_catalog."default",
    CONSTRAINT part_pkey PRIMARY KEY (part_number)
)

TABLESPACE pg_default;

ALTER TABLE loinc.part
    OWNER to postgres;
ALTER TABLE loinc.part OWNER to postgres;




CREATE TABLE IF NOT EXISTS loinc.rplaybook
(
    loinc_number text COLLATE pg_catalog."default" NOT NULL,
    long_common_name text COLLATE pg_catalog."default",
    part_number text COLLATE pg_catalog."default" NOT NULL,
    part_type_name text COLLATE pg_catalog."default",
    part_name text COLLATE pg_catalog."default",
    part_sequence_order text COLLATE pg_catalog."default" NOT NULL,
    rid text COLLATE pg_catalog."default",
    preferred_name text COLLATE pg_catalog."default",
    rpid text COLLATE pg_catalog."default",
    long_name text COLLATE pg_catalog."default",
    rp_id bigint NOT NULL DEFAULT nextval('loinc.rplaybook_rp_id_seq'::regclass),
    CONSTRAINT rplaybook_pkey PRIMARY KEY (rp_id),
    CONSTRAINT rplaybook_loinc_number_fkey FOREIGN KEY (loinc_number)
        REFERENCES loinc.loinc_term (loinc_num) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE,
    CONSTRAINT rplaybook_part_number_fkey FOREIGN KEY (part_number)
        REFERENCES loinc.part (part_number) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE SET NULL
)

TABLESPACE pg_default;

ALTER TABLE loinc.rplaybook
    OWNER to postgres;
ALTER TABLE loinc.rplaybook OWNER to postgres;

-- Index: loinc.idx_rplaybook_loinc
CREATE INDEX IF NOT EXISTS idx_rplaybook_loinc
    ON loinc.rplaybook USING btree
    (loinc_number COLLATE pg_catalog."default" ASC NULLS LAST)
    TABLESPACE pg_default;
-- Index: loinc.idx_rplaybook_partseq
CREATE INDEX IF NOT EXISTS idx_rplaybook_partseq
    ON loinc.rplaybook USING btree
    (part_sequence_order COLLATE pg_catalog."default" ASC NULLS LAST)
    TABLESPACE pg_default;

-- =============================
-- med schema tables (with tenant / technique changes)
-- =============================


-- Patients (scoped to tenant)
CREATE TABLE IF NOT EXISTS med.patient
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    tenant_id bigint NOT NULL,
    patient_number text COLLATE pg_catalog."default" NOT NULL,
    patient_name text COLLATE pg_catalog."default" NOT NULL,
    is_male boolean NOT NULL,
    birth_date date,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT patient_pkey PRIMARY KEY (id),
    CONSTRAINT patient_patient_number_key UNIQUE (patient_number)
    CONSTRAINT fk_patient_tenant FOREIGN KEY (tenant_id) REFERENCES app.tenant(id) ON DELETE RESTRICT,
    CONSTRAINT uq_patient__tenant_patient_number UNIQUE (tenant_id, patient_number)
)

TABLESPACE pg_default;

ALTER TABLE med.patient
    OWNER to postgres;
ALTER TABLE med.patient OWNER to postgres;




    CREATE TABLE IF NOT EXISTS med.rad_report
-- Studyname (scoped to tenant)
CREATE TABLE IF NOT EXISTS med.rad_studyname
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    study_id bigint NOT NULL,
    is_created boolean NOT NULL DEFAULT true,
    is_mine boolean NOT NULL DEFAULT false,
    created_by text COLLATE pg_catalog."default",
    report_datetime timestamp with time zone,
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    tenant_id bigint NOT NULL,
    studyname text COLLATE pg_catalog."default" NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    report jsonb NOT NULL DEFAULT '{}'::jsonb,
    CONSTRAINT rad_report_pkey PRIMARY KEY (id),
    CONSTRAINT uq_rad_report__studyid_reportdt UNIQUE (study_id, report_datetime),
    CONSTRAINT rad_report_study_id_fkey FOREIGN KEY (study_id)
        REFERENCES med.rad_study (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
    CONSTRAINT rad_studyname_pkey PRIMARY KEY (id),
    CONSTRAINT fk_rad_studyname_tenant FOREIGN KEY (tenant_id) REFERENCES app.tenant(id) ON DELETE RESTRICT,
    CONSTRAINT uq_rad_studyname UNIQUE (tenant_id, studyname)
)

TABLESPACE pg_default;

ALTER TABLE med.rad_report
    OWNER to postgres;
ALTER TABLE med.rad_studyname OWNER to postgres;

-- Index: med.idx_rad_report_report_gin
CREATE INDEX IF NOT EXISTS idx_rad_report_report_gin
    ON med.rad_report USING gin
    (report)
-- Technique components (account-scoped, renamed to rad_technique_*)
CREATE TABLE IF NOT EXISTS med.rad_technique_prefix
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    account_id bigint NOT NULL,
    prefix_text text COLLATE pg_catalog."default" NOT NULL,
    display_order integer NOT NULL DEFAULT 0,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT rad_technique_prefix_pkey PRIMARY KEY (id),
    CONSTRAINT uq_rad_technique_prefix_text UNIQUE (account_id, prefix_text)
)
TABLESPACE pg_default;

ALTER TABLE med.rad_technique_prefix OWNER to postgres;



    CREATE TABLE IF NOT EXISTS med.rad_study
CREATE TABLE IF NOT EXISTS med.rad_technique_suffix
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    patient_id bigint NOT NULL,
    studyname_id bigint NOT NULL,
    study_datetime timestamp with time zone NOT NULL,
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    account_id bigint NOT NULL,
    suffix_text text COLLATE pg_catalog."default" NOT NULL,
    display_order integer NOT NULL DEFAULT 0,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT rad_study_pkey PRIMARY KEY (id),
    CONSTRAINT uq_rad_study_patient_name_time UNIQUE (patient_id, studyname_id, study_datetime),
    CONSTRAINT rad_study_patient_id_fkey FOREIGN KEY (patient_id)
        REFERENCES med.patient (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE,
    CONSTRAINT rad_study_studyname_id_fkey FOREIGN KEY (studyname_id)
        REFERENCES med.rad_studyname (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE RESTRICT
    CONSTRAINT rad_technique_suffix_pkey PRIMARY KEY (id),
    CONSTRAINT uq_rad_technique_suffix_text UNIQUE (account_id, suffix_text)
)

TABLESPACE pg_default;

ALTER TABLE med.rad_study
    OWNER to postgres;
ALTER TABLE med.rad_technique_suffix OWNER to postgres;

-- Index: med.idx_study_patient_when
CREATE INDEX IF NOT EXISTS idx_study_patient_when
    ON med.rad_study USING btree
    (patient_id ASC NULLS LAST, study_datetime DESC NULLS FIRST)
CREATE TABLE IF NOT EXISTS med.rad_technique_tech
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    account_id bigint NOT NULL,
    tech_text text COLLATE pg_catalog."default" NOT NULL,
    display_order integer NOT NULL DEFAULT 0,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT rad_technique_tech_pkey PRIMARY KEY (id),
    CONSTRAINT uq_rad_technique_tech_text UNIQUE (account_id, tech_text)
)
TABLESPACE pg_default;

ALTER TABLE med.rad_technique_tech OWNER to postgres;




    CREATE TABLE IF NOT EXISTS med.rad_study_technique_combination
-- Individual technique (prefix + tech + suffix), account-scoped
CREATE TABLE IF NOT EXISTS med.rad_technique
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    study_id bigint NOT NULL,
    combination_id bigint NOT NULL,
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    account_id bigint NOT NULL,
    prefix_id bigint,
    tech_id bigint NOT NULL,
    suffix_id bigint,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT rad_study_technique_combination_pkey PRIMARY KEY (id),
    CONSTRAINT uq_study_combination UNIQUE (study_id),
    CONSTRAINT rad_study_technique_combination_combination_id_fkey FOREIGN KEY (combination_id)
        REFERENCES med.technique_combination (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE RESTRICT,
    CONSTRAINT rad_study_technique_combination_study_id_fkey FOREIGN KEY (study_id)
        REFERENCES med.rad_study (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
    CONSTRAINT rad_technique_pkey PRIMARY KEY (id),
    CONSTRAINT uq_rad_technique_components UNIQUE (account_id, prefix_id, tech_id, suffix_id),
    CONSTRAINT rad_technique_prefix_id_fkey FOREIGN KEY (prefix_id)
        REFERENCES med.rad_technique_prefix (id) MATCH SIMPLE ON UPDATE NO ACTION ON DELETE RESTRICT,
    CONSTRAINT rad_technique_suffix_id_fkey FOREIGN KEY (suffix_id)
        REFERENCES med.rad_technique_suffix (id) MATCH SIMPLE ON UPDATE NO ACTION ON DELETE RESTRICT,
    CONSTRAINT rad_technique_tech_id_fkey FOREIGN KEY (tech_id)
        REFERENCES med.rad_technique_tech (id) MATCH SIMPLE ON UPDATE NO ACTION ON DELETE RESTRICT
)

TABLESPACE pg_default;

ALTER TABLE med.rad_study_technique_combination
    OWNER to postgres;
ALTER TABLE med.rad_technique OWNER to postgres;

CREATE INDEX IF NOT EXISTS idx_rad_technique_tech_id
    ON med.rad_technique USING btree (tech_id ASC NULLS LAST)
    TABLESPACE pg_default;




    CREATE TABLE IF NOT EXISTS med.rad_studyname
-- Technique combination (account-scoped)
CREATE TABLE IF NOT EXISTS med.rad_technique_combination
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    studyname text COLLATE pg_catalog."default" NOT NULL,
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    account_id bigint NOT NULL,
    combination_name text COLLATE pg_catalog."default",
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT rad_studyname_pkey PRIMARY KEY (id),
    CONSTRAINT uq_rad_studyname UNIQUE (studyname)
    CONSTRAINT rad_technique_combination_pkey PRIMARY KEY (id)
)

TABLESPACE pg_default;

ALTER TABLE med.rad_studyname
    OWNER to postgres;
ALTER TABLE med.rad_technique_combination OWNER to postgres;




    CREATE TABLE IF NOT EXISTS med.rad_studyname_loinc_part
-- Combination items
CREATE TABLE IF NOT EXISTS med.rad_technique_combination_item
(
    id bigint NOT NULL DEFAULT nextval('med.rad_studyname_loinc_part_id_seq'::regclass),
    studyname_id bigint NOT NULL,
    part_number text COLLATE pg_catalog."default" NOT NULL,
    part_sequence_order text COLLATE pg_catalog."default" NOT NULL DEFAULT 'A'::text,
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    combination_id bigint NOT NULL,
    technique_id bigint NOT NULL,
    sequence_order integer NOT NULL DEFAULT 0,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT rad_studyname_loinc_part_pkey PRIMARY KEY (id),
    CONSTRAINT uq_rad_studyname_loinc_part__studyname_part_seq UNIQUE (studyname_id, part_number, part_sequence_order),
    CONSTRAINT fk_loinc_part FOREIGN KEY (part_number)
        REFERENCES loinc.part (part_number) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE,
    CONSTRAINT fk_studyname FOREIGN KEY (studyname_id)
        REFERENCES med.rad_studyname (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
    CONSTRAINT rad_technique_combination_item_pkey PRIMARY KEY (id),
    CONSTRAINT uq_rad_technique_combination_item UNIQUE (combination_id, technique_id, sequence_order),
    CONSTRAINT rad_technique_combination_item_combination_id_fkey FOREIGN KEY (combination_id)
        REFERENCES med.rad_technique_combination (id) MATCH SIMPLE ON UPDATE NO ACTION ON DELETE CASCADE,
    CONSTRAINT rad_technique_combination_item_technique_id_fkey FOREIGN KEY (technique_id)
        REFERENCES med.rad_technique (id) MATCH SIMPLE ON UPDATE NO ACTION ON DELETE RESTRICT
)

TABLESPACE pg_default;

ALTER TABLE med.rad_studyname_loinc_part
    OWNER to postgres;
ALTER TABLE med.rad_technique_combination_item OWNER to postgres;

-- Index: med.ix_rad_studyname_loinc_part_studyname
CREATE INDEX IF NOT EXISTS ix_rad_studyname_loinc_part_studyname
    ON med.rad_studyname_loinc_part USING btree
    (studyname_id ASC NULLS LAST)
CREATE INDEX IF NOT EXISTS idx_rad_technique_combination_item_combination
    ON med.rad_technique_combination_item USING btree (combination_id ASC NULLS LAST)
    TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_rad_technique_combination_item_technique
    ON med.rad_technique_combination_item USING btree (technique_id ASC NULLS LAST)
    TABLESPACE pg_default;



-- Study-technique links (unchanged names, reference combination id)
CREATE TABLE IF NOT EXISTS med.rad_studyname_technique_combination
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    studyname_id bigint NOT NULL,
    combination_id bigint NOT NULL,
    is_default boolean NOT NULL DEFAULT false,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT rad_studyname_technique_combination_pkey PRIMARY KEY (id),
    CONSTRAINT uq_studyname_combination UNIQUE (studyname_id, combination_id),
    CONSTRAINT rad_studyname_technique_combination_combination_id_fkey FOREIGN KEY (combination_id)
        REFERENCES med.technique_combination (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE,
        REFERENCES med.rad_technique_combination (id) MATCH SIMPLE ON UPDATE NO ACTION ON DELETE CASCADE,
    CONSTRAINT rad_studyname_technique_combination_studyname_id_fkey FOREIGN KEY (studyname_id)
        REFERENCES med.rad_studyname (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
        REFERENCES med.rad_studyname (id) MATCH SIMPLE ON UPDATE NO ACTION ON DELETE CASCADE
)

TABLESPACE pg_default;

ALTER TABLE med.rad_studyname_technique_combination
    OWNER to postgres;
ALTER TABLE med.rad_studyname_technique_combination OWNER to postgres;

-- Index: med.idx_rad_studyname_technique_combination_combination
CREATE INDEX IF NOT EXISTS idx_rad_studyname_technique_combination_combination
    ON med.rad_studyname_technique_combination USING btree
    (combination_id ASC NULLS LAST)
    ON med.rad_studyname_technique_combination USING btree (combination_id ASC NULLS LAST)
    TABLESPACE pg_default;
-- Index: med.idx_rad_studyname_technique_combination_studyname
CREATE INDEX IF NOT EXISTS idx_rad_studyname_technique_combination_studyname
    ON med.rad_studyname_technique_combination USING btree
    (studyname_id ASC NULLS LAST)
    ON med.rad_studyname_technique_combination USING btree (studyname_id ASC NULLS LAST)
    TABLESPACE pg_default;





    CREATE TABLE IF NOT EXISTS med.technique
CREATE TABLE IF NOT EXISTS med.rad_study
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    prefix_id bigint,
    tech_id bigint NOT NULL,
    suffix_id bigint,
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    patient_id bigint NOT NULL,
    studyname_id bigint NOT NULL,
    study_datetime timestamp with time zone NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT technique_pkey PRIMARY KEY (id),
    CONSTRAINT uq_technique_components UNIQUE (prefix_id, tech_id, suffix_id),
    CONSTRAINT technique_prefix_id_fkey FOREIGN KEY (prefix_id)
        REFERENCES med.technique_prefix (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE RESTRICT,
    CONSTRAINT technique_suffix_id_fkey FOREIGN KEY (suffix_id)
        REFERENCES med.technique_suffix (id) MATCH SIMPLE
    CONSTRAINT rad_study_pkey PRIMARY KEY (id),
    CONSTRAINT uq_rad_study_patient_name_time UNIQUE (patient_id, studyname_id, study_datetime),
    CONSTRAINT rad_study_patient_id_fkey FOREIGN KEY (patient_id)
        REFERENCES med.patient (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE RESTRICT,
    CONSTRAINT technique_tech_id_fkey FOREIGN KEY (tech_id)
        REFERENCES med.technique_tech (id) MATCH SIMPLE
        ON DELETE CASCADE,
    CONSTRAINT rad_study_studyname_id_fkey FOREIGN KEY (studyname_id)
        REFERENCES med.rad_studyname (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE RESTRICT
)

TABLESPACE pg_default;

ALTER TABLE med.technique
    OWNER to postgres;

-- Index: med.idx_technique_tech_id
CREATE INDEX IF NOT EXISTS idx_technique_tech_id
    ON med.technique USING btree
    (tech_id ASC NULLS LAST)
TABLESPACE pg_default;





    CREATE TABLE IF NOT EXISTS med.technique_combination
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    combination_name text COLLATE pg_catalog."default",
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT technique_combination_pkey PRIMARY KEY (id)
)
ALTER TABLE med.rad_study OWNER to postgres;

CREATE INDEX IF NOT EXISTS idx_study_patient_when
    ON med.rad_study USING btree
    (patient_id ASC NULLS LAST, study_datetime DESC NULLS FIRST)
    TABLESPACE pg_default;

ALTER TABLE med.technique_combination
    OWNER to postgres;




    CREATE TABLE IF NOT EXISTS med.technique_combination_item
CREATE TABLE IF NOT EXISTS med.rad_study_technique_combination
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    study_id bigint NOT NULL,
    combination_id bigint NOT NULL,
    technique_id bigint NOT NULL,
    sequence_order integer NOT NULL DEFAULT 0,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT technique_combination_item_pkey PRIMARY KEY (id),
    CONSTRAINT uq_combination_technique_seq UNIQUE (combination_id, technique_id, sequence_order),
    CONSTRAINT technique_combination_item_combination_id_fkey FOREIGN KEY (combination_id)
        REFERENCES med.technique_combination (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE,
    CONSTRAINT technique_combination_item_technique_id_fkey FOREIGN KEY (technique_id)
        REFERENCES med.technique (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE RESTRICT
    CONSTRAINT rad_study_technique_combination_pkey PRIMARY KEY (id),
    CONSTRAINT uq_study_combination UNIQUE (study_id),
    CONSTRAINT rad_study_technique_combination_combination_id_fkey FOREIGN KEY (combination_id)
        REFERENCES med.rad_technique_combination (id) MATCH SIMPLE ON UPDATE NO ACTION ON DELETE RESTRICT,
    CONSTRAINT rad_study_technique_combination_study_id_fkey FOREIGN KEY (study_id)
        REFERENCES med.rad_study (id) MATCH SIMPLE ON UPDATE NO ACTION ON DELETE CASCADE
)

TABLESPACE pg_default;

ALTER TABLE med.technique_combination_item
    OWNER to postgres;

-- Index: med.idx_technique_combination_item_combination
CREATE INDEX IF NOT EXISTS idx_technique_combination_item_combination
    ON med.technique_combination_item USING btree
    (combination_id ASC NULLS LAST)
    TABLESPACE pg_default;
-- Index: med.idx_technique_combination_item_technique
CREATE INDEX IF NOT EXISTS idx_technique_combination_item_technique
    ON med.technique_combination_item USING btree
    (technique_id ASC NULLS LAST)
TABLESPACE pg_default;



ALTER TABLE med.rad_study_technique_combination OWNER to postgres;

    CREATE TABLE IF NOT EXISTS med.technique_prefix
-- Reports (unchanged)
CREATE TABLE IF NOT EXISTS med.rad_report
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    prefix_text text COLLATE pg_catalog."default" NOT NULL,
    display_order integer NOT NULL DEFAULT 0,
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    study_id bigint NOT NULL,
    is_created boolean NOT NULL DEFAULT true,
    is_mine boolean NOT NULL DEFAULT false,
    created_by text COLLATE pg_catalog."default",
    report_datetime timestamp with time zone,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT technique_prefix_pkey PRIMARY KEY (id),
    CONSTRAINT uq_technique_prefix_text UNIQUE (prefix_text)
    report jsonb NOT NULL DEFAULT '{}'::jsonb,
    CONSTRAINT rad_report_pkey PRIMARY KEY (id),
    CONSTRAINT uq_rad_report__studyid_reportdt UNIQUE (study_id, report_datetime),
    CONSTRAINT rad_report_study_id_fkey FOREIGN KEY (study_id)
        REFERENCES med.rad_study (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)

TABLESPACE pg_default;

ALTER TABLE med.technique_prefix
    OWNER to postgres;





    CREATE TABLE IF NOT EXISTS med.technique_suffix
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    suffix_text text COLLATE pg_catalog."default" NOT NULL,
    display_order integer NOT NULL DEFAULT 0,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT technique_suffix_pkey PRIMARY KEY (id),
    CONSTRAINT uq_technique_suffix_text UNIQUE (suffix_text)
)
ALTER TABLE med.rad_report OWNER to postgres;

CREATE INDEX IF NOT EXISTS idx_rad_report_report_gin
    ON med.rad_report USING gin (report)
    TABLESPACE pg_default;

ALTER TABLE med.technique_suffix
    OWNER to postgres;





    CREATE TABLE IF NOT EXISTS med.technique_tech
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    tech_text text COLLATE pg_catalog."default" NOT NULL,
    display_order integer NOT NULL DEFAULT 0,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT technique_tech_pkey PRIMARY KEY (id),
    CONSTRAINT uq_technique_tech_text UNIQUE (tech_text)
)

TABLESPACE pg_default;
-- =============================
-- Views compatible with repository queries
-- =============================
-- v_technique_display: render "prefix tech suffix" text
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

ALTER TABLE med.technique_tech
    OWNER to postgres;
-- v_technique_combination_display: join combination items and aggregate display
CREATE OR REPLACE VIEW med.v_technique_combination_display AS
SELECT c.id,
       c.combination_name,
       string_agg(v.technique_display, ' + ' ORDER BY i.sequence_order) AS combination_display
FROM med.rad_technique_combination c
JOIN med.rad_technique_combination_item i ON i.combination_id = c.id
JOIN med.v_technique_display v ON v.id = i.technique_id
GROUP BY c.id, c.combination_name;





