# Lebanon Basketball Stadium Reservation System

A full-stack ASP.NET Core 8 MVC application that lets players across Lebanon find, compare and reserve basketball courts online. Built for the COMP420 Application Development course.

---

## Problem Statement

Basketball players and teams in Lebanon have no centralized platform to check court availability, compare prices, or make reservations — relying instead on phone calls and personal contacts. This system provides a searchable, bookable directory of courts nationwide.

---

## Features

### Customer
- Search and filter stadiums by location (Governorate / District / Area), price and court type
- Sort results by newest, name, price or rating
- View stadium details, courts, opening hours, reviews and real-time availability
- Book a court from a two-week availability calendar with live price preview
- Cancel reservations within a configurable window
- Review completed reservations (1–5 stars)
- Save favorite stadiums
- Receive in-app notifications (submitted, confirmed, rejected, reminder, completed)

### Stadium Manager
- Register stadiums (pending admin approval) with photo upload
- Manage courts and bulk-generate time slots with day exclusions
- Confirm or reject booking requests with a reason
- Track revenue, ratings and pending requests on the dashboard

### Administrator
- Approve or reject stadium registrations
- Manage users: search, filter by role, activate/deactivate, change roles
- Manage the Lebanese location hierarchy (Governorate → District → Area)
- View system-wide reports with daily breakdown and per-stadium revenue

---

## Technology Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8 MVC |
| Language | C# 12 |
| Database | SQLite (single file, zero setup) |
| ORM | Entity Framework Core 8 (Code First) |
| Identity | ASP.NET Core Identity |
| Logging | Serilog (Console + rolling file) |
| API | ASP.NET Core Web API + Swagger/OpenAPI |
| Authentication | Cookie (MVC) + JWT Bearer (API) |
| UI | Bootstrap 5 + Bootstrap Icons |
| Testing | xUnit + Moq |

---

## Architecture

```
Browser
   ↓
ASP.NET Core MVC (Web)          REST API (JWT)
   ↓                                  ↓
        Business Layer (Services + DTOs)
                    ↓
      Data Layer (Repositories + Unit of Work)
                    ↓
             EF Core 8 → SQLite
```

### Solution Projects

| Project | Type | Purpose |
|---|---|---|
| `LebanonBasketballReservation.Web` | ASP.NET Core MVC | Main user-facing application |
| `LebanonBasketballReservation.Business` | Class Library | Services, DTOs, domain exceptions |
| `LebanonBasketballReservation.Data` | Class Library | Entities, DbContext, Repositories, UoW |
| `LebanonBasketballReservation.API` | ASP.NET Core Web API | REST API + Swagger |
| `LebanonBasketballReservation.Tests` | xUnit | Unit tests |

Both the Web and API projects point at the **same** SQLite file (`LebanonBasketball.db` at the solution root), so data is shared between them.

---

## Database Design

### Location Hierarchy
```
Governorate → District → Area → Stadium → Court → TimeSlot → Reservation
```

### Key Entities
`ApplicationUser`, `Governorate`, `District`, `Area`, `Stadium`, `Court`, `StadiumImage`, `OpeningHour`, `TimeSlot`, `Reservation`, `Notification`, `Review`, `Favorite`, `AuditLog`

### Double-Booking Prevention
Three independent layers:
1. **Unique index** on `Reservations(TimeSlotId)` — the database refuses a second booking for a slot
2. **Atomic claim** — `UPDATE TimeSlots SET IsAvailable = 0 WHERE Id = @id AND IsAvailable = 1` inside a transaction, so exactly one of N concurrent requests wins
3. **UI** only offers slots that are free and have not yet started

---

## Time Zone Handling

Reservations are wall-clock events at the venue, but timestamps are stored in UTC. All comparisons between the two go through `IDateTimeProvider`, which resolves the venue zone from `ReservationSettings:TimeZone` (default `Asia/Beirut`, falling back to `Middle East Standard Time` on Windows). This keeps the cancellation window, "already started" checks and reminders correct across Lebanon's DST changes.

---

## User Roles

| Role | Access |
|---|---|
| `Customer` | Browse, reserve, cancel, review, favorites |
| `StadiumManager` | Manage own stadiums, courts, time slots, reservations |
| `Admin` | Full system access — users, stadiums, locations, reports |

Role names are centralized in `RoleNames` so a typo cannot silently disable an `[Authorize]` check.

---

## Reservation Workflow

```
Customer picks a court, date and slot
         ↓
Confirm → Status: Pending  (slot atomically claimed)
         ↓
Manager confirms → Confirmed   |   Manager rejects → Rejected (slot released)
         ↓
Background service (every 15 min)
         ↓
End time passes → Completed → customer can leave a review
```

---

## API Endpoints

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/login` | Public | Obtain a JWT bearer token |
| POST | `/api/auth/register` | Public | Register a customer and get a token |
| GET | `/api/auth/me` | Bearer | Current user profile |
| GET | `/api/stadiums` | Public | Search stadiums (paginated) |
| GET | `/api/stadiums/{id}` | Public | Stadium details |
| GET | `/api/stadiums/{id}/courts` | Public | Courts at a stadium |
| GET | `/api/stadiums/{id}/availability` | Public | Slots for a court on a date |
| GET | `/api/stadiums/{id}/calendar` | Public | Two weeks of availability |
| GET | `/api/stadiums/{id}/reviews` | Public | Stadium reviews |
| GET | `/api/locations/governorates` | Public | All governorates |
| GET | `/api/locations/districts/{govId}` | Public | Districts by governorate |
| GET | `/api/locations/areas/{distId}` | Public | Areas by district |
| GET | `/api/locations/hierarchy` | Public | Full location tree |
| GET | `/api/reservations` | Bearer | My reservations |
| GET | `/api/reservations/{id}` | Bearer | Single reservation |
| POST | `/api/reservations` | Bearer | Create reservation |
| PUT | `/api/reservations/{id}/cancel` | Bearer | Cancel reservation |
| POST | `/api/reservations/{id}/review` | Bearer | Review a completed booking |

Swagger UI is served at the API root URL.

---

## Installation

### Prerequisites
- .NET 8 SDK (Visual Studio 2022 or `dotnet` CLI)

No database server is required — SQLite is created and seeded automatically on first run.

### Steps

```bash
git clone <repository-url>
cd msweid
dotnet restore
dotnet run --project LebanonBasketballReservation.Web
```

The database is migrated and seeded on startup. To run the API alongside it:

```bash
dotnet run --project LebanonBasketballReservation.API
```

In Visual Studio: right-click the solution → **Set Startup Projects** → set both `Web` and `API` to Start.

### Configuration

`appsettings.json` holds non-secret settings. The JWT signing key is **not** committed — it comes from `appsettings.Development.json` (a development-only value is provided) or, for anything real, from user-secrets or an environment variable:

```bash
dotnet user-secrets set "Jwt:Key" "<a random string of at least 32 characters>" \
  --project LebanonBasketballReservation.API
```

The API refuses to start if `Jwt:Key` is missing or shorter than 32 characters.

Seed account passwords can be overridden with `SeedUsers:AdminPassword`, `SeedUsers:ManagerPassword` and `SeedUsers:CustomerPassword`.

### Seed Accounts

| Role | Email | Password |
|---|---|---|
| Admin | admin@lbbasket.com | Admin@123! |
| Manager | manager@lbbasket.com | Manager@123! |
| Customer | customer@lbbasket.com | Customer@123! |

These are development defaults. Override them via configuration before deploying anywhere real.

---

## Security

- Antiforgery tokens applied globally via `AutoValidateAntiforgeryTokenAttribute`
- Role-based authorization on every area through base controllers
- **Ownership enforced in the service layer by id** — managers can only touch their own stadiums, customers only their own reservations
- Deactivated accounts are blocked at login and their existing cookies are invalidated via security-stamp rotation
- Account lockout after 5 failed attempts (15 minutes)
- Upload validation: extension allow-list, size limit, magic-byte check, generated filenames, path-traversal guards
- DTOs and ViewModels prevent over-posting
- EF Core parameterized queries prevent SQL injection; Razor auto-encoding prevents XSS
- Domain exceptions carry safe messages; unexpected errors return a generic page and are logged
- Serilog never logs passwords or tokens

---

## Background Service

`ReservationStatusService` runs on the configured interval (default 15 minutes):
1. Marks confirmed reservations whose end time has passed as **Completed** and invites a review
2. Sends **reminders** the configured number of hours before an upcoming reservation

Both use venue-local time, and the reminder flag is persisted before notifications are sent so a mid-loop failure cannot double-notify.

---

## Unit Tests

```bash
dotnet test
```

Coverage spans reservation rules (ownership, cancellation window, status transitions, race-condition handling, review validation), availability (past-slot exclusion, cancelled-slot reuse, calendar grouping), stadium ownership and approval, and time-slot generation (interval maths, duplicate skipping, multi-day spans, excluded days, invalid ranges). Time-dependent behaviour is driven by a fixed `TestClock`, so results do not depend on when the suite runs.

---

## COMP420 Requirements Checklist

| Requirement | Status |
|---|---|
| .NET 8 LTS | ✅ |
| ASP.NET Core MVC | ✅ |
| EF Core + relational database | ✅ (SQLite) |
| N-Layer Architecture | ✅ (5 projects) |
| Repository Pattern | ✅ |
| Unit of Work | ✅ (with transactions) |
| Identity + Role Authorization | ✅ |
| Serilog | ✅ |
| Exception Handling | ✅ (global middleware, typed domain exceptions) |
| REST API | ✅ |
| Swagger/OpenAPI | ✅ (with JWT auth flow) |
| Client + Server Validation | ✅ |
| Optimized Queries | ✅ (IQueryable, AsNoTracking, no N+1) |
| Background Service | ✅ |
| 3–5 CRUD Modules | ✅ (Stadium, Court, TimeSlot, Location, User) |
| Dashboards | ✅ (Customer, Manager, Admin) |
| Reports | ✅ |
| Responsive Bootstrap 5 UI | ✅ |
| Unit Tests | ✅ |
| Git + .gitignore | ✅ |
| README + Documentation | ✅ |
| Real-World Lebanese Problem | ✅ |
