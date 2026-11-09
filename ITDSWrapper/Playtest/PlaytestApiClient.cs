using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

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

    public string? Username { get; set; }

    public PlaytestApiClient()
    {
        foreach (var attr in Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>())
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
        _requestsQueue.Enqueue(new(_sessionGuid, Username?.Replace("\0", "") ?? string.Empty, _platformId,
            string.Join("", logs)));
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
}