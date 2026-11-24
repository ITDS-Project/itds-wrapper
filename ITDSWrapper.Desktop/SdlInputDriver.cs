using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Threading;
using ITDSWrapper.Assets;
using ITDSWrapper.Core;
using ITDSWrapper.Input;
using Libretro.NET.Bindings;
using Silk.NET.Input;
using Silk.NET.Input.Sdl;
using Silk.NET.SDL;

namespace ITDSWrapper.Desktop;

public class SdlInputDriver : IInputDriver
{
    private readonly SdlInputContextHost _contextHost;
    private IInputContext? _inputContext;
    private IGamepad? _gamepad;
    private Action<IGamepad, Button>? _buttonDownHandler;
    private Action<IGamepad, Button>? _buttonUpHandler;
    private Action<IGamepad, Thumbstick>? _thumbstickMovedHandler;
    private Action<IGamepad, Trigger>? _triggerMovedHandler;
    private readonly Dictionary<uint, Action> _specialActions = [];

    private Action<object?>? _recordInput;
    private object? _recordedInput;

    private readonly Dictionary<uint, List<SdlControllerInput?>> _controlsDictionary = [];

    public bool RequestInputUpdate { get; set; }
    public bool IsRecordingInput => _recordInput is not null;
    public event Action? ControllerChanged;
    private bool _requestControl;

    public SdlInputDriver(SdlInputContextHost contextHost)
    {
        SdlInput.RegisterPlatform();
        _contextHost = contextHost;
    }

    public bool HasInputContext => _inputContext is not null;
    public bool HasGamepad => _gamepad is not null;

    public void SetInputContext()
    {
        if (_contextHost.View?.IsInitialized != true)
            return;

        _inputContext = _contextHost.View?.CreateInput();
        if (HasInputContext)
        {
            _inputContext!.ConnectionChanged += (device, _) =>
            {
                if (device is not IGamepad gamepad)
                    return;

                if (gamepad.IsConnected)
                    SetGamepad(gamepad);
                else if (ReferenceEquals(_gamepad, gamepad))
                    SetGamepad(null);
            };
            Dispatcher.UIThread.Invoke(() => _contextHost.View?.DoEvents());
            if (_inputContext.Gamepads.Count > 0)
            {
                SetGamepad(_inputContext.Gamepads[0]);
            }
        }
    }

    public int PumpView()
    {
        Dispatcher.UIThread.Invoke(() => _contextHost.View?.DoEvents());
        if (_requestControl)
        {
            _requestControl = false;
            return 1;
        }

        return -1;
    }

    public void SetGamepad(IGamepad? gamepad)
    {
        if (_gamepad is not null)
        {
            if (_buttonDownHandler is not null)
                _gamepad.ButtonDown -= _buttonDownHandler;
            if (_buttonUpHandler is not null)
                _gamepad.ButtonUp -= _buttonUpHandler;
            if (_thumbstickMovedHandler is not null)
                _gamepad.ThumbstickMoved -= _thumbstickMovedHandler;
            if (_triggerMovedHandler is not null)
                _gamepad.TriggerMoved -= _triggerMovedHandler;
        }

        _gamepad = gamepad;
        _controlsDictionary.Clear();
        _buttonDownHandler = null;
        _buttonUpHandler = null;
        _thumbstickMovedHandler = null;
        _triggerMovedHandler = null;
        ControllerChanged?.Invoke();

        if (_gamepad is null)
            return;

        if (_controlsDictionary.Count == 0)
        {
            _controlsDictionary.Add(RetroBindings.RETRO_DEVICE_ID_JOYPAD_A, []);
            _controlsDictionary.Add(RetroBindings.RETRO_DEVICE_ID_JOYPAD_B, []);
            _controlsDictionary.Add(RetroBindings.RETRO_DEVICE_ID_JOYPAD_X, []);
            _controlsDictionary.Add(RetroBindings.RETRO_DEVICE_ID_JOYPAD_Y, []);
            _controlsDictionary.Add(RetroBindings.RETRO_DEVICE_ID_JOYPAD_L, []);
            _controlsDictionary.Add(RetroBindings.RETRO_DEVICE_ID_JOYPAD_R, []);
            _controlsDictionary.Add(RetroBindings.RETRO_DEVICE_ID_JOYPAD_UP, []);
            _controlsDictionary.Add(RetroBindings.RETRO_DEVICE_ID_JOYPAD_RIGHT, []);
            _controlsDictionary.Add(RetroBindings.RETRO_DEVICE_ID_JOYPAD_DOWN, []);
            _controlsDictionary.Add(RetroBindings.RETRO_DEVICE_ID_JOYPAD_LEFT, []);
            _controlsDictionary.Add(RetroBindings.RETRO_DEVICE_ID_JOYPAD_START, []);
            _controlsDictionary.Add(RetroBindings.RETRO_DEVICE_ID_JOYPAD_SELECT, []);

            foreach (Button button in _gamepad.Buttons)
            {
                switch (button.Name)
                {
                    case ButtonName.A:
                        _controlsDictionary[RetroBindings.RETRO_DEVICE_ID_JOYPAD_A].Add(new(button));
                        break;
                    case ButtonName.B:
                        _controlsDictionary[RetroBindings.RETRO_DEVICE_ID_JOYPAD_B].Add(new(button));
                        break;
                    case ButtonName.X:
                        _controlsDictionary[RetroBindings.RETRO_DEVICE_ID_JOYPAD_X].Add(new(button));
                        break;
                    case ButtonName.Y:
                        _controlsDictionary[RetroBindings.RETRO_DEVICE_ID_JOYPAD_Y].Add(new(button));
                        break;
                    case ButtonName.LeftBumper:
                        _controlsDictionary[RetroBindings.RETRO_DEVICE_ID_JOYPAD_L].Add(new(button));
                        break;
                    case ButtonName.RightBumper:
                        _controlsDictionary[RetroBindings.RETRO_DEVICE_ID_JOYPAD_R].Add(new(button));
                        break;
                    case ButtonName.DPadUp:
                        _controlsDictionary[RetroBindings.RETRO_DEVICE_ID_JOYPAD_UP].Add(new(button));
                        break;
                    case ButtonName.DPadRight:
                        _controlsDictionary[RetroBindings.RETRO_DEVICE_ID_JOYPAD_RIGHT].Add(new(button));
                        break;
                    case ButtonName.DPadDown:
                        _controlsDictionary[RetroBindings.RETRO_DEVICE_ID_JOYPAD_DOWN].Add(new(button));
                        break;
                    case ButtonName.DPadLeft:
                        _controlsDictionary[RetroBindings.RETRO_DEVICE_ID_JOYPAD_LEFT].Add(new(button));
                        break;
                    case ButtonName.Start:
                        _controlsDictionary[RetroBindings.RETRO_DEVICE_ID_JOYPAD_START].Add(new(button));
                        break;
                    case ButtonName.Back:
                        _controlsDictionary[RetroBindings.RETRO_DEVICE_ID_JOYPAD_SELECT].Add(new(button)
                            { SpecialAction = _specialActions[RetroBindings.RETRO_DEVICE_ID_JOYPAD_SELECT] });
                        break;
                }
            }

            if (_gamepad?.Thumbsticks.Count > 0)
            {
                _controlsDictionary[RetroBindings.RETRO_DEVICE_ID_JOYPAD_UP]
                    .Add(new((_gamepad.Thumbsticks[0], ThumbstickDirection.NEGATIVE_Y)));
                _controlsDictionary[RetroBindings.RETRO_DEVICE_ID_JOYPAD_RIGHT]
                    .Add(new((_gamepad.Thumbsticks[0], ThumbstickDirection.POSITIVE_X)));
                _controlsDictionary[RetroBindings.RETRO_DEVICE_ID_JOYPAD_DOWN]
                    .Add(new((_gamepad.Thumbsticks[0], ThumbstickDirection.POSITIVE_Y)));
                _controlsDictionary[RetroBindings.RETRO_DEVICE_ID_JOYPAD_LEFT]
                    .Add(new((_gamepad.Thumbsticks[0], ThumbstickDirection.NEGATIVE_X)));
            }
        }

        _buttonDownHandler = (_, button) => Push(button);
        _buttonUpHandler = (_, button) => Release(button);
        _thumbstickMovedHandler = (_, thumbstick) =>
        {
            Push(thumbstick);
            Release(thumbstick);
        };
        _triggerMovedHandler = (_, trigger) =>
        {
            Push(trigger);
            Release(trigger);
        };

        _gamepad!.ButtonDown += _buttonDownHandler;
        _gamepad.ButtonUp += _buttonUpHandler;
        _gamepad.ThumbstickMoved += _thumbstickMovedHandler;
        _gamepad.TriggerMoved += _triggerMovedHandler;
    }

    public void Shutdown()
    {
    }

    public uint[] GetInputKeys()
    {
        return _controlsDictionary.Keys.ToArray();
    }

    public void SetSpecialAction(uint button, Action specialAction)
    {
        if (!_specialActions.TryAdd(button, specialAction))
        {
            _specialActions[button] = specialAction;
        }
    }

    public void SetBinding<T>(uint input, IGameInput<T>? binding)
    {
        switch (binding)
        {
            case SdlControllerInput sdlInput:
                if (!_controlsDictionary.TryGetValue(input, out List<SdlControllerInput?>? existingInput))
                {
                    _controlsDictionary.Add(input, [sdlInput]);
                    if (_specialActions.TryGetValue(input, out Action? specialAction))
                    {
                        sdlInput.SpecialAction = specialAction;
                    }
                }
                else
                {
                    _controlsDictionary[input].Clear();
                    _controlsDictionary[input].Add(sdlInput);
                    if (_specialActions.TryGetValue(input, out Action? specialAction))
                    {
                        sdlInput.SpecialAction = specialAction;
                    }
                }
                break;
                
            case null:
                _controlsDictionary[input].Clear();
                break;
        }
    }

    public string GetLocalisedBinding(ControllerButton button)
    {
        foreach ((uint a, List<SdlControllerInput?> b) in _controlsDictionary)
        {
            if (b.Any(i => i is not null && GetGenericButtonFromSdl(i) == button))
            {
                return InputDescriptions.Descriptions[a];
            }
        }
        return Strings.BindingNotMapped;
    }

    public bool QueryInput(uint id)
    {
        return _controlsDictionary.ContainsKey(id) && _controlsDictionary[id].Any(i => i?.IsSet ?? false);
    }

    public void Push<T>(T binding)
    {
        _requestControl = true;
        foreach (List<SdlControllerInput?> list in _controlsDictionary.Values)
        {
            foreach (SdlControllerInput? input in list)
            {
                input?.Press(binding);
            }
        }

        if (binding is Thumbstick thumbstick)
        {
            if (thumbstick.X > SdlControllerInput.InputSensitivity)
            {
                _recordInput?.Invoke((thumbstick, ThumbstickDirection.POSITIVE_X));
            }
            else if (thumbstick.X < -SdlControllerInput.InputSensitivity)
            {
                _recordInput?.Invoke((thumbstick, ThumbstickDirection.NEGATIVE_X));
            }
            else if (thumbstick.Y > SdlControllerInput.InputSensitivity)
            {
                _recordInput?.Invoke((thumbstick, ThumbstickDirection.POSITIVE_Y));
            }
            else if (thumbstick.Y < -SdlControllerInput.InputSensitivity)
            {
                _recordInput?.Invoke((thumbstick, ThumbstickDirection.NEGATIVE_Y));
            }
        }
        else if (SdlControllerInput.CheckPressed(binding))
        {
            _recordInput?.Invoke(binding);
        }
    }

    public void Release<T>(T binding)
    {
        foreach (List<SdlControllerInput?> list in _controlsDictionary.Values)
        {
            foreach (SdlControllerInput? input in list)
                input?.Release(binding);
        }
    }

    public void DoRumble(ushort strength)
    {
        foreach (IMotor motor in _gamepad?.VibrationMotors ?? [])
        {
            motor.Speed = (float)strength / ushort.MaxValue;
        }
    }

    public uint[] GetActionGlyphId(string button)
    {
        return [];
    }
    
    public ControllerBrand GetControllerBrand()
    {
        if (_gamepad is null)
            return ControllerBrand.NONE;
        Sdl sdl = Sdl.GetApi();
        if (sdl.IsGameController(_gamepad.Index) == SdlBool.True)
        {
            return sdl.GameControllerTypeForIndex(_gamepad.Index) switch
            {
                GameControllerType.Xbox360 or GameControllerType.Xboxone => ControllerBrand.XBOX,
                GameControllerType.PS3 or GameControllerType.PS4 or GameControllerType.PS5 => ControllerBrand.PLAYSTATION,
                GameControllerType.NintendoSwitchPro or GameControllerType.Virtual => ControllerBrand.NINTENDO_SWITCH, // Joy-Cons often map to Switch Pro or Virtual
                _ => InputDescriptions.MatchControllerBrandByName(_gamepad.Name),
            };
        }
        return InputDescriptions.MatchControllerBrandByName(_gamepad.Name);
    }

    private static ControllerButton GetGenericButtonFromSdl(SdlControllerInput? input)
    {
        if (input == null)
        {
            return ControllerButton.Unknown;
        }
        object? id = input.GetInputId();
        if (id is ButtonName buttonName)
        {
            return buttonName switch
            {
                ButtonName.A => ControllerButton.A,
                ButtonName.B => ControllerButton.B,
                ButtonName.X => ControllerButton.X,
                ButtonName.Y => ControllerButton.Y,
                ButtonName.DPadUp => ControllerButton.DPadUp,
                ButtonName.DPadDown => ControllerButton.DPadDown,
                ButtonName.DPadLeft => ControllerButton.DPadLeft,
                ButtonName.DPadRight => ControllerButton.DPadRight,
                ButtonName.LeftStick => ControllerButton.LeftStickClick,
                ButtonName.RightStick => ControllerButton.RightStickClick,
                ButtonName.LeftBumper => ControllerButton.LeftBumper,
                ButtonName.RightBumper => ControllerButton.RightBumper,
                ButtonName.Start => ControllerButton.Start,
                ButtonName.Back => ControllerButton.Select,
                _ => ControllerButton.Unknown,
            };
        }
        if (id is ThumbstickWithDirection thumbstick)
        {
            return thumbstick.Index switch
            {
                1 => thumbstick.Direction switch
                {
                    ThumbstickDirection.NEGATIVE_X => ControllerButton.RightStickLeft,
                    ThumbstickDirection.NEGATIVE_Y => ControllerButton.RightStickUp,
                    ThumbstickDirection.POSITIVE_X => ControllerButton.RightStickRight,
                    ThumbstickDirection.POSITIVE_Y => ControllerButton.RightStickDown,
                    _ => ControllerButton.Unknown,
                },
                _ => thumbstick.Direction switch
                {
                    ThumbstickDirection.NEGATIVE_X => ControllerButton.LeftStickLeft,
                    ThumbstickDirection.NEGATIVE_Y => ControllerButton.LeftStickUp,
                    ThumbstickDirection.POSITIVE_X => ControllerButton.LeftStickRight,
                    ThumbstickDirection.POSITIVE_Y => ControllerButton.LeftStickDown,
                    _ => ControllerButton.Unknown,
                },
            };
        }
        if (id is int triggerIndex)
        {
            return triggerIndex switch
            {
                0 => ControllerButton.LeftTrigger,
                1 => ControllerButton.RightTrigger,
                _ => ControllerButton.Unknown,
            };
        }

        return ControllerButton.Unknown;
    }

    public string GetControllerRawName()
    {
        return _gamepad?.Name ?? string.Empty;
    }

    public Dictionary<uint, ControllerButton?> GetControllerBindings()
    {
        var bindings = new Dictionary<uint, ControllerButton?>();
        foreach ((uint a, List<SdlControllerInput?> b) in _controlsDictionary)
        {
            foreach (SdlControllerInput? input in b)
            {
                if (b is not null)
                {
                    bindings[a] = GetGenericButtonFromSdl(input);
                }
            }
        }
        return bindings.ToDictionary();
    }

    public Dictionary<uint, PhysicalKey?> GetKeyBindings()
    {
        return [];
    }

    public async Task<object?> RecordInput(InputBinding binding)
    {
        _recordInput = b =>
        {
            _recordedInput = b;
            _recordInput = null;
        };
        SdlControllerInput? sdlInput = null;
        for (int i = 0; i < 50; i++)
        {
            if (_recordedInput is not null)
            {
                sdlInput = new(_recordedInput);
                SetBinding(binding.Action, sdlInput);
                break;
            }
            await Task.Delay(TimeSpan.FromSeconds(0.1));
        }
        _recordInput = null;
        _recordedInput = null;
        
        return GetGenericButtonFromSdl(sdlInput);
    }

    public void RestoreInputs(Settings settings)
    {
        if (settings.ControllerBindings.Count <= 0)
            return;
        foreach (var controllerBinding in settings.ControllerBindings)
        {
            SdlControllerInput? input = controllerBinding.Value switch
            {
                ControllerButton.A => new(_gamepad?.Buttons.FirstOrDefault(b => b.Name == ButtonName.A)),
                ControllerButton.B => new(_gamepad?.Buttons.FirstOrDefault(b => b.Name == ButtonName.B)),
                ControllerButton.X => new(_gamepad?.Buttons.FirstOrDefault(b => b.Name == ButtonName.X)),
                ControllerButton.Y => new(_gamepad?.Buttons.FirstOrDefault(b => b.Name == ButtonName.Y)),
                ControllerButton.DPadUp => new(_gamepad?.Buttons.FirstOrDefault(b => b.Name == ButtonName.DPadUp)),
                ControllerButton.DPadRight => new(_gamepad?.Buttons.FirstOrDefault(b => b.Name == ButtonName.DPadRight)),
                ControllerButton.DPadDown => new(_gamepad?.Buttons.FirstOrDefault(b => b.Name == ButtonName.DPadDown)),
                ControllerButton.DPadLeft => new(_gamepad?.Buttons.FirstOrDefault(b => b.Name == ButtonName.DPadLeft)),
                ControllerButton.LeftBumper => new(_gamepad?.Buttons.FirstOrDefault(b => b.Name == ButtonName.LeftBumper)),
                ControllerButton.RightBumper => new(_gamepad?.Buttons.FirstOrDefault(b => b.Name == ButtonName.RightBumper)),
                ControllerButton.LeftTrigger => new(_gamepad?.Triggers.FirstOrDefault(t => t.Index == 0)),
                ControllerButton.RightTrigger => new(_gamepad?.Triggers.FirstOrDefault(t => t.Index == 1)),
                ControllerButton.LeftStickUp => new((_gamepad?.Thumbsticks.FirstOrDefault(s => s.Index == 0), ThumbstickDirection.NEGATIVE_Y)),
                ControllerButton.LeftStickRight => new((_gamepad?.Thumbsticks.FirstOrDefault(s => s.Index == 0), ThumbstickDirection.POSITIVE_X)),
                ControllerButton.LeftStickDown => new((_gamepad?.Thumbsticks.FirstOrDefault(s => s.Index == 0), ThumbstickDirection.POSITIVE_Y)),
                ControllerButton.LeftStickLeft => new((_gamepad?.Thumbsticks.FirstOrDefault(s => s.Index == 0), ThumbstickDirection.NEGATIVE_X)),
                ControllerButton.LeftStickClick => new(_gamepad?.Buttons.FirstOrDefault(b => b.Name == ButtonName.LeftStick)),
                ControllerButton.RightStickUp => new((_gamepad?.Thumbsticks.FirstOrDefault(s => s.Index == 1), ThumbstickDirection.NEGATIVE_Y)),
                ControllerButton.RightStickRight => new((_gamepad?.Thumbsticks.FirstOrDefault(s => s.Index == 1), ThumbstickDirection.POSITIVE_X)),
                ControllerButton.RightStickDown => new((_gamepad?.Thumbsticks.FirstOrDefault(s => s.Index == 1), ThumbstickDirection.POSITIVE_Y)),
                ControllerButton.RightStickLeft => new((_gamepad?.Thumbsticks.FirstOrDefault(s => s.Index == 1), ThumbstickDirection.NEGATIVE_X)),
                ControllerButton.RightStickClick => new(_gamepad?.Buttons.FirstOrDefault(b => b.Name == ButtonName.RightStick)),
                ControllerButton.Select => new(_gamepad?.Buttons.FirstOrDefault(b => b.Name == ButtonName.Back)),
                ControllerButton.Start => new(_gamepad?.Buttons.FirstOrDefault(b => b.Name == ButtonName.Start)),
                _ => null,
            };
            SetBinding(controllerBinding.Key,
                controllerBinding.Value is not null
                    ? input
                    : null);
        }
    }
}