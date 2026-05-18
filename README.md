# Price Tracker

Price Tracker is a platform that enables users to monitor product prices across multiple online stores within specific regions, receive notifications when prices drop below a target value, and analyze historical pricing trends over time. The system continuously collects and stores product pricing data from different stores through scheduled background scraping jobs, enabling users to compare prices, track fluctuations, and make informed purchasing decisions.

## Project Structure

```
price-tracker/
├── backend/                         # .NET 10 — Clean Architecture
│   ├── PriceTracker.API/            # Controllers, Middleware, Mappings, Extensions
│   ├── PriceTracker.Application/    # DTOs, Interfaces, Services, Validators
│   ├── PriceTracker.Domain/         # Entities, Enums, Exceptions
│   ├── PriceTracker.Infrastructure/ # EF Core, Authentication, Email, Jobs
│   ├── PriceTracker.Tests/          # Unit & Integration tests
│   └── PriceTracker.slnx           # Solution file
├── frontend/                        # Frontend (TBD)
├── scraper/                         # Price scraper service (TBD)
├── docker/                          # Dockerfile & docker-compose
├── docs/                            # ERD diagrams & endpoint documentation
│   ├── erd/                         # Database ERD diagrams
│   └── end-points/                  # API endpoint specifications
└── scripts/                         # Database init & seed scripts
```

## Tech Stack

- **Backend:** .NET 10, ASP.NET Core, Entity Framework Core
- **Database:** PostgreSQL
- **Authentication:** JWT Bearer tokens
- **Validation:** 
- **Mapping:** 
- **Email:** 
- **Job Scheduling:** 
- **Testing:** 

## Getting Started
