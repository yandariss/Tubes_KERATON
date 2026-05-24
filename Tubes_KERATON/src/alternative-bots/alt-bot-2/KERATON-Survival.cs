// =============================================================================
// KERATON-Survival.cs — Alternatif 2 (UPGRADED)
// Strategi  : Survival Greedy
// Heuristik : score = enemyCount * selfEnergy
//
// Upgrade v2:
//  - Deteksi tembakan musuh dari perubahan energi
//  - Dodge otomatis saat mendeteksi incoming bullet
//  - Circle movement mengelilingi tengah arena
//  - Tetap tembak musuh lemah meski mode survival
//  - Anti-wall lebih canggih
// =============================================================================
using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class KERATONSurvival : Bot
{
    static void Main(string[] args) => new KERATONSurvival().Start();
    KERATONSurvival() : base(BotInfo.FromFile("KERATON-Survival.json")) { }

    private int _enemyCount = 5;
    private double _lastEnergy = 100;
    private int _circleDir = 1;
    private int _circleCount = 0;

    public override void Run()
    {
        BodyColor = Color.FromArgb(0x14, 0x5A, 0x32);
        TurretColor = Color.FromArgb(0x1E, 0x8B, 0x4E);
        RadarColor = Color.FromArgb(0xAD, 0xFF, 0x2F);
        BulletColor = Color.FromArgb(0x7F, 0xFF, 0x00);
        GunColor = Color.FromArgb(0x2E, 0x8B, 0x57);

        while (IsRunning)
        {
            // ══════════════════════════════════════════════
            // GREEDY: score = enemyCount * selfEnergy
            // Tinggi → banyak musuh hidup → kumpulkan
            // survival score dulu, jangan ambil risiko
            // ══════════════════════════════════════════════
            double score = _enemyCount * Energy;

            TurnRadarRight(360);

            if (score > 200)
            {
                // SURVIVAL MODE: circle movement di tengah arena
                CircleCenter();
            }
            else
            {
                // ATTACK MODE: musuh tinggal sedikit/energi rendah
                TurnRadarRight(360);
            }

            AvoidWalls();
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        double dist = DistanceTo(e.X, e.Y);
        double score = _enemyCount * Energy;

        // Deteksi: apakah musuh baru saja menembak?
        bool enemyFired = e.Energy < _lastEnergy - 0.09;
        _lastEnergy = e.Energy;

        if (enemyFired)
        {
            // Dodge: bergerak tegak lurus dari musuh
            _circleDir *= -1;
            TurnRight(90 * _circleDir);
            Forward(150);
        }

        // Kunci radar
        double radarB = NormalizeRelativeAngle(BearingTo(e.X, e.Y) - RadarDirection);
        TurnRadarRight(radarB * 2);

        // ── TEMBAK ────────────────────────────────────
        double fp;
        if (score > 200)
        {
            // Survival mode: tembak hemat, kecuali musuh hampir mati
            fp = e.Energy < 20 ? 3.0 : 0.5;
        }
        else
        {
            // Attack mode: tembak serius
            if (dist < 200) fp = 3.0;
            else if (dist < 400) fp = 2.0;
            else fp = 1.0;
            if (e.Energy < 15) fp = 3.0;
        }

        if (Energy - fp > 5)
        {
            // Prediksi posisi musuh
            double bulletSpeed = 20 - (3 * fp);
            double travelTime = dist / bulletSpeed;
            double pX = e.X + Math.Sin(e.Direction * Math.PI / 180) * e.Speed * travelTime;
            double pY = e.Y + Math.Cos(e.Direction * Math.PI / 180) * e.Speed * travelTime;

            TurnGunRight(NormalizeRelativeAngle(BearingTo(pX, pY) - GunDirection));
            Fire(fp);
        }
    }

    private void CircleCenter()
    {
        // Bergerak melingkar mengelilingi tengah arena
        // sangat sulit ditembak karena gerakannya kurva
        _circleCount++;
        double cx = ArenaWidth / 2.0, cy = ArenaHeight / 2.0;
        double distCenter = DistanceTo(cx, cy);

        if (distCenter > 250)
        {
            // Gerak menuju tengah
            TurnRight(NormalizeRelativeAngle(BearingTo(cx, cy) - Direction));
            Forward(distCenter - 200);
        }
        else
        {
            // Sudah di sekitar tengah: circle
            TurnRight(15 * _circleDir);
            Forward(80);
        }

        if (_circleCount % 30 == 0) _circleDir *= -1;
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

    public override void OnBotDeath(BotDeathEvent e)
    {
        _enemyCount = Math.Max(0, _enemyCount - 1);
    }

    public override void OnHitByBullet(HitByBulletEvent e)
    {
        _circleDir *= -1;
        TurnRight(90 - CalcBearing(e.Bullet.Direction));
        Forward(120);
    }

    public override void OnHitBot(HitBotEvent e)
    {
        TurnGunRight(NormalizeRelativeAngle(BearingTo(e.X, e.Y) - GunDirection));
        Fire(3.0);
        Back(80);
    }

    public override void OnHitWall(HitWallEvent e)
    {
        Back(80);
        TurnRight(90);
        Forward(100);
    }
}
