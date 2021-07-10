using Mirror;

namespace QuarksWorld
{
    public struct ConsoleMessage : NetworkMessage
    {
        public string text;
    }

    public struct SnapshotMessage : NetworkMessage
    {
        public int tick;
        public EntitySnapshot[] entities;
    }

}
