# Blitz.cloud Deployment Guide for ToDo & Notes API

This guide provides step-by-step instructions to deploy the backend service to Blitz.cloud paired with Blitz Managed PostgreSQL.

---

## 1. Architecture Overview

- **Service**: ASP.NET Core 8 Web API containerized via the root `Dockerfile` (or `Backend/Dockerfile`).
- **Database**: Blitz.cloud Managed PostgreSQL 17 (injects `DATABASE_URL`).
- **File Storage**: Container persistent volume mounted at `/app/wwwroot/files` ensuring uploaded images survive restarts and redeployments.
- **Port**: Exposed on port `8080` (with dynamic `$PORT` binding support).
- **Edge Router**: Blitz Edge terminates TLS/HTTPS, handles automatic SSL certificates, and forwards headers (`X-Forwarded-For`, `X-Forwarded-Proto`, `X-Forwarded-Host`) directly to port `8080`.
- **Nginx Note**: Nginx is NOT required on Blitz.cloud because Blitz Edge handles all edge routing and SSL termination natively, and ASP.NET Core handles CORS, Swagger, and static files. For traditional self-managed Linux VPS servers, `docker-compose.vps.yml` is provided.

---

## 2. Step 1: Provision & Connect Managed PostgreSQL

1. Open your Blitz.cloud dashboard: [https://dashboard.blitz.cloud/databases](https://dashboard.blitz.cloud/databases).
2. Create or open your managed PostgreSQL database (e.g. `todo-db`).
3. Click **"Connect an app"** and select your ToDo application (`todo app`).
   - Blitz automatically links the database and injects `DATABASE_URL`.
   - The application automatically runs EF Core migrations (`MigrateAsync()`) on boot with resilient retry logic to initialize all tables.

---

## 3. Step 2: Configure Environment Variables in Blitz Dashboard

In Blitz Dashboard -> Your App -> **Settings** -> **Environment Variables**, configure the following variables:

| Variable | Value / Description |
| :--- | :--- |
| `DATABASE_URL` | *(Injected automatically by Blitz when connecting PostgreSQL database)* |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_HTTPS_PORT` | `443` |
| `JWT__Key` | A strong random 64-character secret key |
| `JWT__Issuer` | `https://gshe.blitz.cloud` (your backend public URL) |
| `JWT__Audience` | `ToDoListClient` |
| `JWT__AccessTokenExpirationMinutes` | `60` |
| `JWT__RefreshTokenExpirationDays` | `7` |
| `BaseURL` | `https://gshe.blitz.cloud` |
| `Frontend__AllowedBaseUrls__0` | `https://your-notes-app.vercel.app` (your frontend Vercel URL) |
| `Email__Host` | `smtp-relay.brevo.com` |
| `Email__Port` | `2525` *(port 2525 bypasses outbound SMTP blocks on Blitz)* |
| `Email__SmtpUsername` | Your Brevo SMTP login email |
| `Email__SmtpPassword` | Your Brevo SMTP key / master password |
| `Email__SenderEmail` | Your verified sender email address |
| `Email__SenderDisplayName` | `ToDo & Notes App` |
| `Authentication__Google__ClientId` | *(Optional)* Google OAuth Client ID |
| `Authentication__Google__ClientSecret` | *(Optional)* Google OAuth Client Secret |

---

## 4. Step 3: Google OAuth Setup (If Google Sign-In is Used)

1. Open [Google Cloud Console](https://console.cloud.google.com/apis/credentials).
2. Under **Authorized Redirect URIs**, add:
   ```text
   https://gshe.blitz.cloud/signin-google
   ```
3. Under **Authorized JavaScript Origins**, add:
   ```text
   https://your-notes-app.vercel.app
   ```

---

## 5. Step 4: Verification Checklist

After deployment completes:
- Root Wake-Up / Health: `GET https://gshe.blitz.cloud/` -> HTTP 200 `{"status":"Healthy","availability":"online","service":"ToDo & Notes API","database":"Connected",...}`
- Startup Readiness: `GET https://gshe.blitz.cloud/api/system/startup` -> HTTP 200 `{"success":true,"message":"Backend application and database dependency are ready.","data":{"status":"Ready","service":"ToDo & Notes API","database":"Ready"}}`
- Healthcheck: `GET https://gshe.blitz.cloud/health` -> HTTP 200 `{"status":"Healthy","availability":"online","database":"Connected"}`
- Swagger Documentation: `GET https://gshe.blitz.cloud/swagger`
- Image Storage: Images are served via `GET https://gshe.blitz.cloud/files/tabs/{fileName}.webp`
