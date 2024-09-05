namespace XREngine.Physics
{
    [Flags]
    public enum ECollisionGroup : ushort
    {
        All             = 0xFFFF,
        None            = 0x0000,
        Default         = 0x0001,
        Pawns           = 0x0002,
        Characters      = 0x0004,
        Vehicles        = 0x0008,
        StaticWorld     = 0x0010,
        DynamicWorld    = 0x0020,
        PhysicsObjects  = 0x0040,
        Interactables   = 0x0080,
        Projectiles     = 0x0100,
        Camera          = 0x0200,
        Volumes         = 0x0400,
        Foliage         = 0x0800,
        Aux1            = 0x1000,
        Aux2            = 0x2000,
        Aux3            = 0x4000,
        Aux4            = 0x8000,
    }
}
