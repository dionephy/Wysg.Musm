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

-- Index: loinc.idx_loinc_component_trgm
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





-- View: loinc.rp_join

-- DROP VIEW loinc.rp_join;

CREATE OR REPLACE VIEW loinc.rp_join AS
 SELECT rp.loinc_number,
    rp.long_common_name,
    rp.part_number,
    rp.part_type_name,
    rp.part_name,
    rp.part_sequence_order,
    rp.rid,
    rp.preferred_name,
    rp.rpid,
    rp.long_name,
    t.class,
    t.component,
    t.system,
    t.method_typ,
    t.scale_typ
   FROM loinc.rplaybook rp
     LEFT JOIN loinc.loinc_term t ON t.loinc_num = rp.loinc_number;

ALTER TABLE loinc.rp_join
    OWNER TO postgres;







CREATE TABLE IF NOT EXISTS med.patient
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    patient_number text COLLATE pg_catalog."default" NOT NULL,
    patient_name text COLLATE pg_catalog."default" NOT NULL,
    is_male boolean NOT NULL,
    birth_date date,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT patient_pkey PRIMARY KEY (id),
    CONSTRAINT patient_patient_number_key UNIQUE (patient_number)
)

TABLESPACE pg_default;

ALTER TABLE med.patient
    OWNER to postgres;




CREATE TABLE IF NOT EXISTS med.rad_report
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    study_id bigint NOT NULL,
    is_created boolean NOT NULL DEFAULT true,
    is_mine boolean NOT NULL DEFAULT false,
    created_by text COLLATE pg_catalog."default",
    report_datetime timestamp with time zone,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    report jsonb NOT NULL DEFAULT '{}'::jsonb,
    CONSTRAINT rad_report_pkey PRIMARY KEY (id),
    CONSTRAINT uq_rad_report__studyid_reportdt UNIQUE (study_id, report_datetime),
    CONSTRAINT rad_report_study_id_fkey FOREIGN KEY (study_id)
        REFERENCES med.rad_study (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)

TABLESPACE pg_default;

ALTER TABLE med.rad_report
    OWNER to postgres;

-- Index: med.idx_rad_report_report_gin
CREATE INDEX IF NOT EXISTS idx_rad_report_report_gin
    ON med.rad_report USING gin
    (report)
    TABLESPACE pg_default;








CREATE TABLE IF NOT EXISTS med.rad_study
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    patient_id bigint NOT NULL,
    studyname_id bigint NOT NULL,
    study_datetime timestamp with time zone NOT NULL,
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
)

TABLESPACE pg_default;

ALTER TABLE med.rad_study
    OWNER to postgres;

-- Index: med.idx_study_patient_when
CREATE INDEX IF NOT EXISTS idx_study_patient_when
    ON med.rad_study USING btree
    (patient_id ASC NULLS LAST, study_datetime DESC NULLS FIRST)
    TABLESPACE pg_default;







CREATE TABLE IF NOT EXISTS med.rad_studyname
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    studyname text COLLATE pg_catalog."default" NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT rad_studyname_pkey PRIMARY KEY (id),
    CONSTRAINT uq_rad_studyname UNIQUE (studyname)
)

TABLESPACE pg_default;

ALTER TABLE med.rad_studyname
    OWNER to postgres;







CREATE TABLE IF NOT EXISTS med.rad_studyname_loinc_part
(
    id bigint NOT NULL DEFAULT nextval('med.rad_studyname_loinc_part_id_seq'::regclass),
    studyname_id bigint NOT NULL,
    part_number text COLLATE pg_catalog."default" NOT NULL,
    part_sequence_order text COLLATE pg_catalog."default" NOT NULL DEFAULT 'A'::text,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT rad_studyname_loinc_part_pkey PRIMARY KEY (id),
    CONSTRAINT rad_studyname_loinc_part_studyname_id_part_number_key UNIQUE (studyname_id, part_number),
    CONSTRAINT fk_loinc_part FOREIGN KEY (part_number)
        REFERENCES loinc.part (part_number) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE,
    CONSTRAINT fk_studyname FOREIGN KEY (studyname_id)
        REFERENCES med.rad_studyname (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)

TABLESPACE pg_default;

ALTER TABLE med.rad_studyname_loinc_part
    OWNER to postgres;

-- Index: med.ix_rad_studyname_loinc_part_studyname
CREATE INDEX IF NOT EXISTS ix_rad_studyname_loinc_part_studyname
    ON med.rad_studyname_loinc_part USING btree
    (studyname_id ASC NULLS LAST)
    TABLESPACE pg_default;








