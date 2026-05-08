using System;
namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Session : ISessionService
        {
            private readonly RaceServer _owner;

            public Session(RaceServer owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }
        }
    }
}

