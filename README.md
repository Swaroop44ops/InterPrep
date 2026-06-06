# Topic Explorer Hub (Phase 1 Skeleton)

This project contains the Phase 1 skeleton setup for a full-stack application. It features:
- **Frontend**: Vite + React + TypeScript + CSS (glassmorphic cyber dark-mode theme)
- **Backend**: ASP.NET Core Web API (.NET 9) with Entity Framework Core and PostgreSQL (Npgsql) wired to Supabase
- **Deployment-Ready**: Includes a multi-stage Dockerfile for Render and vercel routing configuration for Vercel.

---

## Project Structure

```text
topic-hub/
├── backend/                  # ASP.NET Core Web API Project
│   ├── Controllers/          # API Controllers (GET /api/topics)
│   ├── Data/                 # DB Context & Seed Data
│   ├── Models/               # DB Entity Models (Topic)
│   ├── Program.cs            # API bootstrap, CORS, Npgsql configuration
│   ├── Dockerfile            # Container config for Render deployment
│   └── backend.csproj
├── frontend/                 # Vite + React Frontend Project
│   ├── src/
│   │   ├── App.tsx           # React UI with data fetching & state management
│   │   ├── index.css         # Premium cyber-dark mode styles
│   │   └── main.tsx
│   ├── .env.local            # Local environment variables
│   ├── .env.production       # Production environment variables
│   ├── vercel.json           # Client routing configuration
│   └── package.json
└── README.md                 # Setup and deployment manual (this file)
```

---

## Local Development Setup

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js (v18+) & npm](https://nodejs.org/)
- A [Supabase account](https://supabase.com/) (or a local PostgreSQL database instance)

---

### Step 1: Database Setup (Supabase)
1. Log in to Supabase and create a new project.
2. Under project settings, navigate to **Database** and copy your **URI Connection String** (under Transaction connection string, or Session connection string. Ensure you replace `[YOUR-PASSWORD]` with your actual database password).
3. The connection string format should look like:
   `Host=aws-0-us-west-1.pooler.supabase.com;Database=postgres;Username=postgres.xxxx;Password=xxxx;Port=5432`

---

### Step 2: Running the Backend
1. Open a terminal and navigate to the `backend/` directory:
   ```bash
   cd backend
   ```
2. Set the connection string as an environment variable (choose the command for your OS):
   - **Windows (PowerShell)**:
     ```powershell
     $env:DATABASE_URL="YOUR_SUPABASE_CONNECTION_STRING"
     ```
   - **macOS/Linux**:
     ```bash
     export DATABASE_URL="YOUR_SUPABASE_CONNECTION_STRING"
     ```
3. Run the backend Web API:
   ```bash
   dotnet run
   ```
   The backend will start and bind to `http://localhost:5100`.
   *(Note: On first request to `http://localhost:5100/api/topics`, EF Core will automatically create the `Topics` table and populate it with seed data).*

---

### Step 3: Running the Frontend
1. Open a new terminal and navigate to the `frontend/` directory:
   ```bash
   cd frontend
   ```
2. Install dependencies:
   ```bash
   npm install
   ```
3. Run the development server:
   ```bash
   npm run dev
   ```
   The frontend will start on `http://localhost:5173`. Open this URL in your browser to view the premium dashboard and the fetched topics!

---

## Deployment Guide

### 1. Deploying Backend to Render
Render can host ASP.NET Core apps using the included multi-stage Dockerfile:

1. Push your code to a GitHub repository.
2. Sign in to [Render](https://render.com/) and click **New > Web Service**.
3. Connect your GitHub repository.
4. Configure the Web Service settings:
   - **Name**: `topic-hub-backend`
   - **Language**: `Docker`
   - **Docker Context**: `backend` (if you are deploying from a monorepo, or leave as root `.` if you specify the Dockerfile Path as `backend/Dockerfile` in Render advanced settings).
5. In **Advanced Settings**, add the following **Environment Variables**:
   - `DATABASE_URL` = *[Your Supabase Connection String]*
   - `CORS_ALLOWED_ORIGINS` = `https://your-frontend-app.vercel.app` (You can update this after deploying to Vercel).
6. Click **Create Web Service**. Render will build the Docker container and deploy your API.

---

### 2. Deploying Frontend to Vercel
Vercel is optimized for building and serving Vite applications:

1. Sign in to [Vercel](https://vercel.com/) and click **Add New > Project**.
2. Connect your GitHub repository.
3. Configure the Vercel project:
   - **Root Directory**: Select `frontend`
   - **Framework Preset**: `Vite` (automatically detected)
4. Add the following **Environment Variable** under the environment variables section:
   - `VITE_API_URL` = `https://your-backend-app.onrender.com` (Use the URL provided by Render for your Web Service).
5. Click **Deploy**. Vercel will build and serve your static React bundle.
6. Copy the Vercel deployment URL and paste it back into your Render backend's `CORS_ALLOWED_ORIGINS` environment variable to secure the connection.
