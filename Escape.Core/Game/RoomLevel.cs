using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Escape.Core
{
    internal class RoomLevel
    {
        public int Number;
        public string Name;
        public string Objective;
        public int TimeLimitSeconds;

        public Vector2 PlayerStart;
        public List<Rectangle> Walls = new List<Rectangle>();
        public List<ItemPickup> Items = new List<ItemPickup>();
        public List<DoorObj> Doors = new List<DoorObj>();
        public List<TrapObj> Traps = new List<TrapObj>();
        public List<EnemyCharacter> Enemies = new List<EnemyCharacter>();

        public int RequiredItemCount;
        public bool DarkEnvironment;

        public int WorldWidth;
        public int WorldHeight;

        public string ThemeLabel = "Dungeon";
        public string FloorTexturePath = "Tiles/floor_dungeon";
        public string WallTexturePath = "Tiles/wall_dungeon";
    }
}
