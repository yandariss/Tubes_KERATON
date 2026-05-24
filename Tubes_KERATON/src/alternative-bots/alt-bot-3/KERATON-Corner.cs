// =============================================================================
// KERATON-Corner.cs — Alternatif 3 (UPGRADED)
// Strategi  : Corner Control Greedy
// Heuristik : score = (maxDist - cornerDist) + (1 / enemyEnergy)
//
// Upgrade v2:
//  - Strafe (gerakan lateral) saat menembak
//  - Deteksi tembakan musuh dari perubahan energi
//  - Prediksi posisi musuh (linear targeting)
//  - Posisi optimal: tepi arena (bukan sudut) untuk
//    memaksimalkan jangkauan penglihatan musuh di sudut
//  - Eksekusi cepat musuh hampir mati
// =============================================================================
using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class KERATONCorner : Bot
{
    static void Main(string[] args) => new KERATONCorner().Start();
    KERATONCorner() : base(BotInfo.FromFile("KERATON-Corner.json")) { }

    private int _strafeDir = 1;
    private int _count = 0;
    private double _lastEnergy = 100;

    public override void Run()
    {
        BodyColor = Color.FromArgb(0x4B, 0x00, 0x82);
        TurretColor = Color.FromArgb(0x80, 0x00, 0x80);
        RadarColor = Color.FromArgb(0xDA, 0x70, 0xD6);
        BulletColor = Color.FromArgb(0xFF, 0x00, 0xFF);
        GunColor = Color.FromArgb(0x6A, 0x0D, 0xAD);

        while (IsRunning)
        {
            TurnRadarRight(360);

            // Bot sendiri jauhi sudut arena
            bool inCorner =
                (X < 100 || X > ArenaWidth - 100) &&
                (Y < 100 || Y > ArenaHeight - 100);

            if (inCorner)
            {
                TurnRight(NormalizeRelativeAngle(
                    BearingTo(ArenaWidth / 2.0, ArenaHeight / 2.0) - Direction));
                Forward(200);
            }

            AvoidWalls();
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        double dist = DistanceTo(e.X, e.Y);
        double w = ArenaWidth;
        double h = ArenaHeight;
        _count++;

        // Deteksi musuh menembak
        bool enemyFired = e.Energy < _lastEnergy - 0.09;
        _lastEnergy = e.Energy;
        if (enemyFired) _strafeDir *= -1;

        // ══════════════════════════════════════════════════════
        // GREEDY: score = (maxDist - cornerDist) + (1/energy)
        // ══════════════════════════════════════════════════════
        double[] cDists = {
            Math.Sqrt(e.X * e.X + e.Y * e.Y),
            Math.Sqrt((e.X-w)*(e.X-w) + e.Y * e.Y),
            Math.Sqrt(e.X * e.X + (e.Y-h)*(e.Y-h)),
            Math.Sqrt((e.X-w)*(e.X-w) + (e.Y-h)*(e.Y-h))
        };
        double minCD = cDists[0];
        foreach (var d in cDists) if (d < minCD) minCD = d;
        double maxDist = Math.Sqrt(w * w + h * h);
        double score = (maxDist - minCD) + (1.0 / Math.Max(e.Energy, 0.1));

        // Kunci radar
        double radarB = NormalizeRelativeAngle(BearingTo(e.X, e.Y) - RadarDirection);
        TurnRadarRight(radarB * 2);

        // ── FIREPOWER ─────────────────────────────────
        double fp;
        if (dist < 150) fp = 3.0;
        else if (dist < 300) fp = 2.5;
        else if (dist < 500) fp = 2.0;
        else fp = 1.0;
        if (e.Energy <= 16) fp = 3.0;
        if (Energy < 20) fp = Math.Min(fp, 1.0);

        // ── LINEAR TARGETING ──────────────────────────
        double bulletSpeed = 20 - (3 * fp);
        double travelTime = dist / bulletSpeed;
        double pX = e.X + Math.Sin(e.Direction * Math.PI / 180) * e.Speed * travelTime;
        double pY = e.Y + Math.Cos(e.Direction * Math.PI / 180) * e.Speed * travelTime;

        TurnGunRight(NormalizeRelativeAngle(BearingTo(pX, pY) - GunDirection));
        Fire(fp);

        // ── STRAFE MOVEMENT ───────────────────────────
        // Bergerak tegak lurus dari musuh sambil menembak
        // Musuh di sudut sulit berbalik cepat, kita lebih lincah
        if (_count % 12 == 0) _strafeDir *= -1;
        double bodyB = NormalizeRelativeAngle(BearingTo(e.X, e.Y) - Direction);
        TurnRight(bodyB + (90 * _strafeDir));
        Forward(100);

        // Kejar musuh hampir mati
        if (e.Energy < 8 && dist < 250)
        {
            TurnRight(NormalizeRelativeAngle(BearingTo(e.X, e.Y) - Direction));
            Forward(dist);
        }

        AvoidWalls();
    }

    private void AvoidWalls()
    {
        double m = 80;
        if (X < m || X > ArenaWidth - m || Y < m || Y > ArenaHeight - m)
        {
            TurnRight(NormalizeRelativeAngle(
                BearingTo(ArenaWidth / 2.0, ArenaHeight / 2.0) - Direction));
            Forward(150);
        }
    }

    public override void OnHitByBullet(HitByBulletEvent e)
    {
        _strafeDir *= -1;
        TurnRight(90 - CalcBearing(e.Bullet.Direction));
        Forward(120);
    }

    public override void OnHitBot(HitBotEvent e)
    {
        TurnGunRight(NormalizeRelativeAngle(BearingTo(e.X, e.Y) - GunDirection));
        Fire(3.0);
        Back(80);
        TurnRight(90);
    }

    public override void OnHitWall(HitWallEvent e)
    {
        Back(80);
        TurnRight(135);
        Forward(100);
    }
}
