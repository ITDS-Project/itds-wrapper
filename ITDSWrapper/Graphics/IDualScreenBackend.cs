using System;

namespace ITDSWrapper.Graphics;

public interface IDualScreenBackend
{
    public bool IsActive { get; set; }
    event Action? ActiveChanged;
    public void SetSecondFrame(byte[] pixels, uint width, uint height);
    
    public void SwapDualScreens();
}