using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using ITDSWrapper.Assets;
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
    public ImmutableDictionary<uint, ControllerButton?> GetControllerBindings();
    public ImmutableDictionary<uint, PhysicalKey?> GetKeyBindings();
    public Task<object?> RecordInput(InputBinding binding);
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
                    ControllerButton.DPadUp => "\ue01d\ue00f",
                    ControllerButton.DPadRight => "\ue01d\ue011",
                    ControllerButton.DPadDown => "\ue01d\ue010",
                    ControllerButton.DPadLeft => "\ue01d\ue012",
                    ControllerButton.LeftStickUp => "\ue022\ue00f",
                    ControllerButton.LeftStickRight => "\ue022\ue011",
                    ControllerButton.LeftStickDown => "\ue022\ue010",
                    ControllerButton.LeftStickLeft => "\ue022\ue012",
                    ControllerButton.LeftStickClick => "\ue022",
                    ControllerButton.RightStickUp => "\ue023\ue00f",
                    ControllerButton.RightStickRight => "\ue023\ue012",
                    ControllerButton.RightStickDown => "\ue023\ue010",
                    ControllerButton.RightStickLeft => "\ue023\ue011",
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
                    ControllerButton.DPadUp => "\ue01d\ue00f",
                    ControllerButton.DPadRight => "\ue01d\ue011",
                    ControllerButton.DPadDown => "\ue01d\ue010",
                    ControllerButton.DPadLeft => "\ue01d\ue012",
                    ControllerButton.LeftStickUp => "\ue022\ue00f",
                    ControllerButton.LeftStickRight => "\ue022\ue012",
                    ControllerButton.LeftStickDown => "\ue022\ue010",
                    ControllerButton.LeftStickLeft => "\ue022\ue011",
                    ControllerButton.LeftStickClick => "\ue022",
                    ControllerButton.RightStickUp => "\ue023\ue00f",
                    ControllerButton.RightStickRight => "\ue023\ue011",
                    ControllerButton.RightStickDown => "\ue023\ue010",
                    ControllerButton.RightStickLeft => "\ue023\ue012",
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
                    ControllerButton.DPadUp => "\ue01d\ue00f",
                    ControllerButton.DPadRight => "\ue01d\ue011",
                    ControllerButton.DPadDown => "\ue01d\ue010",
                    ControllerButton.DPadLeft => "\ue01d\ue012",
                    ControllerButton.LeftStickUp => "\ue022\ue00f",
                    ControllerButton.LeftStickRight => "\ue022\ue012",
                    ControllerButton.LeftStickDown => "\ue022\ue010",
                    ControllerButton.LeftStickLeft => "\ue022\ue011",
                    ControllerButton.LeftStickClick => "\ue022",
                    ControllerButton.RightStickUp => "\ue023\ue00f",
                    ControllerButton.RightStickRight => "\ue023\ue011",
                    ControllerButton.RightStickDown => "\ue023\ue010",
                    ControllerButton.RightStickLeft => "\ue023\ue012",
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
                    ControllerButton.A => "\ue021",
                    ControllerButton.B => "\ue020",
                    ControllerButton.X => "\ue01f",
                    ControllerButton.Y => "\ue01e",
                    ControllerButton.LeftBumper => "\ue01b",
                    ControllerButton.RightBumper => "\ue01c",
                    ControllerButton.DPadUp => "\ue01d\ue00f",
                    ControllerButton.DPadRight => "\ue01d\ue011",
                    ControllerButton.DPadDown => "\ue01d\ue010",
                    ControllerButton.DPadLeft => "\ue01d\ue012",
                    ControllerButton.LeftStickUp => "\ue022\ue00f",
                    ControllerButton.LeftStickRight => "\ue022\ue012",
                    ControllerButton.LeftStickDown => "\ue022\ue010",
                    ControllerButton.LeftStickLeft => "\ue022\ue011",
                    ControllerButton.LeftStickClick => "\ue022",
                    ControllerButton.RightStickUp => "\ue023\ue00f",
                    ControllerButton.RightStickRight => "\ue023\ue011",
                    ControllerButton.RightStickDown => "\ue023\ue010",
                    ControllerButton.RightStickLeft => "\ue023\ue012",
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