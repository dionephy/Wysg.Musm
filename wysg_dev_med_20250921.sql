-- med_min_json_schema_cs_v2.sql
-- Minimal local schema (case-sensitive studyname, JSON reports), with composite unique on rad_study.

CREATE SCHEMA IF NOT EXISTS med;

-- Patients (binary sex as boolean; TRUE = male, FALSE = female)
CREATE TABLE IF NOT EXISTS med.patient (
  id             BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  patient_number TEXT   NOT NULL UNIQUE,
  patient_name   TEXT   NOT NULL,
  is_male        BOOLEAN NOT NULL,
  birth_date     DATE,
  created_at     TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Study name master (exactly: id, studyname, created_at) with case-sensitive UNIQUE
CREATE TABLE IF NOT EXISTS med.rad_studyname (
  id         BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  studyname  TEXT NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  CONSTRAINT uq_rad_studyname UNIQUE (studyname)   -- case-sensitive uniqueness
);

-- Imaging studies (now with composite uniqueness)
CREATE TABLE IF NOT EXISTS med.rad_study (
  id             BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  patient_id     BIGINT NOT NULL REFERENCES med.patient(id) ON DELETE CASCADE,
  studyname_id   BIGINT NOT NULL REFERENCES med.rad_studyname(id) ON DELETE RESTRICT,
  study_datetime TIMESTAMPTZ NOT NULL,
  created_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
  CONSTRAINT uq_rad_study_patient_name_time UNIQUE (patient_id, studyname_id, study_datetime)
);

-- Helpful query index
CREATE INDEX IF NOT EXISTS idx_study_patient_when
  ON med.rad_study (patient_id, study_datetime DESC);

-- Reports (JSON payloads + boolean is_created + optional split_index)
CREATE TABLE IF NOT EXISTS med.rad_report (
  id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  study_id        BIGINT  NOT NULL REFERENCES med.rad_study(id) ON DELETE CASCADE,
  is_created      BOOLEAN NOT NULL DEFAULT TRUE,   -- TRUE: app-created, FALSE: PACS-retrieved
  is_mine         BOOLEAN NOT NULL DEFAULT FALSE,
  created_by      TEXT,
  report_datetime TIMESTAMPTZ,
  created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

  report          JSONB  NOT NULL DEFAULT '{}'::jsonb,   -- may include: header, findings, header_and_findings, conclusion
  refined_report  JSONB,                                  -- may include: header, findings, conclusion
  split_index     INTEGER                                 -- boundary index in header_and_findings (optional)
);

-- JSON lookups (optional but useful)
CREATE INDEX IF NOT EXISTS idx_rad_report_report_gin  ON med.rad_report USING GIN (report);
CREATE INDEX IF NOT EXISTS idx_rad_report_refined_gin ON med.rad_report USING GIN (refined_report);

-- View: best-available raw/refined text + split_index + studyname
CREATE OR REPLACE VIEW med.report_texts AS
SELECT
  r.id AS report_id,
  s.id AS study_id,
  p.id AS patient_id,
  COALESCE(
    NULLIF(COALESCE(r.report->>'header','') || E'\n\n' || COALESCE(r.report->>'findings',''), ''),
    r.report->>'header_and_findings',
    ''
  ) AS raw_text,
  COALESCE(
    NULLIF(COALESCE(r.refined_report->>'header','') || E'\n\n' || COALESCE(r.refined_report->>'findings',''), ''),
    r.refined_report->>'conclusion',
    ''
  ) AS refined_text,
  r.split_index,
  r.is_created,
  s.study_datetime,
  rsn.studyname
FROM med.rad_report    r
JOIN med.rad_study     s   ON s.id = r.study_id
JOIN med.patient       p   ON p.id = s.patient_id
JOIN med.rad_studyname rsn ON rsn.id = s.studyname_id;
