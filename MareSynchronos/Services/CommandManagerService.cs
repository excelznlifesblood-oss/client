﻿using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using ShoninSync.FileCache;
using ShoninSync.MareConfiguration;
using ShoninSync.MareConfiguration.Models;
using ShoninSync.Services.Mediator;
using ShoninSync.Services.ServerConfiguration;
using ShoninSync.UI;
using ShoninSync.WebAPI;
using System.Globalization;

namespace ShoninSync.Services;

public sealed class CommandManagerService : IDisposable
{
    private const string _commandName = "/shoninsync";
    private const string _commandAlias = "/ssync";

    private readonly ApiController _apiController;
    private readonly ICommandManager _commandManager;
    private readonly MareMediator _mediator;
    private readonly MareConfigService _mareConfigService;
    private readonly PerformanceCollectorService _performanceCollectorService;
    private readonly CacheMonitor _cacheMonitor;
    private readonly ServerConfigurationManager _serverConfigurationManager;

    public CommandManagerService(ICommandManager commandManager, PerformanceCollectorService performanceCollectorService,
        ServerConfigurationManager serverConfigurationManager, CacheMonitor periodicFileScanner,
        ApiController apiController, MareMediator mediator, MareConfigService mareConfigService)
    {
        _commandManager = commandManager;
        _performanceCollectorService = performanceCollectorService;
        _serverConfigurationManager = serverConfigurationManager;
        _cacheMonitor = periodicFileScanner;
        _apiController = apiController;
        _mediator = mediator;
        _mareConfigService = mareConfigService;
        _commandManager.AddHandler(_commandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the Shonin Sync UI" + Environment.NewLine + Environment.NewLine +
                "Additionally possible commands:" + Environment.NewLine +
                "\t /shoninsync toggle - Disconnects from Shonin Sync, if connected. Connects to Shonin Sync, if disconnected" + Environment.NewLine +
                "\t /shoninsync toggle on|off - Connects or disconnects to Shonin Sync respectively" + Environment.NewLine +
                "\t /shoninsync gpose - Opens the Shonin Sync Character Data Hub window" + Environment.NewLine +
                "\t /shoninsync analyze - Opens the Shonin Sync Character Data Analysis window" + Environment.NewLine +
                "\t /shoninsync settings - Opens the Shonin Sync Settings window"
        });
        _commandManager.AddHandler(_commandAlias, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the Shonin Sync UI" + Environment.NewLine + Environment.NewLine +
                          "Additionally possible commands:" + Environment.NewLine +
                          "\t /ssync toggle - Disconnects from Shonin Sync, if connected. Connects to Shonin Sync, if disconnected" +
                          Environment.NewLine +
                          "\t /ssync toggle on|off - Connects or disconnects to Shonin Sync respectively" +
                          Environment.NewLine +
                          "\t /ssync gpose - Opens the Shonin Sync Character Data Hub window" +
                          Environment.NewLine +
                          "\t /ssync analyze - Opens the Shonin Sync Character Data Analysis window" +
                          Environment.NewLine +
                          "\t /ssync settings - Opens the Shonin Sync Settings window"
        });
    }

    public void Dispose()
    {
        _commandManager.RemoveHandler(_commandName);
    }

    private void OnCommand(string command, string args)
    {
        var splitArgs = args.ToLowerInvariant().Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries);

        if (splitArgs.Length == 0)
        {
            // Interpret this as toggling the UI
            if (_mareConfigService.Current.HasValidSetup())
                _mediator.Publish(new UiToggleMessage(typeof(CompactUi)));
            else
                _mediator.Publish(new UiToggleMessage(typeof(IntroUi)));
            return;
        }

        if (!_mareConfigService.Current.HasValidSetup())
            return;

        if (string.Equals(splitArgs[0], "toggle", StringComparison.OrdinalIgnoreCase))
        {
            if (_apiController.ServerState == WebAPI.SignalR.Utils.ServerState.Disconnecting)
            {
                _mediator.Publish(new NotificationMessage("Shonin Sync disconnecting", "Cannot use /toggle while Shonin Sync is still disconnecting",
                    NotificationType.Error));
            }

            if (_serverConfigurationManager.CurrentServer == null) return;
            var fullPause = splitArgs.Length > 1 ? splitArgs[1] switch
            {
                "on" => false,
                "off" => true,
                _ => !_serverConfigurationManager.CurrentServer.FullPause,
            } : !_serverConfigurationManager.CurrentServer.FullPause;

            if (fullPause != _serverConfigurationManager.CurrentServer.FullPause)
            {
                _serverConfigurationManager.CurrentServer.FullPause = fullPause;
                _serverConfigurationManager.Save();
                _ = _apiController.CreateConnectionsAsync();
            }
        }
        else if (string.Equals(splitArgs[0], "gpose", StringComparison.OrdinalIgnoreCase))
        {
            _mediator.Publish(new UiToggleMessage(typeof(CharaDataHubUi)));
        }
        else if (string.Equals(splitArgs[0], "rescan", StringComparison.OrdinalIgnoreCase))
        {
            _cacheMonitor.InvokeScan();
        }
        else if (string.Equals(splitArgs[0], "perf", StringComparison.OrdinalIgnoreCase))
        {
            if (splitArgs.Length > 1 && int.TryParse(splitArgs[1], CultureInfo.InvariantCulture, out var limitBySeconds))
            {
                _performanceCollectorService.PrintPerformanceStats(limitBySeconds);
            }
            else
            {
                _performanceCollectorService.PrintPerformanceStats();
            }
        }
        else if (string.Equals(splitArgs[0], "medi", StringComparison.OrdinalIgnoreCase))
        {
            _mediator.PrintSubscriberInfo();
        }
        else if (string.Equals(splitArgs[0], "analyze", StringComparison.OrdinalIgnoreCase))
        {
            _mediator.Publish(new UiToggleMessage(typeof(DataAnalysisUi)));
        }
        else if (string.Equals(splitArgs[0], "settings", StringComparison.OrdinalIgnoreCase))
        {
            _mediator.Publish(new UiToggleMessage(typeof(SettingsUi)));
        }
    }
}