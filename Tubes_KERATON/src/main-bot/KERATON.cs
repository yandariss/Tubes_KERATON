// =============================================================================
// KERATON.cs — Main Bot (UPGRADED)
// Strategi  : Confidence Scoring Greedy
// Heuristik : score = enemyEnergy / distance
//
// Upgrade v2:
//  - Iterative linear targeting (prediksi lebih akurat)
//  - Adaptive firepower dengan energy management
//  - Perpendicular movement (strafe) menghindari peluru
//  - Radar lock pada target terpilih
//  - Eksekusi cepat musuh hampir mati
// =============================================================================
using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class KERATON : Bot
{
    static void Main(string[] args) => new KERATON().Start();
    KERATON() : base(BotInfo.FromFile("KERATON.json")) { }

    // Target saat ini
    private double _tX, _tY, _tDir, _tSpeed, _tEnergy;
    private bool _hasTarget = false;
    private int _moveDir = 1;
    private int _moveCount = 0;
    private double _lastEnergy = 100;

    public override void Run()
    {
        BodyColor = Color.FromArgb(0x1A, 0x53, 0x76);
        TurretColor = Color.FromArgb(0x2E, 0x86, 0xC1);
        RadarColor = Color.FromArgb(0x00, 0xFF, 0xFF);
        BulletColor = Color.FromArgb(0xFF, 0xD7, 0x00);
        GunColor = Color.FromArgb(0x0E, 0x6B, 0xB6);
        TracksColor = Color.FromArgb(0x0A, 0x29, 0x4D);

        while (IsRunning)
        {
            TurnRadarRight(360);
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        double dist = DistanceTo(e.X, e.Y);

        // ══════════════════════════════════════════════
        // GREEDY: score = enemyEnergy / distance
        // ══════════════════════════════════════════════
        double score = e.Energy / Math.Max(dist, 1);
        double bestScore = _hasTarget
            ? _tEnergy / Math.Max(DistanceTo(_tX, _tY), 1)
            : double.NegativeInfinity;

        if (score >= bestScore)
        {
            _tX = e.X; _tY = e.Y;
            _tDir = e.Direction; _tSpeed = e.Speed;
            _tEnergy = e.Energy;
            _hasTarget = true;
        }

        // ── FIREPOWER ADAPTIF ─────────────────────────
        double fp;
        if (dist < 100) fp = 3.0;
        else if (dist < 200) fp = 2.5;
        else if (dist < 350) fp = 2.0;
        else if (dist < 500) fp = 1.5;
        else if (dist < 700) fp = 1.0;
        else fp = 0.5;

        // Eksekusi: musuh hampir mati → tembak max
        if (e.Energy <= 16) fp = 3.0;
        // Hemat energi: kita lemah → tembak minimal
        if (Energy < 30 && dist > 300) fp = 0.5;
        // Jangan bunuh diri
        if (Energy - fp < 5) fp = 0.1;

        // ── ITERATIVE LINEAR TARGETING ────────────────
        // Iterasi prediksi posisi musuh untuk akurasi tinggi
        double bulletSpeed = 20 - (3 * fp);
        double pX = e.X, pY = e.Y;
        for (int i = 0; i < 10; i++)
        {
            double travelTime = DistanceTo(pX, pY) / bulletSpeed;
            pX = e.X + Math.Sin(e.Direction * Math.PI / 180) * e.Speed * travelTime;
            pY = e.Y + Math.Cos(e.Direction * Math.PI / 180) * e.Speed * travelTime;
        }

        // Kunci radar ke target
        double radarBearing = NormalizeRelativeAngle(BearingTo(e.X, e.Y) - RadarDirection);
        TurnRadarRight(radarBearing * 2);

        // Arahkan gun ke prediksi posisi
        double gunBearing = NormalizeRelativeAngle(BearingTo(pX, pY) - GunDirection);
        TurnGunRight(gunBearing);
        Fire(fp);

        // ── PERPENDICULAR MOVEMENT (STRAFE) ───────────
        // Bergerak tegak lurus dari arah musuh
        // membuat kita sangat sulit ditembak
        _moveCount++;
        if (_moveCount % 15 == 0) _moveDir *= -1;

        double bodyBearing = NormalizeRelativeAngle(BearingTo(e.X, e.Y) - Direction);
        TurnRight(bodyBearing + (90 * _moveDir));
        Forward(100 * _moveDir);

        // Kejar musuh hampir mati untuk ram bonus
        if (e.Energy < 10 && dist < 300)
        {
            TurnRight(NormalizeRelativeAngle(BearingTo(e.X, e.Y) - Direction));
            Forward(dist);
        }

        // ── HINDARI DINDING ───────────────────────────
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
        // Musuh menembak: deteksi dari perubahan energi
        // Dodge agresif: belok 90° dari arah datang peluru
        _moveDir *= -1;
        TurnRight(90 - CalcBearing(e.Bullet.Direction));
        Forward(150);
    }

    public override void OnHitBot(HitBotEvent e)
    {
        // Nabrak musuh: tembak langsung daya penuh
        TurnGunRight(NormalizeRelativeAngle(BearingTo(e.X, e.Y) - GunDirection));
        Fire(3.0);
        // Mundur lalu strafe
        Back(80);
        TurnRight(90);
        Forward(100);
    }

    public override void OnHitWall(HitWallEvent e)
    {
        Back(100);
        TurnRight(135);
        Forward(100);
    }

    public override void OnBotDeath(BotDeathEvent e)
    {
        _hasTarget = false;
    }
}