using System;
#if IS_WINDOWS
using System.Globalization;
using System.Speech.Synthesis;
#elif IS_LINUX
using System.Runtime.InteropServices;
using System.Text;
#endif

using ITDSWrapper.Accessibility;

namespace ITDSWrapper.Desktop;

public unsafe partial class DesktopScreenReader : IScreenReader
{
#if IS_WINDOWS
    private SpeechSynthesizer? _synthesizer;
#endif

    public static DesktopScreenReader? Instantiate(string language)
    {
        try
        {
            DesktopScreenReader reader = new();
            if (reader.Initialize(GetPlatformSpecificLanguageCode(language)))
            {
                return reader;
            }

            reader.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize screen reader: {ex.Message}");
        }

        return null;
    }

    public bool Initialize(string language)
    {
#if IS_LINUX
        bool success = Initialize(EspeakAudioOutput.AUDIO_OUTPUT_PLAYBACK, 0, Path.Combine(AppContext.BaseDirectory, "espeak-ng-data"), 0) != -1;
        
        Voice voice = new()
        {
            language = (sbyte*)Marshal.StringToHGlobalAnsi(language),
        };
        GCHandle voiceHandle = GCHandle.Alloc(voice, GCHandleType.Pinned);
        success = success && SetLanguage(voiceHandle.AddrOfPinnedObject()) == 0;
        voiceHandle.Free();
        return success;
#elif IS_WINDOWS
#pragma warning disable CA1416
        _synthesizer = new();
        _synthesizer.SelectVoiceByHints(VoiceGender.NotSet, VoiceAge.NotSet, 0, CultureInfo.GetCultureInfo(language));
#pragma warning restore CA1416
        return true;
#endif
// Unreachable... unless you're on macOS
#pragma warning disable CS0162 // Unreachable code detected
        return false;
#pragma warning restore CS0162 // Unreachable code detected
    }

    public void Speak(string text)
    {
#if IS_LINUX
        if (IsPlaying() == 1)
        {
            _ = Cancel();
        }

        text = text.Insert(text.Length, "\0");
        _ = Synthesize(text, Encoding.UTF8.GetByteCount(text), 0, EspeakPositionType.POS_CHARACTER, 0, 1,
            IntPtr.Zero, IntPtr.Zero);
#elif IS_WINDOWS
#pragma warning disable CA1416
        _synthesizer?.SpeakAsyncCancelAll();
        _synthesizer?.SpeakAsync(text);
#pragma warning restore CA1416
#endif
    }

    public void SetLanguage(string language)
    {
        language = language switch
        {
            "ja" => "ja",
            _ => "en-gb",
        };
#if IS_LINUX
        Voice voice = new()
        {
            language = (sbyte*)Marshal.StringToHGlobalAnsi(language),
        };
        GCHandle voiceHandle = GCHandle.Alloc(voice, GCHandleType.Pinned);
        SetLanguage(voiceHandle.AddrOfPinnedObject());
        voiceHandle.Free();
#elif IS_WINDOWS
#pragma warning disable CA1416
        _synthesizer?.SelectVoiceByHints(VoiceGender.NotSet, VoiceAge.NotSet, 0, CultureInfo.GetCultureInfo(language));
#pragma warning restore CA1416
#endif
    }

    public void Dispose()
    {
#if IS_LINUX
#elif IS_WINDOWS
#pragma warning disable CA1416
        _synthesizer?.Dispose();
#pragma warning restore CA1416
#endif
    }

    public static string GetPlatformSpecificLanguageCode(string language)
    {
        return language switch
        {
#if IS_LINUX
            _ => "en-uk",
#elif MACOS
            _ => "en-GB",
#else
            _ => "en-GB",
#endif
        };
    }

#if IS_LINUX
    [StructLayout(LayoutKind.Sequential)]
    private struct Voice
    {
        public sbyte* name;
        public sbyte* language;
        public sbyte* identifier;
        public byte gender;
        public byte age;
        public byte variant;
        public byte xx1;
        public int score;
        public nint spare;
    }

    private enum EspeakPositionType
    {
        POS_CHARACTER = 1,
        POS_WORD,
        POS_SENTENCE,
    }

    private enum EspeakAudioOutput
    {
        /// <summary>
        /// PLAYBACK mode: plays the audio data, supplies events to the calling program
        /// </summary>
        AUDIO_OUTPUT_PLAYBACK,

        /// <summary>
        /// RETRIEVAL mode: supplies audio data and events to the calling program
        /// </summary>
        AUDIO_OUTPUT_RETRIEVAL,

        /// <summary>
        /// SYNCHRONOUS mode: as RETRIEVAL but doesn't return until synthesis is completed
        /// </summary>
        AUDIO_OUTPUT_SYNCHRONOUS,

        /// <summary>
        /// Synchronous playback
        /// </summary>
        AUDIO_OUTPUT_SYNCH_PLAYBACK,
    }

    [LibraryImport("espeak-ng", EntryPoint = "espeak_Initialize", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int Initialize(EspeakAudioOutput output, int bufferLength, string? path, int options);

    [LibraryImport("espeak-ng", EntryPoint = "espeak_SetVoiceByProperties")]
    private static partial uint SetLanguage(nint properties);

    [LibraryImport("espeak-ng", EntryPoint = "espeak_Synth", StringMarshalling = StringMarshalling.Utf8)]
    private static partial uint Synthesize(string text, nint size, uint position, EspeakPositionType type,
        uint endPosition, uint flags, IntPtr uniqueIdentifier, IntPtr userData);

    [LibraryImport("espeak-ng", EntryPoint = "espeak_Cancel")]
    private static partial uint Cancel();

    [LibraryImport("espeak-ng", EntryPoint = "espeak_IsPlaying")]
    private static partial int IsPlaying();
#endif
}