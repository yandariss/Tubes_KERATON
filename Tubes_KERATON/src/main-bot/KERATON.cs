// =============================================================================
// KERATON.cs
// Kelompok  : KERATON
// Strategi  : Confidence Scoring Greedy
// Heuristik : score = enemyEnergy / distance
//
// Penjelasan strategi:
//   Setiap turn, bot mengevaluasi semua musuh yang terdeteksi radar dan memilih
//   target dengan nilai score tertinggi menggunakan formula:
//       score = enemyEnergy / distance
//   Nilai score tinggi berarti musuh lemah (energi rendah) sekaligus dekat,
//   sehingga mudah dieliminasi dan peluang tembakan mengenai lebih besar.
//   Bot juga menerapkan gerakan zig-zag dan penghindaran dinding otomatis.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class KERATON : Bot
{
    // ─── Struktur data musuh ──────────────────────────────────────────────────
    /// <summary>Menyimpan data terbaru setiap musuh yang pernah terdeteksi.</summary>
    private Dictionary<int, EnemyInfo> _enemyData = new();

    // ─── State movement ───────────────────────────────────────────────────────
    private int _turnCount     = 0;
    private int _moveDirection = 1;   // 1 = maju, -1 = mundur

    // ─── Konstanta ────────────────────────────────────────────────────────────
    private const double WALL_MARGIN  = 60.0;   // Jarak aman dari dinding (piksel)
    private const int    ZIGZAG_FREQ  = 20;     // Frekuensi pergantian arah (turn)
    private const double MOVE_STEP    = 80.0;   // Jarak per langkah zig-zag
    private const double TURN_STEP    = 15.0;   // Sudut belok zig-zag (derajat)

    // ─── Entry point ──────────────────────────────────────────────────────────
    static void Main(string[] args) => new KERATON().Start();

    // ─── Konstruktor ──────────────────────────────────────────────────────────
    KERATON() : base(BotInfo.FromFile("KERATON.json")) { }

    // =========================================================================
    // Run() — Loop utama bot, dijalankan setiap awal ronde
    // =========================================================================
    public override void Run()
    {
        // Warna identitas bot KERATON (biru kerajaan)
        BodyColor   = Color.FromArgb(0x1A, 0x53, 0x76);  // Biru tua
        TurretColor = Color.FromArgb(0x2E, 0x86, 0xC1);  // Biru sedang
        RadarColor  = Color.FromArgb(0x00, 0xFF, 0xFF);  // Cyan
        BulletColor = Color.FromArgb(0xFF, 0xD7, 0x00);  // Gold
        ScanColor   = Color.FromArgb(0x00, 0xFF, 0x88);  // Hijau neon
        GunColor    = Color.FromArgb(0x0E, 0x6B, 0xB6);  // Biru medium
        TracksColor = Color.FromArgb(0x0A, 0x29, 0x4D);  // Biru sangat tua

        // Radar dan gun bergerak independen dari badan tank
        IsAdjustGunForBodyTurn  = true;
        IsAdjustRadarForGunTurn = true;

        while (IsRunning)
        {
            _turnCount++;

            // 1) Radar selalu berputar penuh agar scan arc tidak pernah nol
            SetTurnRadarRight(double.PositiveInfinity);

            // 2) Hindari dinding sebelum bergerak
            AvoidWalls();

            // 3) Gerakan zig-zag untuk mempersulit musuh menembak
            ZigZagMove();

            // 4) Tembak target terbaik berdasarkan greedy scoring
            ExecuteBestShot();

            // Kirim semua perintah ke server dalam satu turn
            Execute();
        }
    }

    // =========================================================================
    // OnScannedBot — Dipanggil setiap kali radar mendeteksi musuh
    // =========================================================================
    public override void OnScannedBot(ScannedBotEvent evt)
    {
        // Simpan / perbarui data musuh yang terdeteksi
        _enemyData[evt.ScannedBotId] = new EnemyInfo
        {
            BotId     = evt.ScannedBotId,
            X         = evt.X,
            Y         = evt.Y,
            Energy    = evt.Energy,
            Direction = evt.Direction,
            Speed     = evt.Speed
        };
    }

    // =========================================================================
    // OnBotDeath — Hapus data musuh yang sudah mati dari dictionary
    // =========================================================================
    public override void OnBotDeath(BotDeathEvent evt)
    {
        _enemyData.Remove(evt.VictimId);
    }

    // =========================================================================
    // OnHitByBullet — Reaksi saat terkena peluru: belok 90° dari arah datang
    // =========================================================================
    public override void OnHitByBullet(HitByBulletEvent evt)
    {
        // Putar badan 90 derajat dari arah datangnya peluru
        double bearing = CalcBearing(evt.Bullet.Direction);
        SetTurnRight(90 - bearing);
    }

    // =========================================================================
    // OnHitWall — Putar badan saat menabrak dinding
    // =========================================================================
    public override void OnHitWall(HitWallEvent evt)
    {
        // Balikkan arah pergerakan untuk keluar dari dinding
        _moveDirection *= -1;
        SetTurnRight(45);
    }

    // =========================================================================
    // ExecuteBestShot — Inti logika greedy: pilih target terbaik lalu tembak
    //
    // FUNGSI HEURISTIK: score = enemyEnergy / distance
    //   - enemyEnergy rendah → musuh mudah dieliminasi (dapat Bullet Damage Bonus)
    //   - distance kecil    → peluang tembakan mengenai lebih besar
    //   - Kombinasi keduanya menghasilkan pilihan target paling menguntungkan
    // =========================================================================
    private void ExecuteBestShot()
    {
        // Tidak menembak jika tidak ada data musuh atau meriam masih panas
        if (_enemyData.Count == 0 || GunHeat > 0) return;

        EnemyInfo bestTarget = null;
        double    bestScore  = double.NegativeInfinity;

        foreach (var enemy in _enemyData.Values)
        {
            double distance = DistanceTo(enemy.X, enemy.Y);
            if (distance < 1.0) continue;  // Cegah pembagian dengan nol

            // ── FUNGSI HEURISTIK GREEDY ─────────────────────────────────────
            // score = enemyEnergy / distance
            // Semakin tinggi score, semakin prioritas musuh ini sebagai target
            double score = enemy.Energy / distance;

            if (score > bestScore)
            {
                bestScore  = score;
                bestTarget = enemy;
            }
        }

        if (bestTarget == null) return;

        // Arahkan turret ke posisi target
        double bearing    = BearingTo(bestTarget.X, bestTarget.Y);
        double gunBearing = NormalizeRelativeAngle(bearing - GunDirection);
        SetTurnGunRight(gunBearing);

        // Tentukan daya tembak berdasarkan jarak (greedy lokal)
        double dist      = DistanceTo(bestTarget.X, bestTarget.Y);
        double firepower = CalculateFirePower(dist);

        // Tembak jika meriam sudah dingin dan energi mencukupi
        if (GunHeat == 0 && Energy > firepower)
            SetFire(firepower);
    }

    // =========================================================================
    // CalculateFirePower — Tentukan daya tembak optimal berdasarkan jarak
    //
    // Logika greedy lokal: daya tembak besar saat dekat (damage maksimal),
    // daya kecil saat jauh (hemat energi, peluru lebih cepat).
    // =========================================================================
    private double CalculateFirePower(double distance)
    {
        if (distance < 150) return 3.0;   // Sangat dekat: daya maksimum
        if (distance < 300) return 2.0;   // Dekat: daya tinggi
        if (distance < 500) return 1.0;   // Sedang: daya menengah
        return 0.5;                       // Jauh: hemat energi
    }

    // =========================================================================
    // ZigZagMove — Gerakan zig-zag untuk menghindari peluru musuh
    //
    // Bot berganti arah setiap ZIGZAG_FREQ turn sehingga gerakannya sulit
    // diprediksi oleh sistem targeting musuh.
    // =========================================================================
    private void ZigZagMove()
    {
        if (_turnCount % ZIGZAG_FREQ == 0)
            _moveDirection *= -1;   // Balik arah secara periodik

        SetForward(MOVE_STEP * _moveDirection);
        SetTurnRight(TURN_STEP * _moveDirection);
    }

    // =========================================================================
    // AvoidWalls — Deteksi dan hindari tabrakan dengan dinding arena
    //
    // Jika bot terlalu dekat dengan tepi arena, arahkan menuju tengah arena
    // untuk mencegah wall damage yang menguras energi.
    // =========================================================================
    private void AvoidWalls()
    {
        bool nearLeft   = X < WALL_MARGIN;
        bool nearRight  = X > ArenaWidth  - WALL_MARGIN;
        bool nearBottom = Y < WALL_MARGIN;
        bool nearTop    = Y > ArenaHeight - WALL_MARGIN;

        if (nearLeft || nearRight || nearBottom || nearTop)
        {
            // Putar badan menghadap tengah arena lalu maju
            double centerX       = ArenaWidth  / 2.0;
            double centerY       = ArenaHeight / 2.0;
            double bearingCenter = BearingTo(centerX, centerY);
            SetTurnRight(NormalizeRelativeAngle(bearingCenter - Direction));
            SetForward(100);
        }
    }
}

// =============================================================================
// EnemyInfo — Menyimpan data snapshot musuh dari event OnScannedBot
// =============================================================================
public class EnemyInfo
{
    public int    BotId     { get; set; }
    public double X         { get; set; }
    public double Y         { get; set; }
    public double Energy    { get; set; }
    public double Direction { get; set; }
    public double Speed     { get; set; }
}
