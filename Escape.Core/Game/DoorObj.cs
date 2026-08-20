using Microsoft.Xna.Framework;

namespace Escape.Core
{
    internal enum DoorKind { Normal, Locked, Exit }

    internal class DoorObj
    {
        public Rectangle Bounds;
        public DoorKind Kind;
        public string RequiredKeyColor;
        public bool IsOpen;

        public DoorObj(Rectangle bounds, DoorKind kind, string requiredKeyColor = null)
        {
            Bounds = bounds;
            Kind = kind;
            RequiredKeyColor = requiredKeyColor;
            IsOpen = kind == DoorKind.Normal;
        }
    }
}
