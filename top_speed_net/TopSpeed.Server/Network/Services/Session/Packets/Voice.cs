namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void RegisterVoicePackets()
        {
            _voice.RegisterPackets(_pktReg);
        }
    }
}
