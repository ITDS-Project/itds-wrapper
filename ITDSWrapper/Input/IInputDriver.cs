using System;
using System.Collections.Generic;
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
    public string GetLocalisedBinding<T>(IGameInput<T>? input);
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
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_A, "" },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_B, "" },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_X, "" },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_Y, "" },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_L, "" },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_R, "" },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_UP, "" },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_DOWN, "" },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_LEFT, "" },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_RIGHT, "" },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_START, "" },
        { RetroBindings.RETRO_DEVICE_ID_JOYPAD_SELECT, "" },
    };
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