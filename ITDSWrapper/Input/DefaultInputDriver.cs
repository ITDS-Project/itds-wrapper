using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using ITDSWrapper.Assets;
using ITDSWrapper.Core;
using Libretro.NET.Bindings;

namespace ITDSWrapper.Input;

public class DefaultInputDriver : IInputDriver
{
    private readonly Dictionary<uint, object?> _bindings;
    private readonly Dictionary<uint, Action> _specialActions = [];
    private PhysicalKey? _recordedKey;

    public bool RequestInputUpdate { get; set; }
    public bool IsRecordingInput { get; private set; }
    public event Action? ControllerChanged;

    public DefaultInputDriver(bool isMobile, Action openSettings)
    {
        if (isMobile)
        {
            _bindings = new()
            {
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_A, null },
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_B, null },
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_X, null },
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_Y, null },
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_R, null },
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_L, null },
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_UP, null },
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_RIGHT, null },
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_DOWN, null },
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_LEFT, null },
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_START, null },
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_SELECT, null },
            };
        }
        else
        {
            _bindings = new()
            {
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_A, new PhysicalKeyInput(PhysicalKey.X) },
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_B, new PhysicalKeyInput(PhysicalKey.Z) },
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_X, new PhysicalKeyInput(PhysicalKey.S) },
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_Y, new PhysicalKeyInput(PhysicalKey.A) },
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_R, new PhysicalKeyInput(PhysicalKey.W) },
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_L, new PhysicalKeyInput(PhysicalKey.Q) },
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_UP, new PhysicalKeyInput(PhysicalKey.ArrowUp) },
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_RIGHT, new PhysicalKeyInput(PhysicalKey.ArrowRight) },
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_DOWN, new PhysicalKeyInput(PhysicalKey.ArrowDown) },
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_LEFT, new PhysicalKeyInput(PhysicalKey.ArrowLeft) },
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_START, new PhysicalKeyInput(PhysicalKey.Enter) },
                { RetroBindings.RETRO_DEVICE_ID_JOYPAD_SELECT, new PhysicalKeyInput(PhysicalKey.Escape) { SpecialAction = openSettings } },
            };
            SetSpecialAction(RetroBindings.RETRO_DEVICE_ID_JOYPAD_SELECT, openSettings);
        }
    }

    public void Shutdown()
    {
    }

    public uint[] GetInputKeys()
    {
        return [.. _bindings.Keys];
    }

    public void SetSpecialAction(uint button, Action specialAction)
    {
        if (!_specialActions.TryAdd(button, specialAction))
        {
            _specialActions[button] = specialAction;
        }

        if (_bindings.TryGetValue(button, out object? input) && input is PhysicalKeyInput keyInput)
        {
            keyInput.SpecialAction = specialAction;
        }
    }

    public void SetBinding<T>(uint input, IGameInput<T>? binding)
    {
        if (!_bindings.TryAdd(input, binding))
        {
            _bindings[input] = binding;
        }

        if (_specialActions.TryGetValue(input, out Action? specialAction))
        {
            SetSpecialAction(input, specialAction);
        }
    }

    public string GetLocalisedBinding(ControllerButton button)
    {
        return Strings.BindingUnknown;
    }

    public bool QueryInput(uint id)
    {
        return _bindings.ContainsKey(id) && _bindings[id] is IGameInputSettable { IsSet: true };
    }

    public void Push<T>(T binding)
    {
        foreach (IGameInput<T>? input in _bindings.Values.Where(i => i is IGameInput<T>))
        {
            input?.Press(binding);
        }
    }

    public void Release<T>(T binding)
    {
        foreach (IGameInput<T>? input in _bindings.Values.Where(i => i is IGameInput<T>))
        {
            input?.Release(binding);
        }
    }

    public void DoRumble(ushort strength)
    {
    }

    public ControllerBrand GetControllerBrand()
    {
        return ControllerBrand.NONE;
    }

    public string GetControllerRawName()
    {
        return "Default Input";
    }

    public Dictionary<uint, ControllerButton?> GetControllerBindings()
    {
        return [];
    }

    public Dictionary<uint, PhysicalKey?> GetKeyBindings()
    {
        return _bindings.ToDictionary(binding => binding.Key,
            binding =>
            {
                if (binding.Value is PhysicalKeyInput keyInput)
                {
                    return keyInput.PhysicalKey;
                }

                return (PhysicalKey?)null;
            });
    }

    public async Task<object?> RecordInput(InputBinding binding)
    {
        PhysicalKeyInput? keyInput = null;
        IsRecordingInput = true;
        for (int i = 0; i < 50; i++)
        {
            if (_recordedKey is not null)
            {
                keyInput = new((PhysicalKey)_recordedKey!);
                SetBinding(binding.Action, keyInput);
                break;
            }
            await Task.Delay(TimeSpan.FromSeconds(0.1));
        }
        _recordedKey = null;
        IsRecordingInput = false;
        
        return keyInput?.PhysicalKey;
    }

    public void RestoreInputs(Settings settings)
    {
        if (settings.KeyBindings.Count <= 0)
            return;
        foreach (var keyBinding in settings.KeyBindings)
        {
            SetBinding(keyBinding.Key,
                keyBinding.Value is not null ? new PhysicalKeyInput((PhysicalKey)keyBinding.Value) : null);
        }
    }

    public void KeyPressed(object? sender, KeyEventArgs eventArgs)
    {
        _recordedKey = eventArgs.PhysicalKey;
    }
}