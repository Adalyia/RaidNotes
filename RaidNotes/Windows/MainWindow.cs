using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using RaidNotes.Models;

namespace RaidNotes.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly Configuration config;
    private static string Filter = string.Empty;
    private static uint SelectedZoneId = 0;

    public MainWindow(Plugin plugin)
        : base($"{plugin.WindowSystem.Namespace}", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoResize)
    {
        Size = new Vector2(750, 600);

        this.plugin = plugin;
        config = this.plugin.Configuration;
    }

    public void Dispose() 
    {
        GC.SuppressFinalize(this);
    }

    public override void Draw()
    {
        // Setup our two columns, the first being the zone list, the second being the note editor
        ImGui.Columns(2, "##window_columns", false);
        ImGui.SetColumnWidth(0, 275);
        ImGui.SetColumnWidth(1, 475);

        // Zone list
        var zoneSelected = Plugin.Zones.Exists(x => x.Id == SelectedZoneId) && config.Notes.ContainsKey(SelectedZoneId);
        if (ImGui.BeginCombo("##zone_list", !zoneSelected ? "Select a zone" : Plugin.Zones.Find(x => x.Id == SelectedZoneId)?.ToString() ?? "Select a zone"))
        {
            ImGui.InputTextWithHint("##zone_select_filters", "Filter", ref Filter, 50); // Filter for the zone list

            foreach (var zone in Plugin.Zones)
            {
                if (Filter != string.Empty && !zone.ToString().Contains(Filter, StringComparison.OrdinalIgnoreCase)) continue; // Skip if the filter is set and the zone doesn't match

                // Apply color to the zone name if notes are enabled for it
                var zoneNotesEnabled = config.Notes.TryGetValue(zone.Id, out var zoneSettings) && zoneSettings.Enabled;
                if (zoneNotesEnabled) ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.0f, 1.0f, 0.0f, 1.0f));

                if (ImGui.Selectable(zone.ToString(), zoneSelected && zone.Id == SelectedZoneId))
                {
                    if (!config.Notes.ContainsKey(zone.Id))
                    {
                        // If the zone doesn't have notes, add it to the configuration
                        config.Notes.Add(zone.Id, new ZoneSettings(false));
                        config.Save();
                        Plugin.Log.Debug($"Added zone {zone} to configuration.");
                    }


                    SelectedZoneId = zone.Id;
                    Filter = string.Empty; // Once we select a zone clear the filter
                }

                if (zoneNotesEnabled) ImGui.PopStyleColor(); // End color change
            }
            

            ImGui.EndCombo(); // End zone list combo
        }

        ImGui.SameLine();
        
        if (ImGui.Button("Config"))
        {
            plugin.ToggleConfigUI();
        }

        // Only show the note list and note settings if a zone is selected
        if (zoneSelected) {

            // Note list
            if (ImGui.BeginCombo("##zone_note_list", $"{config.Notes[SelectedZoneId].SelectedNoteIndex} - {config.Notes[SelectedZoneId].Notes[config.Notes[SelectedZoneId].SelectedNoteIndex].Title}"))
            {
                for (var i = 0; i < config.Notes[SelectedZoneId].Notes.Count; i++)
                {
                    if (ImGui.Selectable($"{i} - {config.Notes[SelectedZoneId].Notes[i].Title}", config.Notes[SelectedZoneId].SelectedNoteIndex == i))
                    {
                        config.Notes[SelectedZoneId].SelectedNoteIndex = i;
                        config.Save();
                        plugin.PushNote();
                    }
                }

                ImGui.EndCombo(); // End note list combo
            }

            ImGui.SameLine();

            // Create a new note for the current zone
            if (ImGui.Button("New Note"))
            {
                config.Notes[SelectedZoneId].addNote(new Note());
                config.Notes[SelectedZoneId].SelectedNoteIndex = config.Notes[SelectedZoneId].Notes.Count - 1;
                config.Save();
                plugin.PushNote();
            }


            // Whether notes for the current zone are enabled 
            var currentZoneEnabled = config.Notes[SelectedZoneId].Enabled;
            if (ImGui.Checkbox("Notes Enabled for this Zone", ref currentZoneEnabled))
            {
                config.Notes[SelectedZoneId].Enabled = currentZoneEnabled;
                config.Save();
                plugin.SortZones();
                plugin.PushNote();
            }

        }
        
        ImGui.NextColumn(); // Col 2


        if (!zoneSelected) ImGui.BeginDisabled(); // Disable note editing if no zone is selected

        // Note editor title
        var noteTitle = zoneSelected ? config.Notes[SelectedZoneId].Notes[config.Notes[SelectedZoneId].SelectedNoteIndex].Title : string.Empty;
        ImGui.SetNextItemWidth(475);
        if (ImGui.InputText("##current_note_title", ref noteTitle, 255))
        {
            config.Notes[SelectedZoneId].Notes[config.Notes[SelectedZoneId].SelectedNoteIndex].Title = noteTitle;
            config.Save();
        }

        // Note editor body
        ImGui.BeginChildFrame(1, new Vector2(475, 500));

        var noteContent = zoneSelected ? config.Notes[SelectedZoneId].Notes[config.Notes[SelectedZoneId].SelectedNoteIndex].Body : "Select a zone to see notes";
        if (ImGui.InputTextMultiline("##current_note_body", ref noteContent, 65536, new Vector2(500, 3200), ImGuiInputTextFlags.AllowTabInput))
        {
            config.Notes[SelectedZoneId].Notes[config.Notes[SelectedZoneId].SelectedNoteIndex].Body = noteContent;
            config.Save();
            plugin.PushNote();
        }
        
        ImGui.EndChildFrame();

        // Delete note button, the user must hold shift before clicking to delete a note
        switch (ImGui.GetIO().KeyShift)
        {
            case true:

                if (ImGui.Button("Delete Note"))
                {
                    config.Notes[SelectedZoneId].removeNoteAt(config.Notes[SelectedZoneId].SelectedNoteIndex);
                    config.Notes[SelectedZoneId].SelectedNoteIndex--;

                    if (config.Notes[SelectedZoneId].SelectedNoteIndex < 0)
                    {
                        config.Notes[SelectedZoneId].SelectedNoteIndex = 0;
                    }

                    if (config.Notes[SelectedZoneId].Notes.Count == 0)
                    {
                        config.Notes.Remove(SelectedZoneId);
                        
                        SelectedZoneId = 0;
                    }

                    config.Save();
                    plugin.PushNote();
                }

                break;
            case false:
                ImGui.BeginDisabled();

                ImGui.Button("Delete Note");

                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                {
                    ImGui.SetTooltip("Hold Shift and click to delete this note");
                }

                ImGui.EndDisabled();

                break;
        }
        if (!zoneSelected) ImGui.EndDisabled();
    }
}
