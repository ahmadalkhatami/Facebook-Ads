# Progress & TODO — Facebook Ads Scalping Engine

> Last updated: 2026-04-27
> Branch: main | Commit terakhir: Phase 1 Initial Commit

---

## Status Keseluruhan

```
Phase 1 — Core Engine      ████████████████████  100% ✅
Phase 2 — Completion       ████░░░░░░░░░░░░░░░░   20% 🔄
Phase 3 — Advanced         ░░░░░░░░░░░░░░░░░░░░    0% ⬜
```

---

## Phase 1 — Core Automation Engine ✅ SELESAI

Semua komponen core sudah berjalan. Diverifikasi langsung dari kode.

### Yang Sudah Jadi

- [x] **Scraper** (`src/scraper/scraper.py`)
  - Tokopedia GraphQL API + Shopee public API
  - Filter: rating ≥ 4.5, sold ≥ 1.000 unit
  - 10 keyword trending, deduplication, simpan ke PostgreSQL
  - Jadwal: setiap 6 jam via Docker

- [x] **Database Schema** (`deploy/init.sql`)
  - 5 tabel: products, campaigns, creatives, metrics, decision_logs
  - EF Core context terhubung ke PostgreSQL

- [x] **AI Scoring** (`src/backend/Services/ProductService.cs`)
  - GPT-4o-mini menilai produk 0–10
  - Kriteria: viral potential, margin, demand, creative potential

- [x] **Creative Generation** (`src/backend/Services/CreativeService.cs`)
  - Generate 5 hook iklan via AI
  - Generate script video + caption

- [x] **Campaign Launch** (`src/backend/Services/TrafficService.cs`)
  - Buat campaign di Facebook (status PAUSED dulu)
  - Budget awal $50/hari

- [x] **Decision Engine** (`src/backend/Services/DecisionService.cs`)
  - KILL: spend > 2× harga produk & revenue = 0
  - SCALE: ROAS > 2 → budget × 1.2 (maks 1.5×, cap $500)
  - REPLACE_CREATIVE: CTR < 1.5% → log + alert
  - Safety: hard cap $500/hari per campaign

- [x] **Orchestration Loop** (`src/backend/Worker.cs`)
  - Background service, jalan setiap 1 jam
  - Max 3 campaign baru per siklus

- [x] **OpenAI Service** (`src/backend/Infrastructure/AI/OpenAIService.cs`)
  - Retry 3× dengan exponential backoff
  - Fallback ke mock response jika API error

- [x] **Telegram Bot** (`src/bot/bot.js`)
  - Subscribe Redis channel `fb_ads_alerts`
  - Kirim notifikasi real-time ke Telegram

- [x] **Dashboard API** (`src/backend/Controllers/DashboardController.cs`)
  - 5 endpoint: `/stats`, `/campaigns`, `/products`, `/decisions`, `/metrics/daily`
  - Data nyata dari PostgreSQL

- [x] **Docker Compose** (`docker-compose.yml`)
  - 5 service: db, redis, backend, scraper, bot
  - Siap deploy satu perintah

---

## Phase 2 — Completion & Real Integration 🔄 IN PROGRESS (20%)

### ✅ Sudah Ada (tapi belum sempurna)
- Dashboard UI sudah mencoba fetch dari API, tapi fallback ke mock data jika error

### ❌ Belum Dikerjakan

#### 2.1 — Facebook AdSet + Ad Creation
**File:** `src/backend/Infrastructure/FacebookApi/FacebookApiClient.cs`

Saat ini `FacebookApiClient` hanya punya:
- `CreateCampaignAsync()` — buat Campaign saja
- `PauseCampaignAsync()`
- `UpdateBudgetAsync()`
- `GetCampaignMetricsAsync()`

Yang **belum ada** (iklan tidak akan tayang tanpa ini):
- [ ] `CreateAdSetAsync()` — targeting audience, placement, jadwal
- [ ] `CreateAdAsync()` — creative + link landing page
- [ ] Flow lengkap: Campaign → AdSet → Ad

> **Dampak:** Campaign sudah dibuat di Facebook tapi tidak ada Ad di dalamnya.
> Iklan tidak akan tayang sama sekali tanpa AdSet dan Ad.

---

#### 2.2 — Dashboard UI Terhubung ke API
**File:** `dashboard/src/App.tsx`

Yang bermasalah sekarang:
- [ ] `stats` state punya **hardcoded initial value** (`totalSpend: 12450.50` dll) — angka palsu muncul saat loading
- [ ] `chartData` (grafik 7 hari) **sepenuhnya hardcoded**, tidak fetch dari `/api/dashboard/metrics/daily`
- [ ] Fallback ke `mockCampaigns` jika API error — padahal API `/api/dashboard/campaigns` sudah real

Yang perlu dikerjakan:
- [ ] Ganti initial state `stats` dengan nilai nol / loading state
- [ ] Fetch `chartData` dari `GET /api/dashboard/metrics/daily`
- [ ] Tambah loading skeleton saat data diambil
- [ ] Tampilkan decision log (`GET /api/dashboard/decisions`)
- [ ] Tambah tabel produk dengan score AI

---

#### 2.3 — Facebook Token Auto-Refresh
**File:** belum ada

- [ ] Token Facebook expire setiap ~60 hari
- [ ] Perlu mekanisme refresh token otomatis atau alert sebelum expire
- [ ] Minimal: log warning 7 hari sebelum expire

---

#### 2.4 — Revenue Tracking via CAPI
**File:** `src/backend/Services/TrackingService.cs`

`TrackingService` sudah ada dan bisa kirim event ke Facebook CAPI, tapi:
- [ ] Tidak ada webhook yang menerima notifikasi order dari landing page
- [ ] `Revenue` di tabel `campaigns` tidak pernah diupdate secara otomatis
- [ ] Decision KILL/SCALE berdasarkan revenue 0 terus karena revenue tidak masuk

---

#### 2.5 — Bug Fixes yang Sudah Diketahui

- [ ] **`ConversionEvent.Currency` = `"USD"`** harus diganti `"IDR"`
  - File: `src/backend/Core/Entities/ConversionEvent.cs`, baris 12

- [ ] **`ReplaceCreativeAsync` tidak benar-benar ganti creative**
  - Sekarang hanya log + alert, tapi tidak generate creative baru dan update di Facebook
  - File: `src/backend/Services/DecisionService.cs`, baris 134–139

- [ ] **`GetCampaignMetricsAsync` tidak ada error handling** untuk response kosong/invalid
  - Jika Facebook API return format berbeda, akan crash
  - File: `src/backend/Infrastructure/FacebookApi/FacebookApiClient.cs`, baris 34–40

---

## Phase 3 — Advanced Features ⬜ BELUM DIMULAI

- [ ] **Landing page builder integration**
  - Hubungkan landing page order dengan CAPI webhook
  - Auto-generate landing page per produk

- [ ] **Advanced Analytics Dashboard**
  - Grafik per-campaign (bukan aggregate)
  - Breakdown per produk, per keyword scraper
  - Export laporan ke CSV/Excel

- [ ] **Multi-account Facebook**
  - Support lebih dari 1 ad account
  - Rotasi account untuk menghindari limit

- [ ] **Auto Creative Replacement**
  - Saat CTR < 1.5%, otomatis generate creative baru dan upload ke Facebook
  - Sekarang hanya alert, aksi nyata belum ada

- [ ] **Scraper yang lebih robust**
  - Tokopedia/Shopee public API bisa berubah kapan saja
  - Pertimbangkan scraping berbasis Playwright/Selenium sebagai backup

- [ ] **Token Management UI**
  - Tampilkan status token Facebook di dashboard
  - Alert sebelum expire + tombol refresh manual

---

## Urutan Pengerjaan yang Disarankan (Phase 2)

```
Priority 1 (BLOCKER — iklan tidak tayang tanpa ini):
  └─ 2.1 Facebook AdSet + Ad Creation

Priority 2 (DATA — revenue selalu 0 tanpa ini):
  └─ 2.4 Revenue Tracking webhook

Priority 3 (MINOR BUG — mudah, cepat):
  └─ 2.5 Bug Fixes (Currency IDR, error handling)

Priority 4 (UX — dashboard masih pakai data palsu):
  └─ 2.2 Dashboard UI fix

Priority 5 (MAINTENANCE — perlu sebelum production):
  └─ 2.3 Token auto-refresh
```

---

## File yang Perlu Disentuh untuk Phase 2

| File | Perubahan |
|------|-----------|
| `src/backend/Infrastructure/FacebookApi/FacebookApiClient.cs` | Tambah `CreateAdSetAsync()`, `CreateAdAsync()` |
| `src/backend/Services/TrafficService.cs` | Update `LaunchCampaignAsync()` untuk flow Campaign→AdSet→Ad |
| `src/backend/Services/DecisionService.cs` | Fix `ReplaceCreativeAsync()` agar benar-benar replace |
| `src/backend/Core/Entities/ConversionEvent.cs` | Ganti default Currency ke `"IDR"` |
| `dashboard/src/App.tsx` | Remove hardcoded mock, fetch chart dari API |
| `src/backend/` (file baru) | `WebhookController.cs` untuk terima order dari landing page |
