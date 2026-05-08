using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void RegisterLivePackets()
        {
            _live.RegisterPackets(_pktReg);
        }
    }
}
