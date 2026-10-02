using System;
using Silk.NET.OpenAL;

namespace ITDSWrapper.Audio;

public class SilkNetOpenALBackend : IAudioBackend
{
    private readonly BufferFormat _sourceALFormat;        // 8 or 16 bit buffer
    private readonly int _sampleRate;

    private int _currentBuffer;

    /// <summary>
    /// Gets or sets the Device
    /// Should be set before a call to InitPlayer
    /// </summary>
    public string? DeviceName { get; set; }
    
    /// <summary>
    /// Gets or sets the number of buffers used
    /// Should be set before a call to InitPlayer
    /// </summary>
    public int NumberOfBuffers { get; set; }

    private unsafe Device* _device;
    public ALContext? Alc { get; private set; }
    public AL? Al { get; private set; }
    
    private unsafe Context* _context;
    
    //private int _alSource;
    private uint _alSource;

    //private int[] alBuffers;
    private uint[]? _alBuffers;

    private bool _paused;
    
    public unsafe SilkNetOpenALBackend(double sampleRate, int numBuffers)
    {
        NumberOfBuffers = numBuffers;
        _sampleRate = (int)sampleRate;
        
        _sourceALFormat = GetBufferFormat(2);
        
        Alc = ALContext.GetApi(soft: true);
        Al = AL.GetApi(soft: true);


        _device = Alc.OpenDevice(DeviceName);

        _context = Alc.CreateContext(_device, null);

        Alc.MakeContextCurrent(_context);

        //Al.GenSource(out _alSource);
        _alSource = Al.GenSource();

        //Al.Source(_alSource, ALSourcef.Gain, 1f);
        Al.SetSourceProperty(_alSource, SourceFloat.Gain, 1f);

        //alBuffers = new int[NumberOfBuffers];
        _alBuffers = new uint[NumberOfBuffers];
        for (var i = 0; i < NumberOfBuffers; i++)
        {
            //AL.GenBuffer(out alBuffers[i]);
            _alBuffers[i] = Al.GenBuffer();
        }
        
        // _sourceBuffer = new byte[_bufferSizeByte];
    }

    public void Initialize(double sampleRate)
    {
    }

    public void TogglePause()
    {
        if (_paused)
        {
            _paused = false;
            Al!.SetSourceProperty(_alSource, SourceFloat.Gain, 1f);
        }
        else
        {
            _paused = true;
            Al!.SetSourceProperty(_alSource, SourceFloat.Gain, 0f);
            Al.SourcePause(_alSource);
            Al.SourceUnqueueBuffers(_alSource, _alBuffers);
        }
    }

    public void PlaySamples(byte[] samples)
    {
        ReadAndQueueBuffers(_alBuffers!, samples);
        
        Al!.GetSourceProperty(_alSource, GetSourceInteger.BuffersProcessed, out int processed);


        //AL.GetSource(_alSource, ALGetSourcei.SourceState, out state);
        Al!.GetSourceProperty(_alSource, GetSourceInteger.SourceState, out int state);


        if (processed > 0) //there are processed buffers
        {
            //unqueue
            //int[] unqueueBuffers = AL.SourceUnqueueBuffers(_alSource, processed);

            uint[] unqueueBuffers = [(uint)processed];
            Al.SourceUnqueueBuffers(_alSource, unqueueBuffers);
        }

        if ((SourceState)state != SourceState.Playing)
        {
            //AL.SourcePlay(_alSource);
            Al.SourcePlay(_alSource);
        }
    }

    public void SetDevice(string device)
    {
        DeviceName = device;
    }

    private BufferFormat GetBufferFormat(int numChannels)
    {
        if (numChannels == 2)
        {
            return BufferFormat.Stereo16;
        }
        else if (numChannels == 1)
        {
            return BufferFormat.Mono16;
        }
        throw new FormatException("Cannot translate WaveFormat.");
    }
    
    private void ReadAndQueueBuffers(uint[] alBuffers, byte[] samples)
    {
        Al!.BufferData(alBuffers[_currentBuffer], _sourceALFormat, samples, _sampleRate);

        // AL.SourceQueueBuffer(_alSource, alBuffers[i]);
        uint[] buffersToQueue = [alBuffers[_currentBuffer]];
        Al!.SourceQueueBuffers(_alSource, buffersToQueue);
        
        _currentBuffer = (_currentBuffer + 1) % alBuffers.Length;
    }
}