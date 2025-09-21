-- Create mapping table between med.rad_studyname and LOINC parts
CREATE SCHEMA IF NOT EXISTS med;

CREATE TABLE IF NOT EXISTS med.rad_studyname_loinc_part (
    id                   bigserial PRIMARY KEY,
    studyname_id         bigint NOT NULL,
    part_number          text   NOT NULL, -- loinc.part.part_number
    part_sequence_order  text   NOT NULL DEFAULT 'A', -- sequence/order token
    created_at           timestamptz NOT NULL DEFAULT now(),
    UNIQUE (studyname_id, part_number),
    CONSTRAINT fk_studyname FOREIGN KEY (studyname_id) REFERENCES med.rad_studyname(id) ON DELETE CASCADE,
    CONSTRAINT fk_loinc_part FOREIGN KEY (part_number) REFERENCES loinc.part(part_number) ON DELETE CASCADE
);

-- Helpful index by studyname
CREATE INDEX IF NOT EXISTS ix_rad_studyname_loinc_part_studyname ON med.rad_studyname_loinc_part(studyname_id);
