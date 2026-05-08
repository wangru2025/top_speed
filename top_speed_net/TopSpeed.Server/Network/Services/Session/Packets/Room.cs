using System.Net;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void RegisterRoomPackets()
        {
            _room.RegisterPackets(_pktReg);
        }
    }
}
