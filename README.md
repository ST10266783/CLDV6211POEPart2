# EventEase — Venue Booking System

An ASP.NET Core 8 MVC web application for managing venue bookings, events, and customers. Built with Entity Framework Core (SQL LocalDB) and Azure Blob Storage (Azurite for local development).

---

## Features

- **Venues** — Create, view, edit and delete venues with image uploads, availability toggling, and deletion protection when bookings exist.
- **Events** — Manage events with start/end dates and optional cover images.
- **Bookings** — Full booking lifecycle with:
  - Double-booking prevention (overlap detection per venue)
  - Venue availability checks
  - Search by booking reference (BK######), event name, or customer name
  - Consolidated booking view (`vw_BookingDetails`)
- **Dashboard** — Live counts of venues, events, bookings, and recent activity.
- **Image Storage** — Azure Blob Storage via Azurite emulator in development.

---

## Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 8.0+ |
| SQL Server / LocalDB | Any (LocalDB ships with Visual Studio) |
| Azurite | 3.x (npm or VS Code extension) |
| Visual Studio / VS Code | Any recent version |

---

## Getting Started

### 1. Clone / Extract

```
unzip EventEase.zip -d EventEase
cd EventEase
```

### 2. Start Azurite (Blob Storage Emulator)

**Option A — npm (globally):**
```bash
npm install -g azurite
azurite --silent --location ./azurite-data --debug ./azurite-debug.log
```

**Option B — VS Code Extension:**  
Install the *Azurite* extension by Microsoft, then press **F1 → Azurite: Start**.

Azurite listens on `127.0.0.1:10000` (Blob), `10001` (Queue), `10002` (Table).

### 3. Database Setup

The application uses `EnsureCreated()` so the database and tables are created automatically on first run.

**Connection string** (in `appsettings.json`):
```
Server=(localdb)\mssqllocaldb;Database=EventEaseDB;Trusted_Connection=True;MultipleActiveResultSets=true
```

> Alternatively, run `Database/EventEaseDB.sql` against your SQL Server instance to create the schema and sample data manually.

### 4. Run the Application

```bash
cd EventEase
dotnet restore
dotnet run
```

Then open: **https://localhost:5001** (or the port shown in your terminal).

---

## Project Structure

```
EventEase/
├── EventEase/                  # Main ASP.NET Core project
│   ├── Controllers/
│   │   ├── HomeController.cs
│   │   ├── VenuesController.cs
│   │   ├── EventsController.cs
│   │   └── BookingsController.cs
│   ├── Data/
│   │   └── ApplicationDbContext.cs
│   ├── Models/
│   │   ├── Venue.cs
│   │   ├── Event.cs
│   │   ├── Booking.cs
│   │   └── BookingDetailsViewModel.cs
│   ├── Services/
│   │   ├── IBlobStorageService.cs
│   │   └── BlobStorageService.cs
│   ├── Views/
│   │   ├── Home/
│   │   ├── Venues/
│   │   ├── Events/
│   │   ├── Bookings/
│   │   └── Shared/
│   ├── wwwroot/
│   │   ├── css/site.css
│   │   └── js/site.js
│   ├── appsettings.json
│   └── Program.cs
├── Database/
│   └── EventEaseDB.sql         # DDL script + sample data
└── README.md
```

---

## Configuration

### `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EventEaseDB;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "AzureStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "ContainerName": "venue-images"
  }
}
```

### For production (Azure):
Replace `AzureStorage.ConnectionString` with your real Azure Storage account connection string, and `DefaultConnection` with your Azure SQL connection string.

---

## Double-Booking Logic

A booking is rejected if any **existing booking for the same venue** satisfies:

```
existingStart < newEnd  AND  existingEnd > newStart
```

This catches all overlap patterns including containment and partial overlaps. The check excludes the booking being edited (by `BookingId`).

---

## Image Uploads

- Accepted types: JPEG, PNG, GIF, WebP
- Maximum size: 5 MB
- Images are stored in Azure Blob Storage (`venue-images` container, public read access)
- Filenames are prefixed with a GUID to prevent collisions
- If Azurite is not running, image upload fails gracefully with a warning and a placeholder URL is saved

---

## ERD (Entity Relationship)

```
Venues ──────────< Bookings >──────────── Events
VenueId (PK)       BookingId (PK)          EventId (PK)
Name               VenueId (FK)            Name
Location           EventId (FK)            Description
Capacity           StartDate               StartDate
Description        EndDate                 EndDate
ImageUrl           CustomerName            ImageUrl
IsAvailable        CustomerEmail
                   CustomerPhone
                   Notes
                   BookingDate
```

**View:** `vw_BookingDetails` — joins all three tables for the consolidated booking list.

---

## Booking Reference Format

Each booking gets a computed reference: **BK** followed by the `BookingId` zero-padded to 6 digits.

| BookingId | Reference |
|-----------|-----------|
| 1         | BK000001  |
| 42        | BK000042  |
| 1000      | BK001000  |

---

## Troubleshooting

| Problem | Solution |
|---|---|
| `Cannot open database` | Ensure LocalDB is installed; run `sqllocaldb start` |
| Images not uploading | Start Azurite first; check port 10000 is free |
| `ECONNREFUSED 127.0.0.1:10000` | Azurite is not running — see Step 2 |
| Port already in use | Change `launchSettings.json` applicationUrl |

---

## Tech Stack

- ASP.NET Core 8 MVC
- Entity Framework Core 8 (SQL Server provider)
- Azure Blob Storage SDK 12 (`Azure.Storage.Blobs`)
- Bootstrap 5.3.2 + Bootstrap Icons 1.11.3
- jQuery + jQuery Validation
- Azurite (local blob emulator)
