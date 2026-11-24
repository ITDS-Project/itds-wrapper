using System;
using System.Collections.Generic;
using System.Linq;
using ITDSWrapper.Audio;
#if IS_WINDOWS
using NAudio.CoreAudioApi;
using NAudio.Wave;
#endif

namespace ITDSWrapper.Desktop.Windows;

#pragma warning disable CA1416
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
        _waveProvider = new(new((int)sampleRate, 2), TimeSpan.FromMilliseconds(150));
        _wavePlayer = BuildWasapiPlayer(new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Console));
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
        try
        {
            _waveProvider?.AddSamples(samples, 0, samples.Length);
        }
        catch
        {
            _waveProvider?.ClearBuffer();
        }
#endif
    }

    public void SelectDevice(string device)
    {
#if IS_WINDOWS
        MMDeviceEnumerator enumerator = new();
        MMDeviceCollection devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        MMDevice? mmDevice = devices.FirstOrDefault(d => d.DeviceFriendlyName.Equals(device));
        _wavePlayer = BuildWasapiPlayer(mmDevice ?? enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console));
        _wavePlayer.Init(_waveProvider!);
        _wavePlayer?.Play();
#endif
    }

    public List<string> GetDeviceNames()
    {
#if IS_WINDOWS
        return
        [
            .. new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                .Select(d => d.DeviceFriendlyName)
        ];
#else
        return [];
#endif
    }
    
#if IS_WINDOWS
    private WasapiPlayer BuildWasapiPlayer(MMDevice device)
    {
        return new WasapiPlayerBuilder()
            .WithDevice(device)
            .WithCategory(AudioStreamCategory.GameEffects)
            .Build();
    }
#endif
}
#pragma warning restore CA1416