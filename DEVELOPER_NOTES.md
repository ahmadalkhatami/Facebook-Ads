# 🚀 Facebook Ads Scalping Engine — Developer Notes

> **Project Path**: `/Users/ahmadalkhatami/Documents/Project AI/Facebook Ads`
> **Last Updated**: 2026-04-24
> **Current Phase**: Phase 1 ✅ Complete → Phase 2 🔜

---

## ⚡ Quick Start

```bash
# 1. Isi dulu API keys di .env (lihat section API Keys di bawah)

# 2. Jalankan semua services
docker-compose up --build

# Dashboard: http://localhost:5173
# API:       http://localhost:5000/swagger
```

---

## 📊 Status Kesiapan

| Komponen | Status | Keterangan |
|---|---|---|
| Backend (.NET) | ✅ Compiles | 0 errors, 0 warnings |
| OpenAI Service | ✅ Real | GPT-4o-mini, retry 3x, fallback graceful |
| Scraper | ✅ Real | Tokopedia + Shopee, 10 keyword trending |
| Decision Engine | ✅ Working | Kill/Scale/Replace + safety limiter |
| Telegram Bot | ✅ Working | Redis pub/sub alerts |
| Dashboard API | ✅ Real Data | 5 endpoints: stats, campaigns, products, decisions, metrics |
| Dashboard UI | 🟡 Mock Data | Phase 2: connect ke API |
| FB Adset + Ad | ❌ Missing | Phase 2: create full ads flow |
| Revenue Tracking | ❌ Missing | Phase 2: CAPI + webhook |
| Landing Page | ❌ Missing | Phase 3 |

---

## 🔑 API Keys yang Dibutuhkan

Edit file `.env`:

```env
# Facebook (dari developers.facebook.com → My Apps)
FB_APP_ID=           # App ID
FB_APP_SECRET=       # App Secret
FB_ACCESS_TOKEN=     # Long-lived user token (~60 hari), format: EAAxxxxxxx
FB_AD_ACCOUNT_ID=    # Format: act_123456789 (dengan prefix "act_")

# OpenAI (dari platform.openai.com → API Keys)
OPENAI_API_KEY=      # Format: sk-proj-xxxxx

# Telegram (dari @BotFather di Telegram)
TELEGRAM_TOKEN=      # Format: 123456789:AAAA-xxxxx
TELEGRAM_CHAT_ID=    # Chat ID kamu (kirim pesan ke @userinfobot)
```

---

## 🧠 Cara Kerja Bot

```
Setiap 6 jam (Scraper):
  Tokopedia + Shopee → filter rating>4.5, sold>1000 → simpan ke DB

Setiap 1 jam (Worker):
  1. Produk baru → AI scoring (GPT-4o-mini)
  2. Produk score≥8 → Generate creative → Launch campaign (max 3/cycle)
  3. Campaign aktif → Fetch metrics dari FB API
  4. Decision Engine:
     - spend > 2x harga produk & revenue=0 → KILL
     - ROAS > 2 → SCALE +20% (max 1.5x, max $500/hari)
     - CTR < 1.5% → REPLACE CREATIVE
  5. Semua keputusan → Telegram alert + log ke DB
```

---

## 📁 Struktur Service

```
Worker (BackgroundService)
  → OrchestrationService      # Koordinasi full loop
      → ProductService        # AI scoring produk
      → CreativeService       # Generate hook/caption/script
      → TrafficService        # Interaksi FB API
  → DecisionService           # Evaluasi & keputusan campaign
      → TrafficService        # Eksekusi keputusan ke FB
      → Redis                 # Publish alert
```

---

## ⚠️ Safety Features

- **MAX_DAILY_SPEND**: $500 per campaign — bot auto-pause jika melewati ini
- **MAX_SCALE_MULTIPLIER**: 1.5x — bot tidak bisa scale lebih dari 1.5x per cycle
- **MAX_LAUNCHES_PER_CYCLE**: 3 campaign baru per jam
- **Semua campaign baru**: Status `PAUSED` dulu di Facebook, aktifkan manual
- **Decision Log**: Semua keputusan tercatat di tabel `decision_logs`

---

## 🔜 Phase 2 — Yang Harus Dikerjakan Berikutnya

1. **Facebook Adset + Ad creation** — Sekarang hanya create Campaign, belum bisa iklan
2. **Dashboard UI** — Sambungkan ke backend API (hapus mock data)
3. **FB Token Refresh** — Token expire 60 hari, perlu auto-refresh
4. **Revenue Tracking** — Sambungkan CAPI dengan webhook order

---

## 🐛 Known Issues / Catatan

- `ConversionEvent.Currency` default `"USD"` — perlu diubah ke `"IDR"` untuk Indonesia
- Tokopedia/Shopee scraper bergantung pada public API yang bisa berubah sewaktu-waktu
- Dashboard frontend (`App.tsx`) masih menggunakan hardcoded mock data
- `FacebookApiClient` belum punya method untuk create AdSet dan Ad

---

## 🧪 Verifikasi Build

```bash
cd src/backend
dotnet build
# Expected: Build succeeded. 0 Warning(s). 0 Error(s).
```
