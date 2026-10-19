# ToDo List VPS Blades — Backend & Nginx Deployment

Clean Architecture ASP.NET Core 8 Web API for personal notes, notebook tabs, and persistent WebP image management.

## Architecture

- **Domain Layer**: Clean aggregate roots (`Tab`, `TabContent`, `TabImage`, `ApplicationUser`, `RefreshTokenTable`, `LoginAttempt`).
- **Application Layer**: MediatR CQRS commands and queries, FluentValidation pipeline validation, ErrorOr result contracts, DTO mappings.
- **Infrastructure Layer**: EF Core with Managed PostgreSQL (`Npgsql`), image processing with size-aware progressive WebP conversion (`SixLabors.ImageSharp`), persistent filesystem storage under `wwwroot/files`, and SMTP email delivery.
- **API Layer**: Thin controllers, JWT Bearer authentication with token rotation, optional Google OAuth, Swagger documentation, and health monitoring.

## Local Development with Docker Compose

1. Copy `.env.example` to `.env` and configure your settings.
2. Run Docker Compose:
   ```bash
   docker compose up -d --build
   ```
3. Test the endpoints:
   - Wake-Up: `http://localhost/api/system/wake-up` -> `{"status":"ready","value":"Gaber"}`
   - Startup Readiness: `http://localhost/api/system/startup`
   - Swagger: `http://localhost/swagger`
   - Health check: `http://localhost/health`

## Production Deployment to Blitz.cloud

Refer to `blitz-deployment-instructions.md` for full instructions.
