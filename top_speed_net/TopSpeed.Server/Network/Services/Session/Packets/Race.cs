using System.Net;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void RegisterRacePackets()
        {
            _race.RegisterPackets(_pktReg);
        }
    }
}
