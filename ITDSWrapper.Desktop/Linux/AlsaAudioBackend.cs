using System.Linq;
using ITDSWrapper.Audio;
#if IS_LINUX
using NAudio.Wave;
using NAudio.Wave.Alsa;
#endif

namespace ITDSWrapper.Desktop.Linux;

#pragma warning disable CA1416
public class AlsaAudioBackend : IAudioBackend
{
#if IS_LINUX
    private AlsaOut? _wavePlayer;
    private BufferedWaveProvider? _waveProvider;
    private bool _paused;
#endif

    public void Initialize(double sampleRate)
    {
#if IS_LINUX
        _waveProvider = new(new((int)sampleRate, 2));
        _wavePlayer = new();
        _wavePlayer.Init(_waveProvider);
        _wavePlayer?.Play();
#endif
    }

    public void TogglePause()
    {
#if IS_LINUX
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
#if IS_LINUX
        _waveProvider?.AddSamples(samples, 0, samples.Length);
#endif
    }

    public void SetDevice(string device)
    {
#if IS_LINUX
        if (!AlsaDeviceEnumerator.GetPlaybackDevices().Any(d => d.Name == device))
            return;
        
        _wavePlayer?.Dispose();
        _wavePlayer = new(device);
#endif
    }
}
#pragma warning restore CA1416