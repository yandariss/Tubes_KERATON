// =============================================================================
// KERATON-Aggressive.cs — Alternatif 1 (UPGRADED)
// Strategi  : Aggressive Greedy
// Heuristik : score = 1 / distance
//
// Upgrade v2:
//  - Selalu mengejar dan menempel musuh
//  - Predictive firing saat mengejar
//  - Ram strategy: sengaja menabrak musuh lemah
//  - Tidak berhenti bergerak
// =============================================================================
using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class KERATONAggressive : Bot
{
    static void Main(string[] args) => new KERATONAggressive().Start();
    KERATONAggressive() : base(BotInfo.FromFile("KERATON-Aggressive.json")) { }

    private int _moveDir = 1;
    private int _count = 0;

    public override void Run()
    {
        BodyColor = Color.FromArgb(0x8B, 0x00, 0x00);
        TurretColor = Color.FromArgb(0xFF, 0x00, 0x00);
        RadarColor = Color.FromArgb(0xFF, 0x69, 0xB4);
        BulletColor = Color.FromArgb(0xFF, 0xA5, 0x00);
        GunColor = Color.FromArgb(0xB2, 0x22, 0x22);

        while (IsRunning)
        {
            TurnRadarRight(360);
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        double dist = DistanceTo(e.X, e.Y);
        _count++;

        // ══════════════════════════════════════════
        // GREEDY: score = 1 / distance
        // Musuh terdekat selalu diserang habis-habisan
        // ══════════════════════════════════════════

        // Kunci radar ke musuh
        double radarB = NormalizeRelativeAngle(BearingTo(e.X, e.Y) - RadarDirection);
        TurnRadarRight(radarB * 2);

        // Prediksi posisi musuh
        double fp = 3.0;
        double bulletSpeed = 20 - (3 * fp);
        double travelTime = dist / bulletSpeed;
        double pX = e.X + Math.Sin(e.Direction * Math.PI / 180) * e.Speed * travelTime;
        double pY = e.Y + Math.Cos(e.Direction * Math.PI / 180) * e.Speed * travelTime;

        // Arahkan gun ke prediksi posisi & tembak daya penuh
        TurnGunRight(NormalizeRelativeAngle(BearingTo(pX, pY) - GunDirection));
        Fire(fp);

        // RAM STRATEGY: kalau musuh lemah & dekat, tabrak!
        if (e.Energy < 20 && dist < 150)
        {
            TurnRight(NormalizeRelativeAngle(BearingTo(e.X, e.Y) - Direction));
            Forward(dist + 50); // Terobos sampai kena
        }
        else
        {
            // Kejar musuh sambil zigzag kecil
            double bodyB = NormalizeRelativeAngle(BearingTo(e.X, e.Y) - Direction);
            TurnRight(bodyB);
            if (_count % 10 == 0) _moveDir *= -1;
            TurnRight(20 * _moveDir);
            Forward(Math.Min(dist, 120));
        }

        AvoidWalls();
    }

    private void AvoidWalls()
    {
        double m = 60;
        if (X < m || X > ArenaWidth - m || Y < m || Y > ArenaHeight - m)
        {
            TurnRight(NormalizeRelativeAngle(
                BearingTo(ArenaWidth / 2.0, ArenaHeight / 2.0) - Direction));
            Forward(100);
        }
    }

    public override void OnHitByBullet(HitByBulletEvent e)
    {
        // Agresif: terus maju meski kena peluru
        _moveDir *= -1;
        Forward(80);
    }

    public override void OnHitBot(HitBotEvent e)
    {
        // Nabrak musuh: tembak terus!
        TurnGunRight(NormalizeRelativeAngle(BearingTo(e.X, e.Y) - GunDirection));
        Fire(3.0);
        Fire(3.0);
    }

    public override void OnHitWall(HitWallEvent e)
    {
        Back(80);
        TurnRight(90);
        Forward(100);
    }
}