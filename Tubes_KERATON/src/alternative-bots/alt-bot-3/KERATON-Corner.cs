// =============================================================================
// KERATON-Corner.cs
// Kelompok  : KERATON
// Strategi  : Corner Control Greedy (Alternatif 3)
// Heuristik : score = cornerDist + (1 / enemyEnergy)
//
// Penjelasan strategi:
//   Bot memprioritaskan musuh yang berada di sudut arena karena mereka
//   memiliki ruang gerak yang sangat terbatas, sehingga lebih mudah ditembak.
//   Faktor energi musuh (1/enemyEnergy) ditambahkan sebagai tiebreaker:
//   jika ada dua musuh di sudut, prioritaskan yang lebih lemah.
//   Bot sendiri selalu berusaha menghindari sudut arena.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class KERATONCorner : Bot
{
    private Dictionary<int, CornerEnemyInfo> _enemies = new();

    static void Main(string[] args) => new KERATONCorner().Start();

    KERATONCorner() : base(BotInfo.FromFile("KERATON-Corner.json")) { }

    public override void Run()
    {
        // Warna identitas: ungu kerajaan
        BodyColor   = Color.FromArgb(0x4B, 0x00, 0x82);  // Indigo
        TurretColor = Color.FromArgb(0x80, 0x00, 0x80);  // Purple
        RadarColor  = Color.FromArgb(0xDA, 0x70, 0xD6);  // Orchid
        BulletColor = Color.FromArgb(0xFF, 0x00, 0xFF);  // Magenta
        ScanColor   = Color.FromArgb(0xEE, 0x82, 0xEE);  // Violet
        GunColor    = Color.FromArgb(0x6A, 0x0D, 0xAD);  // Purple Dark

        IsAdjustGunForBodyTurn  = true;
        IsAdjustRadarForGunTurn = true;

        while (IsRunning)
        {
            SetTurnRadarRight(double.PositiveInfinity);

            // Hindari sudut untuk diri sendiri
            AvoidCorners();

            // Tembak musuh yang terjebak di sudut
            ShootCornerTarget();

            Execute();
        }
    }

    public override void OnScannedBot(ScannedBotEvent evt)
    {
        _enemies[evt.ScannedBotId] = new CornerEnemyInfo
        {
            BotId  = evt.ScannedBotId,
            X      = evt.X,
            Y      = evt.Y,
            Energy = evt.Energy
        };
    }

    public override void OnBotDeath(BotDeathEvent evt)
    {
        _enemies.Remove(evt.VictimId);
    }

    public override void OnHitByBullet(HitByBulletEvent evt)
    {
        double bearing = CalcBearing(evt.Bullet.Direction);
        SetTurnRight(90 - bearing);
    }

    // =========================================================================
    // ShootCornerTarget — Heuristik: score = cornerDist + (1 / enemyEnergy)
    //
    // cornerDist = jarak musuh ke sudut arena terdekat.
    // Nilai score tinggi → musuh dekat sudut (terjebak) DAN energi rendah.
    // =========================================================================
    private void ShootCornerTarget()
    {
        if (_enemies.Count == 0 || GunHeat > 0) return;

        // Empat sudut arena
        double w = ArenaWidth, h = ArenaHeight;
        var corners = new (double cx, double cy)[]
        {
            (0, 0), (w, 0), (0, h), (w, h)
        };

        CornerEnemyInfo bestTarget = null;
        double          bestScore  = double.NegativeInfinity;

        foreach (var enemy in _enemies.Values)
        {
            if (enemy.Energy <= 0) continue;

            // Jarak musuh ke sudut arena terdekat
            double minCornerDist = double.MaxValue;
            foreach (var (cx, cy) in corners)
            {
                double d = Math.Sqrt(Math.Pow(enemy.X - cx, 2) + Math.Pow(enemy.Y - cy, 2));
                if (d < minCornerDist) minCornerDist = d;
            }

            // HEURISTIK: score = cornerDist + (1 / enemyEnergy)
            // cornerDist rendah → musuh dekat sudut (lebih prioritas)
            // Agar musuh dekat sudut punya score LEBIH TINGGI, kita balik:
            // score = (maxPossibleDist - cornerDist) + (1 / enemyEnergy)
            double maxDist = Math.Sqrt(w * w + h * h);
            double score   = (maxDist - minCornerDist) + (1.0 / enemy.Energy);

            if (score > bestScore)
            {
                bestScore  = score;
                bestTarget = enemy;
            }
        }

        if (bestTarget == null) return;

        double bearing    = BearingTo(bestTarget.X, bestTarget.Y);
        double gunBearing = NormalizeRelativeAngle(bearing - GunDirection);
        SetTurnGunRight(gunBearing);

        double dist = DistanceTo(bestTarget.X, bestTarget.Y);
        double fp   = dist < 200 ? 2.5 : dist < 400 ? 1.5 : 0.8;

        if (GunHeat == 0 && Energy > fp)
            SetFire(fp);
    }

    // =========================================================================
    // AvoidCorners — Bot sendiri menghindari sudut arena
    // Bergerak zig-zag di area tengah untuk mempertahankan mobilitas.
    // =========================================================================
    private void AvoidCorners()
    {
        const double cornerMargin = 100.0;
        bool inCorner =
            (X < cornerMargin || X > ArenaWidth  - cornerMargin) &&
            (Y < cornerMargin || Y > ArenaHeight - cornerMargin);

        if (inCorner)
        {
            // Jika di sudut: segera keluar menuju tengah
            double bearing = BearingTo(ArenaWidth / 2.0, ArenaHeight / 2.0);
            SetTurnRight(NormalizeRelativeAngle(bearing - Direction));
            SetForward(150);
        }
        else
        {
            // Bergerak normal dengan menghindari dinding
            if (X < 60 || X > ArenaWidth - 60 || Y < 60 || Y > ArenaHeight - 60)
            {
                double bearing = BearingTo(ArenaWidth / 2.0, ArenaHeight / 2.0);
                SetTurnRight(NormalizeRelativeAngle(bearing - Direction));
                SetForward(100);
            }
            else
            {
                SetForward(60);
                SetTurnRight(20);
            }
        }
    }
}

public class CornerEnemyInfo
{
    public int    BotId  { get; set; }
    public double X      { get; set; }
    public double Y      { get; set; }
    public double Energy { get; set; }
}
