using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace RaidNotes.Windows;

public class NoteWindow : Window, IDisposable
{
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
        ImGui.Text(NoteContent);
    }
}
