using System;
using System.Collections.Generic;
using Libretro.NET.Bindings;
using Silk.NET.Input;

namespace ITDSWrapper.Input;

public interface IInputDriver
{
    public enum ControllerBrand
    {
        NONE,
        GENERIC,
        XBOX,
        PLAYSTATION,
        NINTENDO_SWITCH,
        STEAM_DECK
    }

    protected static readonly Dictionary<uint, string> InputDescriptions = new()
    {
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_A, "Confirm/Advance Text" },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_B, "Back/Run" },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_X, "Open Pouch" },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_Y, "Toggle Auto/Open Map" },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_L, "Pan Cam Left" },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_R, "Pan Cam Right" },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_UP, "Navigate/Move Up" },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_DOWN, "Navigate/Move Down" },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_LEFT, "Navigate/Move Left" },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_RIGHT, "Navigate/Move Right" },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_START, "Pause" },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_SELECT, "Status" },
    };
    
    public bool RequestInputUpdate { get; set; }

    public event Action ControllerChanged;
    public void Shutdown();
    public uint[] GetInputKeys();
    public void SetSpecialAction(uint button, Action specialAction);
    public void SetBinding<T>(uint input, IGameInput<T>? binding);
    public string GetLocalisedBinding<T>(IGameInput<T>? input);
    public bool QueryInput(uint id);
    public void Push<T>(T binding);
    public void Release<T>(T binding);
    public void DoRumble(ushort strength);
    public ControllerBrand GetControllerBrand();
    
    protected static ControllerBrand MatchControllerBrandByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return ControllerBrand.GENERIC;

        if (name.Contains("Xbox", StringComparison.OrdinalIgnoreCase) || 
            name.Contains("XInput", StringComparison.OrdinalIgnoreCase))
            return ControllerBrand.XBOX;

        if (name.Contains("PS4", StringComparison.OrdinalIgnoreCase) || 
            name.Contains("PS5", StringComparison.OrdinalIgnoreCase) || 
            name.Contains("DualShock", StringComparison.OrdinalIgnoreCase) || 
            name.Contains("DualSense", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Sony", StringComparison.OrdinalIgnoreCase))
            return ControllerBrand.PLAYSTATION;

        if (name.Contains("Switch", StringComparison.OrdinalIgnoreCase) || 
            name.Contains("Joy-Con", StringComparison.OrdinalIgnoreCase) || 
            name.Contains("Nintendo", StringComparison.OrdinalIgnoreCase))
            return ControllerBrand.NINTENDO_SWITCH;

        if (name.Contains("Steam Deck", StringComparison.OrdinalIgnoreCase) || 
            name.Contains("Steam Virtual Gamepad", StringComparison.OrdinalIgnoreCase))
            return ControllerBrand.STEAM_DECK;

        return ControllerBrand.GENERIC;
    }
}