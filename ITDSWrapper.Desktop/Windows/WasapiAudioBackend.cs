using ITDSWrapper.Audio;
#if IS_WINDOWS
using NAudio.Wave;
#endif

namespace ITDSWrapper.Desktop.Windows;

public class WasapiAudioBackend : IAudioBackend
{
#if IS_WINDOWS
    private WasapiOut? _wavePlayer;
    private BufferedWaveProvider? _waveProvider;
    private bool _paused;
#endif

    public void Initialize(double sampleRate)
    {
#if IS_WINDOWS
        _waveProvider = new(new((int)sampleRate, 2));
        _wavePlayer = new();
        _wavePlayer.Init(_waveProvider);
        _wavePlayer?.Play();
#endif
    }

    public void TogglePause()
    {
#if IS_WINDOWS
        if (_paused)
        {
            _wavePlayer?.Play();
            _paused = false;
        }
        else
        {
            _wavePlayer?.Pause();
            _paused = true;
        }
#endif
    }

    public void PlaySamples(byte[] samples)
    {
#if IS_WINDOWS
        _waveProvider?.AddSamples(samples, 0, samples.Length);
#endif
    }

    public void SetDevice(string device)
    {
#if IS_WINDOWS
        
#endif
    }

    public WrapperAudioDevice[] GetDeviceList()
    {
#if IS_WINDOWS
        
#endif
        return [];
    }
}