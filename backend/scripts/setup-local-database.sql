\set ON_ERROR_STOP on

SELECT 'CREATE ROLE mapcepte LOGIN PASSWORD ''mapcepte_dev'''
WHERE NOT EXISTS (
    SELECT 1
    FROM pg_catalog.pg_roles
    WHERE rolname = 'mapcepte'
)
\gexec

ALTER ROLE mapcepte WITH LOGIN PASSWORD 'mapcepte_dev';

SELECT 'CREATE DATABASE mapcepte OWNER mapcepte'
WHERE NOT EXISTS (
    SELECT 1
    FROM pg_catalog.pg_database
    WHERE datname = 'mapcepte'
)
\gexec

\connect mapcepte

CREATE EXTENSION IF NOT EXISTS postgis;
