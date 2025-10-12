-- Study Technique Feature Tables
-- Created: 2025-01-12
-- Description: Tables for managing study techniques with prefix, tech, and suffix components

-- Table: med.technique_prefix
-- Stores prefix options for techniques (e.g., "axial", "coronal", "sagittal", "3D", "intracranial", "neck", "")
CREATE TABLE IF NOT EXISTS med.technique_prefix
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    prefix_text text COLLATE pg_catalog."default" NOT NULL,
    display_order int NOT NULL DEFAULT 0,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT technique_prefix_pkey PRIMARY KEY (id),
    CONSTRAINT uq_technique_prefix_text UNIQUE (prefix_text)
)
TABLESPACE pg_default;

ALTER TABLE med.technique_prefix
    OWNER to postgres;

COMMENT ON TABLE med.technique_prefix IS 'Prefix options for study techniques (e.g., axial, coronal, sagittal, 3D, intracranial, neck, blank)';
COMMENT ON COLUMN med.technique_prefix.prefix_text IS 'The prefix text; empty string represents blank/no prefix';
COMMENT ON COLUMN med.technique_prefix.display_order IS 'Display order for UI dropdown';


-- Table: med.technique_tech
-- Stores tech options for techniques (e.g., "T1", "T2", "GRE", "SWI", "DWI", "CE-T1", "TOF-MRA", "CE-MRA", "3T")
CREATE TABLE IF NOT EXISTS med.technique_tech
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    tech_text text COLLATE pg_catalog."default" NOT NULL,
    display_order int NOT NULL DEFAULT 0,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT technique_tech_pkey PRIMARY KEY (id),
    CONSTRAINT uq_technique_tech_text UNIQUE (tech_text)
)
TABLESPACE pg_default;

ALTER TABLE med.technique_tech
    OWNER to postgres;

COMMENT ON TABLE med.technique_tech IS 'Tech options for study techniques (e.g., T1, T2, GRE, SWI, DWI, CE-T1, TOF-MRA, CE-MRA, 3T)';
COMMENT ON COLUMN med.technique_tech.tech_text IS 'The tech text';
COMMENT ON COLUMN med.technique_tech.display_order IS 'Display order for UI dropdown';


-- Table: med.technique_suffix
-- Stores suffix options for techniques (e.g., "of sellar fossa")
CREATE TABLE IF NOT EXISTS med.technique_suffix
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    suffix_text text COLLATE pg_catalog."default" NOT NULL,
    display_order int NOT NULL DEFAULT 0,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT technique_suffix_pkey PRIMARY KEY (id),
    CONSTRAINT uq_technique_suffix_text UNIQUE (suffix_text)
)
TABLESPACE pg_default;

ALTER TABLE med.technique_suffix
    OWNER to postgres;

COMMENT ON TABLE med.technique_suffix IS 'Suffix options for study techniques (e.g., of sellar fossa)';
COMMENT ON COLUMN med.technique_suffix.suffix_text IS 'The suffix text; empty string represents blank/no suffix';
COMMENT ON COLUMN med.technique_suffix.display_order IS 'Display order for UI dropdown';


-- Table: med.technique
-- Stores individual technique components (single technique = prefix + tech + suffix)
CREATE TABLE IF NOT EXISTS med.technique
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    prefix_id bigint,
    tech_id bigint NOT NULL,
    suffix_id bigint,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT technique_pkey PRIMARY KEY (id),
    CONSTRAINT uq_technique_components UNIQUE (prefix_id, tech_id, suffix_id),
    CONSTRAINT technique_prefix_id_fkey FOREIGN KEY (prefix_id)
        REFERENCES med.technique_prefix (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE RESTRICT,
    CONSTRAINT technique_tech_id_fkey FOREIGN KEY (tech_id)
        REFERENCES med.technique_tech (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE RESTRICT,
    CONSTRAINT technique_suffix_id_fkey FOREIGN KEY (suffix_id)
        REFERENCES med.technique_suffix (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE RESTRICT
)
TABLESPACE pg_default;

ALTER TABLE med.technique
    OWNER to postgres;

COMMENT ON TABLE med.technique IS 'Individual technique combining prefix, tech, and suffix';
COMMENT ON COLUMN med.technique.prefix_id IS 'Optional prefix FK; NULL represents blank prefix';
COMMENT ON COLUMN med.technique.tech_id IS 'Required tech FK';
COMMENT ON COLUMN med.technique.suffix_id IS 'Optional suffix FK; NULL represents blank suffix';

CREATE INDEX IF NOT EXISTS idx_technique_tech_id
    ON med.technique USING btree
    (tech_id ASC NULLS LAST)
    TABLESPACE pg_default;


-- Table: med.technique_combination
-- Stores technique combinations (e.g., "axial T1 + axial T2 + coronal T2 + sagittal T1 of sellar fossa")
CREATE TABLE IF NOT EXISTS med.technique_combination
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    combination_name text COLLATE pg_catalog."default",
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT technique_combination_pkey PRIMARY KEY (id)
)
TABLESPACE pg_default;

ALTER TABLE med.technique_combination
    OWNER to postgres;

COMMENT ON TABLE med.technique_combination IS 'Combination of multiple techniques';
COMMENT ON COLUMN med.technique_combination.combination_name IS 'Optional name for the combination for easy identification';


-- Table: med.technique_combination_item
-- Join table linking technique_combination to individual techniques with sequence order
CREATE TABLE IF NOT EXISTS med.technique_combination_item
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    combination_id bigint NOT NULL,
    technique_id bigint NOT NULL,
    sequence_order int NOT NULL DEFAULT 0,
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
)
TABLESPACE pg_default;

ALTER TABLE med.technique_combination_item
    OWNER to postgres;

COMMENT ON TABLE med.technique_combination_item IS 'Join table linking combinations to individual techniques with ordering';
COMMENT ON COLUMN med.technique_combination_item.sequence_order IS 'Order of technique in combination (for display purposes)';

CREATE INDEX IF NOT EXISTS idx_technique_combination_item_combination
    ON med.technique_combination_item USING btree
    (combination_id ASC NULLS LAST)
    TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS idx_technique_combination_item_technique
    ON med.technique_combination_item USING btree
    (technique_id ASC NULLS LAST)
    TABLESPACE pg_default;


-- Table: med.rad_studyname_technique_combination
-- Links studynames to technique combinations (many-to-many) with optional default flag
CREATE TABLE IF NOT EXISTS med.rad_studyname_technique_combination
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    studyname_id bigint NOT NULL,
    combination_id bigint NOT NULL,
    is_default boolean NOT NULL DEFAULT false,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT rad_studyname_technique_combination_pkey PRIMARY KEY (id),
    CONSTRAINT uq_studyname_combination UNIQUE (studyname_id, combination_id),
    CONSTRAINT rad_studyname_technique_combination_studyname_id_fkey FOREIGN KEY (studyname_id)
        REFERENCES med.rad_studyname (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE,
    CONSTRAINT rad_studyname_technique_combination_combination_id_fkey FOREIGN KEY (combination_id)
        REFERENCES med.technique_combination (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)
TABLESPACE pg_default;

ALTER TABLE med.rad_studyname_technique_combination
    OWNER to postgres;

COMMENT ON TABLE med.rad_studyname_technique_combination IS 'Links studynames to technique combinations with optional default flag';
COMMENT ON COLUMN med.rad_studyname_technique_combination.is_default IS 'Only one default combination per studyname; zero or one';

CREATE INDEX IF NOT EXISTS idx_rad_studyname_technique_combination_studyname
    ON med.rad_studyname_technique_combination USING btree
    (studyname_id ASC NULLS LAST)
    TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS idx_rad_studyname_technique_combination_combination
    ON med.rad_studyname_technique_combination USING btree
    (combination_id ASC NULLS LAST)
    TABLESPACE pg_default;


-- Table: med.rad_study_technique_combination
-- Links individual studies to technique combinations (zero or one per study)
CREATE TABLE IF NOT EXISTS med.rad_study_technique_combination
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    study_id bigint NOT NULL,
    combination_id bigint NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT rad_study_technique_combination_pkey PRIMARY KEY (id),
    CONSTRAINT uq_study_combination UNIQUE (study_id),
    CONSTRAINT rad_study_technique_combination_study_id_fkey FOREIGN KEY (study_id)
        REFERENCES med.rad_study (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE,
    CONSTRAINT rad_study_technique_combination_combination_id_fkey FOREIGN KEY (combination_id)
        REFERENCES med.technique_combination (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE RESTRICT
)
TABLESPACE pg_default;

ALTER TABLE med.rad_study_technique_combination
    OWNER to postgres;

COMMENT ON TABLE med.rad_study_technique_combination IS 'Links individual studies to technique combinations; zero or one per study';
COMMENT ON COLUMN med.rad_study_technique_combination.study_id IS 'FK to rad_study; unique constraint ensures one combination per study max';


-- View: med.v_technique_display
-- Helper view to display technique as "prefix tech suffix"
CREATE OR REPLACE VIEW med.v_technique_display AS
SELECT 
    t.id,
    COALESCE(tp.prefix_text, '') AS prefix_text,
    tt.tech_text,
    COALESCE(ts.suffix_text, '') AS suffix_text,
    TRIM(
        CONCAT_WS(' ',
            NULLIF(COALESCE(tp.prefix_text, ''), ''),
            tt.tech_text,
            NULLIF(COALESCE(ts.suffix_text, ''), '')
        )
    ) AS technique_display
FROM med.technique t
LEFT JOIN med.technique_prefix tp ON t.prefix_id = tp.id
JOIN med.technique_tech tt ON t.tech_id = tt.id
LEFT JOIN med.technique_suffix ts ON t.suffix_id = ts.id;

ALTER VIEW med.v_technique_display
    OWNER TO postgres;

COMMENT ON VIEW med.v_technique_display IS 'Display view for technique showing combined prefix + tech + suffix';


-- View: med.v_technique_combination_display
-- Helper view to display technique combination as joined technique names
CREATE OR REPLACE VIEW med.v_technique_combination_display AS
SELECT 
    tc.id,
    tc.combination_name,
    STRING_AGG(vtd.technique_display, ' + ' ORDER BY tci.sequence_order) AS combination_display,
    COUNT(tci.id) AS technique_count
FROM med.technique_combination tc
LEFT JOIN med.technique_combination_item tci ON tc.id = tci.combination_id
LEFT JOIN med.v_technique_display vtd ON tci.technique_id = vtd.id
GROUP BY tc.id, tc.combination_name;

ALTER VIEW med.v_technique_combination_display
    OWNER TO postgres;

COMMENT ON VIEW med.v_technique_combination_display IS 'Display view for technique combination showing all techniques joined with +';


-- Sample data insert for common prefixes
INSERT INTO med.technique_prefix (prefix_text, display_order) VALUES
('', 0),
('axial', 1),
('coronal', 2),
('sagittal', 3),
('3D', 4),
('intracranial', 5),
('neck', 6)
ON CONFLICT (prefix_text) DO NOTHING;

-- Sample data insert for common techs
INSERT INTO med.technique_tech (tech_text, display_order) VALUES
('T1', 1),
('T2', 2),
('GRE', 3),
('SWI', 4),
('DWI', 5),
('CE-T1', 6),
('TOF-MRA', 7),
('CE-MRA', 8),
('3T', 9)
ON CONFLICT (tech_text) DO NOTHING;

-- Sample data insert for common suffixes
INSERT INTO med.technique_suffix (suffix_text, display_order) VALUES
('', 0),
('of sellar fossa', 1)
ON CONFLICT (suffix_text) DO NOTHING;
