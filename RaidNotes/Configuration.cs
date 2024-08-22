using Dalamud.Configuration;
using Dalamud.Plugin;
using RaidNotes.Models;
using System;
using System.Collections.Generic;

namespace RaidNotes;

[Serializable]
public class Configuration : IPluginConfiguration
{
    // Config version, to be incremented whenever the config structure changes
    public int Version { get; set; } = 0;


    public bool GlobalEnable { get; set; } = true;
    public bool NoteWindowLocked { get; set; } = false;
    public bool HiddenOutsideCombat { get; set; } = false;


    public Dictionary<uint, ZoneSettings> Notes = [];

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
