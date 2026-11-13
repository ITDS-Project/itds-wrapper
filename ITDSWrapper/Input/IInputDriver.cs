using System;
using System.Collections.Generic;
using ITDSWrapper.Assets;
using Libretro.NET.Bindings;

namespace ITDSWrapper.Input;

public interface IInputDriver
{
    public bool RequestInputUpdate { get; set; }

    public event Action ControllerChanged;
    public void Shutdown();
    public uint[] GetInputKeys();
    public void SetSpecialAction(uint button, Action specialAction);
    public void SetBinding<T>(uint input, IGameInput<T>? binding);
    public string GetLocalisedBinding(ControllerButton button);
    public bool QueryInput(uint id);
    public void Push<T>(T binding);
    public void Release<T>(T binding);
    public void DoRumble(ushort strength);
    public ControllerBrand GetControllerBrand();
}

public static class InputDescriptions
{
    public static readonly Dictionary<uint, string> Descriptions = new()
    {
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_A, Strings.BindingADescription },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_B, Strings.BindingBDescription },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_X, Strings.BindingXDescription },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_Y, Strings.BindingYDescription },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_L, Strings.BindingLDescription },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_R, Strings.BindingRDescription },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_UP, Strings.BindingDUpDescription },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_DOWN, Strings.BindingDDownDescription },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_LEFT, Strings.BindingDLeftDescription },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_RIGHT, Strings.BindingDRightDescription },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_START, Strings.BindingStartDescription },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_SELECT, Strings.BindingSelectDescription },
    };
    
    public static ControllerBrand MatchControllerBrandByName(string name)
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

public enum ControllerBrand
{
    NONE,
    GENERIC,
    XBOX,
    PLAYSTATION,
    NINTENDO_SWITCH,
    STEAM_DECK,
}