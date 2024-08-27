using System;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace RaidNotes.Windows;

public class NoteWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly Configuration config;
    private readonly ImGuiWindowFlags defaultFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoNav;
    public string NoteContent = string.Empty;

    public NoteWindow(Plugin plugin) : base($"{plugin.WindowSystem.Namespace} Current Note")
    {
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoTitleBar;

        Size = new Vector2(500, 500);
        SizeConstraints = new WindowSizeConstraints() { MinimumSize = new Vector2(100, 100), MaximumSize = new Vector2(1920, 1080)};


        this.plugin = plugin;
        SizeCondition = ImGuiCond.FirstUseEver;
        config = plugin.Configuration;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
    public override void PreDraw()
    {
        Flags = defaultFlags;

        if (config.NoteWindowLocked)
        {
            Flags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoInputs; // Lock resizing, moving, and inputs if the window is locked

        }
    }
    public override void Draw()
    {
        var noteLines = NoteContent.Split('\n');
        foreach (var line in noteLines)
        {
            var timeCode = Regex.Match(line, @"{t:(\d{1,2}:\d{1,2})}");

            if (timeCode.Success && timeCode.Groups.Count == 2)
            {
                try
                {
                    var timeStamp = TimeSpan.ParseExact(timeCode.Groups[1].Value, "mm\\:ss", CultureInfo.InvariantCulture);
                    var combatTime = (DateTime.Now - this.plugin.CombatStartTime);
                    var timeRemaining = (timeStamp - combatTime);
                    var timeDiff = this.plugin.CombatStartTime is null ? timeStamp : timeRemaining < TimeSpan.Zero ? TimeSpan.Zero : timeRemaining;

                    if (timeDiff!.Value.TotalSeconds <= 0)
                    {
                        if (this.plugin.Configuration.LinesHiddenPastTime) continue;
                        ImGui.TextColored(new Vector4(1, 0, 0, 1), $"{timeDiff:mm\\:ss}");
                    }
                    else
                    {
                        ImGui.TextColored(new Vector4(0, 1, 0, 1), $"{timeDiff:mm\\:ss}");
                    }
                    ImGui.SameLine();
                    ImGui.Text(line.Replace(timeCode.Groups[0].Value, ""));


                }
                catch (FormatException e)
                {
                    ImGui.Text($"Invalid Timestamp");
                    Plugin.Log.Error(e, $"Invalid Timestamp");
                }
            }
            else
            {
                ImGui.Text(line);
            }
            
        }
    }
}
