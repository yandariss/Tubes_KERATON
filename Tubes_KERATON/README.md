# Tubes_KERATON
**IF25-21013 Strategi Algoritma — Semester Genap 2026/2027**  
Institut Teknologi Sumatera

---

## Kelompok KERATON
| Nama | NIM |
|------|-----|
| Anggota 1 | NIM 1 |
| Anggota 2 | NIM 2 |
| Anggota 3 | NIM 3 |

---

## Deskripsi Singkat Algoritma Greedy

Tugas besar ini mengimplementasikan **algoritma greedy** pada bot permainan **Robocode Tank Royale**. Setiap turn, bot mengevaluasi semua musuh yang terdeteksi radar dan membuat keputusan **secara lokal optimal** — memilih aksi terbaik yang tersedia saat itu tanpa merencanakan ke depan.

### Bot yang Dikembangkan

| Bot | File | Heuristik | Strategi |
|-----|------|-----------|---------|
| **KERATON** *(main)* | `src/main-bot/` | `score = enemyEnergy / distance` | **Confidence Scoring Greedy** — pilih musuh yang lemah sekaligus dekat |
| KERATON-Aggressive | `src/alternative-bots/alt-bot-1/` | `score = 1 / distance` | Selalu serang musuh terdekat dengan daya penuh |
| KERATON-Survival | `src/alternative-bots/alt-bot-2/` | `score = enemyCount × selfEnergy` | Prioritas bertahan hidup; serang hanya saat kondisi aman |
| KERATON-Corner | `src/alternative-bots/alt-bot-3/` | `score = cornerDist + (1/enemyEnergy)` | Prioritas musuh yang terjebak di sudut arena |

---

## Struktur Repository

```
Tubes_KERATON/
├── src/
│   ├── main-bot/                    # Bot utama (Confidence Scoring Greedy)
│   │   ├── KERATON.cs
│   │   ├── KERATON.json
│   │   ├── KERATON.csproj
│   │   ├── KERATON.cmd
│   │   └── KERATON.sh
│   └── alternative-bots/
│       ├── alt-bot-1/               # Aggressive Greedy
│       │   ├── KERATON-Aggressive.cs
│       │   ├── KERATON-Aggressive.json
│       │   ├── KERATON-Aggressive.csproj
│       │   ├── KERATON-Aggressive.cmd
│       │   └── KERATON-Aggressive.sh
│       ├── alt-bot-2/               # Survival Greedy
│       │   ├── KERATON-Survival.cs
│       │   ├── KERATON-Survival.json
│       │   ├── KERATON-Survival.csproj
│       │   ├── KERATON-Survival.cmd
│       │   └── KERATON-Survival.sh
│       └── alt-bot-3/               # Corner Control Greedy
│           ├── KERATON-Corner.cs
│           ├── KERATON-Corner.json
│           ├── KERATON-Corner.csproj
│           ├── KERATON-Corner.cmd
│           └── KERATON-Corner.sh
├── doc/
│   └── KERATON.pdf                  # Laporan tugas besar
└── README.md
```

---

## Requirements

- **Java** ≥ 11 (untuk menjalankan game engine `.jar`)
- **.NET SDK** ≥ 6.0 (untuk mengkompilasi dan menjalankan bot C#)
- Game engine: `robocode-tankroyale-gui-0.30.0.jar` (versi modifikasi asisten ITERA)

Cek versi .NET yang terinstal:
```bash
dotnet --version
```
> Jika versi yang terinstal adalah `net8.0`, ubah `<TargetFramework>net6.0</TargetFramework>` pada file `.csproj` masing-masing bot menjadi `net8.0`.

---

## Cara Menjalankan Game Engine

```bash
java -jar robocode-tankroyale-gui-0.30.0.jar
```

---

## Cara Menjalankan Bot (via GUI)

1. Jalankan game engine (langkah di atas).
2. Klik **Config** → **Bot Root Directories** → masukkan path folder `src/` dari repository ini.
   > ⚠️ Pastikan path **tidak mengandung spasi** (contoh yang salah: `C:\Strategi Algoritma\tubes`).
3. Klik **Battle** → **Start Battle**.
4. Boot bot yang diinginkan dari panel kiri atas, klik **Boot →**.
5. Tambahkan bot ke permainan dari panel kiri bawah, klik **Add →**.
6. Klik **Start Battle**.

---

## Cara Menjalankan Bot (via Terminal)

```bash
# 1. Jalankan game engine dan local server terlebih dahulu

# 2. Ambil bots_secret dari file server.properties
# Linux/macOS:
export SERVER_SECRET=<isi_bots_secret_dari_server.properties>
# Windows CMD:
set SERVER_SECRET=<isi_bots_secret_dari_server.properties>

# 3. Masuk ke folder bot dan jalankan
cd src/main-bot
dotnet run
```

---

## Cara Build Manual

```bash
# Masuk ke folder bot yang ingin di-build
cd src/main-bot

# Build
dotnet build

# Jalankan (setelah build)
dotnet run --no-build
```

Atau gunakan script runner yang sudah disediakan:

**Windows:**
```cmd
cd src\main-bot
KERATON.cmd
```

**Linux / macOS:**
```bash
cd src/main-bot
chmod +x KERATON.sh
./KERATON.sh
```

---

## Troubleshooting

| Masalah | Solusi |
|---------|--------|
| Bot tidak muncul di panel "Joined Bots" | Pastikan path tidak mengandung spasi; cek `<TargetFramework>` sesuai versi .NET |
| `GunHeat` selalu > 0 | Bot menunggu meriam dingin secara otomatis — normal |
| Bot menabrak dinding terus | Pastikan `AvoidWalls()` dipanggil setiap turn di dalam `Run()` |
| Bot tidak terdeteksi radar | `SetTurnRadarRight(double.PositiveInfinity)` harus dipanggil setiap turn |

---

## Laporan

Laporan lengkap tersedia di: `doc/KERATON.pdf`
