// Referenced https://gist.github.com/neilt6/6d07322070470536ea0ba409c343c2a5 while creating this

using System;
using System.Linq;
using System.Threading;
using Android.Content;
using Android.Media;
using ITDSWrapper.Audio;

namespace ITDSWrapper.Android;

public class AndroidAudioBackend : IAudioBackend
{
    private readonly Context _context;
    private readonly SynchronizationContext? _synchronizationContext;
    AudioTrack? _audioTrack;
    float _volume;
    
    public float Volume
    {
        get => _volume;
        set
        {
            _volume = (value < 0.0f) ? 0.0f : (value > 1.0f) ? 1.0f : value;
            _audioTrack?.SetVolume(_volume);
        }
    }

    public int DesiredLatency { get; set; }

    public int NumberOfBuffers { get; set; }
    
    public AudioUsageKind Usage { get; set; }

    public AudioContentType ContentType { get; set; }

    public AudioTrackPerformanceMode PerformanceMode { get; set; }

    public int? AudioSessionId => _audioTrack?.AudioSessionId;

    private bool _pause;
    
    public AndroidAudioBackend(Context context)
    {
        _context = context;
        _synchronizationContext = SynchronizationContext.Current;

        _volume = 1.0f;
        NumberOfBuffers = 2;
        DesiredLatency = 300;
        
        Usage = AudioUsageKind.Game;
        ContentType = AudioContentType.Music;
        PerformanceMode = AudioTrackPerformanceMode.None;
    }

    public void Initialize(double sampleRate)
    {
        //Determine the buffer size
        Encoding encoding = Encoding.Pcm16bit;
        ChannelOut channelMask = ChannelOut.Stereo;
        
        int minBufferSize = AudioTrack.GetMinBufferSize((int)sampleRate, channelMask, encoding);

        _audioTrack = new AudioTrack.Builder()
            .SetAudioAttributes(new AudioAttributes.Builder()
                .SetUsage(Usage)!
                .SetContentType(ContentType)!
                .Build()!)
            .SetAudioFormat(new AudioFormat.Builder()
                .SetEncoding(encoding)!
                .SetSampleRate((int)sampleRate)!
                .SetChannelMask(channelMask)!
                .Build()!)
            .SetBufferSizeInBytes(minBufferSize)
            .SetTransferMode(AudioTrackMode.Stream)
            .SetPerformanceMode(PerformanceMode)
            .Build();
        _audioTrack.SetVolume(Volume);
        _audioTrack.Play();
    }

    public void TogglePause()
    {
        if (_pause)
        {
            _pause = false;
            _audioTrack?.Play();
        }
        else
        {
            _pause = true;
            _audioTrack?.Pause();
            _audioTrack?.Flush();
        }
    }

    public void PlaySamples(byte[] samples)
    {
        int waveBufferSize = (_audioTrack!.BufferSizeInFrames + NumberOfBuffers - 1) / NumberOfBuffers * 2;
        waveBufferSize = (waveBufferSize + 3) & ~3;
        waveBufferSize = waveBufferSize > samples.Length ? samples.Length : waveBufferSize;
        byte[] waveBuffer = new byte[waveBufferSize];

        //Fill the wave buffer with new samples
        Array.Copy(samples, waveBuffer, waveBuffer.Length);
        if (samples.Length > 0)
        {
            //Clear the unused space in the wave buffer if necessary
            if (samples.Length < waveBuffer.Length)
            {
                Array.Clear(waveBuffer, samples.Length, waveBuffer.Length - samples.Length);
            }

            //Write the specified wave buffer to the audio track
            _audioTrack.Write(waveBuffer, 0, waveBuffer.Length);
        }
    }

    public void SetDevice(string device)
    {
        AudioManager? manager = AudioManager.FromContext(_context);
        AudioDeviceInfo[]? devices = manager?.GetDevices(GetDevicesTargets.Outputs);
        if (devices is null || devices.Length == 0)
            return;

        AudioDeviceInfo? selectedDevice = devices.FirstOrDefault(d => d.ProductName == device);
        if (selectedDevice is null)
            return;
        
        _audioTrack?.SetPreferredDevice(selectedDevice);
    }
}