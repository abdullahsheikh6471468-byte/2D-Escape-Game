using Microsoft.Xna.Framework;

namespace Escape.Core
{
    internal enum ItemKind { Key, HealthPickup }

    internal class ItemPickup
    {
        public Vector2 Position;
        public float Size = 28f;
        public string ColorName; // "Red"/"Blue"/"Green"/"Yellow" for keys
        public ItemKind Kind;
        public bool Collected = false;
        public int HealAmount = 25;

        public ItemPickup(Vector2 position, string colorName, ItemKind kind = ItemKind.Key)
        {
            Position = position;
            ColorName = colorName;
            Kind = kind;
        }

        public Rectangle Bounds => new Rectangle(
            (int)(Position.X - Size / 2f), (int)(Position.Y - Size / 2f), (int)Size, (int)Size);

        /// <summary>
        /// Content path (without extension) of the sprite representing this item.
        /// </summary>
        public string SpritePath => Kind == ItemKind.HealthPickup
            ? "Items/potion"
            : $"Items/key_{ColorName.ToLowerInvariant()}";
    }
}
