using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Lumina.Excel.GeneratedSheets;
using RaidNotes.Models;
using RaidNotes.Windows;
using Dalamud.Game.ClientState.Conditions;

using System.Collections.Generic;
using Dalamud.Utility;
using System;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System.Runtime.CompilerServices;

namespace RaidNotes;

public sealed class Plugin : IDalamudPlugin
{
    // Dalamud services
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static ICondition Condition { get; private set; } = null!;



    // Main UI Slash Command
    private const string CommandName = "/notes";

    // Plugin config
    public Configuration Configuration { get; init; }

    // Windows and UI
    public readonly WindowSystem WindowSystem = new("RaidNotes");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    private NoteWindow NoteWindow { get; init; }

    // Zone cache
    public static List<Zone> Zones { get; set; } = [];

    // Combat Time - No idea if there's a better way of doing this
    public DateTime? CombatStartTime { get; set; }


    public Plugin()
    {
        // Load config
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        //Configuration = new Configuration();

        // Populate zone cache
        Zones = GetZones();

        // Create windows
        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);
        NoteWindow = new NoteWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(NoteWindow);

        // Add command handler
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggles the RaidNotes UI."
        });

        // Event handlers
        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        ClientState.TerritoryChanged += OnTerritoryChanged;
        ClientState.Logout += OnLogout;
        ClientState.Login += OnLogin;
        Condition.ConditionChange += OnConditionChange;

        Ready();
    }

    private void OnConditionChange(ConditionFlag flag, bool value)
    {
        // TODO: Dynamic timers/hidden note lines based on combat time elapsed
        if (flag != ConditionFlag.InCombat) return;
        if (value)
        {
            CombatStartTime = DateTime.Now;
        }
        else
        {
            CombatStartTime = null;
        }
        DisplayNoteWindow();
        Log.Debug($"InCombat: {value}");
    }

    private void Ready()
    {
        //// Auto-open main ui on load, this is only for testing purposes
        //ToggleMainUI();
        //ToggleConfigUI();
        SortZones();
        PushNote();
        Log.Debug("Ready!");
    }

    private void OnLogin()
    {
        CombatStartTime = null;
        SortZones(ClientState.TerritoryType);
        PushNote();
        
    }

    private void OnLogout()
    {
        CombatStartTime = null;
        SortZones(0);
        DisplayNoteWindow();
    }

    private void OnTerritoryChanged(ushort zoneId)
    {
        CombatStartTime = null;
        SortZones(zoneId);
        PushNote(zoneId);
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
        NoteWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        Log.Debug("Slash command received.");
        ToggleMainUI(); // Show or hide the main window

    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();

    public void ToggleMainUI() => MainWindow.Toggle();

    public void DisplayNoteWindow()
    {
        // We should hide it if the user is logged out or notes are disabled
        if (!Configuration.GlobalEnable || !ClientState.IsLoggedIn)
        {
            NoteWindow.IsOpen = false;
            Log.Debug("Note window hidden");
            return;
        }

        // If the window isn't locked, always show it. Otherwise, only show it if the zone has notes enabled & other condtions are met.
        if (Configuration.NoteWindowLocked)
        {
            if (Configuration.Notes.TryGetValue(ClientState.TerritoryType, out var zoneSettings) && zoneSettings.Enabled)
            {
                if (Configuration.HiddenOutsideCombat && !Condition[ConditionFlag.InCombat])
                {
                    NoteWindow.IsOpen = false;
                    Log.Debug("Note window hidden");
                    return;
                }
                NoteWindow.IsOpen = true;
                Log.Debug("Note window shown");
                return;
            }
            NoteWindow.IsOpen = false;
            Log.Debug("Note window hidden");
        }
        else
        {
            NoteWindow.IsOpen = true;
            Log.Debug("Note window shown");
        }
    }
    public static List<Zone> GetZones()
    {
        Log.Debug("Updating zone cache.");
        // Load the IDs, Names, and ContentFinderCondition names for all zones
        var zones = new List<Zone>();
        foreach (var zone in DataManager.GetExcelSheet<TerritoryType>()!)
        {
            var id = zone.RowId;
            var name = zone.PlaceName.Value?.Name.ToString() ?? string.Empty;
            if (name == string.Empty) continue; // if the name of the zone is empty we can safely skip it
            var contentFinderCondition = zone.ContentFinderCondition.Value?.Name.ToString() ?? string.Empty;
            var fullName = (contentFinderCondition == string.Empty) ? name : $"{name} ({contentFinderCondition})";

            zones.Add(new Zone(id, fullName));
        }

        return zones;
    }

    public void SortZones(ushort zoneId)
    {
        // Sort zones by provided zoneId, enabled zones, then by Id
        Zones.Sort((x, y) => {
            var xEnabled = Configuration.Notes.TryGetValue(x.Id, out var xSettings) && xSettings.Enabled;
            var yEnabled = Configuration.Notes.TryGetValue(y.Id, out var ySettings) && ySettings.Enabled;

            // matching zoneId should always take highest priority
            if (x.Id == zoneId && y.Id != zoneId) return -1;
            if (x.Id != zoneId && y.Id == zoneId) return 1;

            // then enabled zones
            if (xEnabled && !yEnabled) return -1;
            if (!xEnabled && yEnabled) return 1;

            // then by Id
            return x.Id.CompareTo(y.Id);
        });
    }

    public void SortZones()
    {
        SortZones(ClientState.TerritoryType);
    }

    public void PushNote(ushort zoneId)
    {
        // Update visibility of the note window whenever we alter it's contents
        DisplayNoteWindow();

        if (!Configuration.GlobalEnable) return;

        if (Configuration.Notes.TryGetValue(zoneId, out var zoneSettings))
        {
            if (zoneSettings.Enabled)
            {
                NoteWindow.NoteContent = zoneSettings.Notes[zoneSettings.SelectedNoteIndex].Body;
                Log.Debug($"Pushed note for zone {zoneId}");
                return;
            }
        }
        
        NoteWindow.NoteContent = string.Empty;
        Log.Debug($"Cleared note window");
    }

    public void PushNote()
    {
        PushNote(ClientState.TerritoryType);
    }



}
