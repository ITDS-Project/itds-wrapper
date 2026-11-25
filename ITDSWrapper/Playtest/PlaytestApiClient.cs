using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using DiscUtils.Fat;
using DiscUtils.Streams;
using Libretro.NET;
using SkiaSharp;

namespace ITDSWrapper.Playtest;

public class PlaytestApiClient
{
    private const string PlaytestApiUri = "https://playtest.intothedreamspring.com/api/ingest/";
    private readonly HttpClient _httpClient = new();
    private readonly string? _playtestApiToken;
    private readonly string _sessionGuid = Guid.NewGuid().ToString();

    private readonly string _platformId =
        $"{RuntimeInformation.RuntimeIdentifier} (v{Assembly.GetExecutingAssembly().GetName().Version})";

    private readonly ConcurrentQueue<PlaytestApiRequest> _requestsQueue = new();
    private readonly SemaphoreSlim _flushLock = new(1, 1);

    private int _currentRequest;

    public string? Username { get; set; }

    public PlaytestApiClient()
    {
        foreach (AssemblyMetadataAttribute attr in Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (attr.Key == "PlaytestApiToken")
            {
                _playtestApiToken = attr.Value;
            }
        }

        _httpClient.BaseAddress = new(PlaytestApiUri);
        _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", _playtestApiToken);
    }

    public void QueueLogs(List<string> logs)
    {
        for (var i = 0; i < logs.Count; i++)
        {
            logs[i] = logs[i].Replace(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "~");
        }
        _requestsQueue.Enqueue(new(_sessionGuid, Username?.Replace("\0", "") ?? string.Empty, _platformId,
            string.Join("", logs), _currentRequest++));
    }

    public async Task FlushLogs(bool wait = false)
    {
        if (!await _flushLock.WaitAsync(wait ? Timeout.Infinite : 0))
            return;
        try
        {
            while (_requestsQueue.TryPeek(out PlaytestApiRequest? request))
            {
                if (!await TrySendAsync(request))
                    break;
                _requestsQueue.TryDequeue(out _);
            }
        }
        finally
        {
            _flushLock.Release();
        }
    }

    private async Task<bool> TrySendAsync(PlaytestApiRequest request)
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsync("logs", JsonContent.Create(request));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send logs: {ex.Message}");
                return false;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                await Task.Delay(response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(3));
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to send logs: {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            return (int)response.StatusCode < 500;
        }

        return false;
    }

    public async Task<bool> TrySendScreenshotAsync(SKBitmap screenshot, string feedbackId)
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            HttpResponseMessage response;
            try
            {
                using MemoryStream screenshotStream = new();
                screenshot.Encode(screenshotStream, SKEncodedImageFormat.Png, 100);
                screenshotStream.Seek(0, SeekOrigin.Begin);
                response = await _httpClient.PostAsync($"screenshot?sessionGuid={_sessionGuid}&id={feedbackId}", new StreamContent(screenshotStream) { Headers = { ContentType = new("image/png") }});
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send screenshot: {ex.Message}");
                return false;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                await Task.Delay(response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(3));
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to send screenshot: {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            return (int)response.StatusCode < 500;
        }

        return false;
    }

    private const string SaveFileName = "itds-demo.sav";
    public async Task<bool> TrySendSavefileAsync(string saveDir, string feedbackId)
    {
        // Code borrowed from SteamworksHelper
        // MIT Licensed
        string sdCardPath = Path.Combine(saveDir, "melonDS DS", "dldi_sd_card.bin");
        if (OperatingSystem.IsWindows())
        {
            string sdCardCopyPath = Path.Combine(saveDir, "melonDS DS", "dldi_sd_card_copy.bin");
            Thread.Sleep(500); // Sleep for just a sec before copying the SD card to get write updates
            File.Copy(sdCardPath, sdCardCopyPath, true);
            sdCardPath = sdCardCopyPath;
        }

        byte[] sdCardBytes = await File.ReadAllBytesAsync(sdCardPath);
        using MemoryStream sdCardStream = new(sdCardBytes[0x7E00..]);
        using FatFileSystem sdCardFat = new(sdCardStream, Ownership.None);
        if (!sdCardFat.FileExists(SaveFileName))
        {
            return false;
        }

        await using SparseStream savStream = sdCardFat.OpenFile(SaveFileName, FileMode.Open);
        byte[] savFile = new byte[savStream.Length];
        savStream.ReadExactly(savFile);
        
        if (OperatingSystem.IsWindows())
        {
            File.Delete(sdCardPath);
        }
        // End borrowed segment
        
        for (int attempt = 0; attempt < 5; attempt++)
        {
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsync($"save?sessionGuid={_sessionGuid}&id={feedbackId}", new ByteArrayContent(savFile) { Headers = { ContentType = new("application/octet-stream") }});
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send save file: {ex.Message}");
                return false;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                await Task.Delay(response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(3));
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to send save file: {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            return (int)response.StatusCode < 500;
        }

        return false;
    }
}