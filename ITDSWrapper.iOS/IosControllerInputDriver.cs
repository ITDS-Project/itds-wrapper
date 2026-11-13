using System;
using System.Collections.Generic;
using System.Linq;
using CoreHaptics;
using GameController;
using ITDSWrapper.Assets;
using ITDSWrapper.Input;
using Libretro.NET.Bindings;

namespace ITDSWrapper.iOS;

public class IosControllerInputDriver : IInputDriver
{
    public GCController? Controller { get; private set; }
    private CHHapticEngine? _hapticEngine;
    private readonly Dictionary<uint, IosControllerInput?> _inputsDictionary = [];
    private readonly Dictionary<uint, Action> _specialActions = [];

    public bool RequestInputUpdate { get; set; }
    public event Action? ControllerChanged;

    public void SetController(GCController controller)
    {
        _hapticEngine = Controller?.Haptics?.CreateEngine(GCHapticsLocality.Default);
        Controller = controller;
        _inputsDictionary.Clear();
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_A, new IosControllerInput(Controller.ExtendedGamepad?.ButtonA));
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_B, new IosControllerInput(Controller.ExtendedGamepad?.ButtonB));
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_X, new IosControllerInput(Controller.ExtendedGamepad?.ButtonX));
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_Y, new IosControllerInput(Controller.ExtendedGamepad?.ButtonY));
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_UP, new IosControllerInput(Controller.ExtendedGamepad?.DPad.Up));
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_RIGHT, new IosControllerInput(Controller.ExtendedGamepad?.DPad.Right));
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_DOWN, new IosControllerInput(Controller.ExtendedGamepad?.DPad.Down));
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_LEFT, new IosControllerInput(Controller.ExtendedGamepad?.DPad.Left));
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_L, new IosControllerInput(Controller.ExtendedGamepad?.LeftShoulder));
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_R, new IosControllerInput(Controller.ExtendedGamepad?.RightShoulder));
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_START, new IosControllerInput(Controller.ExtendedGamepad?.ButtonMenu));
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_SELECT, new IosControllerInput(Controller.ExtendedGamepad?.ButtonOptions));
        
        ControllerChanged?.Invoke();
    }
    
    public void Shutdown()
    {
    }

    public uint[] GetInputKeys()
    {
        return _inputsDictionary.Keys.ToArray();
    }

    public void SetSpecialAction(uint button, Action specialAction)
    {
        if (!_specialActions.TryAdd(button, specialAction))
        {
            _specialActions[button] = specialAction;
        }
        if (_inputsDictionary.TryGetValue(button, out IosControllerInput? input))
        {
            input?.SpecialAction = specialAction;
        }
    }

    public void SetBinding<T>(uint input, IGameInput<T>? binding)
    {
        if (binding is IosControllerInput iosBinding)
        {
            if (!_inputsDictionary.TryAdd(input, iosBinding))
            {
                _inputsDictionary[input] = iosBinding;
            }
            if (_specialActions.TryGetValue(input, out Action? specialAction))
            {
                SetSpecialAction(input, specialAction);
            }
        }

        if (binding is null)
        {
            _inputsDictionary[input] = null;
        }
    }

    public string GetLocalisedBinding(ControllerButton button)
    {
        foreach ((uint a, IosControllerInput? b) in _inputsDictionary)
        {
            if (b is not null && b.GetGenericButtonName() == button)
            {
                return InputDescriptions.Descriptions[a];
            }
        }
        return Strings.BindingNotMapped;
    }

    public bool QueryInput(uint id)
    {
        return _inputsDictionary.ContainsKey(id) && (_inputsDictionary[id]?.IsSet ?? false);
    }

    public void Push<T>(T binding)
    {
        if (binding is GCControllerElement iosBinding)
        {
            foreach (IosControllerInput? input in _inputsDictionary.Values)
            {
                input?.Press(iosBinding);
            }
        }
    }

    public void Release<T>(T binding)
    {
        if (binding is GCControllerElement iosBinding)
        {
            foreach (IosControllerInput? input in _inputsDictionary.Values)
            {
                input?.Release(iosBinding);
            }
        }
    }

    public void DoRumble(ushort strength)
    {
        // TODO: Implement haptic rumble
    }

    public ControllerBrand GetControllerBrand()
    {
        return InputDescriptions.MatchControllerBrandByName(Controller?.VendorName ?? string.Empty);
    }
    
    public string GetControllerRawName()
    {
        return Controller?.VendorName ?? string.Empty;
    }
}