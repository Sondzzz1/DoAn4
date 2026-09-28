-- =============================================
-- MIGRATION: Add Latitude and Longitude to Rooms Table
-- Date: 2026-09-26
-- Purpose: Support Google Maps integration
-- =============================================

USE [RoomRentalDb];
GO

-- Check if columns already exist before adding
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('Rooms') 
    AND name = 'Latitude'
)
BEGIN
    ALTER TABLE Rooms
    ADD Latitude DECIMAL(10, 7) NULL;
    
    PRINT 'Column Latitude added to Rooms table';
END
ELSE
BEGIN
    PRINT 'Column Latitude already exists in Rooms table';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('Rooms') 
    AND name = 'Longitude'
)
BEGIN
    ALTER TABLE Rooms
    ADD Longitude DECIMAL(10, 7) NULL;
    
    PRINT 'Column Longitude added to Rooms table';
END
ELSE
BEGIN
    PRINT 'Column Longitude already exists in Rooms table';
END
GO

-- Add index for location-based queries (future feature: search by radius)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_Rooms_Location' 
    AND object_id = OBJECT_ID('Rooms')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Rooms_Location
    ON Rooms (Latitude, Longitude)
    WHERE Latitude IS NOT NULL AND Longitude IS NOT NULL;
    
    PRINT 'Index IX_Rooms_Location created';
END
ELSE
BEGIN
    PRINT 'Index IX_Rooms_Location already exists';
END
GO

-- Verification
SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    NUMERIC_PRECISION,
    NUMERIC_SCALE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Rooms'
AND COLUMN_NAME IN ('Latitude', 'Longitude');
GO

PRINT '✓ Migration completed successfully';
PRINT '✓ Latitude and Longitude columns are now available in Rooms table';
PRINT '⚠ Note: Existing rooms will have NULL values for coordinates';
GO
