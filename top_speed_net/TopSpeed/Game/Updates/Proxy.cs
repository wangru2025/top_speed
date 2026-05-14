namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void ApplyUpdateProxySettings()
        {
            _updateService.ConfigureProxy(_settings.UseUpdateProxy, _settings.UpdateProxyUrlPrefix);
        }
    }
}
