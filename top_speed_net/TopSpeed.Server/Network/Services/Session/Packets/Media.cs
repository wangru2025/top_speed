using System.Net;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void RegisterMediaPackets()
        {
            _media.RegisterPackets(_pktReg);
        }
    }
}
