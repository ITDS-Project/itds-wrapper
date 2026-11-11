using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ITDSWrapper.Accessibility;
using ITDSWrapper.Playtest;
using SkiaSharp;

namespace ITDSWrapper.Core;

public partial class LogInterpreter : IDisposable
{
    protected const string WrapperLogPrefix = "[WRAPPER] ";

    private const string AchievementUnlockedVerb = "ACHIEVEMENT_UNLOCKED";
    private const string BorderSetVerb = "BORDER_SET";
    private const string ScreenReaderVerb = "SCREEN_READER";
    private const string StartupReceivedVerb = "STARTUP_RECEIVED";
    private const string LanguageReceivedVerb = "LANG_RECEIVED";
    private const string SaveTraceVerb = "SAVE_TRACE";

    public bool WatchForSdCreate { get; set; }
    public Action<string>? SetNextBorder { get; set; }

    public IAchievementManager? AchievementManager { get; set; }
    public IScreenReader? ScreenReader { get; set; }

    public bool StartupReceived { get; private set; }
    public bool LangReceived { get; private set; }

    public string? Username
    {
        get => PlaytestApiClient.Username;
        set => PlaytestApiClient.Username = value;
    }
    
    public PlaytestApiClient PlaytestApiClient { get; } = new();
    private readonly List<string> _recentLogs = [];
    private readonly object _recentLogsLock = new();

    public virtual int InterpretLog(string log)
    {
        if (log.Contains("with FileMode 0x") || log.Contains(" interface with ID ") ||
            log.Contains(" homebrew SD card image ") || log.Contains("DSi SD card image found at") ||
            log.Contains("directory: ") || log.Contains("cannot open shared object file: No such file or directory"))
            return -1;

        bool abort = log.Contains("ARM9: data abort") || log.Contains("ARM9: prefetch abort");
        lock (_recentLogsLock)
        {
            _recentLogs.Add($"{DateTimeOffset.UtcNow:u}: {log}");
            if (_recentLogs.Count >= 50 || abort)
            {
                PlaytestApiClient.QueueLogs(_recentLogs);
                _recentLogs.Clear();
            }
        }

        if (abort)
        {
            return -1;
        }

        // The SD card is initialized after the game boots -- if we replace the SD card image here, it will load properly
        int wrapperPrefixLocation = WatchForSdCreate && log.Contains("[melonDS] Game is now booting")
            ? 0
            : log.IndexOf(WrapperLogPrefix, StringComparison.Ordinal);
        if (wrapperPrefixLocation < 0)
        {
            return wrapperPrefixLocation;
        }

        int startIndex = wrapperPrefixLocation + WrapperLogPrefix.Length;
        int endIndex = log.IndexOf(':', startIndex);
        string verb = log[startIndex..endIndex];

        string logParam = log[(endIndex + 2)..^1];
        switch (verb)
        {
            case AchievementUnlockedVerb:
                AchievementManager?.Unlock(logParam);
                break;

            case BorderSetVerb:
                SetNextBorder?.Invoke(logParam);
                break;

            case ScreenReaderVerb:
                ScreenReader?.Speak(TextColorRegex().Replace(logParam, ""));
                break;

            case StartupReceivedVerb:
                StartupReceived = true;
                break;

            case LanguageReceivedVerb:
                LangReceived = true;
                break;

            case SaveTraceVerb:
                if (logParam.StartsWith("DONE"))
                {
                    QueueRecentLogs();
                }

                break;
        }

        return wrapperPrefixLocation;
    }

    public void AddRecentLog(string log)
    {
        lock (_recentLogsLock)
        {
            _recentLogs.Add($"{DateTimeOffset.UtcNow:u}: {log}\n");
        }
    }

    private void QueueRecentLogs()
    {
        lock (_recentLogsLock)
        {
            PlaytestApiClient.QueueLogs(_recentLogs);
            _recentLogs.Clear();
        }
    }

    public async Task FlushLogs(bool wait = false)
    {
        QueueRecentLogs();
        await PlaytestApiClient.FlushLogs(wait);
    }

    public void Dispose()
    {
        ScreenReader?.Dispose();
    }

    [GeneratedRegex(@"\@\d")]
    private static partial Regex TextColorRegex();
}