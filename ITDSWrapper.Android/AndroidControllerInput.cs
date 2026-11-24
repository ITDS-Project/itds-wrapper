using System;
using Android.Views;
using ITDSWrapper.Input;

namespace ITDSWrapper.Android;

public class AndroidControllerInput : IGameInput<AndroidInputContainer?>
{
    private AndroidInputContainer? _input;
    
    public bool IsSet { get; set; }
    public Action? SpecialAction { get; set; }
    
    public AndroidControllerInput(AndroidInputContainer input, Action? specialAction = null)
    {
        _input = input;
        SpecialAction = specialAction;
    }
    
    public void SetInput(AndroidInputContainer? input)
    {
        _input = input;
    }

    public void Press(AndroidInputContainer? input)
    {
        if (input?.Type == AndroidInputType.KEY && input.Key == _input?.Key)
        {
            IsSet = true;
            SpecialAction?.Invoke();
        }
    }

    public void Release(AndroidInputContainer? input)
    {
        if (input?.Type == AndroidInputType.KEY && input.Key == _input?.Key)
        {
            IsSet = false;
        }
    }

    public ControllerButton GetGenericButtonName()
    {
        if (_input is null)
            return ControllerButton.Unknown;
        
        if (_input.Type == AndroidInputType.KEY)
        {
            return _input.Key switch
            {
                Keycode.ButtonA => ControllerButton.A,
                Keycode.ButtonB => ControllerButton.B,
                Keycode.ButtonX => ControllerButton.X,
                Keycode.ButtonY => ControllerButton.Y,
                Keycode.ButtonL1 => ControllerButton.LeftBumper,
                Keycode.ButtonR1 => ControllerButton.RightBumper,
                Keycode.ButtonL2 => ControllerButton.LeftTrigger,
                Keycode.ButtonR2 => ControllerButton.RightTrigger,
                Keycode.DpadUp => ControllerButton.DPadUp,
                Keycode.DpadDown => ControllerButton.DPadDown,
                Keycode.DpadLeft => ControllerButton.DPadLeft,
                Keycode.DpadRight => ControllerButton.DPadRight,
                Keycode.ButtonStart => ControllerButton.Start,
                Keycode.ButtonSelect => ControllerButton.Select,
                Keycode.ButtonThumbl => ControllerButton.LeftStickClick,
                Keycode.ButtonThumbr => ControllerButton.RightStickClick,
                _ => ControllerButton.Unknown,
            };
        }

        return ControllerButton.Unknown;
    }
}

public enum AndroidInputType
{
    KEY,
    MOTION,
}

public record AndroidInputContainer(AndroidInputType Type, Keycode? Key, MotionRange? Motion);