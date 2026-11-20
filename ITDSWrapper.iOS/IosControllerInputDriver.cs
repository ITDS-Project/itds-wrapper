using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using CoreHaptics;
using GameController;
using ITDSWrapper.Assets;
using ITDSWrapper.Core;
using ITDSWrapper.Input;
using Libretro.NET.Bindings;

namespace ITDSWrapper.iOS;

public class IosControllerInputDriver : IInputDriver
{
    public GCController? Controller { get; private set; }
    private CHHapticEngine? _hapticEngine;
    private readonly Dictionary<uint, IosControllerInput?> _inputsDictionary = [];
    private readonly Dictionary<uint, Action> _specialActions = [];

    private Action<GCControllerElement?>? _recordInput;
    private GCControllerElement? _recordedInput;
    
    public bool RequestInputUpdate { get; set; }
    public bool IsRecordingInput { get; }
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

    Dictionary<uint, ControllerButton?> IInputDriver.GetControllerBindings()
    {
        return _inputsDictionary.ToDictionary(i => i.Key, i => i.Value?.GetGenericButtonName());
    }

    Dictionary<uint, PhysicalKey?> IInputDriver.GetKeyBindings()
    {
        return [];
    }

    public async Task<object?> RecordInput(InputBinding binding)
    {
        _recordInput = b => _recordedInput = b;
        IosControllerInput? iosInput = null;
        for (int i = 0; i < 50; i++)
        {
            if (_recordedInput is not null)
            {
                iosInput = new(_recordedInput);
                SetBinding(binding.Action, iosInput);
                break;
            }
            await Task.Delay(TimeSpan.FromSeconds(0.1));
        }
        _recordInput = null;
        _recordedInput = null;
        
        return iosInput?.GetGenericButtonName();
    }

    public void RestoreInputs(Settings settings)
    {if (settings.ControllerBindings.Count <= 0)
            return;
        foreach (var controllerBinding in settings.ControllerBindings)
        {
            GCControllerElement? input = controllerBinding.Value switch
            {
                ControllerButton.A => Controller?.ExtendedGamepad?.ButtonA,
                ControllerButton.B => Controller?.ExtendedGamepad?.ButtonB,
                ControllerButton.X => Controller?.ExtendedGamepad?.ButtonX,
                ControllerButton.Y => Controller?.ExtendedGamepad?.ButtonY,
                ControllerButton.DPadUp => Controller?.ExtendedGamepad?.DPad.Up,
                ControllerButton.DPadRight => Controller?.ExtendedGamepad?.DPad.Right,
                ControllerButton.DPadDown => Controller?.ExtendedGamepad?.DPad.Down,
                ControllerButton.DPadLeft => Controller?.ExtendedGamepad?.DPad.Left,
                ControllerButton.LeftBumper => Controller?.ExtendedGamepad?.LeftShoulder,
                ControllerButton.RightBumper => Controller?.ExtendedGamepad?.RightShoulder,
                ControllerButton.LeftStickClick => Controller?.ExtendedGamepad?.LeftThumbstickButton,
                ControllerButton.RightStickClick => Controller?.ExtendedGamepad?.RightThumbstickButton,
                ControllerButton.Select => Controller?.ExtendedGamepad?.ButtonOptions,
                ControllerButton.Start => Controller?.ExtendedGamepad?.ButtonMenu,
                _ => null,
            };
            SetBinding(controllerBinding.Key, input is not null ? new IosControllerInput(input) : null);
        }
    }
}