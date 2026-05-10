namespace TopSpeed.Protocol
{
    public enum PacketStream : byte
    {
        Control = 0,
        Room = 1,
        RaceState = 2,
        RaceEvent = 3,
        Media = 4,
        Chat = 5,
        Direct = 6,
        Query = 7,
        Live = 8,
        Voice = 9
    }

    public enum PacketDeliveryKind : byte
    {
        Unreliable = 0,
        ReliableOrdered = 1,
        Sequenced = 2
    }

    public readonly struct PacketStreamSpec
    {
        public PacketStreamSpec(PacketStream stream, byte channel, PacketDeliveryKind delivery)
        {
            Stream = stream;
            Channel = channel;
            Delivery = delivery;
        }

        public PacketStream Stream { get; }
        public byte Channel { get; }
        public PacketDeliveryKind Delivery { get; }
    }

    public static class PacketStreams
    {
        public const int Count = 10;

        // Handshake, keepalive, assignment, disconnect.
        public static PacketStreamSpec Control => new PacketStreamSpec(PacketStream.Control, 0, PacketDeliveryKind.ReliableOrdered);
        // Room create/join/leave, room events and track changes.
        public static PacketStreamSpec Room => new PacketStreamSpec(PacketStream.Room, 1, PacketDeliveryKind.ReliableOrdered);
        // Continuous race snapshots.
        public static PacketStreamSpec RaceState => new PacketStreamSpec(PacketStream.RaceState, 2, PacketDeliveryKind.Unreliable);
        // Race lifecycle and critical race events.
        public static PacketStreamSpec RaceEvent => new PacketStreamSpec(PacketStream.RaceEvent, 3, PacketDeliveryKind.ReliableOrdered);
        // Legacy buffered media transfer (begin/chunk/end).
        public static PacketStreamSpec Media => new PacketStreamSpec(PacketStream.Media, 4, PacketDeliveryKind.ReliableOrdered);
        // Chat and room user text messages.
        public static PacketStreamSpec Chat => new PacketStreamSpec(PacketStream.Chat, 5, PacketDeliveryKind.ReliableOrdered);
        // Direct server protocol messages to one client.
        public static PacketStreamSpec Direct => new PacketStreamSpec(PacketStream.Direct, 6, PacketDeliveryKind.ReliableOrdered);
        // Query responses (room list/state/get).
        public static PacketStreamSpec Query => new PacketStreamSpec(PacketStream.Query, 7, PacketDeliveryKind.ReliableOrdered);
        // Real-time live audio frames (control uses delivery override).
        public static PacketStreamSpec Live => new PacketStreamSpec(PacketStream.Live, 8, PacketDeliveryKind.Sequenced);
        // Real-time communicator voice frames (control uses delivery override).
        public static PacketStreamSpec Voice => new PacketStreamSpec(PacketStream.Voice, 9, PacketDeliveryKind.Sequenced);

        public static PacketStreamSpec Get(PacketStream stream)
        {
            return stream switch
            {
                PacketStream.Control => Control,
                PacketStream.Room => Room,
                PacketStream.RaceState => RaceState,
                PacketStream.RaceEvent => RaceEvent,
                PacketStream.Media => Media,
                PacketStream.Chat => Chat,
                PacketStream.Direct => Direct,
                PacketStream.Query => Query,
                PacketStream.Live => Live,
                PacketStream.Voice => Voice,
                _ => Control
            };
        }
    }
}
