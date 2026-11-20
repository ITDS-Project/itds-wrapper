using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Input;
using ITDSWrapper.Assets;
using ITDSWrapper.Core;
using Libretro.NET.Bindings;

namespace ITDSWrapper.Input;

public interface IInputDriver
{
    public bool RequestInputUpdate { get; set; }
    public bool IsRecordingInput { get; }

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
    public string GetControllerRawName();
    public Dictionary<uint, ControllerButton?> GetControllerBindings();
    public Dictionary<uint, PhysicalKey?> GetKeyBindings();
    public Task<object?> RecordInput(InputBinding binding);
    public void RestoreInputs(Settings settings);
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

        if (name.Contains("Odin Controller", StringComparison.OrdinalIgnoreCase))
            return ControllerBrand.AYN_THOR;

        return ControllerBrand.GENERIC;
    }

    public static string GetInputGlyph(ControllerBrand brand, ControllerButton button)
    {
        switch (brand)
        {
            default:
            case ControllerBrand.NONE:
                return string.Empty;
            
            case ControllerBrand.GENERIC:
            case ControllerBrand.AYN_THOR:
            case ControllerBrand.STEAM_DECK:
                return button switch
                {
                    ControllerButton.Start => "\ue015",
                    ControllerButton.Select => "\ue016",
                    ControllerButton.A => "\ue017",
                    ControllerButton.B => "\ue018",
                    ControllerButton.X => "\ue019",
                    ControllerButton.Y => "\ue01a",
                    ControllerButton.LeftBumper => "\ue01b",
                    ControllerButton.RightBumper => "\ue01c",
                    ControllerButton.DPadUp => "\ue02c",
                    ControllerButton.DPadRight => "\ue02f",
                    ControllerButton.DPadDown => "\ue02d",
                    ControllerButton.DPadLeft => "\ue02e",
                    ControllerButton.LeftStickUp => "\ue030",
                    ControllerButton.LeftStickRight => "\ue033",
                    ControllerButton.LeftStickDown => "\ue031",
                    ControllerButton.LeftStickLeft => "\ue032",
                    ControllerButton.LeftStickClick => "\ue022",
                    ControllerButton.RightStickUp => "\ue034",
                    ControllerButton.RightStickRight => "\ue037",
                    ControllerButton.RightStickDown => "\ue035",
                    ControllerButton.RightStickLeft => "\ue036",
                    ControllerButton.RightStickClick => "\ue023",
                    ControllerButton.LeftTrigger => "\ue024",
                    ControllerButton.RightTrigger => "\ue025",
                    _ => button == ControllerButton.Unknown ? Strings.BindingNotMapped : button.ToString(),
                };
            
            case ControllerBrand.XBOX:
                return button switch
                {
                    ControllerButton.Start => "\ue02a",
                    ControllerButton.Select => "\ue02b",
                    ControllerButton.A => "\ue017",
                    ControllerButton.B => "\ue018",
                    ControllerButton.X => "\ue019",
                    ControllerButton.Y => "\ue01a",
                    ControllerButton.LeftBumper => "\ue01b",
                    ControllerButton.RightBumper => "\ue01c",
                    ControllerButton.DPadUp => "\ue02c",
                    ControllerButton.DPadRight => "\ue02f",
                    ControllerButton.DPadDown => "\ue02d",
                    ControllerButton.DPadLeft => "\ue02e",
                    ControllerButton.LeftStickUp => "\ue030",
                    ControllerButton.LeftStickRight => "\ue033",
                    ControllerButton.LeftStickDown => "\ue031",
                    ControllerButton.LeftStickLeft => "\ue032",
                    ControllerButton.LeftStickClick => "\ue022",
                    ControllerButton.RightStickUp => "\ue034",
                    ControllerButton.RightStickRight => "\ue036",
                    ControllerButton.RightStickDown => "\ue035",
                    ControllerButton.RightStickLeft => "\ue037",
                    ControllerButton.RightStickClick => "\ue023",
                    ControllerButton.LeftTrigger => "\ue024",
                    ControllerButton.RightTrigger => "\ue025",
                    _ => button == ControllerButton.Unknown ? Strings.BindingNotMapped : button.ToString(),
                };
            
            case ControllerBrand.NINTENDO_SWITCH:
                return button switch
                {
                    ControllerButton.Start => "\ue026",
                    ControllerButton.Select => "\ue027",
                    ControllerButton.A => "\ue017",
                    ControllerButton.B => "\ue018",
                    ControllerButton.X => "\ue019",
                    ControllerButton.Y => "\ue01a",
                    ControllerButton.LeftBumper => "\ue01b",
                    ControllerButton.RightBumper => "\ue01c",
                    ControllerButton.DPadUp => "\ue02c",
                    ControllerButton.DPadRight => "\ue02f",
                    ControllerButton.DPadDown => "\ue02d",
                    ControllerButton.DPadLeft => "\ue02e",
                    ControllerButton.LeftStickUp => "\ue030",
                    ControllerButton.LeftStickRight => "\ue033",
                    ControllerButton.LeftStickDown => "\ue031",
                    ControllerButton.LeftStickLeft => "\ue032",
                    ControllerButton.LeftStickClick => "\ue022",
                    ControllerButton.RightStickUp => "\ue034",
                    ControllerButton.RightStickRight => "\ue036",
                    ControllerButton.RightStickDown => "\ue035",
                    ControllerButton.RightStickLeft => "\ue037",
                    ControllerButton.RightStickClick => "\ue023",
                    ControllerButton.LeftTrigger => "\ue024",
                    ControllerButton.RightTrigger => "\ue025",
                    _ => button == ControllerButton.Unknown ? Strings.BindingNotMapped : button.ToString(),
                };
            
            case ControllerBrand.PLAYSTATION:
                return button switch
                {
                    ControllerButton.Start => "\ue029",
                    ControllerButton.Select => "\ue028",
                    ControllerButton.A => "\ue020",
                    ControllerButton.B => "\ue021",
                    ControllerButton.X => "\ue01f",
                    ControllerButton.Y => "\ue01e",
                    ControllerButton.LeftBumper => "\ue01b",
                    ControllerButton.RightBumper => "\ue01c",
                    ControllerButton.DPadUp => "\ue02c",
                    ControllerButton.DPadRight => "\ue02f",
                    ControllerButton.DPadDown => "\ue02d",
                    ControllerButton.DPadLeft => "\ue02e",
                    ControllerButton.LeftStickUp => "\ue030",
                    ControllerButton.LeftStickRight => "\ue033",
                    ControllerButton.LeftStickDown => "\ue031",
                    ControllerButton.LeftStickLeft => "\ue032",
                    ControllerButton.LeftStickClick => "\ue022",
                    ControllerButton.RightStickUp => "\ue034",
                    ControllerButton.RightStickRight => "\ue036",
                    ControllerButton.RightStickDown => "\ue035",
                    ControllerButton.RightStickLeft => "\ue037",
                    ControllerButton.RightStickClick => "\ue023",
                    ControllerButton.LeftTrigger => "\ue024",
                    ControllerButton.RightTrigger => "\ue025",
                    _ => button == ControllerButton.Unknown ? Strings.BindingNotMapped : button.ToString(),
                };
        }
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
    AYN_THOR,
}