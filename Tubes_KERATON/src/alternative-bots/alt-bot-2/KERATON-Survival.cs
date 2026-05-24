// =============================================================================
// KERATON-Survival.cs
// Kelompok  : KERATON
// Strategi  : Survival Greedy (Alternatif 2)
// Heuristik : score = enemyCount * selfEnergy
//
// Penjelasan strategi:
//   Bot memprioritaskan bertahan hidup selama mungkin untuk mengumpulkan
//   Survival Score (50 poin tiap bot lain mati) dan Last Survival Bonus.
//   Ketika masih banyak musuh (enemyCount tinggi) dan energi bot sendiri
//   masih tinggi, bot menghindari konflik dan bergerak ke zona aman.
//   Bot baru menyerang musuh terlemah ketika kondisi mulai menguntungkan.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class KERATONSurvival : Bot
{
    private Dictionary<int, EnemyData> _enemies = new();

    // Threshold: jika score di atas ini, prioritas survival (hindari konflik)
    private const double SURVIVAL_THRESHOLD = 200.0;

    static void Main(string[] args) => new KERATONSurvival().Start();

    KERATONSurvival() : base(BotInfo.FromFile("KERATON-Survival.json")) { }

    public override void Run()
    {
        // Warna identitas: hijau survivalist
        BodyColor   = Color.FromArgb(0x14, 0x5A, 0x32);  // Dark Green
        TurretColor = Color.FromArgb(0x1E, 0x8B, 0x4E);  // Forest Green
        RadarColor  = Color.FromArgb(0xAD, 0xFF, 0x2F);  // GreenYellow
        BulletColor = Color.FromArgb(0x7F, 0xFF, 0x00);  // Chartreuse
        ScanColor   = Color.FromArgb(0x00, 0xFF, 0x7F);  // SpringGreen
        GunColor    = Color.FromArgb(0x2E, 0x8B, 0x57);  // SeaGreen

        IsAdjustGunForBodyTurn  = true;
        IsAdjustRadarForGunTurn = true;

        while (IsRunning)
        {
            // Radar selalu aktif
            SetTurnRadarRight(double.PositiveInfinity);

            // ── HEURISTIK: score = enemyCount * selfEnergy ───────────────────
            // Semakin banyak musuh dan semakin tinggi energi kita,
            // semakin kita harus menghindari konflik (kumpulkan survival score)
            double score = _enemies.Count * Energy;

            if (score > SURVIVAL_THRESHOLD)
            {
                // Mode SURVIVAL: bergerak ke tengah arena, hindari konflik
                MoveToCenter();
            }
            else
            {
                // Mode ATTACK: musuh sudah sedikit atau energi kita rendah,
                // saatnya menyerang musuh terlemah untuk dapat bullet damage bonus
                AttackWeakest();
            }

            // Selalu hindari dinding
            AvoidWalls();

            Execute();
        }
    }

    public override void OnScannedBot(ScannedBotEvent evt)
    {
        _enemies[evt.ScannedBotId] = new EnemyData
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
        // Hindari tembakan dengan manuver 90 derajat
        double bearing = CalcBearing(evt.Bullet.Direction);
        SetTurnRight(90 - bearing);
    }

    // =========================================================================
    // MoveToCenter — Bergerak menuju tengah arena (zona paling aman)
    // =========================================================================
    private void MoveToCenter()
    {
        double centerX = ArenaWidth  / 2.0;
        double centerY = ArenaHeight / 2.0;
        double dist    = DistanceTo(centerX, centerY);

        if (dist > 80)
        {
            double bearing = BearingTo(centerX, centerY);
            SetTurnRight(NormalizeRelativeAngle(bearing - Direction));
            SetForward(Math.Min(100, dist));
        }
        else
        {
            // Sudah di tengah: putar saja untuk scan
            SetTurnRight(30);
        }
    }

    // =========================================================================
    // AttackWeakest — Serang musuh dengan energi paling rendah
    // =========================================================================
    private void AttackWeakest()
    {
        if (_enemies.Count == 0 || GunHeat > 0) return;

        EnemyData weakest   = null;
        double    minEnergy = double.MaxValue;

        foreach (var e in _enemies.Values)
        {
            if (e.Energy < minEnergy)
            {
                minEnergy = e.Energy;
                weakest   = e;
            }
        }

        if (weakest == null) return;

        double bearing    = BearingTo(weakest.X, weakest.Y);
        double gunBearing = NormalizeRelativeAngle(bearing - GunDirection);
        SetTurnGunRight(gunBearing);

        double dist = DistanceTo(weakest.X, weakest.Y);
        double fp   = dist < 300 ? 2.0 : 1.0;

        if (GunHeat == 0 && Energy > fp)
            SetFire(fp);
    }

    // =========================================================================
    // AvoidWalls — Hindari dinding arena
    // =========================================================================
    private void AvoidWalls()
    {
        const double margin = 60.0;
        if (X < margin || X > ArenaWidth - margin ||
            Y < margin || Y > ArenaHeight - margin)
        {
            double bearing = BearingTo(ArenaWidth / 2.0, ArenaHeight / 2.0);
            SetTurnRight(NormalizeRelativeAngle(bearing - Direction));
            SetForward(100);
        }
    }
}

public class EnemyData
{
    public int    BotId  { get; set; }
    public double X      { get; set; }
    public double Y      { get; set; }
    public double Energy { get; set; }
}
