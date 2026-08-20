using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Escape.Core
{
    /// <summary>
    /// Builds all 10 levels. Each level is a horizontal chain of rooms connected
    /// by doorways (normal or locked). Every key is placed in a room that comes
    /// before the locked door that needs it, so every generated level is
    /// guaranteed solvable by exploring room-by-room toward the exit.
    /// </summary>
    internal static class LevelBuilder
    {
        private static readonly string[] KeyColors = { "Red", "Blue", "Green", "Yellow" };

        public static RoomLevel CreateLevel(int levelNumber)
        {
            var cfg = GetConfig(levelNumber);
            var rnd = new Random(levelNumber * 7919 + 13); // deterministic per level

            var level = new RoomLevel
            {
                Number = levelNumber,
                Name = cfg.Name,
                Objective = cfg.Objective,
                TimeLimitSeconds = cfg.TimeLimitSeconds,
                RequiredItemCount = cfg.KeysNeeded,
                DarkEnvironment = cfg.DarkEnvironment,
                ThemeLabel = cfg.ThemeLabel,
                FloorTexturePath = cfg.FloorTexturePath,
                WallTexturePath = cfg.WallTexturePath
            };

            int roomCount = cfg.RoomCount;
            int roomWidth = 640;
            int roomHeight = 460;
            int marginSide = 40;
            int marginTop = 90;
            int marginBottom = 40;
            int doorGapHeight = 100;
            int wallThickness = 24;

            level.WorldWidth = roomWidth * roomCount + marginSide * 2;
            level.WorldHeight = roomHeight + marginTop + marginBottom;

            var rooms = new List<Rectangle>();
            for (int i = 0; i < roomCount; i++)
                rooms.Add(new Rectangle(marginSide + i * roomWidth, marginTop, roomWidth, roomHeight));

            level.Walls.Add(new Rectangle(marginSide, marginTop - wallThickness, roomWidth * roomCount, wallThickness));
            level.Walls.Add(new Rectangle(marginSide, marginTop + roomHeight, roomWidth * roomCount, wallThickness));
            level.Walls.Add(new Rectangle(marginSide - wallThickness, marginTop - wallThickness, wallThickness, roomHeight + wallThickness * 2));

            int boundaryCount = roomCount - 1;
            var lockedBoundaries = new HashSet<int>();
            if (boundaryCount > 0 && cfg.KeysNeeded > 0)
            {
                float step = (float)boundaryCount / cfg.KeysNeeded;
                for (int k = 0; k < cfg.KeysNeeded; k++)
                {
                    int idx = Math.Min(boundaryCount - 1, (int)Math.Round(step * k + step / 2f));
                    lockedBoundaries.Add(idx);
                }
            }

            int keyIndex = 0;
            for (int b = 0; b < boundaryCount; b++)
            {
                int boundaryX = marginSide + (b + 1) * roomWidth;
                int doorY = marginTop + (roomHeight - doorGapHeight) / 2;

                level.Walls.Add(new Rectangle(boundaryX - wallThickness / 2, marginTop - wallThickness,
                    wallThickness, (doorY - marginTop) + wallThickness));
                int belowY = doorY + doorGapHeight;
                level.Walls.Add(new Rectangle(boundaryX - wallThickness / 2, belowY,
                    wallThickness, (marginTop + roomHeight) - belowY + wallThickness));

                var gapBounds = new Rectangle(boundaryX - wallThickness / 2, doorY, wallThickness, doorGapHeight);

                if (lockedBoundaries.Contains(b))
                {
                    string color = KeyColors[keyIndex % KeyColors.Length];
                    keyIndex++;
                    level.Doors.Add(new DoorObj(gapBounds, DoorKind.Locked, color));

                    var keyRoom = rooms[b];
                    var keyPos = RandomPointInRoom(rnd, keyRoom, 70);
                    level.Items.Add(new ItemPickup(keyPos, color, ItemKind.Key));
                }
                else
                {
                    level.Doors.Add(new DoorObj(gapBounds, DoorKind.Normal));
                }
            }

            int exitX = marginSide + roomCount * roomWidth;
            int exitDoorY = marginTop + (roomHeight - doorGapHeight) / 2;
            level.Walls.Add(new Rectangle(exitX - wallThickness / 2, marginTop - wallThickness,
                wallThickness, (exitDoorY - marginTop) + wallThickness));
            int exitBelowY = exitDoorY + doorGapHeight;
            level.Walls.Add(new Rectangle(exitX - wallThickness / 2, exitBelowY,
                wallThickness, (marginTop + roomHeight) - exitBelowY + wallThickness));
            var exitGap = new Rectangle(exitX - wallThickness / 2, exitDoorY, wallThickness, doorGapHeight);
            level.Doors.Add(new DoorObj(exitGap, DoorKind.Exit));

            level.PlayerStart = new Vector2(rooms[0].X + 80, rooms[0].Y + rooms[0].Height / 2f);

            int trapsPlaced = 0;
            for (int i = 1; i < roomCount && trapsPlaced < cfg.TrapCount; i++)
            {
                int trapsThisRoom = (int)Math.Ceiling((double)(cfg.TrapCount - trapsPlaced) / (roomCount - i));
                for (int t = 0; t < trapsThisRoom && trapsPlaced < cfg.TrapCount; t++)
                {
                    var pos = RandomPointInRoom(rnd, rooms[i], 100);
                    var kind = PickTrapKind(rnd, levelNumber);
                    level.Traps.Add(BuildTrap(kind, pos, cfg.TrapDamage, rnd));
                    trapsPlaced++;
                }
            }

            int enemiesPlaced = 0;
            for (int i = 1; i < roomCount && enemiesPlaced < cfg.EnemyCount; i++)
            {
                var room = rooms[i];
                var p1 = RandomPointInRoom(rnd, room, 90);
                var p2 = RandomPointInRoom(rnd, room, 90);
                var patrol = new List<Vector2> { p1, p2 };
                var kind = enemiesPlaced % 2 == 0 ? EnemyKind.GoblinGreen : EnemyKind.GoblinRed;
                level.Enemies.Add(new EnemyCharacter(kind, p1, patrol,
                    cfg.EnemyPatrolSpeed, cfg.EnemyChaseSpeed, cfg.EnemyDetectionRange, cfg.EnemyDamage));
                enemiesPlaced++;
            }

            int healthPickups = levelNumber >= 3 ? 2 : 1;
            for (int i = 0; i < healthPickups; i++)
            {
                int roomIdx = Math.Min(roomCount - 1, roomCount / 2 + i);
                var pos = RandomPointInRoom(rnd, rooms[roomIdx], 80);
                level.Items.Add(new ItemPickup(pos, "Potion", ItemKind.HealthPickup));
            }

            return level;
        }

        private static Vector2 RandomPointInRoom(Random rnd, Rectangle room, int margin)
        {
            int usableW = Math.Max(1, room.Width - margin * 2);
            int usableH = Math.Max(1, room.Height - margin * 2);
            float x = room.X + margin + (float)rnd.NextDouble() * usableW;
            float y = room.Y + margin + (float)rnd.NextDouble() * usableH;
            return new Vector2(x, y);
        }

        private static TrapKind PickTrapKind(Random rnd, int levelNumber)
        {
            var options = new List<TrapKind> { TrapKind.Spike };
            if (levelNumber >= 3) options.Add(TrapKind.MovingBlock);
            if (levelNumber >= 5) options.Add(TrapKind.Laser);
            if (levelNumber >= 7) options.Add(TrapKind.Fire);
            return options[rnd.Next(options.Count)];
        }

        private static TrapObj BuildTrap(TrapKind kind, Vector2 pos, int damage, Random rnd)
        {
            switch (kind)
            {
                case TrapKind.MovingBlock:
                    bool horizontal = rnd.Next(2) == 0;
                    var mbBounds = new Rectangle((int)pos.X - 22, (int)pos.Y - 22, 44, 44);
                    return new TrapObj(TrapKind.MovingBlock, mbBounds, damage)
                    {
                        MovesHorizontally = horizontal,
                        TravelDistance = 80 + rnd.Next(60),
                        MoveSpeed = 45f + (float)rnd.NextDouble() * 25f
                    };
                case TrapKind.Laser:
                    var laserBounds = new Rectangle((int)pos.X - 12, (int)pos.Y - 70, 24, 140);
                    return new TrapObj(TrapKind.Laser, laserBounds, damage)
                    {
                        CycleLengthMs = 2400,
                        ActiveLengthMs = 800
                    };
                case TrapKind.Fire:
                    var fireBounds = new Rectangle((int)pos.X - 32, (int)pos.Y - 32, 64, 64);
                    return new TrapObj(TrapKind.Fire, fireBounds, damage);
                default:
                    var spikeBounds = new Rectangle((int)pos.X - 22, (int)pos.Y - 22, 44, 44);
                    return new TrapObj(TrapKind.Spike, spikeBounds, damage);
            }
        }

        private class LevelConfig
        {
            public string Name;
            public string Objective;
            public int TimeLimitSeconds;
            public int RoomCount;
            public int KeysNeeded;
            public int TrapCount;
            public int TrapDamage;
            public int EnemyCount;
            public float EnemyPatrolSpeed;
            public float EnemyChaseSpeed;
            public float EnemyDetectionRange;
            public int EnemyDamage;
            public string ThemeLabel;
            public string FloorTexturePath;
            public string WallTexturePath;
            public bool DarkEnvironment;
        }

        private static LevelConfig GetConfig(int n)
        {
            var cfg = new LevelConfig
            {
                RoomCount = Math.Min(3 + n / 2, 8),
                KeysNeeded = Math.Min(1 + n / 3, 4),
                TrapCount = Math.Max(0, n - 2),
                TrapDamage = 10 + n,
                EnemyCount = Math.Max(0, n - 1),
                EnemyPatrolSpeed = 45f + n * 3f,
                EnemyChaseSpeed = 85f + n * 6f,
                EnemyDetectionRange = 110f + n * 8f,
                EnemyDamage = 8 + n,
                TimeLimitSeconds = Math.Max(90, 200 - n * 10),
                DarkEnvironment = false,
                FloorTexturePath = "Tiles/floor_dungeon",
                WallTexturePath = "Tiles/wall_dungeon"
            };

            switch (n)
            {
                case 1:
                    cfg.Name = "Tutorial Escape"; cfg.Objective = "Find the key and escape.";
                    cfg.ThemeLabel = "House"; cfg.EnemyCount = 0; cfg.TrapCount = 0;
                    break;
                case 2:
                    cfg.Name = "Locked Rooms"; cfg.Objective = "Find 2 keys and unlock the exit.";
                    cfg.ThemeLabel = "House";
                    break;
                case 3:
                    cfg.Name = "Trap House"; cfg.Objective = "Avoid the traps, gather the keys, escape.";
                    cfg.ThemeLabel = "Trap House";
                    break;
                case 4:
                    cfg.Name = "Enemy Facility"; cfg.Objective = "Find the access card and reach the exit.";
                    cfg.ThemeLabel = "Facility";
                    cfg.FloorTexturePath = "Tiles/floor_facility"; cfg.WallTexturePath = "Tiles/wall_facility";
                    break;
                case 5:
                    cfg.Name = "Underground Escape"; cfg.Objective = "Navigate the tunnels and find every key.";
                    cfg.ThemeLabel = "Underground";
                    break;
                case 6:
                    cfg.Name = "Security Base"; cfg.Objective = "Disable the security system and escape.";
                    cfg.ThemeLabel = "Security Base";
                    cfg.FloorTexturePath = "Tiles/floor_facility"; cfg.WallTexturePath = "Tiles/wall_facility";
                    break;
                case 7:
                    cfg.Name = "Dark Escape"; cfg.Objective = "Feel your way through the dark and escape.";
                    cfg.ThemeLabel = "Dark Zone"; cfg.DarkEnvironment = true;
                    break;
                case 8:
                    cfg.Name = "Prison Escape"; cfg.Objective = "Slip past the guards and break out.";
                    cfg.ThemeLabel = "Prison";
                    break;
                case 9:
                    cfg.Name = "Final Facility"; cfg.Objective = "Clear every objective and reach the exit.";
                    cfg.ThemeLabel = "Final Facility";
                    cfg.FloorTexturePath = "Tiles/floor_facility"; cfg.WallTexturePath = "Tiles/wall_facility";
                    break;
                case 10:
                    cfg.Name = "Ultimate Escape"; cfg.Objective = "Survive the maze and reach the final exit.";
                    cfg.ThemeLabel = "The Last Exit"; cfg.RoomCount = 8; cfg.KeysNeeded = 4;
                    cfg.FloorTexturePath = "Tiles/floor_facility"; cfg.WallTexturePath = "Tiles/wall_facility";
                    break;
                default:
                    cfg.Name = $"Level {n}"; cfg.Objective = "Escape."; cfg.ThemeLabel = "Facility";
                    break;
            }

            return cfg;
        }
    }
}
