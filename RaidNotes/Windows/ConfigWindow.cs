using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace RaidNotes.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration config;
    private readonly Plugin plugin;

    public ConfigWindow(Plugin plugin) : base($"{plugin.WindowSystem.Namespace} Config")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize;

        this.plugin = plugin;
        config = plugin.Configuration;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public override void Draw()
    {
        // Whether notes are enabled globally or not, this supercedes everything else
        var globalEnable = config.GlobalEnable;
        if (ImGui.Checkbox("Enable Notes", ref globalEnable))
        {
            config.GlobalEnable = globalEnable;
            config.Save();
            plugin.DisplayNoteWindow();

        }

        // Dynamic timers here 

        // Whether notes should be hidden outside combat
        var hiddenOutsideCombat = config.HiddenOutsideCombat;
        if (ImGui.Checkbox("Hide Notes Outside Combat", ref hiddenOutsideCombat))
        {
            config.HiddenOutsideCombat = hiddenOutsideCombat;
            config.Save();
            plugin.DisplayNoteWindow();
        }

        // Locks/Unlocks the note window allowing it to be repositioned/resized
        var noteWindowLocked = config.NoteWindowLocked;
        if (ImGui.Checkbox("Lock Note Window", ref noteWindowLocked))
        {
            config.NoteWindowLocked = noteWindowLocked;
            config.Save();
            plugin.DisplayNoteWindow();
        }
    }
}
