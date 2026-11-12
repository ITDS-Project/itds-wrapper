using ITDSWrapper.Audio;
#if IS_WINDOWS
using NAudio.CoreAudioApi;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;
#endif

namespace ITDSWrapper.Desktop.Windows;

public class WasapiAudioBackend : IAudioBackend
{
#if IS_WINDOWS
    private WasapiPlayer? _wavePlayer;
    private BufferedWaveProvider? _waveProvider;
    private bool _paused;
#endif

    public void Initialize(double sampleRate)
    {
#if IS_WINDOWS
        _waveProvider = new(new((int)sampleRate, 2));
        _wavePlayer = new WasapiPlayerBuilder()
            .WithLowLatency(true)
            .WithLatency(20)
            .WithCategory(AudioStreamCategory.GameEffects)
            .WithEventSync()
            .Build();
        _wavePlayer.Init(new WdlResamplingSampleProvider(_waveProvider.ToSampleProvider(), 48000));
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
}