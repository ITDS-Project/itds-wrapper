using System.Buffers.Binary;
using System.Threading;
using AVFoundation;
using ITDSWrapper.Audio;

namespace AvFoundationBackend;

public class AvFoundationAudioBackend : IAudioBackend
{
    private AVAudioFormat? _sourceAudioFormat;
    private AVAudioFormat? _outputAudioFormat;
    private AVAudioConverter? _audioConverter;
    private AVAudioEngine? _audioEngine;
    private AVAudioPlayerNode? _audioPlayerNode;
    private AVAudioPcmBuffer[] _buffers = [];
    private SemaphoreSlim? _bufferSemaphore;
    private float _volume = 1.0f;
    private int _bufferIndex;
    private uint _bufferFrames;

    private bool _paused;
    
    public float Volume
    {
        get => _volume;
        set
        {
            _volume = (value < 0.0f) ? 0.0f : (value > 1.0f) ? 1.0f : value;
            if (_audioPlayerNode is not null)
            {
                _audioPlayerNode.Volume = _volume;
            }
        }
    }

    public int DesiredLatency { get; set; } = 300;
    public int NumberOfBuffers { get; set; } = 4;
    
    public virtual void Initialize(double sampleRate)
    {
        _sourceAudioFormat = new(AVAudioCommonFormat.PCMInt16, sampleRate, 2, false);
        _audioPlayerNode = new();
        _audioPlayerNode.Volume = _volume;
        _audioEngine = new();
        AVAudioFormat mainFormat = _audioEngine.MainMixerNode.GetBusOutputFormat(0);
        _outputAudioFormat = new(mainFormat.CommonFormat, sampleRate, mainFormat.ChannelCount, mainFormat.Interleaved);
        _audioConverter = new(_sourceAudioFormat, _outputAudioFormat);
        _audioEngine.AttachNode(_audioPlayerNode);
        _audioEngine.Connect(_audioPlayerNode, _audioEngine.MainMixerNode, _outputAudioFormat);

        _buffers = new AVAudioPcmBuffer[NumberOfBuffers];
        int bufferSize = 32768;
        _bufferFrames = (uint)(bufferSize / 4);
        for (int i = 0; i < NumberOfBuffers; i++)
        {
            _buffers[i] = new(_outputAudioFormat, _bufferFrames);
        }

        _bufferSemaphore = new(NumberOfBuffers, NumberOfBuffers);
        _audioEngine.StartAndReturnError(out _);
        _audioPlayerNode.Play();
    }

    public void TogglePause()
    {
        if (!_paused)
        {
            _paused = true;
            _audioPlayerNode?.Pause();
            _audioEngine?.Pause();
        }
        else
        {
            _paused = false;
            if (!(_audioEngine?.Running ?? true))
            {
                _audioEngine?.StartAndReturnError(out _);
            }
            _audioPlayerNode?.Play();
        }
    }

    public void PlaySamples(byte[] samples)
    {
        if (_paused || _sourceAudioFormat is null)
        {
            return;
        }
        
        // Wait for an available buffer
        while (!_bufferSemaphore?.Wait(DesiredLatency / NumberOfBuffers) ?? true) ;

        AVAudioPcmBuffer inBuffer = new(_sourceAudioFormat, _bufferFrames);
        CopyBuffer(samples, inBuffer);
        AVAudioPcmBuffer pcmBuffer = _buffers[_bufferIndex];
        _audioConverter?.ConvertToBuffer(pcmBuffer, inBuffer, out _);
        _audioPlayerNode?.ScheduleBuffer(pcmBuffer, () => _bufferSemaphore?.Release());

        _bufferIndex = (_bufferIndex + 1) % NumberOfBuffers;
    }

    private static unsafe void CopyBuffer(byte[] source, AVAudioPcmBuffer destination)
    {
        uint frames = (uint)(source.Length / 4);
        if (frames > destination.FrameCapacity)
        {
            frames = destination.FrameCapacity;
        }
        short** channelData = (short**)destination.Int16ChannelData;
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < frames; j++)
            {
                *(channelData[i] + j) = BinaryPrimitives.ReadInt16LittleEndian(source[(2 * (j * 2 + i))..]);
            }
        }

        destination.FrameLength = frames;
    }

    public void SetDevice(string device)
    {
    }
}