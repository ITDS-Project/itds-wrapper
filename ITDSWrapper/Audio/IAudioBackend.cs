using System.Collections.Generic;

namespace ITDSWrapper.Audio;

public interface IAudioBackend
{
    public void Initialize(double sampleRate);
    public void TogglePause();
    public void PlaySamples(byte[] samples);
    public void SelectDevice(string device);
    public List<string> GetDeviceNames();
}