using System;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Notify : INotifyService
        {
            private readonly RaceServer _owner;

            public Notify(RaceServer owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }
        }
    }
}
