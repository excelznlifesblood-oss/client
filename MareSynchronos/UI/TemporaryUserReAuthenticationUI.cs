using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Microsoft.Extensions.Logging;
using ShoninSync.MareConfiguration.Models;
using ShoninSync.Services;
using ShoninSync.Services.Mediator;
using ShoninSync.Services.ServerConfiguration;
using ShoninSync.WebAPI;
using System.Numerics;

namespace ShoninSync.UI
{
    public class TemporaryUserReAuthenticationUI: WindowMediatorSubscriberBase
    {
        private readonly DalamudUtilService _dalamudUtilService;
        private readonly ServerConfigurationManager _serverManager;
        private readonly UiSharedService _uiSharedService;
        private readonly ApiController _apiController;
        private Dictionary<string, string> _discordOauthUids;

        public TemporaryUserReAuthenticationUI(
            DalamudUtilService dalamudUtilService,
            ServerConfigurationManager serverManager,
            UiSharedService uiSharedService,
            ILogger<TemporaryUserReAuthenticationUI> logger,
            MareMediator mediator,
            PerformanceCollectorService performanceCollectorService,
            ApiController apiController
        )
            : base(logger, mediator, "Shonin Sync Guest Reauthentication", performanceCollectorService)
        {
            _dalamudUtilService = dalamudUtilService;
            _serverManager = serverManager;
            _uiSharedService = uiSharedService;
            _apiController = apiController;
            SizeConstraints = new WindowSizeConstraints()
            {
                MinimumSize = new Vector2(800, 400),
                MaximumSize = new Vector2(800, 2000),
            };
        }

        protected override void DrawInternal()
        {
            DrawContent();
        }

        private void DrawContent()
        {
            _uiSharedService.BigText("Guest Access Renewal");

            ImGui.Separator();
            var selectedServer = _serverManager.CurrentServer;
            selectedServer.UseOAuth2 = false;
            selectedServer.OAuthToken = null;
            var keyIdx = selectedServer.SecretKeys.Any() ? selectedServer.SecretKeys.Max(p => p.Key) : 0;
            foreach (var selectedServerAuthentication in selectedServer.Authentications)
            {
                selectedServerAuthentication.SecretKeyIdx = keyIdx;
            }
            _serverManager.Save();
            var item = selectedServer.SecretKeys.First();
            using var id = ImRaii.PushId("key" + item.Key);
            var friendlyName = item.Value.FriendlyName;
            if (ImGui.InputText("Secret Key Display Name", ref friendlyName, 255))
            {
                item.Value.FriendlyName = friendlyName;
                _serverManager.Save();
            }
            var key = item.Value.Key;
            if (ImGui.InputText("Secret Key", ref key, 64))
            {
                item.Value.Key = key;
                _serverManager.Save();
            }

            if (_uiSharedService.IconTextButton(FontAwesomeIcon.Save, "Save"))
            {
                foreach (var selectedServerAuthentication in selectedServer.Authentications)
                {
                    selectedServerAuthentication.SecretKeyIdx = keyIdx;
                }
                _serverManager.CurrentServer.FullPause = false;
                _serverManager.Save();
                _ = _apiController.CreateConnectionsAsync();
            }
            
            ImGui.Separator();
        }
    }
}