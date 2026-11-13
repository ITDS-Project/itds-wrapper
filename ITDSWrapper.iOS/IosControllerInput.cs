using System;
using GameController;
using ITDSWrapper.Assets;
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
        if (input?.Equals(_input) ?? false)
        {
            IsSet = true;
        }
    }

    public void Release(GCControllerElement? input)
    {
        if (input?.Equals(_input) ?? false)
        {
            IsSet = false;
        }
    }

    public ControllerButton GetGenericButtonName()
    {
        if (_input is null)
            return ControllerButton.Unknown;
        return ControllerButton.Unknown;
    }
}