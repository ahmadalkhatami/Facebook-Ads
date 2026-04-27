# Facebook Ads Scalping Engine — Ringkasan Proyek

> Mesin otomasi iklan Facebook untuk dropshipping produk trending Indonesia.
> Status: **Phase 1 selesai** (otomasi berjalan) | **Phase 2 in progress** (koneksi UI ke API)

---

## Gambaran Besar

```
Marketplace (Tokopedia/Shopee)
        │
        ▼ setiap 6 jam
  ┌─────────────┐
  │   Scraper   │  Python — ambil produk trending
  │  (Python)   │  Filter: rating ≥4.5, sold ≥1000
  └─────┬───────┘
        │ simpan ke DB
        ▼
  ┌─────────────────────────────────────────────┐
  │               PostgreSQL                    │
  │  products | campaigns | creatives |         │
  │  metrics  | decision_logs                   │
  └─────────────────────────────────────────────┘
        ▲                    │
        │                    ▼ setiap 1 jam
  ┌─────────────┐    ┌───────────────────────┐
  │  Dashboard  │◄───│   Backend .NET Core   │
  │   (React)   │    │   (Decision Engine)   │
  └─────────────┘    └───────────┬───────────┘
                                 │ kirim alert
                                 ▼
                          ┌─────────┐
                          │  Redis  │ pub/sub
                          └────┬────┘
                               │
                               ▼
                      ┌────────────────┐
                      │  Telegram Bot  │ notifikasi real-time
                      │   (Node.js)    │
                      └────────────────┘
```

---

## Stack Teknologi

| Komponen     | Teknologi          | Peran                                  |
|--------------|--------------------|----------------------------------------|
| Backend      | C# / .NET 8        | Otak utama — keputusan & otomasi       |
| Database     | PostgreSQL         | Simpan semua data                      |
| Messaging    | Redis              | Kirim alert antar service              |
| AI           | GPT-4o-mini        | Scoring produk & buat konten iklan     |
| Iklan        | Facebook Graph API | Buat/scale/pause campaign              |
| Scraper      | Python 3           | Ambil produk trending dari marketplace |
| Bot          | Node.js            | Kirim notifikasi ke Telegram           |
| Dashboard    | React + Vite       | Tampilan visual performa iklan         |
| Deploy       | Docker Compose     | Jalankan semua service sekaligus       |

---

## Cara Kerja — Loop Otomasi (Setiap Jam)

```
┌─────────────────────────────────────────────────────────────┐
│                    WORKER (Hourly Loop)                      │
│                                                             │
│  STEP 1 — Score Produk Baru                                 │
│  ┌─────────────────────────────────┐                        │
│  │  Ambil produk status=SCRAPED    │                        │
│  │  → AI scoring (GPT-4o-mini)     │                        │
│  │  → nilai 0–10 (viral, margin,   │                        │
│  │    demand, creative potential)   │                        │
│  │  → simpan score ke DB            │                        │
│  └─────────────────────────────────┘                        │
│                                                             │
│  STEP 2 — Launch Campaign Baru (maks 3 per siklus)          │
│  ┌─────────────────────────────────┐                        │
│  │  Ambil produk score ≥ 8         │                        │
│  │  → AI buat 5 hook iklan         │                        │
│  │  → AI buat script video         │                        │
│  │  → Facebook API: buat campaign  │                        │
│  │  → budget awal: $50/hari        │                        │
│  │  → status campaign = ACTIVE     │                        │
│  └─────────────────────────────────┘                        │
│                                                             │
│  STEP 3 — Evaluasi Campaign Aktif                           │
│  ┌─────────────────────────────────┐                        │
│  │  Tarik metrik dari Facebook API │                        │
│  │  → cek 3 aturan (lihat bawah)   │                        │
│  │  → log keputusan ke DB          │                        │
│  │  → publish alert ke Redis       │                        │
│  └─────────────────────────────────┘                        │
└─────────────────────────────────────────────────────────────┘
```

---

## Aturan Keputusan (Decision Rules)

```
Untuk setiap campaign AKTIF:

  spend ≥ $500/hari?
  ─────────────────── YES → PAUSE (safety limit)
         │ NO
         ▼
  spend > 2× harga produk DAN revenue = 0?
  ──────────────────────────────────────── YES → KILL campaign
         │ NO
         ▼
  ROAS > 2?
  ───────── YES → SCALE budget ×1.2 (maks 1.5×, cap $500/hari)
         │ NO
         ▼
  CTR < 1.5%?
  ─────────── YES → Alert: ganti creative iklan
         │ NO
         ▼
  Tidak ada aksi (campaign jalan normal)
```

**Safety Limits:**
- Maks spend harian: **$500** per campaign
- Maks scaling per aksi: **1.5×**
- Maks launch baru per siklus: **3 campaign**

---

## Struktur Database (5 Tabel)

```
products ──────────────────────────────────────────
  id | name | price | rating | sold_count
  score | status (SCRAPED → LAUNCHED)

campaigns ─────────────────────────────────────────
  id | name | product_id | budget | spend
  revenue | roas | ctr | status (ACTIVE/KILLED)

creatives ─────────────────────────────────────────
  id | product_id | hooks (5 opsi) | caption | script

metrics ────────────────────────────────────────────
  id | campaign_id | spend | revenue | roas | ctr
  recorded_at (snapshot per jam)

decision_logs ──────────────────────────────────────
  id | campaign_id | action (KILL/SCALE/REPLACE_CREATIVE)
  reason | metadata (JSON) | created_at
```

---

## Struktur Folder

```
Facebook-Ads/
├── src/
│   ├── backend/                 ← C# .NET — inti sistem
│   │   ├── Core/Entities/       ← Model data (Campaign, Product, dll)
│   │   ├── Infrastructure/AI/   ← Integrasi OpenAI
│   │   ├── Infrastructure/FacebookApi/ ← Facebook Graph API client
│   │   ├── Infrastructure/Database/    ← EF Core + PostgreSQL
│   │   ├── Services/            ← 6 service utama (lihat bawah)
│   │   ├── Controllers/         ← REST API untuk dashboard (5 endpoint)
│   │   └── Worker.cs            ← Loop otomasi per jam
│   ├── bot/
│   │   └── bot.js               ← Telegram bot (Redis subscriber)
│   └── scraper/
│       └── scraper.py           ← Scraper Tokopedia + Shopee
├── dashboard/
│   └── src/App.tsx              ← React dashboard UI
├── deploy/
│   └── init.sql                 ← Schema PostgreSQL
└── docker-compose.yml           ← Orkestrasi semua service
```

---

## 6 Service Backend

| Service               | Tugasnya                                                   |
|-----------------------|------------------------------------------------------------|
| `ProductService`      | AI scoring produk (0–10)                                   |
| `CreativeService`     | Buat hook, caption, script video lewat AI                  |
| `TrafficService`      | Wrapper Facebook API (launch/scale/pause campaign)         |
| `OrchestrationService`| Koordinasi Product → Creative → Launch (maks 3/siklus)     |
| `DecisionService`     | Evaluasi campaign aktif, eksekusi Kill/Scale/Alert         |
| `TrackingService`     | Facebook Conversion API (CAPI) — tracking event revenue    |

---

## API Endpoints (Dashboard)

```
GET /api/dashboard/stats           → Total spend, revenue, avg ROAS, avg CTR
GET /api/dashboard/campaigns       → Semua campaign + statusnya
GET /api/dashboard/products        → Semua produk + score AI-nya
GET /api/dashboard/decisions       → Log keputusan terbaru (kill/scale/alert)
GET /api/dashboard/metrics/daily   → Data performa 7 hari terakhir
```

---

## Environment Variables yang Dibutuhkan

```env
# Database
DB_HOST, DB_NAME, DB_USER, DB_PASSWORD, REDIS_URL

# Facebook
FB_APP_ID, FB_APP_SECRET, FB_ACCESS_TOKEN, FB_AD_ACCOUNT_ID

# AI
OPENAI_API_KEY          ← kalau kosong, pakai mock response

# Telegram
TELEGRAM_TOKEN, TELEGRAM_CHAT_ID
```

---

## Cara Jalankan

```bash
# Clone & setup
cp .env.example .env        # isi semua variabel
docker compose up -d        # jalankan semua service

# Cek status
docker compose ps
docker compose logs backend -f
docker compose logs scraper -f
```

---

## Status Phase

| Phase   | Status          | Keterangan                                     |
|---------|-----------------|------------------------------------------------|
| Phase 1 | Selesai         | Scraper, scoring, launch, decision engine      |
| Phase 2 | In Progress     | Koneksi React dashboard ke backend API         |
| Phase 3 | Belum dimulai   | Landing page builder + advanced reporting      |

**Yang masih perlu dikerjakan (Phase 2):**
- Hapus mock data di React, ganti dengan call ke `/api/dashboard/...`
- Buat AdSet + Ad di Facebook (sekarang baru buat Campaign saja)
- Implement CAPI webhook untuk tracking revenue otomatis
- Auto-refresh Facebook token (expired setiap ~60 hari)
