-- Platform Database Schemas
-- Creates the necessary schemas for the platform database

-- Create main application schema
CREATE SCHEMA IF NOT EXISTS app;

-- Create audit schema for tracking changes
CREATE SCHEMA IF NOT EXISTS audit;

-- Grant usage on schemas
GRANT USAGE ON SCHEMA app TO platformuser;
GRANT USAGE ON SCHEMA audit TO platformuser;

-- Set default schema search path
ALTER DATABASE platformdb SET search_path TO app, public;

-- Schema documentation
COMMENT ON SCHEMA app IS 'Main application schema containing core platform tables';
COMMENT ON SCHEMA audit IS 'Audit schema for tracking data changes and user actions';

DO $$
BEGIN
    RAISE NOTICE 'Platform database schemas created successfully.';
END $$;