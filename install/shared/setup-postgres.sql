-- PostgreSQL: run as superuser against maintenance DB, e.g.:
--   psql -U postgres -d postgres -v ON_ERROR_STOP=1 -f setup-postgres.sql
-- Database name: hephaestus

DO $$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'tss') THEN
    CREATE ROLE tss LOGIN PASSWORD '123' CREATEDB;
  ELSE
    ALTER ROLE tss WITH PASSWORD '123';
  END IF;
END $$;

SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname = 'hephaestus'
  AND pid <> pg_backend_pid();

DROP DATABASE IF EXISTS hephaestus;

CREATE DATABASE hephaestus
  OWNER tss
  ENCODING 'UTF8'
  TEMPLATE template0;

\connect hephaestus

-- Keep NOTICEs off stderr (e.g. DROP IF EXISTS) so PowerShell does not treat psql as failed.
SET client_min_messages = WARNING;

DROP VIEW IF EXISTS daily_server_serie_stats_view;
DROP VIEW IF EXISTS download_log_view;
DROP VIEW IF EXISTS bot_log_view;

DROP TABLE IF EXISTS dn_log;
DROP TABLE IF EXISTS bot_log;

CREATE TABLE bot_log (
  id                         VARCHAR(100) PRIMARY KEY,
  server                     VARCHAR(30),
  first_seen                 TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
  last_seen                  TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
  first_seen_ip              VARCHAR(30),
  last_seen_ip               VARCHAR(30),
  serie                      VARCHAR(100),
  number_of_requests         INT DEFAULT 1,
  number_of_elevated_requests INT DEFAULT 0,
  number_of_downloads        INT,
  install_calculated         TIMESTAMP WITHOUT TIME ZONE,
  uninstall_calculated       TIMESTAMP WITHOUT TIME ZONE
);

CREATE TABLE dn_log (
  ip                VARCHAR(15) PRIMARY KEY,
  server            VARCHAR(15),
  profile           VARCHAR(100),
  first_seen        TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
  last_seen         TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
  number_of_requests INT DEFAULT 1
);

CREATE OR REPLACE FUNCTION upsert_bot_log(
  p_server   VARCHAR(15),
  p_ip       VARCHAR(15),
  p_id       VARCHAR(100),
  p_elevated INT DEFAULT 0,
  p_serie    VARCHAR(100) DEFAULT NULL,
  p_time_dif INT DEFAULT 0
) RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
  INSERT INTO bot_log (
    id, server, first_seen, last_seen, first_seen_ip, last_seen_ip, serie,
    number_of_requests, number_of_elevated_requests, number_of_downloads,
    install_calculated, uninstall_calculated
  )
  VALUES (
    p_id, p_server, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, p_ip, p_ip, p_serie,
    1, p_elevated, NULL, NULL, NULL
  )
  ON CONFLICT (id) DO UPDATE SET
    last_seen = CURRENT_TIMESTAMP,
    last_seen_ip = EXCLUDED.last_seen_ip,
    number_of_requests = bot_log.number_of_requests + 1,
    number_of_elevated_requests = bot_log.number_of_elevated_requests + p_elevated;
END;
$$;

CREATE OR REPLACE FUNCTION log_dn(
  p_server  VARCHAR(15),
  p_profile VARCHAR(100),
  p_ip      VARCHAR(15)
) RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
  INSERT INTO dn_log (ip, server, profile, first_seen, last_seen, number_of_requests)
  VALUES (p_ip, p_server, p_profile, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 1)
  ON CONFLICT (ip) DO UPDATE SET
    last_seen = CURRENT_TIMESTAMP,
    number_of_requests = dn_log.number_of_requests + 1;
END;
$$;

CREATE OR REPLACE FUNCTION clean()
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
  DELETE FROM dn_log
  WHERE first_seen < (CURRENT_TIMESTAMP - INTERVAL '48 hours');
END;
$$;

CREATE OR REPLACE FUNCTION calc_stats()
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
  UPDATE bot_log
  SET install_calculated = first_seen
  WHERE install_calculated IS NULL
    AND ABS(EXTRACT(EPOCH FROM (last_seen - first_seen)) / 60.0) <= 5;

  UPDATE bot_log
  SET uninstall_calculated = last_seen
  WHERE uninstall_calculated IS NULL
    AND (last_seen::date - first_seen::date) > 10;
END;
$$;

CREATE OR REPLACE VIEW bot_log_view AS
SELECT *
FROM (
  SELECT
    bl.id,
    bl.server,
    bl.first_seen,
    bl.last_seen,
    bl.first_seen_ip,
    bl.last_seen_ip,
    bl.serie,
    bl.number_of_requests,
    bl.number_of_elevated_requests,
    (
      SELECT COUNT(*)::bigint
      FROM dn_log dl
      WHERE dl.ip = bl.first_seen_ip
        AND CAST(dl.first_seen AS date) = CAST(bl.first_seen AS date)
    ) AS number_of_downloads
  FROM bot_log bl
  LIMIT 1000
) sub;

CREATE OR REPLACE VIEW download_log_view AS
SELECT *
FROM (
  SELECT
    ip,
    server,
    profile,
    first_seen,
    last_seen,
    number_of_requests
  FROM dn_log
  LIMIT 1000
) sub;

CREATE OR REPLACE VIEW daily_server_serie_stats_view AS
SELECT
  grp.d0 AS stat_date,
  grp.srv AS server,
  grp.ser AS serie,
  grp.unique_id_count,
  grp.elevated_unique_id_count,
  (
    SELECT COUNT(*)::bigint
    FROM dn_log d
    WHERE CAST(d.first_seen AS date) = grp.d0
      AND EXISTS (
        SELECT 1
        FROM bot_log b2
        WHERE b2.first_seen_ip = d.ip
          AND CAST(b2.first_seen AS date) = grp.d0
          AND b2.server = grp.srv
          AND COALESCE(b2.serie, 'not specified') = grp.ser
      )
  ) AS number_of_downloads,
  grp.install_count,
  grp.uninstall_count
FROM (
  SELECT
    CAST(first_seen AS date) AS d0,
    server AS srv,
    COALESCE(serie, 'not specified') AS ser,
    COUNT(DISTINCT id) AS unique_id_count,
    COUNT(DISTINCT CASE WHEN number_of_elevated_requests > 0 THEN id END) AS elevated_unique_id_count,
    SUM(
      CASE
        WHEN install_calculated IS NOT NULL
          AND CAST(first_seen AS date) = CAST(install_calculated AS date)
        THEN 1 ELSE 0
      END
    ) AS install_count,
    SUM(
      CASE
        WHEN uninstall_calculated IS NOT NULL
          AND CAST(last_seen AS date) = CAST(uninstall_calculated AS date)
        THEN 1 ELSE 0
      END
    ) AS uninstall_count
  FROM bot_log
  GROUP BY CAST(first_seen AS date), server, COALESCE(serie, 'not specified')
) grp;

ALTER SCHEMA public OWNER TO tss;
ALTER TABLE bot_log OWNER TO tss;
ALTER TABLE dn_log OWNER TO tss;
ALTER VIEW bot_log_view OWNER TO tss;
ALTER VIEW download_log_view OWNER TO tss;
ALTER VIEW daily_server_serie_stats_view OWNER TO tss;

DO $$
DECLARE
  r RECORD;
BEGIN
  FOR r IN
    SELECT p.oid::regprocedure AS fp
    FROM pg_proc p
    JOIN pg_namespace n ON n.oid = p.pronamespace
    WHERE n.nspname = 'public'
      AND p.proname = ANY (ARRAY['upsert_bot_log', 'log_dn', 'clean', 'calc_stats'])
  LOOP
    EXECUTE format('ALTER FUNCTION %s OWNER TO tss', r.fp);
  END LOOP;
END $$;
