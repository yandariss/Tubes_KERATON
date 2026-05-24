// =============================================================================
// KERATON-Aggressive.cs
// Kelompok  : KERATON
// Strategi  : Aggressive Greedy (Alternatif 1)
// Heuristik : score = 1 / distance
//
// Penjelasan strategi:
//   Bot selalu menyerang musuh yang berada paling dekat dengan daya tembak
//   penuh (3.0). Filosofinya: musuh terdekat paling mudah ditembak dan
//   berpotensi memberikan Ram Damage bonus jika bot berhasil menerobos.
//   Kelemahan: sangat agresif sehingga rentan kehilangan energi cepat.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class KERATONAggressive : Bot
{
    private Dictionary<int, EnemyInfo> _enemyData = new();
    private int _turnCount = 0;

    static void Main(string[] args) => new KERATONAggressive().Start();

    KERATONAggressive() : base(BotInfo.FromFile("KERATON-Aggressive.json")) { }

    public override void Run()
    {
        // Warna identitas: merah agresif
        BodyColor   = Color.FromArgb(0x8B, 0x00, 0x00);  // Dark Red
        TurretColor = Color.FromArgb(0xFF, 0x00, 0x00);  // Red
        RadarColor  = Color.FromArgb(0xFF, 0x69, 0xB4);  // Hot Pink
        BulletColor = Color.FromArgb(0xFF, 0xA5, 0x00);  // Orange
        ScanColor   = Color.FromArgb(0xFF, 0x00, 0x00);  // Red
        GunColor    = Color.FromArgb(0xB2, 0x22, 0x22);  // FireBrick

        IsAdjustGunForBodyTurn  = true;
        IsAdjustRadarForGunTurn = true;

        while (IsRunning)
        {
            _turnCount++;

            // Radar selalu scan
            SetTurnRadarRight(double.PositiveInfinity);

            // Gerak maju terus ke arah musuh terdekat
            AggressiveMove();

            // Tembak musuh terdekat
            ShootClosest();

            Execute();
        }
    }

    public override void OnScannedBot(ScannedBotEvent evt)
    {
        _enemyData[evt.ScannedBotId] = new EnemyInfo
        {
            BotId  = evt.ScannedBotId,
            X      = evt.X,
            Y      = evt.Y,
            Energy = evt.Energy
        };
    }

    public override void OnBotDeath(BotDeathEvent evt)
    {
        _enemyData.Remove(evt.VictimId);
    }

    // =========================================================================
    // ShootClosest — Heuristik: score = 1 / distance (pilih musuh terdekat)
    // =========================================================================
    private void ShootClosest()
    {
        if (_enemyData.Count == 0 || GunHeat > 0) return;

        EnemyInfo closest   = null;
        double    bestScore = double.NegativeInfinity;

        foreach (var enemy in _enemyData.Values)
        {
            double distance = DistanceTo(enemy.X, enemy.Y);
            if (distance < 1.0) continue;

            // HEURISTIK: score = 1 / distance → musuh terdekat = score tertinggi
            double score = 1.0 / distance;

            if (score > bestScore)
            {
                bestScore = score;
                closest   = enemy;
            }
        }

        if (closest == null) return;

        // Arahkan gun dan tembak dengan daya penuh
        double bearing = BearingTo(closest.X, closest.Y);
        SetTurnGunRight(NormalizeRelativeAngle(bearing - GunDirection));

        if (GunHeat == 0 && Energy > 3.0)
            SetFire(3.0);  // Selalu tembak dengan daya maksimum
    }

    // =========================================================================
    // AggressiveMove — Gerak menuju musuh terdekat
    // =========================================================================
    private void AggressiveMove()
    {
        if (_enemyData.Count == 0) return;

        EnemyInfo closest  = null;
        double    minDist  = double.MaxValue;

        foreach (var enemy in _enemyData.Values)
        {
            double d = DistanceTo(enemy.X, enemy.Y);
            if (d < minDist) { minDist = d; closest = enemy; }
        }

        if (closest == null) return;

        // Putar badan menuju musuh dan maju
        double bearing = BearingTo(closest.X, closest.Y);
        SetTurnRight(NormalizeRelativeAngle(bearing - Direction));
        SetForward(Math.Min(100, minDist));

        // Hindari dinding sederhana
        if (X < 60 || X > ArenaWidth - 60 || Y < 60 || Y > ArenaHeight - 60)
        {
            SetTurnRight(90);
            SetForward(80);
        }
    }
}

public class EnemyInfo
{
    public int    BotId  { get; set; }
    public double X      { get; set; }
    public double Y      { get; set; }
    public double Energy { get; set; }
}
