--
-- PostgreSQL database dump
--

\restrict dIQ75r01oCyZsmnmnbuITrUcZY4UkAqaZ2KJe2KTFq1tGbnIikBHOA9jouHdPzH

-- Dumped from database version 17.6 (Debian 17.6-1.pgdg12+1)
-- Dumped by pg_dump version 17.6 (Debian 17.6-1.pgdg12+1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: app; Type: SCHEMA; Schema: -; Owner: postgres
--

CREATE SCHEMA app;


ALTER SCHEMA app OWNER TO postgres;

--
-- Name: auth; Type: SCHEMA; Schema: -; Owner: postgres
--

CREATE SCHEMA auth;


ALTER SCHEMA auth OWNER TO postgres;

--
-- Name: content; Type: SCHEMA; Schema: -; Owner: postgres
--

CREATE SCHEMA content;


ALTER SCHEMA content OWNER TO postgres;

--
-- Name: ghost; Type: SCHEMA; Schema: -; Owner: postgres
--

CREATE SCHEMA ghost;


ALTER SCHEMA ghost OWNER TO postgres;

--
-- Name: ops; Type: SCHEMA; Schema: -; Owner: postgres
--

CREATE SCHEMA ops;


ALTER SCHEMA ops OWNER TO postgres;

--
-- Name: radstyle; Type: SCHEMA; Schema: -; Owner: postgres
--

CREATE SCHEMA radstyle;


ALTER SCHEMA radstyle OWNER TO postgres;

--
-- Name: snomedct; Type: SCHEMA; Schema: -; Owner: postgres
--

CREATE SCHEMA snomedct;


ALTER SCHEMA snomedct OWNER TO postgres;

--
-- Name: pg_trgm; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS pg_trgm WITH SCHEMA public;


--
-- Name: EXTENSION pg_trgm; Type: COMMENT; Schema: -; Owner: 
--

COMMENT ON EXTENSION pg_trgm IS 'text similarity measurement and index searching based on trigrams';


--
-- Name: unaccent; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS unaccent WITH SCHEMA public;


--
-- Name: EXTENSION unaccent; Type: COMMENT; Schema: -; Owner: 
--

COMMENT ON EXTENSION unaccent IS 'text search dictionary that removes accents';


--
-- Name: vector; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS vector WITH SCHEMA public;


--
-- Name: EXTENSION vector; Type: COMMENT; Schema: -; Owner: 
--

COMMENT ON EXTENSION vector IS 'vector data type and ivfflat and hnsw access methods';


--
-- Name: sct_role; Type: TYPE; Schema: content; Owner: postgres
--

CREATE TYPE content.sct_role AS (
	role_group integer,
	attribute_id character varying(18),
	value_concept_id character varying(18)
);


ALTER TYPE content.sct_role OWNER TO postgres;

--
-- Name: ensure_tenant(text, text); Type: FUNCTION; Schema: app; Owner: postgres
--

CREATE FUNCTION app.ensure_tenant(p_code text, p_name text DEFAULT NULL::text) RETURNS bigint
    LANGUAGE plpgsql
    AS $$

DECLARE

  v_id bigint;

BEGIN

  -- Fast path by CODE

  SELECT id INTO v_id

  FROM app.tenant

  WHERE code = p_code;



  IF v_id IS NOT NULL THEN

    -- Refresh name if provided

    IF p_name IS NOT NULL THEN

      UPDATE app.tenant

         SET name = p_name

       WHERE id = v_id

         AND name IS DISTINCT FROM p_name;

    END IF;

    RETURN v_id;

  END IF;



  -- Create if missing (upsert by CODE)

  INSERT INTO app.tenant(code, name)

  VALUES (p_code, COALESCE(p_name, p_code))  -- default name to code if not given

  ON CONFLICT (code) DO UPDATE

    SET name = COALESCE(EXCLUDED.name, app.tenant.name)

  RETURNING id INTO v_id;



  RETURN v_id;

END;

$$;


ALTER FUNCTION app.ensure_tenant(p_code text, p_name text) OWNER TO postgres;

--
-- Name: ensure_phrase(text, text, boolean, text, jsonb); Type: FUNCTION; Schema: content; Owner: postgres
--

CREATE FUNCTION content.ensure_phrase(p_tenant_name text, p_text text, p_case_sensitive boolean DEFAULT false, p_lang text DEFAULT 'en'::text, p_tags jsonb DEFAULT '{}'::jsonb) RETURNS bigint
    LANGUAGE plpgsql
    AS $$
DECLARE
  v_tenant_id BIGINT;
  v_id        BIGINT;
BEGIN
  v_tenant_id := app.ensure_tenant(p_tenant_name);

  IF p_case_sensitive THEN
    -- exact match
    SELECT id INTO v_id
    FROM content.phrase
    WHERE tenant_id = v_tenant_id
      AND case_sensitive = TRUE
      AND text = p_text
      AND lang = p_lang
      AND active = TRUE;
  ELSE
    -- case-insensitive match
    SELECT id INTO v_id
    FROM content.phrase
    WHERE tenant_id = v_tenant_id
      AND case_sensitive = FALSE
      AND lower(text) = lower(p_text)
      AND lang = p_lang
      AND active = TRUE;
  END IF;

  IF v_id IS NOT NULL THEN
    -- optionally merge tags (keeps old keys unless overwritten)
    UPDATE content.phrase
       SET tags = COALESCE(tags, '{}'::jsonb) || COALESCE(p_tags, '{}'::jsonb),
           updated_at = now()
     WHERE id = v_id;
    RETURN v_id;
  END IF;

  -- not found => insert
  INSERT INTO content.phrase(tenant_id, text, case_sensitive, lang, tags)
  VALUES (v_tenant_id, p_text, p_case_sensitive, p_lang, p_tags)
  RETURNING id INTO v_id;

  RETURN v_id;
END;
$$;


ALTER FUNCTION content.ensure_phrase(p_tenant_name text, p_text text, p_case_sensitive boolean, p_lang text, p_tags jsonb) OWNER TO postgres;

--
-- Name: ensure_phrase_with_sct(bigint, text, character varying, boolean, jsonb, text, text, text, character varying, date); Type: FUNCTION; Schema: content; Owner: postgres
--

CREATE FUNCTION content.ensure_phrase_with_sct(p_tenant_id bigint, p_text text, p_root_concept_id character varying, p_case_sensitive boolean DEFAULT false, p_tags jsonb DEFAULT '{}'::jsonb, p_lang text DEFAULT 'en'::text, p_expression_cg text DEFAULT NULL::text, p_edition text DEFAULT NULL::text, p_module_id character varying DEFAULT NULL::character varying, p_effective_time date DEFAULT NULL::date) RETURNS bigint
    LANGUAGE plpgsql
    AS $$

DECLARE

  v_phrase_id bigint;

BEGIN

  -- Precheck: SNOMED id must exist in concept_latest (FK will enforce too)

  PERFORM 1 FROM snomedct.concept_latest WHERE id = p_root_concept_id;

  IF NOT FOUND THEN

    RAISE EXCEPTION 'SNOMED concept % not found in snomedct.concept_latest', p_root_concept_id

      USING ERRCODE = 'foreign_key_violation';

  END IF;



  -- Find existing phrase by your uniqueness rules (partial unique indexes)

  SELECT id

  INTO   v_phrase_id

  FROM   content.phrase

  WHERE  tenant_id = p_tenant_id

    AND (

          (p_case_sensitive  AND text = p_text)

       OR (NOT p_case_sensitive AND lower(text) = lower(p_text))

        )

  LIMIT 1;



  -- Insert if not found

  IF v_phrase_id IS NULL THEN

    INSERT INTO content.phrase(

      tenant_id, text, text_norm, case_sensitive, tags, lang, active

    )

    VALUES (

      p_tenant_id,

      p_text,

      CASE WHEN p_case_sensitive THEN p_text ELSE lower(p_text) END,

      p_case_sensitive,

      COALESCE(p_tags, '{}'::jsonb),

      p_lang,

      true

    )

    RETURNING id INTO v_phrase_id;

  ELSE

    -- Update light fields if already present

    UPDATE content.phrase

       SET tags = COALESCE(p_tags, '{}'::jsonb),

           lang = p_lang,

           active = true,

           updated_at = now()

     WHERE id = v_phrase_id;

  END IF;



  -- Upsert SNOMED anchor

  INSERT INTO content.phrase_sct(

    phrase_id, root_concept_id, expression_cg, edition, module_id, effective_time

  )

  VALUES (

    v_phrase_id, p_root_concept_id, p_expression_cg, p_edition, p_module_id, p_effective_time

  )

  ON CONFLICT (phrase_id) DO UPDATE

    SET root_concept_id = EXCLUDED.root_concept_id,

        expression_cg   = EXCLUDED.expression_cg,

        edition         = EXCLUDED.edition,

        module_id       = EXCLUDED.module_id,

        effective_time  = EXCLUDED.effective_time;



  RETURN v_phrase_id;

END

$$;


ALTER FUNCTION content.ensure_phrase_with_sct(p_tenant_id bigint, p_text text, p_root_concept_id character varying, p_case_sensitive boolean, p_tags jsonb, p_lang text, p_expression_cg text, p_edition text, p_module_id character varying, p_effective_time date) OWNER TO postgres;

--
-- Name: ensure_phrase_with_sct(bigint, text, character varying, boolean, jsonb, text, text, text, character varying, date, content.sct_role[]); Type: FUNCTION; Schema: content; Owner: postgres
--

CREATE FUNCTION content.ensure_phrase_with_sct(p_tenant_id bigint, p_text text, p_root_concept_id character varying, p_case_sensitive boolean DEFAULT false, p_tags jsonb DEFAULT '{}'::jsonb, p_lang text DEFAULT 'en'::text, p_expression_cg text DEFAULT NULL::text, p_edition text DEFAULT NULL::text, p_module_id character varying DEFAULT NULL::character varying, p_effective_time date DEFAULT NULL::date, p_roles content.sct_role[] DEFAULT NULL::content.sct_role[]) RETURNS bigint
    LANGUAGE plpgsql
    AS $$

DECLARE

  v_phrase_id bigint;

BEGIN

  -- Ensure SNOMED root exists (FK also enforces this)

  PERFORM 1 FROM snomedct.concept_latest WHERE id = p_root_concept_id;

  IF NOT FOUND THEN

    RAISE EXCEPTION 'SNOMED concept % not found in snomedct.concept_latest', p_root_concept_id

      USING ERRCODE = 'foreign_key_violation';

  END IF;



  -- Find existing phrase under your case rules

  SELECT id

    INTO v_phrase_id

    FROM content.phrase

   WHERE tenant_id = p_tenant_id

     AND (

           (p_case_sensitive  AND text = p_text)

        OR (NOT p_case_sensitive AND lower(text) = lower(p_text))

         )

   LIMIT 1;



  -- INSERT: do NOT set text_norm (it’s generated)

  IF v_phrase_id IS NULL THEN

    INSERT INTO content.phrase(

      tenant_id, text, case_sensitive, tags, lang, active

    ) VALUES (

      p_tenant_id,

      p_text,

      p_case_sensitive,

      COALESCE(p_tags, '{}'::jsonb),

      p_lang,

      true

    )

    RETURNING id INTO v_phrase_id;

  ELSE

    -- UPDATE: do NOT set text_norm (it’s generated)

    UPDATE content.phrase

       SET tags       = COALESCE(p_tags, '{}'::jsonb),

           lang       = p_lang,

           active     = true,

           updated_at = now()

     WHERE id = v_phrase_id;

  END IF;



  -- Upsert SNOMED anchor

  INSERT INTO content.phrase_sct(

    phrase_id, root_concept_id, expression_cg, edition, module_id, effective_time

  )

  VALUES (

    v_phrase_id, p_root_concept_id, p_expression_cg, p_edition, p_module_id, p_effective_time

  )

  ON CONFLICT (phrase_id) DO UPDATE

    SET root_concept_id = EXCLUDED.root_concept_id,

        expression_cg   = EXCLUDED.expression_cg,

        edition         = EXCLUDED.edition,

        module_id       = EXCLUDED.module_id,

        effective_time  = EXCLUDED.effective_time;



  -- Optional attribute decomposition

  IF p_roles IS NOT NULL THEN

    -- validate all attribute/value ids exist

    PERFORM 1

      FROM unnest(p_roles) r

      LEFT JOIN snomedct.concept_latest a ON a.id = r.attribute_id

      LEFT JOIN snomedct.concept_latest v ON v.id = r.value_concept_id

     WHERE a.id IS NULL OR v.id IS NULL;

    IF FOUND THEN

      RAISE EXCEPTION 'One or more attribute/value SCTIDs not found in concept_latest';

    END IF;



    DELETE FROM content.phrase_sct_attribute WHERE phrase_id = v_phrase_id;



    INSERT INTO content.phrase_sct_attribute(phrase_id, role_group, attribute_id, value_concept_id)

    SELECT v_phrase_id, r.role_group, r.attribute_id, r.value_concept_id

    FROM unnest(p_roles) r;

  END IF;



  RETURN v_phrase_id;

END

$$;


ALTER FUNCTION content.ensure_phrase_with_sct(p_tenant_id bigint, p_text text, p_root_concept_id character varying, p_case_sensitive boolean, p_tags jsonb, p_lang text, p_expression_cg text, p_edition text, p_module_id character varying, p_effective_time date, p_roles content.sct_role[]) OWNER TO postgres;

--
-- Name: normalize_text(text); Type: FUNCTION; Schema: content; Owner: postgres
--

CREATE FUNCTION content.normalize_text(s text) RETURNS text
    LANGUAGE sql IMMUTABLE PARALLEL SAFE
    AS $$
  SELECT regexp_replace(lower(public.unaccent(s)), '\s+', ' ', 'g')::text
$$;


ALTER FUNCTION content.normalize_text(s text) OWNER TO postgres;

--
-- Name: search_phrases(bigint, text, text, text, public.vector, integer, text); Type: FUNCTION; Schema: content; Owner: postgres
--

CREATE FUNCTION content.search_phrases(p_tenant_id bigint, p_prefix text, p_lang text DEFAULT NULL::text, p_model text DEFAULT 'BAAI/bge-m3'::text, p_context public.vector DEFAULT NULL::public.vector, p_k integer DEFAULT 30, p_hint text DEFAULT NULL::text) RETURNS TABLE(phrase_id bigint, text text, lang text, rank double precision)
    LANGUAGE sql STABLE
    AS $$
WITH params AS (
  SELECT
    p_tenant_id AS tenant_id,
    content.normalize_text(p_prefix) AS qnorm,
    p_prefix AS qraw,
    length(p_prefix) AS qlen,
    p_lang   AS lang_pref,
    p_model  AS model_name,
    p_context AS ctx,
    p_hint    AS hint,
    p_k      AS k
),
cand AS (
  SELECT
    d.id, d.text, d.lang,
    (CASE WHEN d.text ILIKE (SELECT qraw || '%' FROM params) THEN 1.0 ELSE 0.0 END) AS is_prefix,
    similarity(d.text_norm, (SELECT qnorm FROM params)) AS trigram_sim,
    ts_rank(d.tsv, plainto_tsquery('simple', (SELECT qraw FROM params))) AS fts_prefix,
    (CASE
      WHEN (SELECT hint IS NOT NULL AND length(hint) > 0 FROM params)
      THEN ts_rank(d.tsv, plainto_tsquery('simple', (SELECT hint FROM params)))
      ELSE 0.0
     END) AS fts_hint
  FROM content.phrase_search_denorm d, params
  WHERE d.tenant_id = (SELECT tenant_id FROM params)
    AND d.active = true
    AND (
      d.text_norm ILIKE (SELECT qnorm || '%' FROM params)
      OR similarity(d.text_norm, (SELECT qnorm FROM params)) > 0.22
    )
  ORDER BY
    (CASE WHEN d.text ILIKE (SELECT qraw || '%' FROM params) THEN 1 ELSE 0 END) DESC,
    similarity(d.text_norm, (SELECT qnorm FROM params)) DESC
  LIMIT 250
)
SELECT
  c.id AS phrase_id, c.text, c.lang,
  (
    1.35 * c.is_prefix +
    0.75 * c.trigram_sim +
    0.40 * c.fts_prefix +
    0.35 * c.fts_hint +                                   -- << boosts “no evidence of …”
    (CASE WHEN (SELECT lang_pref FROM params) IS NOT NULL AND c.lang = (SELECT lang_pref FROM params) THEN 0.15 ELSE 0 END) +
    (CASE
      WHEN (SELECT qlen FROM params) >= 3 AND (SELECT ctx IS NOT NULL FROM params) THEN
        (-0.9) * (
          SELECT e.vec <=> (SELECT ctx FROM params)
          FROM content.phrase_embedding e, params
          WHERE e.phrase_id = c.id AND e.model = params.model_name
          LIMIT 1
        )
      ELSE 0.0
     END)
  ) AS rank
FROM cand c
ORDER BY rank DESC
LIMIT (SELECT k FROM params);
$$;


ALTER FUNCTION content.search_phrases(p_tenant_id bigint, p_prefix text, p_lang text, p_model text, p_context public.vector, p_k integer, p_hint text) OWNER TO postgres;

--
-- Name: set_phrase_embedding(bigint, text, public.vector); Type: FUNCTION; Schema: content; Owner: postgres
--

CREATE FUNCTION content.set_phrase_embedding(p_phrase_id bigint, p_model text, p_vec public.vector) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
  INSERT INTO content.phrase_embedding(phrase_id, model, vec)
  VALUES (p_phrase_id, p_model, p_vec)
  ON CONFLICT (phrase_id, model)
  DO UPDATE SET vec = EXCLUDED.vec, created_at = now();
END;
$$;


ALTER FUNCTION content.set_phrase_embedding(p_phrase_id bigint, p_model text, p_vec public.vector) OWNER TO postgres;

--
-- Name: touch_updated_at(); Type: FUNCTION; Schema: content; Owner: postgres
--

CREATE FUNCTION content.touch_updated_at() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
  NEW.updated_at := now();
  RETURN NEW;
END;
$$;


ALTER FUNCTION content.touch_updated_at() OWNER TO postgres;

--
-- Name: label_en(bigint); Type: FUNCTION; Schema: snomedct; Owner: postgres
--

CREATE FUNCTION snomedct.label_en(p_conceptid bigint) RETURNS text
    LANGUAGE sql STABLE
    AS $$ SELECT snomedct.label_en(p_conceptid::varchar); $$;


ALTER FUNCTION snomedct.label_en(p_conceptid bigint) OWNER TO postgres;

--
-- Name: label_en(character varying); Type: FUNCTION; Schema: snomedct; Owner: postgres
--

CREATE FUNCTION snomedct.label_en(p_conceptid character varying) RETURNS text
    LANGUAGE sql STABLE
    AS $$
SELECT d.term
FROM snomedct.description_f d
JOIN snomedct.langrefset_f lr ON lr.referencedcomponentid = d.id
WHERE d.conceptid = p_conceptid
  AND d.languagecode='en' AND d.active='1'
  AND lr.active='1' AND lr.acceptabilityid='900000000000548007'
ORDER BY lr.effectivetime DESC, d.effectivetime DESC
LIMIT 1;
$$;


ALTER FUNCTION snomedct.label_en(p_conceptid character varying) OWNER TO postgres;

--
-- Name: label_ko(bigint); Type: FUNCTION; Schema: snomedct; Owner: postgres
--

CREATE FUNCTION snomedct.label_ko(p_conceptid bigint) RETURNS text
    LANGUAGE sql STABLE
    AS $$ SELECT snomedct.label_ko(p_conceptid::varchar); $$;


ALTER FUNCTION snomedct.label_ko(p_conceptid bigint) OWNER TO postgres;

--
-- Name: label_ko(character varying); Type: FUNCTION; Schema: snomedct; Owner: postgres
--

CREATE FUNCTION snomedct.label_ko(p_conceptid character varying) RETURNS text
    LANGUAGE sql STABLE
    AS $$
SELECT d.term
FROM snomedct.description_f d
JOIN snomedct.langrefset_f lr ON lr.referencedcomponentid = d.id
WHERE d.conceptid = p_conceptid
  AND d.languagecode='ko' AND d.active='1'
  AND lr.active='1' AND lr.acceptabilityid='900000000000548007'
ORDER BY lr.effectivetime DESC, d.effectivetime DESC
LIMIT 1;
$$;


ALTER FUNCTION snomedct.label_ko(p_conceptid character varying) OWNER TO postgres;

--
-- Name: label_ko_en(bigint); Type: FUNCTION; Schema: snomedct; Owner: postgres
--

CREATE FUNCTION snomedct.label_ko_en(p_conceptid bigint) RETURNS text
    LANGUAGE sql STABLE
    AS $$ SELECT snomedct.label_ko_en(p_conceptid::varchar); $$;


ALTER FUNCTION snomedct.label_ko_en(p_conceptid bigint) OWNER TO postgres;

--
-- Name: label_ko_en(character varying); Type: FUNCTION; Schema: snomedct; Owner: postgres
--

CREATE FUNCTION snomedct.label_ko_en(p_conceptid character varying) RETURNS text
    LANGUAGE sql STABLE
    AS $$
WITH ko AS (
  SELECT d.term
  FROM snomedct.description_f d
  JOIN snomedct.langrefset_f lr ON lr.referencedcomponentid = d.id
  WHERE d.conceptid = p_conceptid
    AND d.languagecode = 'ko' AND d.active='1'
    AND lr.active='1' AND lr.acceptabilityid='900000000000548007'
  ORDER BY lr.effectivetime DESC, d.effectivetime DESC
  LIMIT 1
),
en AS (
  SELECT d.term
  FROM snomedct.description_f d
  JOIN snomedct.langrefset_f lr ON lr.referencedcomponentid = d.id
  WHERE d.conceptid = p_conceptid
    AND d.languagecode = 'en' AND d.active='1'
    AND lr.active='1' AND lr.acceptabilityid='900000000000548007'
  ORDER BY lr.effectivetime DESC, d.effectivetime DESC
  LIMIT 1
)
SELECT COALESCE((SELECT term FROM ko),(SELECT term FROM en));
$$;


ALTER FUNCTION snomedct.label_ko_en(p_conceptid character varying) OWNER TO postgres;

--
-- Name: refresh_concept_latest(); Type: PROCEDURE; Schema: snomedct; Owner: postgres
--

CREATE PROCEDURE snomedct.refresh_concept_latest()
    LANGUAGE plpgsql
    AS $$

BEGIN

  -- TRUNCATE+INSERT keeps stats healthy and is simple/fast

  TRUNCATE TABLE snomedct.concept_latest;

  INSERT INTO snomedct.concept_latest(id, effectivetime, active, moduleid, definitionstatusid)

  SELECT DISTINCT ON (id)

         id, effectivetime, active, moduleid, definitionstatusid

  FROM   snomedct.concept_f

  ORDER  BY id, effectivetime DESC;

END$$;


ALTER PROCEDURE snomedct.refresh_concept_latest() OWNER TO postgres;

--
-- Name: search_labels(text, text, integer); Type: FUNCTION; Schema: snomedct; Owner: postgres
--

CREATE FUNCTION snomedct.search_labels(q text, lang text DEFAULT NULL::text, max_results integer DEFAULT 50) RETURNS TABLE(conceptid character varying, label_ko_en text, ko text, en text, matched_lang text, matched_term text, score real)
    LANGUAGE sql STABLE
    SET "pg_trgm.similarity_threshold" TO '0.10'
    AS $$
WITH cands AS (
  SELECT
    d.conceptid,
    d.languagecode AS matched_lang,
    d.term         AS matched_term,
    /* Score: prefix/substring boost + trigram + small bump if Preferred */
    (CASE
       WHEN d.term ILIKE q || '%'            THEN 0.90
       WHEN d.term ILIKE '% ' || q || '%'    THEN 0.80
       WHEN d.term ILIKE '%' || q || '%'     THEN 0.70
       ELSE 0.00
     END)
     + public.similarity(d.term, q)
     + COALESCE(CASE WHEN lr.acceptabilityid='900000000000548007' THEN 0.05 ELSE 0 END, 0) AS score,
    d.effectivetime
  FROM snomedct.description_f d
  LEFT JOIN snomedct.langrefset_f lr
         ON lr.referencedcomponentid = d.id
        AND lr.active='1'
  WHERE d.active='1'
    AND (lang IS NULL OR d.languagecode = lang)
    AND (
         d.term ILIKE '%' || q || '%'
         OR public.similarity(d.term, q) >= current_setting('pg_trgm.similarity_threshold')::real
        )
),
best AS (
  SELECT DISTINCT ON (conceptid)
         conceptid, matched_lang, matched_term, score, effectivetime
  FROM cands
  ORDER BY conceptid, score DESC, effectivetime DESC
)
SELECT
  b.conceptid,
  snomedct.label_ko_en(b.conceptid) AS label_ko_en,
  snomedct.label_ko(b.conceptid)    AS ko,
  snomedct.label_en(b.conceptid)    AS en,
  b.matched_lang,
  b.matched_term,
  b.score
FROM best b
ORDER BY b.score DESC
LIMIT COALESCE(max_results, 50);
$$;


ALTER FUNCTION snomedct.search_labels(q text, lang text, max_results integer) OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: tenant; Type: TABLE; Schema: app; Owner: postgres
--

CREATE TABLE app.tenant (
    id bigint NOT NULL,
    code text NOT NULL,
    name text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE app.tenant OWNER TO postgres;

--
-- Name: tenant_id_seq; Type: SEQUENCE; Schema: app; Owner: postgres
--

ALTER TABLE app.tenant ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME app.tenant_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: phrase; Type: TABLE; Schema: content; Owner: postgres
--

CREATE TABLE content.phrase (
    id bigint NOT NULL,
    tenant_id bigint NOT NULL,
    text text NOT NULL,
    text_norm text GENERATED ALWAYS AS (content.normalize_text(text)) STORED,
    case_sensitive boolean DEFAULT false NOT NULL,
    tags jsonb DEFAULT '{}'::jsonb NOT NULL,
    lang text DEFAULT 'en'::text NOT NULL,
    active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    tsv tsvector GENERATED ALWAYS AS (to_tsvector('simple'::regconfig, text)) STORED
);


ALTER TABLE content.phrase OWNER TO postgres;

--
-- Name: phrase_embedding; Type: TABLE; Schema: content; Owner: postgres
--

CREATE TABLE content.phrase_embedding (
    phrase_id bigint NOT NULL,
    model text NOT NULL,
    vec public.vector(1024) NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE content.phrase_embedding OWNER TO postgres;

--
-- Name: phrase_id_seq; Type: SEQUENCE; Schema: content; Owner: postgres
--

ALTER TABLE content.phrase ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME content.phrase_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: phrase_sct; Type: TABLE; Schema: content; Owner: postgres
--

CREATE TABLE content.phrase_sct (
    phrase_id bigint NOT NULL,
    root_concept_id character varying(18) NOT NULL,
    expression_cg text,
    edition text,
    module_id character varying(18),
    effective_time date,
    extras jsonb DEFAULT '{}'::jsonb NOT NULL
);


ALTER TABLE content.phrase_sct OWNER TO postgres;

--
-- Name: phrase_sct_attribute; Type: TABLE; Schema: content; Owner: postgres
--

CREATE TABLE content.phrase_sct_attribute (
    phrase_id bigint NOT NULL,
    role_group integer DEFAULT 0 NOT NULL,
    attribute_id character varying(18) NOT NULL,
    value_concept_id character varying(18) NOT NULL
);


ALTER TABLE content.phrase_sct_attribute OWNER TO postgres;

--
-- Name: phrase_search_denorm; Type: VIEW; Schema: content; Owner: postgres
--

CREATE VIEW content.phrase_search_denorm AS
 SELECT p.id,
    p.tenant_id,
    p.text,
    p.text_norm,
    p.case_sensitive,
    p.tags,
    p.lang,
    p.active,
    ps.root_concept_id,
    p.tsv,
    e.model,
    e.vec
   FROM ((content.phrase p
     LEFT JOIN content.phrase_sct ps ON ((ps.phrase_id = p.id)))
     LEFT JOIN content.phrase_embedding e ON ((e.phrase_id = p.id)));


ALTER VIEW content.phrase_search_denorm OWNER TO postgres;

--
-- Name: pairs; Type: TABLE; Schema: radstyle; Owner: postgres
--

CREATE TABLE radstyle.pairs (
    id bigint NOT NULL,
    tenant_id bigint NOT NULL,
    input text NOT NULL,
    output text NOT NULL,
    input_md5 text GENERATED ALWAYS AS (md5(input)) STORED,
    output_md5 text GENERATED ALWAYS AS (md5(output)) STORED,
    model text,
    temperature real,
    top_p real,
    prompt_version text DEFAULT 'v1'::text,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE radstyle.pairs OWNER TO postgres;

--
-- Name: pairs_id_seq; Type: SEQUENCE; Schema: radstyle; Owner: postgres
--

CREATE SEQUENCE radstyle.pairs_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE radstyle.pairs_id_seq OWNER TO postgres;

--
-- Name: pairs_id_seq; Type: SEQUENCE OWNED BY; Schema: radstyle; Owner: postgres
--

ALTER SEQUENCE radstyle.pairs_id_seq OWNED BY radstyle.pairs.id;


--
-- Name: concept_f; Type: TABLE; Schema: snomedct; Owner: postgres
--

CREATE TABLE snomedct.concept_f (
    id character varying(18) NOT NULL,
    effectivetime character(8) NOT NULL,
    active character(1) NOT NULL,
    moduleid character varying(18) NOT NULL,
    definitionstatusid character varying(18) NOT NULL
);


ALTER TABLE snomedct.concept_f OWNER TO postgres;

--
-- Name: concept_latest; Type: TABLE; Schema: snomedct; Owner: postgres
--

CREATE TABLE snomedct.concept_latest (
    id character varying(18) NOT NULL,
    effectivetime character(8),
    active character(1),
    moduleid character varying(18),
    definitionstatusid character varying(18)
);


ALTER TABLE snomedct.concept_latest OWNER TO postgres;

--
-- Name: description_f; Type: TABLE; Schema: snomedct; Owner: postgres
--

CREATE TABLE snomedct.description_f (
    id character varying(18) NOT NULL,
    effectivetime character(8) NOT NULL,
    active character(1) NOT NULL,
    moduleid character varying(18) NOT NULL,
    conceptid character varying(18) NOT NULL,
    languagecode character varying(2) NOT NULL,
    typeid character varying(18) NOT NULL,
    term text NOT NULL,
    casesignificanceid character varying(18) NOT NULL
);


ALTER TABLE snomedct.description_f OWNER TO postgres;

--
-- Name: langrefset_f; Type: TABLE; Schema: snomedct; Owner: postgres
--

CREATE TABLE snomedct.langrefset_f (
    id uuid NOT NULL,
    effectivetime character(8) NOT NULL,
    active character(1) NOT NULL,
    moduleid character varying(18) NOT NULL,
    refsetid character varying(18) NOT NULL,
    referencedcomponentid character varying(18) NOT NULL,
    acceptabilityid character varying(18) NOT NULL
);


ALTER TABLE snomedct.langrefset_f OWNER TO postgres;

--
-- Name: v_preferred_en; Type: VIEW; Schema: snomedct; Owner: postgres
--

CREATE VIEW snomedct.v_preferred_en AS
 SELECT DISTINCT ON (d.conceptid) d.conceptid,
    d.term
   FROM (snomedct.description_f d
     JOIN snomedct.langrefset_f lr ON (((lr.referencedcomponentid)::text = (d.id)::text)))
  WHERE (((d.languagecode)::text = 'en'::text) AND (d.active = '1'::bpchar) AND (lr.active = '1'::bpchar) AND ((lr.acceptabilityid)::text = '900000000000548007'::text))
  ORDER BY d.conceptid, d.effectivetime DESC;


ALTER VIEW snomedct.v_preferred_en OWNER TO postgres;

--
-- Name: v_preferred_ko; Type: VIEW; Schema: snomedct; Owner: postgres
--

CREATE VIEW snomedct.v_preferred_ko AS
 SELECT DISTINCT ON (d.conceptid) d.conceptid,
    d.term
   FROM (snomedct.description_f d
     JOIN snomedct.langrefset_f lr ON (((lr.referencedcomponentid)::text = (d.id)::text)))
  WHERE (((d.languagecode)::text = 'ko'::text) AND (d.active = '1'::bpchar) AND (lr.active = '1'::bpchar) AND ((lr.acceptabilityid)::text = '900000000000548007'::text))
  ORDER BY d.conceptid, d.effectivetime DESC;


ALTER VIEW snomedct.v_preferred_ko OWNER TO postgres;

--
-- Name: v_label_ko_en; Type: VIEW; Schema: snomedct; Owner: postgres
--

CREATE VIEW snomedct.v_label_ko_en AS
 SELECT c.id AS conceptid,
    COALESCE(ko.term, en.term) AS label_ko_en,
    en.term AS en_term,
    ko.term AS ko_term
   FROM ((( SELECT DISTINCT concept_f.id
           FROM snomedct.concept_f) c
     LEFT JOIN snomedct.v_preferred_en en ON (((en.conceptid)::text = (c.id)::text)))
     LEFT JOIN snomedct.v_preferred_ko ko ON (((ko.conceptid)::text = (c.id)::text)));


ALTER VIEW snomedct.v_label_ko_en OWNER TO postgres;

--
-- Name: mv_label_ko_en; Type: MATERIALIZED VIEW; Schema: snomedct; Owner: postgres
--

CREATE MATERIALIZED VIEW snomedct.mv_label_ko_en AS
 SELECT conceptid,
    label_ko_en,
    en_term,
    ko_term
   FROM snomedct.v_label_ko_en
  WITH NO DATA;


ALTER MATERIALIZED VIEW snomedct.mv_label_ko_en OWNER TO postgres;

--
-- Name: relationship_f; Type: TABLE; Schema: snomedct; Owner: postgres
--

CREATE TABLE snomedct.relationship_f (
    id character varying(18) NOT NULL,
    effectivetime character(8) NOT NULL,
    active character(1) NOT NULL,
    moduleid character varying(18) NOT NULL,
    sourceid character varying(18) NOT NULL,
    destinationid character varying(18) NOT NULL,
    relationshipgroup integer NOT NULL,
    typeid character varying(18) NOT NULL,
    characteristictypeid character varying(18) NOT NULL,
    modifierid character varying(18) NOT NULL
);


ALTER TABLE snomedct.relationship_f OWNER TO postgres;

--
-- Name: textdefinition_f; Type: TABLE; Schema: snomedct; Owner: postgres
--

CREATE TABLE snomedct.textdefinition_f (
    id character varying(18) NOT NULL,
    effectivetime character(8) NOT NULL,
    active character(1) NOT NULL,
    moduleid character varying(18) NOT NULL,
    conceptid character varying(18) NOT NULL,
    languagecode character varying(2) NOT NULL,
    typeid character varying(18) NOT NULL,
    term text NOT NULL,
    casesignificanceid character varying(18) NOT NULL
);


ALTER TABLE snomedct.textdefinition_f OWNER TO postgres;

--
-- Name: pairs id; Type: DEFAULT; Schema: radstyle; Owner: postgres
--

ALTER TABLE ONLY radstyle.pairs ALTER COLUMN id SET DEFAULT nextval('radstyle.pairs_id_seq'::regclass);


--
-- Name: tenant tenant_code_key; Type: CONSTRAINT; Schema: app; Owner: postgres
--

ALTER TABLE ONLY app.tenant
    ADD CONSTRAINT tenant_code_key UNIQUE (code);


--
-- Name: tenant tenant_name_unique; Type: CONSTRAINT; Schema: app; Owner: postgres
--

ALTER TABLE ONLY app.tenant
    ADD CONSTRAINT tenant_name_unique UNIQUE (name);


--
-- Name: tenant tenant_pkey; Type: CONSTRAINT; Schema: app; Owner: postgres
--

ALTER TABLE ONLY app.tenant
    ADD CONSTRAINT tenant_pkey PRIMARY KEY (id);


--
-- Name: phrase_embedding phrase_embedding_pkey; Type: CONSTRAINT; Schema: content; Owner: postgres
--

ALTER TABLE ONLY content.phrase_embedding
    ADD CONSTRAINT phrase_embedding_pkey PRIMARY KEY (phrase_id, model);


--
-- Name: phrase phrase_pkey; Type: CONSTRAINT; Schema: content; Owner: postgres
--

ALTER TABLE ONLY content.phrase
    ADD CONSTRAINT phrase_pkey PRIMARY KEY (id);


--
-- Name: phrase_sct_attribute phrase_sct_attribute_pkey; Type: CONSTRAINT; Schema: content; Owner: postgres
--

ALTER TABLE ONLY content.phrase_sct_attribute
    ADD CONSTRAINT phrase_sct_attribute_pkey PRIMARY KEY (phrase_id, role_group, attribute_id, value_concept_id);


--
-- Name: phrase_sct phrase_sct_pkey; Type: CONSTRAINT; Schema: content; Owner: postgres
--

ALTER TABLE ONLY content.phrase_sct
    ADD CONSTRAINT phrase_sct_pkey PRIMARY KEY (phrase_id);


--
-- Name: pairs pairs_pkey; Type: CONSTRAINT; Schema: radstyle; Owner: postgres
--

ALTER TABLE ONLY radstyle.pairs
    ADD CONSTRAINT pairs_pkey PRIMARY KEY (id);


--
-- Name: pairs pairs_tenant_id_input_md5_output_md5_key; Type: CONSTRAINT; Schema: radstyle; Owner: postgres
--

ALTER TABLE ONLY radstyle.pairs
    ADD CONSTRAINT pairs_tenant_id_input_md5_output_md5_key UNIQUE (tenant_id, input_md5, output_md5);


--
-- Name: concept_f concept_f_pkey; Type: CONSTRAINT; Schema: snomedct; Owner: postgres
--

ALTER TABLE ONLY snomedct.concept_f
    ADD CONSTRAINT concept_f_pkey PRIMARY KEY (id, effectivetime);


--
-- Name: concept_latest concept_latest_pkey; Type: CONSTRAINT; Schema: snomedct; Owner: postgres
--

ALTER TABLE ONLY snomedct.concept_latest
    ADD CONSTRAINT concept_latest_pkey PRIMARY KEY (id);


--
-- Name: description_f description_f_pkey; Type: CONSTRAINT; Schema: snomedct; Owner: postgres
--

ALTER TABLE ONLY snomedct.description_f
    ADD CONSTRAINT description_f_pkey PRIMARY KEY (id, effectivetime);


--
-- Name: langrefset_f langrefset_f_pkey; Type: CONSTRAINT; Schema: snomedct; Owner: postgres
--

ALTER TABLE ONLY snomedct.langrefset_f
    ADD CONSTRAINT langrefset_f_pkey PRIMARY KEY (id, effectivetime);


--
-- Name: relationship_f relationship_f_pkey; Type: CONSTRAINT; Schema: snomedct; Owner: postgres
--

ALTER TABLE ONLY snomedct.relationship_f
    ADD CONSTRAINT relationship_f_pkey PRIMARY KEY (id, effectivetime);


--
-- Name: textdefinition_f textdefinition_f_pkey; Type: CONSTRAINT; Schema: snomedct; Owner: postgres
--

ALTER TABLE ONLY snomedct.textdefinition_f
    ADD CONSTRAINT textdefinition_f_pkey PRIMARY KEY (id, effectivetime);


--
-- Name: ix_phrase_embedding_vec; Type: INDEX; Schema: content; Owner: postgres
--

CREATE INDEX ix_phrase_embedding_vec ON content.phrase_embedding USING ivfflat (vec public.vector_cosine_ops) WITH (lists='100');


--
-- Name: ix_phrase_sct_attr_attr; Type: INDEX; Schema: content; Owner: postgres
--

CREATE INDEX ix_phrase_sct_attr_attr ON content.phrase_sct_attribute USING btree (attribute_id);


--
-- Name: ix_phrase_sct_attr_value; Type: INDEX; Schema: content; Owner: postgres
--

CREATE INDEX ix_phrase_sct_attr_value ON content.phrase_sct_attribute USING btree (value_concept_id);


--
-- Name: ix_phrase_tags; Type: INDEX; Schema: content; Owner: postgres
--

CREATE INDEX ix_phrase_tags ON content.phrase USING gin (tags);


--
-- Name: ix_phrase_trgm; Type: INDEX; Schema: content; Owner: postgres
--

CREATE INDEX ix_phrase_trgm ON content.phrase USING gin (text_norm public.gin_trgm_ops);


--
-- Name: ix_phrase_tsv; Type: INDEX; Schema: content; Owner: postgres
--

CREATE INDEX ix_phrase_tsv ON content.phrase USING gin (tsv);


--
-- Name: uq_phrase_text_ci; Type: INDEX; Schema: content; Owner: postgres
--

CREATE UNIQUE INDEX uq_phrase_text_ci ON content.phrase USING btree (tenant_id, lower(text)) WHERE (case_sensitive = false);


--
-- Name: uq_phrase_text_cs; Type: INDEX; Schema: content; Owner: postgres
--

CREATE UNIQUE INDEX uq_phrase_text_cs ON content.phrase USING btree (tenant_id, text) WHERE (case_sensitive = true);


--
-- Name: idx_pairs_tenant_created; Type: INDEX; Schema: radstyle; Owner: postgres
--

CREATE INDEX idx_pairs_tenant_created ON radstyle.pairs USING btree (tenant_id, created_at DESC);


--
-- Name: description_lang_active_idx; Type: INDEX; Schema: snomedct; Owner: postgres
--

CREATE INDEX description_lang_active_idx ON snomedct.description_f USING btree (languagecode, active);


--
-- Name: description_term_trgm_idx; Type: INDEX; Schema: snomedct; Owner: postgres
--

CREATE INDEX description_term_trgm_idx ON snomedct.description_f USING gin (term public.gin_trgm_ops);


--
-- Name: ix_concept_latest_effectivetime; Type: INDEX; Schema: snomedct; Owner: postgres
--

CREATE INDEX ix_concept_latest_effectivetime ON snomedct.concept_latest USING btree (effectivetime);


--
-- Name: ix_desc_concept; Type: INDEX; Schema: snomedct; Owner: postgres
--

CREATE INDEX ix_desc_concept ON snomedct.description_f USING btree (conceptid);


--
-- Name: ix_lang_refcomp; Type: INDEX; Schema: snomedct; Owner: postgres
--

CREATE INDEX ix_lang_refcomp ON snomedct.langrefset_f USING btree (referencedcomponentid);


--
-- Name: ix_lang_refset; Type: INDEX; Schema: snomedct; Owner: postgres
--

CREATE INDEX ix_lang_refset ON snomedct.langrefset_f USING btree (refsetid);


--
-- Name: ix_rel_dest; Type: INDEX; Schema: snomedct; Owner: postgres
--

CREATE INDEX ix_rel_dest ON snomedct.relationship_f USING btree (destinationid);


--
-- Name: ix_rel_source; Type: INDEX; Schema: snomedct; Owner: postgres
--

CREATE INDEX ix_rel_source ON snomedct.relationship_f USING btree (sourceid);


--
-- Name: ix_rel_type; Type: INDEX; Schema: snomedct; Owner: postgres
--

CREATE INDEX ix_rel_type ON snomedct.relationship_f USING btree (typeid);


--
-- Name: ix_textdef_concept; Type: INDEX; Schema: snomedct; Owner: postgres
--

CREATE INDEX ix_textdef_concept ON snomedct.textdefinition_f USING btree (conceptid);


--
-- Name: mv_label_ko_en_conceptid_idx; Type: INDEX; Schema: snomedct; Owner: postgres
--

CREATE INDEX mv_label_ko_en_conceptid_idx ON snomedct.mv_label_ko_en USING btree (conceptid);


--
-- Name: mv_label_ko_en_label_idx; Type: INDEX; Schema: snomedct; Owner: postgres
--

CREATE INDEX mv_label_ko_en_label_idx ON snomedct.mv_label_ko_en USING btree (label_ko_en);


--
-- Name: phrase trg_phrase_touch; Type: TRIGGER; Schema: content; Owner: postgres
--

CREATE TRIGGER trg_phrase_touch BEFORE UPDATE ON content.phrase FOR EACH ROW EXECUTE FUNCTION content.touch_updated_at();


--
-- Name: phrase_embedding phrase_embedding_phrase_id_fkey; Type: FK CONSTRAINT; Schema: content; Owner: postgres
--

ALTER TABLE ONLY content.phrase_embedding
    ADD CONSTRAINT phrase_embedding_phrase_id_fkey FOREIGN KEY (phrase_id) REFERENCES content.phrase(id) ON DELETE CASCADE;


--
-- Name: phrase_sct_attribute phrase_sct_attribute_attribute_id_fkey; Type: FK CONSTRAINT; Schema: content; Owner: postgres
--

ALTER TABLE ONLY content.phrase_sct_attribute
    ADD CONSTRAINT phrase_sct_attribute_attribute_id_fkey FOREIGN KEY (attribute_id) REFERENCES snomedct.concept_latest(id);


--
-- Name: phrase_sct_attribute phrase_sct_attribute_phrase_id_fkey; Type: FK CONSTRAINT; Schema: content; Owner: postgres
--

ALTER TABLE ONLY content.phrase_sct_attribute
    ADD CONSTRAINT phrase_sct_attribute_phrase_id_fkey FOREIGN KEY (phrase_id) REFERENCES content.phrase(id) ON DELETE CASCADE;


--
-- Name: phrase_sct_attribute phrase_sct_attribute_value_concept_id_fkey; Type: FK CONSTRAINT; Schema: content; Owner: postgres
--

ALTER TABLE ONLY content.phrase_sct_attribute
    ADD CONSTRAINT phrase_sct_attribute_value_concept_id_fkey FOREIGN KEY (value_concept_id) REFERENCES snomedct.concept_latest(id);


--
-- Name: phrase_sct phrase_sct_phrase_id_fkey; Type: FK CONSTRAINT; Schema: content; Owner: postgres
--

ALTER TABLE ONLY content.phrase_sct
    ADD CONSTRAINT phrase_sct_phrase_id_fkey FOREIGN KEY (phrase_id) REFERENCES content.phrase(id) ON DELETE CASCADE;


--
-- Name: phrase phrase_tenant_id_fkey; Type: FK CONSTRAINT; Schema: content; Owner: postgres
--

ALTER TABLE ONLY content.phrase
    ADD CONSTRAINT phrase_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES app.tenant(id);


--
-- Name: pairs pairs_tenant_id_fkey; Type: FK CONSTRAINT; Schema: radstyle; Owner: postgres
--

ALTER TABLE ONLY radstyle.pairs
    ADD CONSTRAINT pairs_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES app.tenant(id) ON DELETE RESTRICT;


--
-- Name: SCHEMA app; Type: ACL; Schema: -; Owner: postgres
--

GRANT USAGE ON SCHEMA app TO wysg_app_ro;
GRANT USAGE ON SCHEMA app TO wysg_app_rw;
GRANT USAGE ON SCHEMA app TO appuser;


--
-- Name: SCHEMA auth; Type: ACL; Schema: -; Owner: postgres
--

GRANT USAGE ON SCHEMA auth TO wysg_app_ro;
GRANT USAGE ON SCHEMA auth TO wysg_app_rw;


--
-- Name: SCHEMA content; Type: ACL; Schema: -; Owner: postgres
--

GRANT USAGE ON SCHEMA content TO wysg_app_ro;
GRANT USAGE ON SCHEMA content TO wysg_app_rw;
GRANT USAGE ON SCHEMA content TO appuser;


--
-- Name: SCHEMA ghost; Type: ACL; Schema: -; Owner: postgres
--

GRANT USAGE ON SCHEMA ghost TO wysg_app_ro;
GRANT USAGE ON SCHEMA ghost TO wysg_app_rw;


--
-- Name: SCHEMA ops; Type: ACL; Schema: -; Owner: postgres
--

GRANT USAGE ON SCHEMA ops TO wysg_app_ro;
GRANT USAGE ON SCHEMA ops TO wysg_app_rw;


--
-- Name: FUNCTION ensure_phrase(p_tenant_name text, p_text text, p_case_sensitive boolean, p_lang text, p_tags jsonb); Type: ACL; Schema: content; Owner: postgres
--

GRANT ALL ON FUNCTION content.ensure_phrase(p_tenant_name text, p_text text, p_case_sensitive boolean, p_lang text, p_tags jsonb) TO appuser;


--
-- Name: FUNCTION normalize_text(s text); Type: ACL; Schema: content; Owner: postgres
--

GRANT ALL ON FUNCTION content.normalize_text(s text) TO appuser;


--
-- Name: FUNCTION touch_updated_at(); Type: ACL; Schema: content; Owner: postgres
--

GRANT ALL ON FUNCTION content.touch_updated_at() TO appuser;


--
-- Name: TABLE tenant; Type: ACL; Schema: app; Owner: postgres
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE app.tenant TO wysg_app_rw;
GRANT SELECT ON TABLE app.tenant TO wysg_app_ro;
GRANT SELECT,INSERT,UPDATE ON TABLE app.tenant TO appuser;


--
-- Name: SEQUENCE tenant_id_seq; Type: ACL; Schema: app; Owner: postgres
--

GRANT SELECT,USAGE ON SEQUENCE app.tenant_id_seq TO appuser;


--
-- Name: TABLE phrase; Type: ACL; Schema: content; Owner: postgres
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE content.phrase TO wysg_app_rw;
GRANT SELECT ON TABLE content.phrase TO wysg_app_ro;
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE content.phrase TO appuser;


--
-- Name: TABLE phrase_embedding; Type: ACL; Schema: content; Owner: postgres
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE content.phrase_embedding TO wysg_app_rw;
GRANT SELECT ON TABLE content.phrase_embedding TO wysg_app_ro;
GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE content.phrase_embedding TO appuser;


--
-- Name: SEQUENCE phrase_id_seq; Type: ACL; Schema: content; Owner: postgres
--

GRANT SELECT,USAGE ON SEQUENCE content.phrase_id_seq TO appuser;


--
-- Name: TABLE phrase_sct; Type: ACL; Schema: content; Owner: postgres
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE content.phrase_sct TO wysg_app_rw;
GRANT SELECT ON TABLE content.phrase_sct TO wysg_app_ro;


--
-- Name: TABLE phrase_sct_attribute; Type: ACL; Schema: content; Owner: postgres
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE content.phrase_sct_attribute TO wysg_app_rw;
GRANT SELECT ON TABLE content.phrase_sct_attribute TO wysg_app_ro;


--
-- Name: TABLE phrase_search_denorm; Type: ACL; Schema: content; Owner: postgres
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE content.phrase_search_denorm TO wysg_app_rw;
GRANT SELECT ON TABLE content.phrase_search_denorm TO wysg_app_ro;


--
-- Name: DEFAULT PRIVILEGES FOR TABLES; Type: DEFAULT ACL; Schema: app; Owner: postgres
--

ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA app GRANT SELECT,INSERT,DELETE,UPDATE ON TABLES TO wysg_app_rw;
ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA app GRANT SELECT ON TABLES TO wysg_app_ro;


--
-- Name: DEFAULT PRIVILEGES FOR TABLES; Type: DEFAULT ACL; Schema: content; Owner: postgres
--

ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA content GRANT SELECT,INSERT,DELETE,UPDATE ON TABLES TO wysg_app_rw;
ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA content GRANT SELECT ON TABLES TO wysg_app_ro;


--
-- Name: DEFAULT PRIVILEGES FOR TABLES; Type: DEFAULT ACL; Schema: ghost; Owner: postgres
--

ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA ghost GRANT SELECT,INSERT,DELETE,UPDATE ON TABLES TO wysg_app_rw;
ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA ghost GRANT SELECT ON TABLES TO wysg_app_ro;


--
-- PostgreSQL database dump complete
--

\unrestrict dIQ75r01oCyZsmnmnbuITrUcZY4UkAqaZ2KJe2KTFq1tGbnIikBHOA9jouHdPzH

