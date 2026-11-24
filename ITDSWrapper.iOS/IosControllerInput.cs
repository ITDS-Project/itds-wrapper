using System;
using GameController;
using ITDSWrapper.Input;

namespace ITDSWrapper.iOS;

public class IosControllerInput : IGameInput<GCControllerElement?>
{
    private GCControllerElement? _input;
    
    public bool IsSet { get; set; }
    public Action? SpecialAction { get; set; }

    public IosControllerInput(GCControllerElement? input, Action? specialAction = null)
    {
        _input = input;
        SpecialAction = specialAction;
    }
    
    public void SetInput(GCControllerElement? input)
    {
        _input = input;
    }

    public void Press(GCControllerElement? input)
    {
        if (input?.LocalizedName?.Equals(_input?.LocalizedName) == true)
        {
            if (input is GCControllerButtonInput { IsPressed: true })
            {
                IsSet = true;
                SpecialAction?.Invoke();
            }
        }
    }

    public void Release(GCControllerElement? input)
    {
        if (input?.LocalizedName?.Equals(_input?.LocalizedName) == true)
        {
            if (input is GCControllerButtonInput { IsPressed: false })
            {
                IsSet = false;
            }
        }
    }

    public ControllerButton GetGenericButtonName()
    {
        if (_input is null)
            return ControllerButton.Unknown;
        if (_input is GCControllerButtonInput button)
        {
            return button.LocalizedName switch
            {
                "Button A" => ControllerButton.A,
                "Button B" => ControllerButton.B,
                "Button X" => ControllerButton.X,
                "Button Y" => ControllerButton.Y,
                "Up" => ControllerButton.DPadUp,
                "Right" => ControllerButton.DPadRight,
                "Down" => ControllerButton.DPadDown,
                "Left" => ControllerButton.DPadLeft,
                "Left Shoulder" => ControllerButton.LeftBumper,
                "Right Shoulder" => ControllerButton.RightBumper,
                "Left Trigger" => ControllerButton.LeftTrigger,
                "Right Trigger" => ControllerButton.RightTrigger,
                "Left Thumbstick Button" => ControllerButton.LeftStickClick,
                "Right Thumbstick Button" => ControllerButton.RightStickClick,
                "Button Menu" => ControllerButton.Start,
                "Button Options" => ControllerButton.Select,
                _ => ControllerButton.Unknown,
            };
        }
        return ControllerButton.Unknown;
    }
}