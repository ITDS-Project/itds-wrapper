using System;
using System.Collections.Generic;
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
    private readonly List<string> _devices = [];
#endif

    public void Initialize(double sampleRate)
    {
#if IS_LINUX
        _waveProvider = new(new((int)sampleRate, 2));
        foreach (AlsaDeviceInfo device in AlsaDeviceEnumerator.GetPlaybackDevices())
        {
            try
            {
                if (device.Name.Equals("null", StringComparison.OrdinalIgnoreCase))
                    continue;
                _wavePlayer = new(device.Name);
                _devices.Add(device.Name);
            }
            catch
            {
                Console.WriteLine($"Failed to initialize Alsa device {device.Name} ({device.Description}");
            }
        }

        _wavePlayer = new();
        _wavePlayer?.Init(_waveProvider);
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

    public void SelectDevice(string device)
    {
#if IS_LINUX
        _wavePlayer = new(device);
        _wavePlayer?.Init(_waveProvider!);
#endif
    }

    public List<string> GetDeviceNames()
    {
#if IS_LINUX
        return _devices;
#else
        return [];
#endif
    }
}
#pragma warning restore CA1416