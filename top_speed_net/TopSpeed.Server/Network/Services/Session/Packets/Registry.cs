using System;
using System.Collections.Generic;
using System.Net;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed class ServerPktReg
    {
        private readonly Dictionary<Command, Entry> _map = new Dictionary<Command, Entry>();

        internal delegate void H(PlayerConnection player, byte[] payload, IPEndPoint endPoint);

        private readonly struct Entry
        {
            public Entry(string module, H handler)
            {
                Module = module;
                Handler = handler;
            }

            public string Module { get; }
            public H Handler { get; }
        }

        public void Add(string module, Command command, H handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (_map.ContainsKey(command))
                throw new InvalidOperationException($"Server packet handler already registered for {command}.");

            _map[command] = new Entry(module ?? string.Empty, handler);
        }

        public bool TryDispatch(Command command, PlayerConnection player, byte[] payload, IPEndPoint endPoint)
        {
            if (!_map.TryGetValue(command, out var entry))
                return false;

            entry.Handler(player, payload, endPoint);
            return true;
        }
    }
}
