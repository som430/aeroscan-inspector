# ✈ AeroScan Inspector

**Point cloud flatness inspection — ASP.NET Core API + Blazor WebAssembly.**

A web application that ingests 3D scan data (.csv / .ply), fits a best-fit plane with PCA (total least squares), and computes per-point signed deviations — producing a pass/fail flatness verdict against a configurable tolerance.

![CI](https://github.com/som430/aeroscan-inspector/actions/workflows/ci.yml/badge.svg)

> Personal learning project. The sample data is synthetic; this is not a production metrology system.

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
│  Plane3D (PCA) · Vector3D (double)          │
│  PointCloudParser · GeometryInspector       │
└──────────────▲──────────────────────────────┘
               │  xUnit
        AeroScan.PointCloud.Tests
```

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 8, C# 12 |
| ORM | Entity Framework Core 8 + SQLite |
| Frontend | Blazor WebAssembly |
| Geometry | Hand-written double-precision vector math and 3×3 Jacobi eigen-solver (no external deps) |
| Tests | xUnit |
| CI | GitHub Actions (restore → build → test) |
| Container | Docker Compose (API + nginx-served Blazor) |

---

## Features

- **Point cloud ingestion** — Upload `.csv` or ASCII `.ply` files
- **Best-fit plane fitting** — PCA on the covariance matrix; works for any plane orientation, including vertical
- **Geometric deviation analysis** — Signed distance per point, flatness (deviation range), min/max/RMS
- **Pass/Fail verdict** — Configurable tolerance per inspection session
- **Degenerate input handling** — fewer than 3 points or collinear points are reported with a reason instead of throwing
- **Deviation heatmap** — Per-point colour-coded table (up to 500 points)
- **Inspection history** — Persistent sessions via EF Core + SQLite
- **REST API** — Create / list / detail / delete + file upload, Swagger UI
- **CI** — Build and unit tests on every push and pull request to `main`

---

## Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)

### Run API
```bash
cd AeroScan.API
dotnet run --launch-profile https
# Swagger UI → https://localhost:7289/swagger
```

### Run Blazor Frontend
```bash
cd AeroScan.Blazor
dotnet run --launch-profile https
# Open → https://localhost:7085
```

The SQLite database (`aeroscan.db`) is created automatically on first start.

### Run Tests
```bash
dotnet test
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

### Example — Response (for `samples/wing_panel_sample.csv`)
```json
{
  "passed": false,
  "totalPoints": 40,
  "flatnessErrorMm": 0.8327,
  "minDeviationMm": -0.0399,
  "maxDeviationMm": 0.7928,
  "rmsDeviationMm": 0.1275,
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

`samples/wing_panel_sample.csv` is a synthetic 8 × 5 grid (50 mm pitch) with a slight tilt, ±0.02 mm noise and one 0.8 mm bump.

---

## How it works

1. **Centroid** — mean of X, Y, Z
2. **Covariance** — 3×3 covariance matrix of the centred points
3. **Eigen-decomposition** — cyclic Jacobi rotations (the matrix is real symmetric)
4. **Normal vector** — eigenvector of the smallest eigenvalue; sign fixed so the dominant component is positive, so the same input always gives the same normal
5. **Degenerate check** — if the second-smallest eigenvalue is ~0 the points are collinear and no plane is defined
6. **Signed distance** — `Normal · (P − Centroid)` per point
7. **Flatness** — `max_deviation − min_deviation`
8. **Pass/Fail** — `flatness ≤ tolerance`

### Why PCA instead of `Z = aX + bY + c`

The first version regressed Z on X and Y and stored vectors as `float`. Two cases broke it, and both are now unit tests:

| Case | Regression + `float` | PCA + `double` |
|---|---|---|
| Vertical plane `x = 5` | normal equations singular → no plane | normal `(1, 0, 0)` |
| 0.001 mm bump at 1,000 m (1e6 mm) offset | flatness ≈ 0.000002 mm (bump lost to rounding) | flatness 0.001 mm |

### Limitations

- Flatness here is the deviation range from the **least-squares** plane. ISO 1101 defines flatness by the **minimum zone** (the closest pair of parallel planes enclosing all points). The least-squares value is never smaller than the minimum-zone value, so this check errs on the side of rejecting parts.
- A point is flagged as out of tolerance when `|deviation| > tolerance`, while the overall verdict uses the full deviation range; the two are reported separately on purpose.
- The detail view stores at most 500 points per session.

---

## Docker

```bash
docker-compose up --build
```

| Service | URL |
|---|---|
| API (Swagger) | http://localhost:8080/swagger |
| Blazor UI | http://localhost:8081 |

In the container the Blazor app reads `wwwroot/appsettings.json` (`ApiBaseUrl` = `http://localhost:8080/`); local runs use `appsettings.Development.json`. The API's allowed CORS origins can be overridden with `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, …

## License
MIT
