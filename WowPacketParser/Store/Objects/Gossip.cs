using System.Collections.Generic;

namespace WowPacketParser.Store.Objects
{
    public class GossipMenu
    {
        public uint MenuId;

        public uint NpcTextId;

        public List<GossipOption> GossipOptions;
    }
}
