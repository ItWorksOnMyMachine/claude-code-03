-- Auth Database Initialization Script
-- This script runs automatically when the PostgreSQL container starts

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create initial schema message
DO $$
BEGIN
    RAISE NOTICE 'Auth database initialized. Run Duende migrations to create schema.';
END $$;