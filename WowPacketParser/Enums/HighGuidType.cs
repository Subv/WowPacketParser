namespace WowPacketParser.Enums
{
    public enum HighGuidType
    {
        Player          = 0x000, // Seen 0x280 for players too
        InstanceSave    = 0x104,
        Group           = 0x105,
        BattleGround    = 0x109,
        MOTransport     = 0x10C,
        Guild           = 0x10F,
        Item            = 0x400, // Container
        DynObject       = 0xF00, // Corpses
        GameObject      = 0xF01,
        Transport       = 0xF02,
        Unit            = 0xF03,
        Pet             = 0xF04,
        Vehicle         = 0xF05,
    }
}
