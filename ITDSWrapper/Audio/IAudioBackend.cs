namespace ITDSWrapper.Audio;

public interface IAudioBackend
{
    public void Initialize(double sampleRate);
    public void TogglePause();
    public void PlaySamples(byte[] samples);
    public void SetDevice(string device);
    public WrapperAudioDevice[] GetDeviceList();
}

public record WrapperAudioDevice(string InternalName, string DisplayName);