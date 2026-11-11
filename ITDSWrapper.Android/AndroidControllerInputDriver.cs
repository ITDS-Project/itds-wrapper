using System;
using System.Collections.Generic;
using System.Linq;
using Android.OS;
using Android.Views;
using ITDSWrapper.Input;
using Libretro.NET.Bindings;

namespace ITDSWrapper.Android;

public class AndroidControllerInputDriver : IInputDriver
{
    public InputDevice? Controller { get; private set; }
    private readonly Dictionary<uint, AndroidControllerInput?> _inputDictionary = [];
    private readonly Dictionary<uint, Action> _specialActions = [];

    public bool RequestInputUpdate { get; set; }

    public void SetController(InputDevice controller)
    {
        Controller = controller;
        _inputDictionary.Clear();
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_A, new AndroidControllerInput(new(AndroidInputType.KEY, Keycode.ButtonA, null)));
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_B, new AndroidControllerInput(new(AndroidInputType.KEY, Keycode.ButtonB, null)));
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_X, new AndroidControllerInput(new(AndroidInputType.KEY, Keycode.ButtonX, null)));
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_Y, new AndroidControllerInput(new(AndroidInputType.KEY, Keycode.ButtonY, null)));
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_UP, new AndroidControllerInput(new(AndroidInputType.KEY, Keycode.DpadUp, null)));
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_RIGHT, new AndroidControllerInput(new(AndroidInputType.KEY, Keycode.DpadRight, null)));
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_DOWN, new AndroidControllerInput(new(AndroidInputType.KEY, Keycode.DpadDown, null)));
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_LEFT, new AndroidControllerInput(new(AndroidInputType.KEY, Keycode.DpadLeft, null)));
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_L, new AndroidControllerInput(new(AndroidInputType.KEY, Keycode.ButtonL1, null)));
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_R, new AndroidControllerInput(new(AndroidInputType.KEY, Keycode.ButtonR1, null)));
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_START, new AndroidControllerInput(new(AndroidInputType.KEY, Keycode.ButtonStart, null)));
        SetBinding(RetroBindings.RETRO_DEVICE_ID_JOYPAD_SELECT, new AndroidControllerInput(new(AndroidInputType.KEY, Keycode.ButtonSelect, null)));
    }
    
    public void Shutdown()
    {
    }

    public uint[] GetInputKeys()
    {
        return _inputDictionary.Keys.ToArray();
    }

    public void SetSpecialAction(uint button, Action specialAction)
    {
        if (!_specialActions.TryAdd(button, specialAction))
        {
            _specialActions[button] = specialAction;
        }
        if (_inputDictionary.TryGetValue(button, out AndroidControllerInput? input))
        {
            input?.SpecialAction = specialAction;
        }
    }

    public void SetBinding<T>(uint input, IGameInput<T>? binding)
    {
        if (binding is AndroidControllerInput androidBinding)
        {
            if (!_inputDictionary.TryAdd(input, androidBinding))
            {
                _inputDictionary[input] = androidBinding;
            }
            if (_specialActions.TryGetValue(input, out Action? specialAction))
            {
                SetSpecialAction(input, specialAction);
            }
        }

        if (binding is null)
        {
            _inputDictionary[input] = null;
        }
    }

    public bool QueryInput(uint id)
    {
        return _inputDictionary.ContainsKey(id) && (_inputDictionary[id]?.IsSet ?? false);
    }

    public void Push<T>(T binding)
    {
        if (binding is AndroidInputContainer androidInput)
        {
            foreach (AndroidControllerInput? input in _inputDictionary.Values)
            {
                input?.Press(androidInput);
            }
        }
    }

    public void Release<T>(T binding)
    {
        lock (_inputDictionary)
        {
            if (binding is AndroidInputContainer androidInput)
            {
                foreach (AndroidControllerInput? input in _inputDictionary.Values)
                {
                    input?.Release(androidInput);
                }
            }
        }
    }

    public void DoRumble(ushort strength)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            Controller?.VibratorManager.DefaultVibrator.Vibrate(VibrationEffect.CreateOneShot(1000, strength / 258 + 1));
        }
        else
        {
            Controller?.Vibrator?.Vibrate(VibrationEffect.CreateOneShot(1000, strength / 258 + 1));
        }
    }
}