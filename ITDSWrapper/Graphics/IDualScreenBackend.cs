using System;

namespace ITDSWrapper.Graphics;

public interface IDualScreenBackend
{
    public bool IsActive { get; set; }
    public event Action? ActiveChanged;
    public event Action<float?, float?>? OnTouch;
    public void SetSecondFrame(byte[] pixels, uint width, uint height);
}