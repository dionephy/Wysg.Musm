CREATE TABLE IF NOT EXISTS app.account
(
    account_id bigint NOT NULL DEFAULT nextval('app.account_account_id_seq'::regclass),
    uid text COLLATE pg_catalog."default" NOT NULL,
    email text COLLATE pg_catalog."default" NOT NULL,
    display_name text COLLATE pg_catalog."default",
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    last_login_at timestamp with time zone,
    CONSTRAINT account_pkey PRIMARY KEY (account_id),
    CONSTRAINT account_email_key UNIQUE (email),
    CONSTRAINT account_uid_key UNIQUE (uid)
)

TABLESPACE pg_default;

ALTER TABLE app.account
    OWNER to postgres;



CREATE TABLE IF NOT EXISTS radium.phrase
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    account_id bigint NOT NULL,
    text text COLLATE pg_catalog."default" NOT NULL,
    active boolean NOT NULL DEFAULT true,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    updated_at timestamp with time zone NOT NULL DEFAULT now(),
    rev bigint NOT NULL DEFAULT nextval('radium.phrase_rev_seq'::regclass),
    CONSTRAINT phrase_pkey PRIMARY KEY (id),
    CONSTRAINT uq_radium_phrase UNIQUE (account_id, text),
    CONSTRAINT phrase_account_id_fkey FOREIGN KEY (account_id)
        REFERENCES app.account (account_id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE,
    CONSTRAINT ck_phrase_text_not_blank CHECK (btrim(text) <> ''::text)
)

TABLESPACE pg_default;

ALTER TABLE radium.phrase
    OWNER to postgres;

-- Index: radium.ix_phrase_account_active
CREATE INDEX IF NOT EXISTS ix_phrase_account_active
    ON radium.phrase USING btree
    (account_id ASC NULLS LAST, active ASC NULLS LAST)
    TABLESPACE pg_default;
-- Index: radium.ix_phrase_account_rev
CREATE INDEX IF NOT EXISTS ix_phrase_account_rev
    ON radium.phrase USING btree
    (account_id ASC NULLS LAST, rev ASC NULLS LAST)
    TABLESPACE pg_default;
-- Trigger: trg_phrase_touch
CREATE OR REPLACE TRIGGER trg_phrase_touch
    BEFORE UPDATE 
    ON radium.phrase
    FOR EACH ROW
    EXECUTE FUNCTION radium.touch_phrase();









CREATE FUNCTION radium.touch_phrase()
    RETURNS trigger
    LANGUAGE plpgsql
    COST 100
    VOLATILE NOT LEAKPROOF
AS $BODY$ BEGIN IF (NEW.active IS DISTINCT FROM OLD.active OR NEW.text IS DISTINCT FROM OLD.text) THEN NEW.updated_at := now(); NEW.rev := nextval('radium.phrase_rev_seq'); ELSE -- no logical change: keep original timestamps / rev 
NEW.updated_at := OLD.updated_at; NEW.rev := OLD.rev; END IF; RETURN NEW; END $BODY$;

ALTER FUNCTION radium.touch_phrase()
    OWNER TO postgres;

