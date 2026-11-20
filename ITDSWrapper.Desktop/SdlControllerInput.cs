using System;
using ITDSWrapper.Input;
using Silk.NET.Input;

namespace ITDSWrapper.Desktop;

public enum ThumbstickDirection
{
    POSITIVE_X,
    POSITIVE_Y,
    NEGATIVE_X,
    NEGATIVE_Y,
}

public record ThumbstickWithDirection(int Index, ThumbstickDirection? Direction);

public class SdlControllerInput : IGameInput<object>
{
    private object? _input;
    private ThumbstickDirection? _direction;

    public bool IsSet { get; set; }
    public Action? SpecialAction { get; set; }

    public SdlControllerInput(object? input)
    {
        SetInput(input);
    }

    public void SetInput(object? input)
    {
        if (input is Button button)
        {
            _input = button;
            _direction = null;
        }
        else if (input is (Thumbstick thumbstick, ThumbstickDirection direction))
        {
            _input = thumbstick;
            _direction = direction;
        }
        else if (input is Trigger trigger)
        {
            _input = trigger;
        }
    }

    public void Press(object? input)
    {
        if (_input is Button button && input is Button received && received.Name == button.Name || input is Thumbstick inThumbstick && _input is Thumbstick thumbstick &&
            inThumbstick.Index == thumbstick.Index || input is Trigger inTrigger && _input is Trigger trigger && inTrigger.Index == trigger.Index)
        {
            if (CheckPressed(input, _direction))
            {
                IsSet = true;
                SpecialAction?.Invoke();
            }
        }
    }

    public static bool CheckPressed(object? input, ThumbstickDirection? direction = null)
    {
        if (input is Button { Pressed: true })
        {
            return true;
        }
        if (input is Thumbstick thumbstick)
        {
            if (direction == ThumbstickDirection.NEGATIVE_X && thumbstick.X <= -0.4f ||
                direction == ThumbstickDirection.POSITIVE_X && thumbstick.X >= 0.4f ||
                direction == ThumbstickDirection.POSITIVE_Y && thumbstick.Y >= 0.4f ||
                direction == ThumbstickDirection.NEGATIVE_Y && thumbstick.Y <= -0.4f)
            {
                return true;
            }

            return false;
        }
        if (input is Trigger { Position: >= 0.4f })
        {
            return true;
        }

        return false;
    }

    public void Release(object? input)
    {
        if (_input is Button button && input is Button { Pressed: false } inButton && inButton.Name == button.Name)
        {
            IsSet = false;
        }
        else if (input is Thumbstick inThumbstick && _input is Thumbstick thumbstick && inThumbstick.Index == thumbstick.Index)
        {
            if (_direction == ThumbstickDirection.NEGATIVE_X && inThumbstick.X > -0.4f ||
                _direction == ThumbstickDirection.POSITIVE_X && inThumbstick.X < 0.4f ||
                _direction == ThumbstickDirection.POSITIVE_Y && inThumbstick.Y < 0.4f ||
                _direction == ThumbstickDirection.NEGATIVE_Y && inThumbstick.Y > -0.4f)
            {
                IsSet = false;
            }
        }
        else if (input is Trigger { Position: < 0.4f } inTrigger && _input is Trigger trigger && inTrigger.Index == trigger.Index)
        {
            IsSet = false;
        }
    }

    public object? GetInputId()
    {
        if (_input is Button button)
        {
            return button.Name;
        }
        if (_input is Thumbstick thumbstick)
        {
            return new ThumbstickWithDirection(thumbstick.Index, _direction);
        }
        if (_input is Trigger trigger)
        {
            return trigger.Index;
        }

        return null;
    }
}