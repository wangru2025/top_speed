using System;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private uint AllocateRoomId()
        {
            if (_rooms.Count == 0)
                return 1;

            uint candidate = 1;
            while (candidate != 0)
            {
                if (!_rooms.ContainsKey(candidate))
                    return candidate;

                candidate++;
            }

            throw new InvalidOperationException("No available room identifiers.");
        }
    }
}
