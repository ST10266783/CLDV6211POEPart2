-- ============================================================
-- EventEase Database Setup Script
-- Target: SQL Server / LocalDB
-- Generated for: EventEaseDB
-- ============================================================

USE master;
GO

-- Create database if it does not exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'EventEaseDB')
BEGIN
    CREATE DATABASE EventEaseDB;
END
GO

USE EventEaseDB;
GO

-- ============================================================
-- TABLE: Venues
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Venues')
BEGIN
    CREATE TABLE Venues (
        VenueId       INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        Name          NVARCHAR(200)   NOT NULL,
        Location      NVARCHAR(300)   NOT NULL,
        Capacity      INT             NOT NULL,
        Description   NVARCHAR(1000)  NULL,
        ImageUrl      NVARCHAR(500)   NULL,
        IsAvailable   BIT             NOT NULL DEFAULT 1
    );
END
GO

-- ============================================================
-- TABLE: Events
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Events')
BEGIN
    CREATE TABLE Events (
        EventId       INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        Name          NVARCHAR(200)   NOT NULL,
        Description   NVARCHAR(1000)  NULL,
        StartDate     DATETIME2       NOT NULL,
        EndDate       DATETIME2       NOT NULL,
        ImageUrl      NVARCHAR(500)   NULL
    );
END
GO

-- ============================================================
-- TABLE: Bookings
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Bookings')
BEGIN
    CREATE TABLE Bookings (
        BookingId       INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        VenueId         INT             NOT NULL,
        EventId         INT             NOT NULL,
        StartDate       DATETIME2       NOT NULL,
        EndDate         DATETIME2       NOT NULL,
        CustomerName    NVARCHAR(200)   NOT NULL,
        CustomerEmail   NVARCHAR(200)   NOT NULL,
        CustomerPhone   NVARCHAR(50)    NULL,
        Notes           NVARCHAR(1000)  NULL,
        BookingDate     DATETIME2       NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_Bookings_Venues
            FOREIGN KEY (VenueId) REFERENCES Venues(VenueId)
            ON DELETE NO ACTION ON UPDATE NO ACTION,

        CONSTRAINT FK_Bookings_Events
            FOREIGN KEY (EventId) REFERENCES Events(EventId)
            ON DELETE NO ACTION ON UPDATE NO ACTION
    );
END
GO

-- ============================================================
-- INDEX: Speed up overlap queries on Bookings
-- ============================================================
IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'IX_Bookings_VenueId_StartDate_EndDate'
      AND object_id = OBJECT_ID('Bookings')
)
BEGIN
    CREATE INDEX IX_Bookings_VenueId_StartDate_EndDate
        ON Bookings (VenueId, StartDate, EndDate);
END
GO

-- ============================================================
-- VIEW: vw_BookingDetails
-- Consolidated booking view joining Bookings, Venues, Events
-- ============================================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_BookingDetails')
    DROP VIEW vw_BookingDetails;
GO

CREATE VIEW vw_BookingDetails AS
SELECT
    b.BookingId,
    'BK' + RIGHT('000000' + CAST(b.BookingId AS VARCHAR), 6) AS BookingReference,
    b.VenueId,
    v.Name          AS VenueName,
    v.Location      AS VenueLocation,
    v.Capacity      AS VenueCapacity,
    v.ImageUrl      AS VenueImageUrl,
    b.EventId,
    e.Name          AS EventName,
    e.Description   AS EventDescription,
    e.StartDate     AS EventStartDate,
    e.EndDate       AS EventEndDate,
    e.ImageUrl      AS EventImageUrl,
    b.StartDate     AS BookingStartDate,
    b.EndDate       AS BookingEndDate,
    b.CustomerName,
    b.CustomerEmail,
    b.CustomerPhone,
    b.Notes,
    b.BookingDate
FROM
    Bookings b
    INNER JOIN Venues v ON b.VenueId = v.VenueId
    INNER JOIN Events e ON b.EventId = e.EventId;
GO

-- ============================================================
-- SAMPLE DATA (optional — comment out for production)
-- ============================================================

-- Venues
IF NOT EXISTS (SELECT 1 FROM Venues)
BEGIN
    INSERT INTO Venues (Name, Location, Capacity, Description, IsAvailable)
    VALUES
        ('The Grand Ballroom',  'Cape Town, Western Cape',  500, 'Elegant ballroom with chandeliers and a sprung dance floor.', 1),
        ('Hillside Pavilion',   'Johannesburg, Gauteng',    200, 'Open-air pavilion with panoramic city views.', 1),
        ('Harbour View Hall',   'Durban, KwaZulu-Natal',    350, 'Modern hall overlooking the Indian Ocean harbour.', 1),
        ('The Vineyard Estate', 'Stellenbosch, Western Cape', 120, 'Intimate estate venue surrounded by working vineyards.', 1),
        ('Sandton Convention Centre Annex', 'Sandton, Gauteng', 800, 'Professional conference and gala space in the heart of Sandton.', 0);
END
GO

-- Events
IF NOT EXISTS (SELECT 1 FROM Events)
BEGIN
    INSERT INTO Events (Name, Description, StartDate, EndDate)
    VALUES
        ('Annual Gala Dinner',       'Black-tie fundraising dinner for corporate partners.',
            '2026-07-15 18:00', '2026-07-15 23:00'),
        ('Tech Summit 2026',         'Two-day technology and innovation conference.',
            '2026-08-20 08:00', '2026-08-21 17:00'),
        ('Wedding Expo',             'Showcase of wedding vendors, caterers, and planners.',
            '2026-09-05 10:00', '2026-09-06 18:00'),
        ('Charity Fun Run After-Party', 'Celebration event following the annual charity run.',
            '2026-06-28 14:00', '2026-06-28 20:00');
END
GO

-- Bookings
IF NOT EXISTS (SELECT 1 FROM Bookings)
BEGIN
    INSERT INTO Bookings (VenueId, EventId, StartDate, EndDate, CustomerName, CustomerEmail, CustomerPhone, BookingDate)
    VALUES
        (1, 1, '2026-07-15 17:00', '2026-07-16 01:00', 'Nomvula Dlamini', 'nomvula@example.com', '+27 82 111 2222', GETUTCDATE()),
        (2, 2, '2026-08-20 07:00', '2026-08-21 18:00', 'James Pretorius',  'james@example.com',   '+27 73 333 4444', GETUTCDATE()),
        (3, 4, '2026-06-28 13:00', '2026-06-28 21:00', 'Fatima Osman',     'fatima@example.com',  '+27 64 555 6666', GETUTCDATE());
END
GO

PRINT 'EventEase database setup complete.';
GO
