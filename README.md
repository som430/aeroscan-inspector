# ✈ AeroScan Inspector

**Point cloud flatness inspection for aerospace manufacturing.**

A cloud-native web application that ingests 3D scan data (.csv / .ply), fits a best-fit plane using PCA + Least Squares, and computes per-point geometric deviations — producing a pass/fail flatness verdict against a configurable tolerance.

![CI](https://github.com/som430/aeroscan-inspector/actions/workflows/ci.yml/badge.svg)

---

## Architecture

```
┌─────────────────────────────────────────────┐
│  AeroScan.Blazor  (Blazor WebAssembly)      │
│  Dashboard · New Inspection · Detail Report │
└──────────────┬──────────────────────────────┘
               │  REST (JSON)
┌──────────────▼──────────────────────────────┐
│  AeroScan.API  (ASP.NET Core 8 Web API)     │
│  InspectionsController · InspectionService  │
│  Entity Framework Core → SQLite             │
└──────────────┬──────────────────────────────┘
               │  project reference
┌──────────────▼──────────────────────────────┐
│  AeroScan.PointCloud  (Class Library)       │
│  Point3D · Plane3D (PCA + Least Squares)    │
│  PointCloudParser · GeometryInspector       │
└─────────────────────────────────────────────┘
```

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 8, C# 12 |
| ORM | Entity Framework Core 8 + SQLite |
| Frontend | Blazor WebAssembly |
| Geometry | System.Numerics.Vector3 (no external deps) |
| CI | GitHub Actions |

---

## Features

- **Point cloud ingestion** — Upload `.csv` or ASCII `.ply` files
- **Best-fit plane fitting** — PCA covariance matrix + Least Squares method
- **Geometric deviation analysis** — Signed distance per point, flatness error, min/max/RMS
- **Pass/Fail verdict** — Configurable tolerance per inspection session
- **Deviation heatmap** — Per-point colour-coded table (up to 500 points)
- **Inspection history** — Persistent sessions via EF Core + SQLite
- **REST API** — Full CRUD + file upload, Swagger UI
- **GitHub Actions CI** — Auto build & test on every push

---

## Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)

### Run API
```bash
cd AeroScan.API
dotnet run
# Swagger UI → https://localhost:7289/swagger
```

### Run Blazor Frontend
```bash
cd AeroScan.Blazor
dotnet run
# Open → https://localhost:7085
```

---

## API Reference

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/inspections` | List all sessions |
| `GET` | `/api/inspections/{id}` | Session detail + deviation snapshot |
| `POST` | `/api/inspections` | Create inspection session |
| `POST` | `/api/inspections/{id}/upload` | Upload point cloud & run inspection |
| `DELETE` | `/api/inspections/{id}` | Delete session |

### Example — Create session
```json
POST /api/inspections
{
  "partName": "Wing Panel A3",
  "partNumber": "BP-7X-0042",
  "toleranceMm": 0.5
}
```

### Example — Upload & inspect
```bash
curl -X POST https://localhost:7289/api/inspections/1/upload \
  -F "file=@samples/wing_panel_sample.csv"
```

### Example — Response
```json
{
  "passed": false,
  "totalPoints": 27,
  "flatnessErrorMm": 0.8384,
  "minDeviationMm": -0.0768,
  "maxDeviationMm": 0.7615,
  "rmsDeviationMm": 0.1520,
  "outOfToleranceCount": 1,
  "toleranceMm": 0.5
}
```

---

## Point Cloud File Format

### CSV
```
# x,y,z or x,y,z,intensity — units: millimeters
0.000,0.000,0.000,1.0
10.000,0.000,0.001,0.9
```

### ASCII PLY
```
ply
format ascii 1.0
element vertex 3
property float x
property float y
property float z
end_header
0.0 0.0 0.0
10.0 0.0 0.001
```

A sample file with a synthetic OOT defect is included at `samples/wing_panel_sample.csv`.

---

## Geometric Metrology: How it works

1. **Centroid** — compute mean X, Y, Z
2. **Least Squares** — solve normal equations for best-fit plane Z = aX + bY + c
3. **Normal vector** — derived as (-a, -b, 1), normalized
4. **Signed distance** — `Normal · (P − Origin)` per point
5. **Flatness error** — `max_deviation − min_deviation`
6. **Pass/Fail** — `flatness_error ≤ tolerance`

Equivalent to ISO 1101 flatness measurement used in CMM metrology workflows.

---

## Boeing BKETC Relevance

| JD Requirement | Implementation |
|---|---|
| C# OOP | All layers in C# 12 |
| REST API | ASP.NET Core Web API, full CRUD |
| Cloud / Microservice architecture | Decoupled API + PointCloud library |
| Blazor (preferred) | Blazor WebAssembly frontend |
| ORM / Database | EF Core 8 + SQLite |
| CI/CD | GitHub Actions |
| Point Cloud / Geometric Metrology | Core inspection engine |

---


## Docker

### Run with Docker Compose
docker-compose up --build
| Service | URL |
|---|---|
| API (Swagger) | http://localhost:8080/swagger |
| Blazor UI | http://localhost:8081 |

## License
MIT
